using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Nhạc trưởng Điều phối Chiến trường Vô tận (Single-Master Endless Battle Director).
    /// Quản lý máy trạng thái 5 pha: InitialDelay -> Dispatching -> CombatTracking -> CleaningBuffer -> BreathingRoom.
    /// Tích hợp thuật toán DDA (Dynamic Difficulty Adjustment), Horizon Lead Distance (280m),
    /// Wave Gating nghiêm ngặt (0 Wave Overlap), và Hyperspace Sector Reset mỗi 15,000m (0 GC Alloc).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailEndlessDirector : MonoBehaviour
    {
        public static VirtualRailEndlessDirector Instance { get; private set; }

        public enum BattleFlowState
        {
            Inactive = 0,
            InitialDelay = 1,
            Dispatching = 2,
            CombatTracking = 3,
            CleaningBuffer = 4,
            BreathingRoom = 5
        }

        [Header("State Machine")]
        [SerializeField] private BattleFlowState _battleState = BattleFlowState.Inactive;
        public BattleFlowState CurrentBattleState => _battleState;

        [Header("Dynamic Difficulty Adjustment (DDA)")]
        [SerializeField] private int _threatLevel = 1;
        public int ThreatLevel => _threatLevel;

        [SerializeField] private float _threatLevelProgress01 = 0f;
        public float ThreatLevelProgress01 => _threatLevelProgress01;

        [SerializeField] private float _sessionTimer = 0f;
        public float SessionTimer => _sessionTimer;

        [SerializeField] private int _wavesCleared = 0;
        public int WavesCleared => _wavesCleared;

        [SerializeField] private int _currentWaveIndex = 0;
        public int CurrentWaveIndex => _currentWaveIndex;

        [Header("Lead & Pacing Tuning (220m - 240m Horizon Range)")]
        [SerializeField] private float _minLeadDistance = 220f;
        [SerializeField] private float _maxLeadDistance = 240f;
        [SerializeField] private float _formationAssemblyTime = 2.2f;

        [Header("DDA Dynamic Rail Speed Scaling")]
        [SerializeField] private bool _scaleRailSpeed = true;
        public bool ScaleRailSpeed { get => _scaleRailSpeed; set => _scaleRailSpeed = value; }
        [SerializeField] private float _speedScalePerLevel = 0.03f;
        [SerializeField] private float _maxRailSpeedCap = 150f;

        // Subsystem References
        private StageData _stageData;
        private VirtualRailAnchor _anchor;
        private VirtualRailWaveSpawner _waveSpawner;
        private VirtualRailAsteroidSpawner _asteroidSpawner;
        private VirtualRailStageDirector _stageDirector;

        // Internal State Tracking (0 GC Primitives)
        private bool _isActive = false;
        private float _stateTimer = 0f;
        private float _waveTimer = 0f;
        private float _currentWaveMaxLifetime = 14f;
        private float _breathingDuration = 3.5f;
        private float _lastWarpZ = 0f;

        // Formation Pattern Pool (Round-Robin / Shuffle-Bag)
        private FormationPattern[] _formationPool;
        private int _patternPoolIndex = 0;
        private int _threatLevelBonus = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEventManager.OnPlayerDestroyed += HandlePlayerDestroyed;
            GameEventManager.OnStartGame += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameEventManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
            GameEventManager.OnStartGame -= HandleGameStarted;
        }

        private void HandlePlayerDestroyed()
        {
            StopEndlessSession();
        }

        private void HandleGameStarted()
        {
            if (_stageData != null && _stageData.progressionSettings.IsEndlessMode)
            {
                InitializeEndlessSession(_stageData, _anchor, _waveSpawner, _asteroidSpawner, _stageDirector);
            }
        }

        /// <summary>
        /// Khởi chạy phiên chiến đấu vô tận theo cấu hình StageData (0 GC Alloc).
        /// </summary>
        public void InitializeEndlessSession(StageData stage, VirtualRailAnchor anchor, VirtualRailWaveSpawner waveSpawner, VirtualRailAsteroidSpawner asteroidSpawner, VirtualRailStageDirector director = null)
        {
            _stageData = stage;
            _anchor = anchor != null ? anchor : VirtualRailAnchor.Instance;
            _waveSpawner = waveSpawner != null ? waveSpawner : VirtualRailWaveSpawner.Instance;
            _asteroidSpawner = asteroidSpawner != null ? asteroidSpawner : VirtualRailAsteroidSpawner.Instance;
            _stageDirector = director != null ? director : VirtualRailStageDirector.Instance;

            _sessionTimer = 0f;
            _threatLevelBonus = 0;
            _threatLevel = 1;
            _threatLevelProgress01 = 0f;
            _wavesCleared = 0;
            _currentWaveIndex = 0;
            _patternPoolIndex = 0;
            _lastWarpZ = (_anchor != null) ? _anchor.transform.position.z : 0f;

            // Khóa cứng Wave Spawner về ScriptedDirectorOnly để triệt tiêu sinh ngẫu nhiên hỗn loạn
            if (_waveSpawner != null)
            {
                _waveSpawner.ControlMode = SpawnerControlMode.ScriptedDirectorOnly;
            }

            // Nạp Formation Pool từ StageData
            if (_stageData != null && _stageData.enemySettings.stageFormationPool != null && _stageData.enemySettings.stageFormationPool.Length > 0)
            {
                _formationPool = _stageData.enemySettings.stageFormationPool;
            }
            else
            {
#if UNITY_EDITOR
                _formationPool = LoadAllEditorFormations();
#else
                _formationPool = null;
#endif
            }

            _isActive = true;
            TransitionTo(BattleFlowState.InitialDelay);
            Debug.Log($"<color=lime><b>[ENDLESS DIRECTOR]</b></color> Khởi động Endless Survival Mode! Formation Pool: {(_formationPool != null ? _formationPool.Length : 0)} mẫu.");
        }

        /// <summary>
        /// Dừng phiên Endless Survival và dọn sạch các đợt quái đang bay.
        /// </summary>
        public void StopEndlessSession()
        {
            _isActive = false;
            _battleState = BattleFlowState.Inactive;
            _stateTimer = 0f;
            _waveTimer = 0f;
            _threatLevelBonus = 0;

            if (_waveSpawner != null)
            {
                _waveSpawner.ForceRetreatCurrentWave();
            }

            if (_asteroidSpawner != null)
            {
                _asteroidSpawner.ResetThreatLevelScaling();
            }

            if (_anchor != null)
            {
                float baseSpeed = (_stageData != null) ? _stageData.GetBaseForwardSpeed() : 70f;
                _anchor.SetSpeedOverride(baseSpeed);
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // 1. Cập nhật đồng hồ phiên & Thuật toán DDA (Mỗi 45s tăng 1 bậc Threat Level + Bonus từ diệt Boss)
            _sessionTimer += Time.deltaTime;
            _threatLevel = 1 + Mathf.FloorToInt(_sessionTimer / 45f) + _threatLevelBonus;
            _threatLevelProgress01 = Mathf.Repeat(_sessionTimer, 45f) / 45f;

            // Cập nhật tốc độ đạn quái theo bậc đe dọa (20m/s -> 32m/s)
            float bulletSpeed = Mathf.Min(32f, 20f + (_threatLevel - 1) * 1.5f);
            if (_waveSpawner != null)
            {
                _waveSpawner.SetStageBulletSpeed(bulletSpeed);
            }

            // Cập nhật thiên thạch theo bậc đe dọa (mật độ dày hơn, tỉ lệ bất hoại tăng)
            if (_asteroidSpawner != null)
            {
                _asteroidSpawner.SetThreatLevelScaling(_threatLevel);
            }

            // Cập nhật tốc độ đường ray tịnh tiến theo DDA (0 GC Alloc)
            if (_scaleRailSpeed && _anchor != null)
            {
                float baseSpeed = (_stageData != null) ? _stageData.GetBaseForwardSpeed() : 70f;
                float scaledSpeed = Mathf.Min(_maxRailSpeedCap, baseSpeed * (1f + _speedScalePerLevel * (_threatLevel - 1)));
                _anchor.SetSpeedOverride(scaledSpeed);
            }

            // 2. Kiểm tra mốc Hyperspace Warp Reset mỗi 15,000m
            if (_anchor != null)
            {
                float distSinceWarp = _anchor.transform.position.z - _lastWarpZ;
                if (distSinceWarp >= 15000f)
                {
                    ExecuteHyperspaceSectorWarp();
                }
            }

            // 3. Tiến trình Máy trạng thái Đơn Chủ (Single-Master State Machine)
            switch (_battleState)
            {
                case BattleFlowState.InitialDelay:
                    _stateTimer += Time.deltaTime;
                    if (_stateTimer >= 2.0f)
                    {
                        TransitionTo(BattleFlowState.Dispatching);
                    }
                    break;

                case BattleFlowState.Dispatching:
                    DispatchNextWave();
                    break;

                case BattleFlowState.CombatTracking:
                    _waveTimer += Time.deltaTime;
                    bool isWaveActive = (_waveSpawner != null && _waveSpawner.IsCurrentWaveActive());

                    if (!isWaveActive)
                    {
                        // Tiêu diệt toàn bộ phi đội thành công!
                        _wavesCleared++;
                        TransitionTo(BattleFlowState.CleaningBuffer);
                    }
                    else if (_waveTimer >= _currentWaveMaxLifetime)
                    {
                        // Hết thời gian tối đa: Quái thoát ly màn hình
                        TransitionTo(BattleFlowState.CleaningBuffer);
                    }
                    break;

                case BattleFlowState.CleaningBuffer:
                    _stateTimer += Time.deltaTime;
                    if (_stateTimer >= 0.2f)
                    {
                        if (_waveSpawner != null)
                        {
                            _waveSpawner.ForceRetreatCurrentWave();
                        }

                        // Tính thời gian nghỉ ngơi DDA (3.5s -> 1.8s)
                        _breathingDuration = Mathf.Max(1.8f, 3.5f - (_threatLevel - 1) * 0.2f);
                        TransitionTo(BattleFlowState.BreathingRoom);
                    }
                    break;

                case BattleFlowState.BreathingRoom:
                    _stateTimer += Time.deltaTime;
                    if (_stateTimer >= _breathingDuration)
                    {
                        TransitionTo(BattleFlowState.Dispatching);
                    }
                    break;
            }
        }

        private void TransitionTo(BattleFlowState newState)
        {
            _battleState = newState;
            _stateTimer = 0f;
        }

        private void DispatchNextWave()
        {
            if (_formationPool == null || _formationPool.Length == 0)
            {
                TransitionTo(BattleFlowState.BreathingRoom);
                return;
            }

            if (_waveSpawner == null)
            {
                _waveSpawner = VirtualRailWaveSpawner.Instance;
                if (_waveSpawner == null) return;
            }

            // Chọn mẫu đội hình theo thứ tự vòng tròn tránh trùng lặp
            FormationPattern pattern = _formationPool[_patternPoolIndex];
            _patternPoolIndex = (_patternPoolIndex + 1) % _formationPool.Length;

            if (pattern == null)
            {
                TransitionTo(BattleFlowState.BreathingRoom);
                return;
            }

            // Sinh thông số biến dị thủ tục (Procedural Customization) theo DDA
            VirtualFormationAnchor.FormationCustomizationParams customParams = GenerateFormationParams();

            // Cự ly xuất hiện biến thiên tự nhiên trong dải [220m, 240m] theo từng wave (0 GC Alloc)
            float t = ((_currentWaveIndex % 5) / 4.0f);
            float leadDist = Mathf.Lerp(_minLeadDistance, _maxLeadDistance, t);
            float assemblyTime = _formationAssemblyTime > 0.5f ? _formationAssemblyTime : 2.2f;

            bool spawned = _waveSpawner.SpawnScriptedFormation(pattern, customParams, leadDist, assemblyTime);
            if (spawned)
            {
                _currentWaveMaxLifetime = pattern.maxLifetime > 2f ? pattern.maxLifetime : 14f;
                _waveTimer = 0f;
                _currentWaveIndex++;
                TransitionTo(BattleFlowState.CombatTracking);
            }
            else
            {
                // Nếu pool đang quá tải, chờ một nhịp ngắn rồi thử lại
                _breathingDuration = 0.6f;
                TransitionTo(BattleFlowState.BreathingRoom);
            }
        }

        private VirtualFormationAnchor.FormationCustomizationParams GenerateFormationParams()
        {
            // Độ giãn nở đội hình biến thiên mượt mà: 0.85x đến 1.25x
            float spread = Mathf.Clamp(1.0f + 0.05f * ((_currentWaveIndex % 5) - 2), 0.85f, 1.25f);

            // Đảo cánh ngang luân phiên 50%
            bool mirror = (_currentWaveIndex % 2 == 1);

            // Nâng cấp lớp quái vật theo Threat Level:
            // L1: Scout -> L2: Flanker -> L3: Interceptor -> L4+: Shield Bearer xen kẽ
            EnemyArchetype? archOverride = null;
            if (_threatLevel == 1)
            {
                archOverride = EnemyArchetype.Scout;
            }
            else if (_threatLevel == 2)
            {
                archOverride = EnemyArchetype.Flanker;
            }
            else if (_threatLevel == 3)
            {
                archOverride = EnemyArchetype.Interceptor;
            }
            else
            {
                archOverride = (_currentWaveIndex % 2 == 0) ? EnemyArchetype.ShieldBearer : EnemyArchetype.Interceptor;
            }

            // 4. Cơ chế Biến hình Đội hình (Dynamic Formation Morphing):
            // 50% số đợt (các đợt lẻ) tự động kích hoạt biến hình sang mẫu kế tiếp sau 4 giây giao chiến
            FormationPattern morphTarget = null;
            float morphDelay = 4.0f;
            float morphDuration = 1.8f;

            if (_formationPool != null && _formationPool.Length > 1 && (_currentWaveIndex % 2 == 1))
            {
                int targetIdx = (_patternPoolIndex + 1) % _formationPool.Length;
                morphTarget = _formationPool[targetIdx];
            }

            // 5. Phản ứng mất chỉ huy (Leader Death Reaction):
            // Tỉ lệ MaintainFormation (Giữ vững đội hình kỷ luật thép) chiếm ĐA SỐ ÁP ĐẢO (60% tổng số đợt),
            // cao hơn rõ rệt so với mọi cơ chế riêng lẻ khác (Kamikaze ~10-20%, BreakShield 10%, Panic 10%, Retreat 10%).
            LeaderDeathReaction reactOverride;
            int reactionCycle = _currentWaveIndex % 10;

            switch (reactionCycle)
            {
                case 2:
                    reactOverride = (_threatLevel >= 3) ? LeaderDeathReaction.KamikazeCharge : LeaderDeathReaction.BreakShieldAndEngage;
                    break;
                case 4:
                    reactOverride = LeaderDeathReaction.BreakShieldAndEngage;
                    break;
                case 6:
                    reactOverride = LeaderDeathReaction.PanicScatter;
                    break;
                case 8:
                    reactOverride = (_threatLevel >= 3) ? LeaderDeathReaction.KamikazeCharge : LeaderDeathReaction.RetreatWarp;
                    break;
                case 0:
                case 1:
                case 3:
                case 5:
                case 7:
                case 9:
                default:
                    reactOverride = LeaderDeathReaction.MaintainFormation; // 60% tỉ lệ
                    break;
            }

            return new VirtualFormationAnchor.FormationCustomizationParams
            {
                spreadMultiplier = spread,
                mirrorHorizontal = mirror,
                archetypeOverride = archOverride,
                motionProfileOverride = null,
                entryStyleOverride = null,
                reactionOverride = reactOverride,
                morphTargetOverride = morphTarget,
                morphDelay = morphDelay,
                morphDuration = morphDuration
            };
        }

        private void ExecuteHyperspaceSectorWarp()
        {
            if (_anchor == null) return;

            _lastWarpZ = _anchor.transform.position.z;
            GameEventManager.IncrementScore(10000);

            if (_stageDirector != null && _stageDirector.notificationUI != null)
            {
                _stageDirector.notificationUI.ShowNotification("SECTOR CLEARED!", "HYPERSPACE BONUS +10,000 PTS", 3.2f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.RadioChirp, 1.0f);
            }

            Debug.Log("<color=yellow><b>[HYPERSPACE WARP RESET]</b></color> Đạt mốc 15,000m! Tặng +10,000 điểm thưởng sống sót!");
        }

        /// <summary>
        /// Kích hoạt khi Boss F3 Corvette bị tiêu diệt trong chế độ Endless Survival (0 GC Alloc).
        /// - Tăng bậc đe dọa (Threat Level) lên +1
        /// - Đặt lại trạng thái chiến trường sang BreathingRoom (thời gian nghỉ 5.0s)
        /// - Kích hoạt thông báo UI "BOSS ELIMINATED! SECTOR THREAT LEVEL UP"
        /// - Phát âm thanh RadioChirp
        /// </summary>
        public void OnBossIncursionDefeated()
        {
            if (!_isActive) return;

            _threatLevelBonus++;
            _threatLevel = 1 + Mathf.FloorToInt(_sessionTimer / 45f) + _threatLevelBonus;
            _wavesCleared++;
            _breathingDuration = 5.0f;
            TransitionTo(BattleFlowState.BreathingRoom);

            if (_stageDirector != null && _stageDirector.notificationUI != null)
            {
                _stageDirector.notificationUI.ShowNotification("BOSS ELIMINATED!", $"THREAT LEVEL ESCALATED TO LEVEL {_threatLevel}", 4.0f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.RadioChirp, 1.0f);
            }

            Debug.Log($"<color=lime><b>[ENDLESS DIRECTOR]</b></color> Boss Incursion Defeated! Threat Level tăng lên {_threatLevel}.");
        }

#if UNITY_EDITOR
        private static FormationPattern[] LoadAllEditorFormations()
        {
            var list = new List<FormationPattern>();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:FormationPattern", new[] { "Assets/Scripts/VirtualRail" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                var p = UnityEditor.AssetDatabase.LoadAssetAtPath<FormationPattern>(path);
                if (p != null) list.Add(p);
            }
            return list.ToArray();
        }
#endif
    }
}
