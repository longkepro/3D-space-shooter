using System;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    public enum StageState
    {
        Inactive,
        Intro,
        InJourney,
        Climax,
        WarpTransition,
        StageCompleted,
        AllStagesCleared
    }

    /// <summary>
    /// Nhạc trưởng điều phối chu trình màn chơi (Stage Director - Chuẩn Star Fox 64 Arcade).
    /// Hỗ trợ kiến trúc Pure Data-Driven:
    /// - Tự do tùy biến thời gian hoàn thành (Phút/Giây hoặc Mét Z hoặc Diệt quái).
    /// - Thực thi kịch bản Timeline theo mốc (0 GC Alloc).
    /// - Chuyển giao liền mạch không tải lại Scene (Seamless In-Place Transition).
    /// - Quản lý trận đấu Boss và ghìm tốc độ ray.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailStageDirector : MonoBehaviour
    {
        public static VirtualRailStageDirector Instance { get; private set; }

        [Header("Stage Sequence")]
        [Tooltip("Danh sách các màn chơi theo thứ tự tuần tự")]
        public StageData[] stages;
        public int currentStageIndex = 0;

        [Header("State Tracking")]
        [SerializeField] private StageState currentState = StageState.Inactive;
        public StageState CurrentState => currentState;

        [Header("Subsystem References")]
        public VirtualRailAnchor anchor;
        public VirtualRailWaveSpawner waveSpawner;
        public VirtualRailAsteroidSpawner asteroidSpawner;
        public VirtualRailHorizonBackdrop backdrop;
        public VirtualRailSpaceDust spaceDust;
        public Light directionalLight;
        public StageNotificationUI notificationUI;

        [Header("Default Fallback Assets")]
        [Tooltip("Skybox đen không gian mặc định khi Stage không chỉ định")]
        public Material defaultSkyboxMaterial;

        // Runtime Tracking (0 GC Alloc primitives)
        private float _stageStartZ = 0f;
        private float _stageTimer = 0f;
        private int _nextTimelineIndex = 0;
        private int _enemiesKilled = 0;
        private bool _isBossSpawned = false;
        private GameObject _activeBossInstance = null;
        private float _warpTimer = 0f;
        private float _warpDuration = 2.5f;
        private float _normalSpeed = 28f;
        private float _warpPeakSpeed = 85f;
        private bool _isMidWarpSwapped = false;
        private float _climaxTimer = 0f;
        private bool _pendingStageStart = false;
        private bool _stagePaletteApplied = false;
        private float _waveGatingTimer = 0f;
        private bool _isWaveGated = false;
        private float _nextFindReferencesTime = 0f;

        public StageData CurrentStageData => (stages != null && currentStageIndex >= 0 && currentStageIndex < stages.Length) ? stages[currentStageIndex] : null;

        public float StageTimer => _stageTimer;
        public int EnemiesKilled => _enemiesKilled;

        /// <summary>
        /// Tiến trình hoàn thành màn chơi (0.0 đến 1.0) theo chế độ đã cấu hình.
        /// </summary>
        public float StageProgress01
        {
            get
            {
                var stage = CurrentStageData;
                if (stage == null || anchor == null) return 0f;

                if (stage.progressionSettings.IsEndlessMode)
                {
                    var endless = GetComponent<VirtualRailEndlessDirector>();
                    return endless != null ? endless.ThreatLevelProgress01 : Mathf.Repeat(_stageTimer / 45f, 1f);
                }

                if (stage.progressionSettings.completionMode == StageCompletionMode.TimeDuration)
                {
                    float targetSec = stage.GetTargetDurationSeconds();
                    return targetSec > 0.01f ? Mathf.Clamp01(_stageTimer / targetSec) : 0f;
                }
                else if (stage.progressionSettings.completionMode == StageCompletionMode.EliminationQuota)
                {
                    int quota = stage.progressionSettings.targetEliminationCount;
                    return quota > 0 ? Mathf.Clamp01((float)_enemiesKilled / quota) : 0f;
                }
                else
                {
                    float targetDist = stage.GetTargetDistanceZ();
                    if (targetDist <= 0.001f) return 0f;
                    float distTraveled = anchor.transform.position.z - _stageStartZ;
                    return Mathf.Clamp01(distTraveled / targetDist);
                }
            }
        }

        public float StageDistanceRemaining
        {
            get
            {
                var stage = CurrentStageData;
                if (stage == null || anchor == null) return 0f;
                float distTraveled = anchor.transform.position.z - _stageStartZ;
                return Mathf.Max(0f, stage.GetTargetDistanceZ() - distTraveled);
            }
        }

        public float StageTimeRemainingSeconds
        {
            get
            {
                var stage = CurrentStageData;
                if (stage == null) return 0f;
                return Mathf.Max(0f, stage.GetTargetDurationSeconds() - _stageTimer);
            }
        }

        // Global Events
        public static event Action<StageData> OnStageStarted;
        public static event Action<StageData> OnStageClimaxReached;
        public static event Action<StageData, StageData> OnWarpTransitionStarted;
        public static event Action OnAllStagesCompleted;
        public static event Action<string, string, float> OnRadioTransmission;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDirectorInstance()
        {
            if (FindFirstObjectByType<VirtualRailStageDirector>() == null)
            {
                GameObject dirObj = new GameObject("[VirtualRail_StageDirector]");
                dirObj.AddComponent<VirtualRailStageDirector>();
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            FindReferences();
        }

        private void Start()
        {
            FindReferences();

            // Nếu tàu đã bắt đầu chạy hoặc đang trong Play mode, tự động kích hoạt Stage 0
            if (anchor != null && anchor.CurrentSpeed > 0.5f && currentState == StageState.Inactive)
            {
                StartStage(0);
            }
        }

        private void OnEnable()
        {
            GameEventManager.OnStartGame += HandleGameStarted;
            GameEventManager.OnPlayerDestroyed += HandlePlayerDestroyed;
            GameEventManager.OnIncrementScore += HandleEnemyKilledScore;
        }

        private void OnDisable()
        {
            GameEventManager.OnStartGame -= HandleGameStarted;
            GameEventManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
            GameEventManager.OnIncrementScore -= HandleEnemyKilledScore;
        }

        private void HandleEnemyKilledScore(int points)
        {
            _enemiesKilled++;
        }

        private void FindReferences()
        {
            if (stages == null || stages.Length == 0)
            {
                // Ưu tiên nạp từ Resources/Stages (Tương thích 100% cả PC lẫn Android APK)
                var s1 = Resources.Load<StageData>("Stages/StageData_01");
                var s2 = Resources.Load<StageData>("Stages/StageData_02");
                var s3 = Resources.Load<StageData>("Stages/StageData_03");

#if UNITY_EDITOR
                if (s1 == null) s1 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/Resources/Stages/StageData_01.asset");
                if (s2 == null) s2 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/Resources/Stages/StageData_02.asset");
                if (s3 == null) s3 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/Resources/Stages/StageData_03.asset");
#endif
                var list = new System.Collections.Generic.List<StageData>();
                if (s1 != null) list.Add(s1);
                if (s2 != null) list.Add(s2);
                if (s3 != null) list.Add(s3);
                stages = list.ToArray();
            }

            if (anchor == null)
            {
                anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();
            }

            if (waveSpawner == null)
            {
                waveSpawner = VirtualRailWaveSpawner.Instance != null ? VirtualRailWaveSpawner.Instance : FindAnyObjectByType<VirtualRailWaveSpawner>();
                if (waveSpawner == null && anchor != null)
                {
                    waveSpawner = anchor.GetComponentInChildren<VirtualRailWaveSpawner>();
                }
            }

            if (asteroidSpawner == null)
            {
                asteroidSpawner = VirtualRailAsteroidSpawner.Instance != null ? VirtualRailAsteroidSpawner.Instance : FindAnyObjectByType<VirtualRailAsteroidSpawner>();
                if (asteroidSpawner == null && anchor != null)
                {
                    asteroidSpawner = anchor.GetComponentInChildren<VirtualRailAsteroidSpawner>();
                }
            }

            if (backdrop == null)
            {
                backdrop = FindAnyObjectByType<VirtualRailHorizonBackdrop>();
                if (backdrop == null && anchor != null)
                {
                    backdrop = anchor.GetComponentInChildren<VirtualRailHorizonBackdrop>();
                }
            }

            if (spaceDust == null)
            {
                spaceDust = FindAnyObjectByType<VirtualRailSpaceDust>();
                if (spaceDust == null && anchor != null)
                {
                    spaceDust = anchor.GetComponentInChildren<VirtualRailSpaceDust>();
                }
            }

            if (notificationUI == null)
            {
                notificationUI = StageNotificationUI.Instance != null ? StageNotificationUI.Instance : FindAnyObjectByType<StageNotificationUI>();
                if (notificationUI == null)
                {
                    Canvas canvas = FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        notificationUI = CreateRuntimeNotificationUI(canvas.transform);
                    }
                }
            }

            if (defaultSkyboxMaterial == null)
            {
#if UNITY_EDITOR
                defaultSkyboxMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat");
#endif
            }

            if (directionalLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        directionalLight = lights[i];
                        break;
                    }
                }
            }
        }

        private static StageNotificationUI CreateRuntimeNotificationUI(Transform canvasParent)
        {
            GameObject notifObj = new GameObject("[StageNotificationUI]");
            notifObj.transform.SetParent(canvasParent, false);

            RectTransform rt = notifObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.6f);
            rt.anchorMax = new Vector2(1f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            CanvasGroup cg = notifObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(notifObj.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.1f, 0.95f, 1.0f, 1.0f);

            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(notifObj.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.5f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            Text subText = subObj.AddComponent<Text>();
            subText.font = titleText.font;
            subText.fontSize = 20;
            subText.fontStyle = FontStyle.Normal;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(1.0f, 0.95f, 0.4f, 0.9f);

            StageNotificationUI notifUI = notifObj.AddComponent<StageNotificationUI>();
            notifUI.bannerCanvasGroup = cg;
            notifUI.titleText = titleText;
            notifUI.subtitleText = subText;
            return notifUI;
        }

        private void HandleGameStarted()
        {
            _nextFindReferencesTime = 0f;
            FindReferences();
            if (anchor != null && waveSpawner != null)
            {
                // Nếu Anchor.Respawn() đã khởi chạy màn chơi rồi thì không gọi lặp
                if (currentState != StageState.InJourney)
                {
                    StartStage(0, true);
                }
            }
            else
            {
                _pendingStageStart = true;
                currentState = StageState.Inactive;
            }
        }

        private void HandlePlayerDestroyed()
        {
            currentState = StageState.Inactive;
            _pendingStageStart = false;
            _stagePaletteApplied = false;
            currentStageIndex = 0;
            if (_activeBossInstance != null)
            {
                Destroy(_activeBossInstance);
                _activeBossInstance = null;
            }
            var endlessDirector = GetComponent<VirtualRailEndlessDirector>();
            if (endlessDirector != null)
            {
                endlessDirector.StopEndlessSession();
            }
        }

        /// <summary>
        /// Bắt đầu nạp và khởi chạy một màn chơi cụ thể.
        /// </summary>
        public void StartStage(int stageIdx)
        {
            StartStage(stageIdx, false);
        }

        /// <summary>
        /// Bắt đầu nạp và khởi chạy một màn chơi cụ thể kèm cờ ép buộc tải lại.
        /// </summary>
        public void StartStage(int stageIdx, bool forceReload)
        {
            FindReferences();

            if (stages == null || stages.Length == 0) return;

            int targetStageIndex = Mathf.Clamp(stageIdx, 0, stages.Length - 1);
            StageData stage = stages[targetStageIndex];
            if (stage == null) return;

            // Idempotent Guard: Nếu đã đang bay trong Stage này và không yêu cầu nạp lại ép buộc,
            // bỏ qua việc re-apply môi trường để triệt tiêu hoàn toàn hiện tượng chớp giật nhấp nháy màn hình.
            if (!forceReload && currentState == StageState.InJourney && currentStageIndex == targetStageIndex)
            {
                return;
            }

            currentStageIndex = targetStageIndex;
            _pendingStageStart = false;

            if (_activeBossInstance != null)
            {
                Destroy(_activeBossInstance);
                _activeBossInstance = null;
            }

            if (anchor != null)
            {
                _stageStartZ = anchor.transform.position.z;
            }

            _stageTimer = 0f;
            _nextTimelineIndex = 0;
            _enemiesKilled = 0;
            _isBossSpawned = false;
            _climaxTimer = 0f;
            _waveGatingTimer = 0f;
            _isWaveGated = false;

            _normalSpeed = stage.GetBaseForwardSpeed();
            _warpDuration = stage.GetWarpDuration();
            _warpPeakSpeed = _normalSpeed * stage.GetWarpSpeedMultiplier();

            // 1. Áp dụng Môi trường & Khí quyển
            ApplyEnvironment(stage.environmentSettings);

            // 2. Áp dụng Tốc độ đường ray
            if (anchor != null)
            {
                anchor.SetSpeedOverride(_normalSpeed);
                anchor.CurrentSpeed = _normalSpeed;
            }

            // 3. Áp dụng Bảng quái vật & Vector xuất hiện (Không dùng fallback mặc định)
            if (waveSpawner != null)
            {
                waveSpawner.ControlMode = SpawnerControlMode.ScriptedDirectorOnly;
                waveSpawner.ApplyStagePalette(stage.enemySettings);
                _stagePaletteApplied = true;
            }
            else
            {
                _stagePaletteApplied = false;
            }

            // 3.1. Kích hoạt VirtualRailEndlessDirector nếu đang chạy Endless Survival
            var endlessDirector = GetComponent<VirtualRailEndlessDirector>();
            if (stage.progressionSettings.IsEndlessMode)
            {
                if (endlessDirector == null)
                {
                    endlessDirector = gameObject.AddComponent<VirtualRailEndlessDirector>();
                }
                endlessDirector.enabled = true;
                endlessDirector.InitializeEndlessSession(stage, anchor, waveSpawner, asteroidSpawner, this);
            }
            else
            {
                if (endlessDirector != null)
                {
                    endlessDirector.enabled = false;
                }
            }

            // 4. Áp dụng Thiên thạch & Chướng ngại vật
            if (asteroidSpawner != null)
            {
                asteroidSpawner.ApplyStageHazardSettings(stage.hazardSettings);
            }

            // 5. Cập nhật UI thông báo phong cách Arcade Star Fox 64
            if (notificationUI != null)
            {
                notificationUI.SetStageNumber(stage.stageIndex);
                notificationUI.ShowNotification(stage.stageTitle, stage.stageSubtitle, 3.5f);
            }

            // 6. Phát nhạc nền Stage BGM (0 GC Alloc)
            if (AudioManager.Instance != null && stage != null)
            {
                AudioManager.Instance.PlayStageMusic(stage.audioSettings.stageBgm, stage.audioSettings.musicVolume);
                AudioManager.Instance.PlaySFX2D(AudioSFXType.RadioChirp, 0.75f);
            }

            currentState = StageState.InJourney;
            OnStageStarted?.Invoke(stage);
        }

        private void Update()
        {
            // Phím nóng Debug: N hoặc F2 để Warp sang Stage kế tiếp tức thì khi test trong Play mode
            if (Application.isPlaying && (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.F2)))
            {
                StartWarpTransition();
            }

            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Bước tiến trạng thái tất định (Deterministic State Tick).
        /// Cho phép kiểm thử tự động chính xác từng frame độc lập với chu kỳ render Unity.
        /// </summary>
        public void Tick(float dt)
        {
            // Self-Healing Runtime Binding: Tự động kết nối với Anchor & Spawners ngay khi tàu xuất hiện trong Scene
            // Tối ưu hóa: Throttled 1s khi thiếu thành phần, triệt tiêu hoàn toàn Scene Scan mỗi frame
            if ((anchor == null || waveSpawner == null || asteroidSpawner == null || notificationUI == null) && Time.time >= _nextFindReferencesTime)
            {
                _nextFindReferencesTime = Time.time + 1.0f;
                FindReferences();
            }

            if (anchor == null) return;

            // Tự động bắt đầu màn nếu có cờ chờ hoặc tàu bắt đầu di chuyển khi chưa active
            if (currentState == StageState.Inactive && (_pendingStageStart || anchor.CurrentSpeed > 0.5f))
            {
                StartStage(currentStageIndex);
            }

            // Đảm bảo Stage Palette (tắt quái ngẫu nhiên, nạp kịch bản) được áp dụng ngay khi waveSpawner sẵn sàng
            if (!_stagePaletteApplied && waveSpawner != null && CurrentStageData != null)
            {
                waveSpawner.ControlMode = SpawnerControlMode.ScriptedDirectorOnly;
                waveSpawner.ApplyStagePalette(CurrentStageData.enemySettings);
                _stagePaletteApplied = true;
            }

            switch (currentState)
            {
                case StageState.InJourney:
                    if (CurrentStageData != null && CurrentStageData.progressionSettings.IsEndlessMode)
                    {
                        // Endless Survival Mode: Được điều phối 100% độc quyền bởi VirtualRailEndlessDirector
                        // Không chạy timeline tĩnh và không trigger Climax
                    }
                    else
                    {
                        if (!_isWaveGated)
                        {
                            _stageTimer += dt;
                        }
                        ProcessTimelineEvents(dt);
                        UpdateJourneyState();
                    }
                    break;

                case StageState.Climax:
                    UpdateClimaxState(dt);
                    break;

                case StageState.WarpTransition:
                    UpdateWarpTransitionState(dt);
                    break;
            }

            // Cập nhật thanh tiến trình cho HUD (0 GC Alloc)
            if (notificationUI != null && currentState == StageState.InJourney)
            {
                notificationUI.UpdateProgressBar(StageProgress01);
            }
        }

        private void ProcessTimelineEvents(float dt)
        {
            StageData stage = CurrentStageData;
            if (stage == null || stage.timelineEvents == null || _nextTimelineIndex >= stage.timelineEvents.Length) return;

            ref StageTimelineEvent evt = ref stage.timelineEvents[_nextTimelineIndex];
            float currentVal = (evt.triggerMode == TimelineTriggerMode.TimeInSeconds)
                ? _stageTimer
                : (anchor != null ? (anchor.transform.position.z - _stageStartZ) : 0f);

            if (currentVal >= evt.triggerValue)
            {
                // Kiểm tra Wave Gating: Nếu là sự kiện sinh quái và yêu cầu chờ đợt trước dọn sạch
                if (evt.eventType == StageTimelineEventType.SpawnScriptedSquad && waveSpawner != null)
                {
                    if (evt.waitMode == WaveWaitMode.WaitForStrictClear)
                    {
                        if (waveSpawner.IsCurrentWaveActive())
                        {
                            _isWaveGated = true;
                            return; // Tạm dừng timeline, chờ người chơi tiêu diệt sạch đợt trước
                        }
                    }
                    else if (evt.waitMode == WaveWaitMode.WaitForClearOrTimeout)
                    {
                        float timeout = evt.maxWaitTimeout > 0.1f ? evt.maxWaitTimeout : 14.0f;
                        if (waveSpawner.IsCurrentWaveActive())
                        {
                            if (_waveGatingTimer < timeout)
                            {
                                _isWaveGated = true;
                                _waveGatingTimer += dt;
                                return; // Tiếp tục chờ giao chiến
                            }
                            else
                            {
                                // Quá hạn Timeout an toàn: Ép wave cũ rút lui giải phóng sàn đấu cho wave mới
                                waveSpawner.ForceRetreatCurrentWave();
                            }
                        }
                    }
                }

                // Sàn đấu đã sẵn sàng: mở khóa Gating và kích hoạt sự kiện
                _isWaveGated = false;
                _waveGatingTimer = 0f;
                ExecuteTimelineEvent(in evt);
                _nextTimelineIndex++;
            }
        }

        private void ExecuteTimelineEvent(in StageTimelineEvent evt)
        {
            switch (evt.eventType)
            {
                case StageTimelineEventType.RadioTransmission:
                    float duration = evt.messageDuration > 0.5f ? evt.messageDuration : 3.5f;
                    if (notificationUI != null)
                    {
                        notificationUI.ShowNotification(evt.messageTitle, evt.messageSubtitle, duration);
                    }
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX2D(AudioSFXType.RadioChirp, 0.75f);
                    }
                    OnRadioTransmission?.Invoke(evt.messageTitle, evt.messageSubtitle, duration);
                    break;

                case StageTimelineEventType.EnvironmentShift:
                    if (evt.changeFog)
                    {
                        RenderSettings.fog = evt.enableFog;
                        if (evt.enableFog)
                        {
                            RenderSettings.fogColor = evt.fogColor;
                            RenderSettings.fogDensity = evt.fogDensity;
                        }
                    }
                    if (evt.changeSkybox && evt.newSkybox != null)
                    {
                        RenderSettings.skybox = evt.newSkybox;
                        DynamicGI.UpdateEnvironment();
                    }
                    break;

                case StageTimelineEventType.SpawnScriptedSquad:
                    if (waveSpawner != null)
                    {
                        if (evt.scriptedFormationPattern != null)
                        {
                            waveSpawner.SpawnScriptedFormation(evt.scriptedFormationPattern);
                        }
                        else
                        {
                            waveSpawner.SpawnScriptedSquad(evt.customPrefab, evt.vectorType, evt.squadCount);
                        }
                    }
                    break;

                case StageTimelineEventType.SpeedChange:
                    if (anchor != null && evt.targetSpeed > 1f)
                    {
                        _normalSpeed = evt.targetSpeed;
                        anchor.SetSpeedOverride(evt.targetSpeed);
                        anchor.CurrentSpeed = evt.targetSpeed;
                    }
                    break;

                case StageTimelineEventType.Checkpoint:
                    if (notificationUI != null)
                    {
                        notificationUI.ShowNotification("CHECKPOINT REACHED", "MISSION PROGRESS SAVED", 2.5f);
                    }
                    break;

                case StageTimelineEventType.SpawnBoss:
                    TriggerBossEncounter();
                    break;
            }
        }

        private void UpdateJourneyState()
        {
            StageData stage = CurrentStageData;
            if (stage == null) return;
            if (stage.progressionSettings.IsEndlessMode) return;

            bool reachedEnd = false;
            switch (stage.progressionSettings.completionMode)
            {
                case StageCompletionMode.TimeDuration:
                    reachedEnd = _stageTimer >= stage.GetTargetDurationSeconds();
                    break;

                case StageCompletionMode.DistanceTraveled:
                    float traveled = anchor.transform.position.z - _stageStartZ;
                    reachedEnd = traveled >= stage.GetTargetDistanceZ();
                    break;

                case StageCompletionMode.EliminationQuota:
                    reachedEnd = _enemiesKilled >= stage.progressionSettings.targetEliminationCount;
                    break;

                case StageCompletionMode.DefeatBoss:
                    if (!_isBossSpawned)
                    {
                        TriggerBossEncounter();
                    }
                    break;

                case StageCompletionMode.SurvivalUntilBoss:
                    float dist = anchor.transform.position.z - _stageStartZ;
                    if (_stageTimer >= stage.GetTargetDurationSeconds() || dist >= stage.GetTargetDistanceZ())
                    {
                        if (!_isBossSpawned)
                        {
                            TriggerBossEncounter();
                        }
                    }
                    break;
            }

            if (reachedEnd)
            {
                TriggerClimax();
            }
        }

        public void TriggerBossEncounter()
        {
            if (_isBossSpawned) return;
            _isBossSpawned = true;

            StageData stage = CurrentStageData;
            if (stage != null && stage.climaxSettings.bossPrefab != null)
            {
                if (stage.climaxSettings.holdRailOnBoss && anchor != null)
                {
                    anchor.CurrentSpeed = Mathf.Max(5f, _normalSpeed * 0.25f);
                }

                Vector3 spawnPos = anchor.transform.position + anchor.ForwardTangent * 180f;
                _activeBossInstance = Instantiate(stage.climaxSettings.bossPrefab, spawnPos, Quaternion.LookRotation(-anchor.ForwardTangent));

                if (notificationUI != null)
                {
                    notificationUI.ShowNotification("WARNING: MASSIVE SIGNATURE", "TARGET DESTROY PROTOCOL ENGAGED", 4f);
                }

                if (AudioManager.Instance != null)
                {
                    AudioClip bossClip = stage != null ? stage.audioSettings.bossBgm : null;
                    float vol = stage != null ? stage.audioSettings.musicVolume : 1f;
                    AudioManager.Instance.PlayBossMusic(bossClip, vol);
                }
            }
            else
            {
                TriggerClimax();
            }
        }

        private void TriggerClimax()
        {
            currentState = StageState.Climax;
            _climaxTimer = 0f;

            StageData stage = CurrentStageData;
            OnStageClimaxReached?.Invoke(stage);
        }

        private void UpdateClimaxState(float dt)
        {
            _climaxTimer += dt;

            StageData stage = CurrentStageData;
            if (stage != null)
            {
                // Nếu là DefeatBoss và có Boss instance đang sống, đợi boss chết
                if (stage.climaxSettings.climaxType == StageClimaxType.DefeatBoss && _activeBossInstance != null)
                {
                    return;
                }

                // Chế độ thời gian hoặc cự ly: đợi 1.5s ngắt nhịp rồi kích hoạt Warp
                if (_climaxTimer >= 1.5f)
                {
                    StartWarpTransition();
                }
            }
            else if (_climaxTimer >= 1.5f)
            {
                StartWarpTransition();
            }
        }

        /// <summary>
        /// Kích hoạt chu trình nhảy vọt Hyperspace Warp để chuyển màn (0 GC Alloc).
        /// </summary>
        [ContextMenu("Debug: Force Warp to Next Stage")]
        public void StartWarpTransition()
        {
            if (currentState == StageState.WarpTransition) return;

            if (anchor == null)
            {
                anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();
            }

            if (anchor != null && anchor.CurrentSpeed > 1f)
            {
                _normalSpeed = anchor.CurrentSpeed;
                _warpPeakSpeed = _normalSpeed * 3f;
            }

            currentState = StageState.WarpTransition;
            _warpTimer = 0f;
            _isMidWarpSwapped = false;

            StageData currentStage = CurrentStageData;
            int nextIdx = currentStageIndex + 1;
            StageData nextStage = (stages != null && nextIdx < stages.Length) ? stages[nextIdx] : null;

            if (notificationUI != null)
            {
                string nextTarget = nextStage != null ? nextStage.stageTitle : "MISSION COMPLETE";
                notificationUI.ShowNotification("WARP SPEED ENGAGED", nextTarget, _warpDuration);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.SmartBomb, 0.6f, 1.4f);
            }
            VirtualRailCameraRig.TriggerShake(0.35f);

            OnWarpTransitionStarted?.Invoke(currentStage, nextStage);
        }

        private void UpdateWarpTransitionState(float dt)
        {
            _warpTimer += dt;
            float progress = Mathf.Clamp01(_warpTimer / Mathf.Max(0.1f, _warpDuration));

            // Đường cong vận tốc Warp: Tăng vọt lên cực đại ở giữa, rồi hạ cánh êm đềm ở cuối
            float speedFactor;
            if (progress < 0.5f)
            {
                float tIn = progress * 2f; // 0 -> 1
                speedFactor = Mathf.SmoothStep(0f, 1f, tIn);
                if (anchor != null)
                {
                    anchor.CurrentSpeed = Mathf.Lerp(_normalSpeed, _warpPeakSpeed, speedFactor);
                }
            }
            else
            {
                // Điểm giữa Warp: Hoán đổi môi trường sang Màn kế tiếp ngay trong vệt sáng cực đại
                if (!_isMidWarpSwapped)
                {
                    SwapToNextStageData();
                    _isMidWarpSwapped = true;
                }

                float tOut = (progress - 0.5f) * 2f; // 0 -> 1
                speedFactor = Mathf.SmoothStep(1f, 0f, tOut);
                if (anchor != null)
                {
                    anchor.CurrentSpeed = Mathf.Lerp(_normalSpeed, _warpPeakSpeed, speedFactor);
                }
            }

            // Hoàn tất chu trình Warp
            if (progress >= 1.0f)
            {
                CompleteWarpTransition();
            }
        }

        private void SwapToNextStageData()
        {
            int nextIndex = currentStageIndex + 1;

            if (stages == null || nextIndex >= stages.Length)
            {
                // Đã hoàn thành tất cả các màn!
                currentState = StageState.AllStagesCleared;
                if (notificationUI != null)
                {
                    notificationUI.ShowNotification("ALL STAGES CLEARED!", "EXCELLENT FLIGHT RECORD", 5.0f);
                }
                OnAllStagesCompleted?.Invoke();
                return;
            }

            currentStageIndex = nextIndex;
            StageData nextStage = stages[currentStageIndex];
            if (nextStage == null) return;

            if (anchor != null)
            {
                _stageStartZ = anchor.transform.position.z;
            }

            _stageTimer = 0f;
            _nextTimelineIndex = 0;
            _enemiesKilled = 0;
            _isBossSpawned = false;
            _activeBossInstance = null;

            _normalSpeed = nextStage.GetBaseForwardSpeed();
            _warpDuration = nextStage.GetWarpDuration();
            _warpPeakSpeed = _normalSpeed * nextStage.GetWarpSpeedMultiplier();

            // 1. Hoán đổi Môi trường
            ApplyEnvironment(nextStage.environmentSettings);

            // 2. Hoán đổi Bảng quái vật
            if (waveSpawner != null)
            {
                waveSpawner.ControlMode = SpawnerControlMode.ScriptedDirectorOnly;
                waveSpawner.ApplyStagePalette(nextStage.enemySettings);
                _stagePaletteApplied = true;
            }

            // 3. Hoán đổi Thiên thạch
            if (asteroidSpawner != null)
            {
                asteroidSpawner.ApplyStageHazardSettings(nextStage.hazardSettings);
            }

            if (notificationUI != null)
            {
                notificationUI.SetStageNumber(nextStage.stageIndex);
            }

            OnStageStarted?.Invoke(nextStage);
        }

        private void CompleteWarpTransition()
        {
            if (currentState == StageState.AllStagesCleared)
            {
                if (anchor != null)
                {
                    anchor.SetSpeedOverride(_normalSpeed);
                    anchor.CurrentSpeed = _normalSpeed;
                }
                return;
            }

            currentState = StageState.InJourney;
            if (anchor != null)
            {
                anchor.SetSpeedOverride(_normalSpeed);
                anchor.CurrentSpeed = _normalSpeed;
            }
        }

        /// <summary>
        /// Áp dụng các thông số thị giác môi trường mượt mà (Skybox, Fog, Backdrop, Ánh sáng, Bụi sao).
        /// </summary>
        public void ApplyEnvironment(StageEnvironmentSettings env)
        {
            // Skybox: Ưu tiên Skybox của màn, nếu để trống thì fallback về Skybox đen không gian ban đầu
            // Tối ưu hóa GPU: Chỉ đổi và nướng lại DynamicGI khi Material thực sự thay đổi khác hiện tại
            Material targetSkybox = env.skyboxMaterial != null ? env.skyboxMaterial : defaultSkyboxMaterial;
            if (targetSkybox != null && RenderSettings.skybox != targetSkybox)
            {
                RenderSettings.skybox = targetSkybox;
                DynamicGI.UpdateEnvironment();
                Debug.Log($"<color=cyan><b>[StageDirector] ĐÃ ĐỔI SKYBOX SANG:</b> {targetSkybox.name}</color>");
            }

            // Ambient Light
            RenderSettings.ambientLight = env.ambientLightColor;

            // Fog
            RenderSettings.fog = env.enableFog;
            if (env.enableFog)
            {
                RenderSettings.fogColor = env.fogColor;
                RenderSettings.fogDensity = env.fogDensity;
            }

            // Directional Light
            if (directionalLight != null)
            {
                directionalLight.color = env.directionalLightColor;
                directionalLight.intensity = env.lightIntensity;
            }

            // Backdrop: Tự động điều phối phông nền chân trời trực diện
            if (backdrop == null) backdrop = FindAnyObjectByType<VirtualRailHorizonBackdrop>();
            if (backdrop != null)
            {
                backdrop.ApplyStageSettings(env);
                if (env.backdropTexture != null || env.backdropMaterial != null)
                {
                    string backdropName = env.backdropTexture != null ? env.backdropTexture.name : env.backdropMaterial.name;
                    Debug.Log($"<color=cyan><b>[StageDirector] ĐÃ ĐỔI HORIZON BACKDROP SANG:</b> {backdropName}</color>");
                }
            }

            // Space Dust
            if (spaceDust != null)
            {
                spaceDust.SetDustSettings(env.spaceDustColor, env.spaceDustCount);
            }
        }

        /// <summary>
        /// Kích hoạt thông điệp bộ đàm radio tác chiến từ bất kỳ hệ thống nào (0 GC Alloc).
        /// </summary>
        public static void TriggerRadioTransmission(string speaker, string message, float duration = 3.5f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.RadioChirp, 0.75f);
            }
            OnRadioTransmission?.Invoke(speaker, message, duration);
        }
    }
}
