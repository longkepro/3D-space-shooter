using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Chế độ vận hành của bộ sinh quái:
    /// - ScriptedDirectorOnly: 100% quái và formation do StageDirector điều phối qua Timeline kịch bản (Production/Arcade).
    /// - EndlessSandboxDemo: Tự động đếm giờ sinh quái ngẫu nhiên tuần hoàn (Chỉ dùng kiểm thử sandbox).
    /// </summary>
    public enum SpawnerControlMode
    {
        ScriptedDirectorOnly = 0,
        EndlessSandboxDemo = 1
    }

    /// <summary>
    /// Bộ điều phối đợt tấn công đường ray (Rail Wave Spawner - Hướng Star Fox 64).
    /// Tuân thủ TDD v1.0.0:
    /// - Quản lý 10 Vector xuất hiện không gian (Spatial Vectors).
    /// - Quản lý máy trạng thái vòng đời 4 pha (EnemyLifecycleFSM).
    /// - Biên đạo phi đội hình bay (Formation Choreography) với Bézier 3D và Panic Scatter.
    /// - Quản lý Object Pool tái sử dụng 100% (0 GC Alloc).
    /// </summary>
    public class VirtualRailWaveSpawner : MonoBehaviour
    {
        public static VirtualRailWaveSpawner Instance { get; private set; }

        [Header("Control Mode (Kiến Trúc Module Hóa)")]
        [Tooltip("Chế độ điều phối: ScriptedDirectorOnly = 100% theo kịch bản StageDirector; EndlessSandboxDemo = sinh quái ngẫu nhiên")]
        [SerializeField] private SpawnerControlMode controlMode = SpawnerControlMode.ScriptedDirectorOnly;
        public SpawnerControlMode ControlMode
        {
            get => controlMode;
            set => controlMode = value;
        }

        [Header("Configuration")]
        public VirtualRailAnchor anchor;
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private int poolSizePerType = 8;

        [Header("Wave Spawning Settings")]
        [Tooltip("Cự ly sinh quái vật phía trước người chơi theo trục Z (m)")]
        [SerializeField] private float spawnLeadDistance = 220f;
        [Tooltip("Chu kỳ sinh đợt tấn công mới (giây)")]
        [SerializeField] private float waveInterval = 3.5f;
        [Tooltip("Cự ly thu hồi quái vật khi người chơi bay vượt qua phía sau (m)")]
        [SerializeField] private float despawnBehindDistance = 50f;

        [Header("Spatial Vectors (TDD v1.0.0 Phần II)")]
        [SerializeField] private bool useSpatialVectors = true;
        [SerializeField] private bool enable4PhaseFSM = true;

        [Header("Formation Choreography (TDD v1.0.0 Phần III)")]
        [SerializeField] private bool enableFormations = true;
        [SerializeField] private int formationWaveInterval = 3;

        private readonly List<EnemyMovement> _pool = new List<EnemyMovement>();
        private readonly List<VirtualFormationAnchor> _formationPool = new List<VirtualFormationAnchor>();
        private Transform _poolRoot;
        private float _nextWaveTime = 0f;
        private bool _isSpawning = false;
        private bool _isInitialized = false;
        private int _vectorCycleIndex = 0;
        private int _waveCounter = 0;
        private Camera _mainCam;

        private static readonly SpatialVectorType[] ACTIVE_SPATIAL_VECTORS = new SpatialVectorType[]
        {
            SpatialVectorType.FrontalHeadOn,
            SpatialVectorType.RearAmbush,
            SpatialVectorType.LateralFlankingLeft,
            SpatialVectorType.OverheadDiveBomb,
            SpatialVectorType.LateralFlankingRight,
            SpatialVectorType.SubSurfaceBreach,
            SpatialVectorType.WarpInDecloak
        };

        private SpatialVectorType[] _activeVectors = null;
        private GameObject[] _stageEnemyPrefabs = null;
        private FormationPattern[] _stageFormationPatterns = null;
        private int _formationPatternCycleIndex = 0;
        private bool _allowShooting = true;
        private float _stageBulletSpeed = 22f;
        private float _stageFireCooldownScale = 1.0f;
        private float _stageShooterRatio = 1.0f;
        public float StageShooterRatio => _stageShooterRatio;
        private bool _stageEnableFormations = true;
        private float _stageWaveInterval = -1f;
        private int _stageFormationInterval = -1;
        private float _stageLeadDistance = -1f;
        private bool _stageEnablePeriodicWaves = false;

        // --- Active Wave Tracking & Gating (0 GC Alloc) ---
        private readonly List<EnemyMovement> _activeWaveEnemies = new List<EnemyMovement>(16);
        private VirtualFormationAnchor _activeWaveFormationAnchor = null;
        private float _currentWaveTimer = 0f;
        private float _currentWaveMaxLifetime = 14f;
        private bool _isCurrentWaveTracking = false;

        /// <summary>
        /// Nạp thông số bảng quái và các vector xuất hiện theo màn chơi (0 GC Alloc, không dùng default ngầm).
        /// </summary>
        public void ApplyStagePalette(StageEnemySettings settings)
        {
            _activeVectors = (settings.allowedVectors != null && settings.allowedVectors.Length > 0) ? settings.allowedVectors : null;
            _stageEnemyPrefabs = (settings.stageEnemyPrefabs != null && settings.stageEnemyPrefabs.Length > 0) ? settings.stageEnemyPrefabs : null;
            _stageFormationPatterns = (settings.stageFormationPool != null && settings.stageFormationPool.Length > 0) ? settings.stageFormationPool : null;
            _allowShooting = settings.allowShooting;
            _stageBulletSpeed = settings.bulletSpeed > 1f ? settings.bulletSpeed : 22f;
            _stageFireCooldownScale = 1.0f;
            _stageShooterRatio = 1.0f;
            _stageEnableFormations = settings.enableFormations && (settings.formationIntervalWaves > 0);

            // Chế độ ScriptedDirectorOnly: KHÓA CỨNG 100% việc sinh đợt quái tuần hoàn ngẫu nhiên
            if (controlMode == SpawnerControlMode.ScriptedDirectorOnly)
            {
                _stageEnablePeriodicWaves = false;
            }
            else
            {
                _stageEnablePeriodicWaves = settings.enablePeriodicWaves && (settings.waveInterval > 0.1f);
            }

            if (settings.waveInterval > 0.1f)
            {
                _stageWaveInterval = settings.waveInterval;
            }
            if (settings.formationIntervalWaves > 0)
            {
                _stageFormationInterval = settings.formationIntervalWaves;
            }
            if (settings.spawnLeadDistance > 10f)
            {
                _stageLeadDistance = settings.spawnLeadDistance;
            }
        }

        /// <summary>
        /// Điều chỉnh tốc độ đạn quái vật phục vụ thuật toán DDA (Dynamic Difficulty Adjustment).
        /// </summary>
        public void SetStageBulletSpeed(float bulletSpeed)
        {
            _stageBulletSpeed = bulletSpeed;
            if (_activeWaveEnemies != null)
            {
                for (int i = 0; i < _activeWaveEnemies.Count; i++)
                {
                    if (_activeWaveEnemies[i] != null && _activeWaveEnemies[i].gameObject.activeSelf)
                    {
                        var atk = _activeWaveEnemies[i].GetComponent<EnemyAttack>();
                        if (atk != null) atk.SetBulletSpeedOverride(bulletSpeed);
                    }
                }
            }
        }

        /// <summary>
        /// Điều chỉnh hệ số giãn cách bắn đạn của quái vật phục vụ thuật toán DDA (Dynamic Difficulty Adjustment).
        /// </summary>
        public void SetStageFireCooldownScale(float cooldownScale)
        {
            _stageFireCooldownScale = Mathf.Clamp(cooldownScale, 0.45f, 1.0f);
            if (_activeWaveEnemies != null)
            {
                for (int i = 0; i < _activeWaveEnemies.Count; i++)
                {
                    if (_activeWaveEnemies[i] != null && _activeWaveEnemies[i].gameObject.activeSelf)
                    {
                        var atk = _activeWaveEnemies[i].GetComponent<EnemyAttack>();
                        if (atk != null) atk.SetFireCooldownScale(_stageFireCooldownScale);
                    }
                }
            }
        }

        /// <summary>
        /// Điều chỉnh tỉ lệ số lượng tàu địch được phép bắn trong một đợt quái (Shooter Quota) theo DDA Threat Level.
        /// Giá trị trong dải [0.20, 1.0] (0 GC Alloc).
        /// </summary>
        public void SetStageShooterRatio(float ratio)
        {
            _stageShooterRatio = Mathf.Clamp(ratio, 0.20f, 1.0f);
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializePool();
        }

        private void Start()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();
            if (_mainCam == null) _mainCam = Camera.main;
            if (!_isSpawning && anchor != null && anchor.CurrentSpeed > 0.5f)
            {
                StartSpawning();
            }
        }

        private void OnEnable()
        {
            GameEventManager.OnStartGame += StartSpawning;
            GameEventManager.OnPlayerDestroyed += StopSpawning;
        }

        private void OnDisable()
        {
            GameEventManager.OnStartGame -= StartSpawning;
            GameEventManager.OnPlayerDestroyed -= StopSpawning;
        }

        public void InitializePool()
        {
            if (_isInitialized) return;

            if (anchor == null) anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();

            // Khởi tạo gốc Pool quái vật độc lập trong World Space (0 GC Alloc, không bị kéo dạt theo tàu mẹ)
            if (_poolRoot == null)
            {
                GameObject existingRoot = GameObject.Find("[VirtualRail_EnemyPool]");
                if (existingRoot != null)
                {
                    _poolRoot = existingRoot.transform;
                }
                else
                {
                    GameObject rootObj = new GameObject("[VirtualRail_EnemyPool]");
                    _poolRoot = rootObj.transform;
                    _poolRoot.position = Vector3.zero;
                    _poolRoot.rotation = Quaternion.identity;
                }
            }

            var cfg = anchor != null ? anchor.config : null;

            // Tự động tìm nạp trọn bộ 5 Prefab Enemy chuẩn hóa (StarSparrow + Squad Leader + Shield Bearer)
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                var e0 = Resources.Load<GameObject>("Prefabs/Enemy (0) - Scout") ?? Resources.Load<GameObject>("Prefabs/Enemy (0)");
                var e1 = Resources.Load<GameObject>("Prefabs/Enemy (1) - Flanker") ?? Resources.Load<GameObject>("Prefabs/Enemy (1)");
                var e2 = Resources.Load<GameObject>("Prefabs/Enemy (2) - Interceptor") ?? Resources.Load<GameObject>("Prefabs/Enemy (2)");
                var e3 = Resources.Load<GameObject>("Prefabs/Enemy (3) - Squad Leader") ?? Resources.Load<GameObject>("Prefabs/Enemy (3)");
                var e4 = Resources.Load<GameObject>("Prefabs/Enemy (4) - Shield Bearer") ?? Resources.Load<GameObject>("Prefabs/Enemy (4)");

#if UNITY_EDITOR
                if (e0 == null) e0 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Enemy (0) - Scout.prefab");
                if (e1 == null) e1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Enemy (1) - Flanker.prefab");
                if (e2 == null) e2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Enemy (2) - Interceptor.prefab");
                if (e3 == null) e3 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Enemy (3) - Squad Leader.prefab");
                if (e4 == null) e4 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Enemy (4) - Shield Bearer.prefab");
#endif
                var list = new List<GameObject>();
                if (e0 != null) list.Add(e0);
                if (e1 != null) list.Add(e1);
                if (e2 != null) list.Add(e2);
                if (e3 != null) list.Add(e3);
                if (e4 != null) list.Add(e4);
                if (list.Count > 0)
                {
                    enemyPrefabs = list.ToArray();
                }
            }

            if (enemyPrefabs != null)
            {
                for (int p = 0; p < enemyPrefabs.Length; p++)
                {
                    if (enemyPrefabs[p] == null) continue;
                    for (int i = 0; i < poolSizePerType; i++)
                    {
                        GameObject enemyObj = Instantiate(enemyPrefabs[p], _poolRoot);
                        enemyObj.name = enemyPrefabs[p].name + "_Pooled_" + i;
                        enemyObj.tag = "Enemy";
                        enemyObj.SetActive(false);

                        // Đảm bảo toàn bộ collider con có Tag Enemy để dò trúng đạn và Bullet Magnetism
                        var colliders = enemyObj.GetComponentsInChildren<Collider>(true);
                        for (int c = 0; c < colliders.Length; c++)
                        {
                            colliders[c].gameObject.tag = "Enemy";
                        }
                        if (colliders.Length == 0)
                        {
                            var box = enemyObj.AddComponent<BoxCollider>();
                            box.size = new Vector3(4f, 1.5f, 6f);
                        }

                        var movement = enemyObj.GetComponent<EnemyMovement>();
                        if (movement == null) movement = enemyObj.AddComponent<EnemyMovement>();
                        _pool.Add(movement);

                        // Gắn sẵn EnemyHealth, FSM và Wingman để 0 GC Alloc lúc runtime
                        var health = enemyObj.GetComponent<EnemyHealth>();
                        if (health == null) health = enemyObj.AddComponent<EnemyHealth>();

                        var explosion = enemyObj.GetComponent<Explosion>();
                        if (explosion == null) explosion = enemyObj.AddComponent<Explosion>();

                        var attack = enemyObj.GetComponent<EnemyAttack>();
                        if (attack == null) attack = enemyObj.AddComponent<EnemyAttack>();

                        EnemyArchetype archType = EnemyArchetype.Scout;
                        if (p == 1) archType = EnemyArchetype.Flanker;
                        else if (p == 2) archType = EnemyArchetype.Interceptor;
                        else if (p == 3) archType = EnemyArchetype.SquadLeader;
                        else if (p == 4) archType = EnemyArchetype.ShieldBearer;

                        var preset = EnemyData.CreateDefaultPreset(archType);
                        if (movement != null) movement.ApplyArchetype(preset);
                        if (health != null) health.ApplyData(preset);

                        if (enemyObj.GetComponent<EnemyLifecycleFSM>() == null)
                        {
                            enemyObj.AddComponent<EnemyLifecycleFSM>();
                        }
                        if (enemyObj.GetComponent<FormationWingman>() == null)
                        {
                            enemyObj.AddComponent<FormationWingman>();
                        }
                    }
                }
            }

            // Khởi tạo CollectiblePool nếu chưa có trong scene
            if (VirtualRailCollectiblePool.Instance == null)
            {
                GameObject colPoolObj = new GameObject("[VirtualRail_CollectiblePool]");
                colPoolObj.AddComponent<VirtualRailCollectiblePool>();
            }

            // Khởi tạo sẵn 2 Formation Anchors trong World Space (0 GC Alloc)
            for (int f = 0; f < 2; f++)
            {
                GameObject fObj = new GameObject($"[VirtualFormationAnchor_{f}]");
                fObj.transform.SetParent(_poolRoot, false);
                VirtualFormationAnchor fa = fObj.AddComponent<VirtualFormationAnchor>();
                fObj.SetActive(false);
                _formationPool.Add(fa);
            }

            _isInitialized = true;
        }

        public void StartSpawning()
        {
            if (_isSpawning) return;
            _isSpawning = true;
            if (controlMode == SpawnerControlMode.EndlessSandboxDemo)
            {
                _nextWaveTime = Time.time + 1.5f; // Đợi 1.5s sau khi vào game rồi bắt đầu đợt 1 (Chế độ Sandbox)
            }
            else
            {
                _nextWaveTime = float.MaxValue; // Chế độ ScriptedDirectorOnly: Tuyệt đối không tự đếm giờ sinh quái
            }
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            _isCurrentWaveTracking = false;
            _activeWaveEnemies.Clear();
            _activeWaveFormationAnchor = null;
            RecycleAll();
        }

        private void Update()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance;
            if (anchor == null) return;

            var cfg = anchor.config;
            bool enabledByConfig = cfg == null || cfg.useRailWaveSpawner;
            if (!enabledByConfig) return;

            // Tự động kích hoạt khi tàu bắt đầu di chuyển nếu chưa nhận được sự kiện UI
            if (!_isSpawning && anchor.CurrentSpeed > 0.5f)
            {
                StartSpawning();
            }

            if (!_isSpawning) return;

            float effectiveInterval = _stageWaveInterval > 0f ? _stageWaveInterval : (cfg != null ? cfg.waveSpawnInterval : waveInterval);
            float effectiveLead = _stageLeadDistance > 0f ? _stageLeadDistance : (cfg != null ? cfg.waveSpawnDistance : spawnLeadDistance);

            // 1. Quét sinh đợt quái mới:
            // CHỈ sinh đợt quái tuần hoàn nếu đang ở chế độ EndlessSandboxDemo VÀ _stageEnablePeriodicWaves được bật
            if (controlMode == SpawnerControlMode.EndlessSandboxDemo && _stageEnablePeriodicWaves && Time.time >= _nextWaveTime)
            {
                SpawnWave(effectiveLead);
                _nextWaveTime = Time.time + effectiveInterval;
            }

            // 2. Tự động thu hồi các quái vật đã bị người chơi bỏ xa lại phía sau
            float playerZ = anchor.transform.position.z;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    if (_pool[i].transform.position.z < playerZ - despawnBehindDistance)
                    {
                        _pool[i].Recycle();
                    }
                }
            }

            // 3. Tự động kiểm tra Timeout của Wave hiện tại: nếu đã quá hạn mà quái chưa chết hết, ép rút lui để nhường sàn đấu
            if (_isCurrentWaveTracking)
            {
                _currentWaveTimer += Time.deltaTime;
                if (_currentWaveTimer >= _currentWaveMaxLifetime)
                {
                    ForceRetreatCurrentWave();
                }
                else if (!IsCurrentWaveActive())
                {
                    _isCurrentWaveTracking = false;
                }
            }
        }

        private void SpawnWave(float leadDist)
        {
            if (anchor == null) return;

            var cfg = anchor.config;
            bool useSpatial = (cfg != null) ? cfg.useSpatialVectors : useSpatialVectors;
            bool useFSM = (cfg != null) ? cfg.enable4PhaseLifecycle : enable4PhaseFSM;
            bool useFormations = (cfg != null) ? cfg.enableFormations : enableFormations;
            int interval = _stageFormationInterval > 0 ? _stageFormationInterval : ((cfg != null) ? cfg.formationIntervalWaves : formationWaveInterval);

            if (!useSpatial)
            {
                SpawnLegacyWave(leadDist);
                return;
            }

            _waveCounter++;

            // Kiểm tra đợt bay đội hình (Formation Wave)
            if (useFormations && (_waveCounter % interval == 0))
            {
                if (TrySpawnFormationWave(leadDist, cfg))
                {
                    return;
                }
            }

            Camera mainCam = _mainCam != null ? _mainCam : Camera.main;
            int countInWave = Random.Range(2, 4);
            int allowedShooters = Mathf.Max(1, Mathf.RoundToInt(countInWave * _stageShooterRatio));

            for (int i = 0; i < countInWave; i++)
            {
                EnemyMovement availableEnemy = GetAvailableStandardEnemy();
                if (availableEnemy == null) break;

                // Chọn vector tiếp theo theo chu kỳ (nếu stage không chỉ định vector thì dùng FrontalHeadOn cơ bản)
                SpatialVectorType vectorType;
                if (_activeVectors != null && _activeVectors.Length > 0)
                {
                    vectorType = _activeVectors[_vectorCycleIndex % _activeVectors.Length];
                    _vectorCycleIndex++;
                }
                else
                {
                    vectorType = SpatialVectorType.FrontalHeadOn;
                }

                SpatialTrajectoryData trajectory = SpatialVectorFactory.GenerateTrajectory(vectorType, anchor, mainCam, leadDist);

                availableEnemy.ResetState();
                availableEnemy.transform.localScale = Vector3.one;
                availableEnemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : anchor.transform);

                var attackComp = availableEnemy.GetComponent<EnemyAttack>();
                if (attackComp != null)
                {
                    attackComp.enabled = _allowShooting && (i < allowedShooters);
                    attackComp.SetFireCooldownScale(_stageFireCooldownScale);
                    if (_stageBulletSpeed > 5f)
                    {
                        attackComp.SetBulletSpeedOverride(_stageBulletSpeed);
                    }
                }

                var wingman = availableEnemy.GetComponent<FormationWingman>();
                if (wingman != null)
                {
                    wingman.ResetState();
                    wingman.enabled = false;
                }

                if (useFSM)
                {
                    var fsm = availableEnemy.GetComponent<EnemyLifecycleFSM>();
                    if (fsm != null)
                    {
                        fsm.enabled = true;
                        fsm.InitializeTrajectory(trajectory, anchor);
                    }
                }
                else
                {
                    var fsm = availableEnemy.GetComponent<EnemyLifecycleFSM>();
                    if (fsm != null)
                    {
                        fsm.ResetState();
                        fsm.enabled = false;
                    }
                    availableEnemy.enabled = true;
                    availableEnemy.transform.position = trajectory.spawnPosition;
                    availableEnemy.transform.rotation = trajectory.spawnRotation;
                }

                availableEnemy.gameObject.SetActive(true);
            }
        }

        private bool TrySpawnFormationWave(float leadDist, VirtualRailConfig cfg)
        {
            float formDist = (cfg != null) ? cfg.formationSpawnDistance : 110f;

            // Nếu màn chơi có cấu hình Data-Driven Formation Pattern riêng, ưu tiên sử dụng
            if (_stageFormationPatterns != null && _stageFormationPatterns.Length > 0)
            {
                FormationPattern pattern = _stageFormationPatterns[_formationPatternCycleIndex % _stageFormationPatterns.Length];
                _formationPatternCycleIndex++;
                if (pattern != null && SpawnScriptedFormation(pattern, formDist))
                {
                    return true;
                }
            }

            VirtualFormationAnchor availableAnchor = GetAvailableFormationAnchor();
            if (availableAnchor == null) return false;

            List<EnemyMovement> enemies = new List<EnemyMovement>(5);

            if (cfg != null && cfg.enableModularEnemies)
            {
                // 1. Tìm đúng Squad Leader (Enemy 3) cho vị trí chỉ huy Slot 0
                EnemyMovement leader = GetAvailableEnemyByArchetype(EnemyArchetype.SquadLeader);
                if (leader == null) leader = GetAvailableEnemy();
                if (leader != null) enemies.Add(leader);

                // 2. Tìm 2 Shield Bearer (Enemy 4) cho vị trí hộ vệ Slot 1 & 2
                bool useShieldBearers = cfg.enableShieldBearerEnemies;
                for (int s = 0; s < 2; s++)
                {
                    EnemyMovement shield = useShieldBearers ? GetAvailableEnemyByArchetype(EnemyArchetype.ShieldBearer) : null;
                    if (shield == null) shield = GetAvailableEnemyByArchetype(EnemyArchetype.Flanker);
                    if (shield == null) shield = GetAvailableEnemy();
                    if (shield != null && !enemies.Contains(shield)) enemies.Add(shield);
                }

                // 3. Rút Flanker / Scout cho các vị trí còn lại
                while (enemies.Count < 5)
                {
                    EnemyMovement wing = GetAvailableEnemyByArchetype(EnemyArchetype.Flanker);
                    if (wing == null) wing = GetAvailableEnemyByArchetype(EnemyArchetype.Scout);
                    if (wing == null) wing = GetAvailableEnemy();
                    if (wing == null || enemies.Contains(wing)) break;
                    enemies.Add(wing);
                }
            }
            else
            {
                enemies = GetAvailableEnemies(5);
            }

            if (enemies.Count < 3) return false;

            VirtualFormationAnchor.FormationType fType = (enemies.Count >= 5)
                ? VirtualFormationAnchor.FormationType.WedgeV
                : VirtualFormationAnchor.FormationType.EchelonLine;

            Vector3 startPos = anchor.transform.position + anchor.ForwardTangent * formDist + anchor.transform.up * 4f;
            float assemblyTime = (cfg != null) ? cfg.formationAssemblyTime : 1.2f;

            if (cfg != null && cfg.enableModularEnemies)
            {
                // Nếu tàu chỉ huy chưa mang archetype SquadLeader thì gán chỉ số
                if (enemies[0].ArchetypeData == null || enemies[0].ArchetypeData.archetype != EnemyArchetype.SquadLeader)
                {
                    enemies[0].ApplyArchetype(EnemyData.CreateDefaultPreset(EnemyArchetype.SquadLeader));
                }
                enemies[0].transform.localScale = Vector3.one;

                bool useShieldBearers = cfg.enableShieldBearerEnemies && enemies.Count >= 3;

                for (int k = 1; k < enemies.Count; k++)
                {
                    if (useShieldBearers && (k == 1 || k == 2))
                    {
                        if (enemies[k].ArchetypeData == null || enemies[k].ArchetypeData.archetype != EnemyArchetype.ShieldBearer)
                        {
                            enemies[k].ApplyArchetype(EnemyData.CreateDefaultPreset(EnemyArchetype.ShieldBearer));
                        }
                    }
                    else
                    {
                        if (enemies[k].ArchetypeData == null || (enemies[k].ArchetypeData.archetype != EnemyArchetype.Flanker && enemies[k].ArchetypeData.archetype != EnemyArchetype.Scout))
                        {
                            enemies[k].ApplyArchetype(EnemyData.CreateDefaultPreset(EnemyArchetype.Flanker));
                        }
                    }
                    enemies[k].transform.localScale = Vector3.one;
                }
            }
            else
            {
                for (int k = 0; k < enemies.Count; k++)
                {
                    enemies[k].transform.localScale = Vector3.one;
                }
            }

            int allowedShooters = Mathf.Max(1, Mathf.RoundToInt(enemies.Count * _stageShooterRatio));
            for (int k = 0; k < enemies.Count; k++)
            {
                var atk = enemies[k].GetComponent<EnemyAttack>();
                if (atk != null)
                {
                    atk.enabled = _allowShooting && (k < allowedShooters);
                    atk.SetFireCooldownScale(_stageFireCooldownScale);
                    if (_stageBulletSpeed > 5f)
                    {
                        atk.SetBulletSpeedOverride(_stageBulletSpeed);
                    }
                }
            }

            availableAnchor.InitializeFormation(fType, anchor, startPos, enemies, assemblyTime);
            return true;
        }

        private VirtualFormationAnchor GetAvailableFormationAnchor()
        {
            for (int i = 0; i < _formationPool.Count; i++)
            {
                if (_formationPool[i] != null && !_formationPool[i].gameObject.activeSelf)
                {
                    return _formationPool[i];
                }
            }
            return null;
        }

        private EnemyMovement GetAvailableEnemyByArchetype(EnemyArchetype archetype)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    if (_pool[i].ArchetypeData != null && _pool[i].ArchetypeData.archetype == archetype)
                    {
                        return _pool[i];
                    }
                }
            }
            return null;
        }

        private EnemyMovement GetAvailableStandardEnemy()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    if (_pool[i].ArchetypeData != null &&
                        (_pool[i].ArchetypeData.archetype == EnemyArchetype.Scout ||
                         _pool[i].ArchetypeData.archetype == EnemyArchetype.Flanker ||
                         _pool[i].ArchetypeData.archetype == EnemyArchetype.Interceptor))
                    {
                        return _pool[i];
                    }
                }
            }
            return GetAvailableEnemy();
        }

        private List<EnemyMovement> GetAvailableEnemies(int count)
        {
            List<EnemyMovement> list = new List<EnemyMovement>(count);
            for (int i = 0; i < _pool.Count && list.Count < count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    list.Add(_pool[i]);
                }
            }
            return list;
        }

        private void SpawnLegacyWave(float leadDist)
        {
            Vector3 playerPos = anchor.transform.position;
            float targetZ = playerPos.z + leadDist;

            int countInWave = Random.Range(1, 4);
            float baseOffsetX = Random.Range(-14f, 14f);
            float baseOffsetY = Random.Range(3f, 13f);

            for (int i = 0; i < countInWave; i++)
            {
                EnemyMovement availableEnemy = GetAvailableEnemy();
                if (availableEnemy == null) break;

                Vector3 spawnPos = new Vector3(
                    baseOffsetX + (i - 1) * 6f,
                    baseOffsetY + (i % 2 == 0 ? 2f : -2f),
                    targetZ + (i * 15f)
                );

                availableEnemy.transform.position = spawnPos;
                Vector3 dirToPlayer = (playerPos - spawnPos).normalized;
                availableEnemy.transform.rotation = Quaternion.LookRotation(dirToPlayer);

                availableEnemy.ResetState();
                availableEnemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : anchor.transform);
                availableEnemy.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Kích hoạt Vector 9: Cluster Fracture khi một thiên thạch hoặc tàu mẹ phát nổ (TDD Phần II.1).
        /// </summary>
        public void TriggerClusterFracture(Vector3 explosionCenter)
        {
            // Tránh sinh các tàu chiến quái vật chiến đấu dính chùm vào tâm vụ nổ.
            // Phân rã mảnh vụn hình ảnh trong tương lai sẽ sử dụng Particle VFX Pool thay vì rút thực thể từ Enemy Pool.
        }

        private EnemyMovement GetAvailableEnemy()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Sinh một cụm phi đội kịch bản đặc biệt từ Timeline kịch bản (0 GC Alloc).
        /// </summary>
        public void SpawnScriptedSquad(GameObject customPrefab, SpatialVectorType vectorType, int count)
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance;
            if (anchor == null) return;

            // Dọn sạch tàn dư đợt quái cũ trước khi kích hoạt đợt mới (0 Wave Overlap)
            ForceRetreatCurrentWave();

            Camera mainCam = _mainCam != null ? _mainCam : Camera.main;
            float leadDist = _stageLeadDistance > 10f ? _stageLeadDistance : spawnLeadDistance;
            int actualCount = Mathf.Clamp(count, 1, 6);
            int allowedShooters = Mathf.Max(1, Mathf.RoundToInt(actualCount * _stageShooterRatio));

            _activeWaveEnemies.Clear();
            _activeWaveFormationAnchor = null;
            _currentWaveTimer = 0f;
            _currentWaveMaxLifetime = 14f;
            _isCurrentWaveTracking = true;

            for (int i = 0; i < actualCount; i++)
            {
                EnemyMovement enemy = GetAvailableStandardEnemy();
                if (enemy == null) break;

                var wingman = enemy.GetComponent<FormationWingman>();
                if (wingman != null)
                {
                    wingman.ResetState();
                    wingman.enabled = false;
                }

                SpatialTrajectoryData trajectory = SpatialVectorFactory.GenerateTrajectory(vectorType, anchor, mainCam, leadDist + (i * 12f));
                enemy.ResetState();
                enemy.transform.localScale = Vector3.one;
                enemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : anchor.transform);

                var attack = enemy.GetComponent<EnemyAttack>();
                if (attack != null)
                {
                    attack.enabled = _allowShooting && (i < allowedShooters);
                    attack.SetFireCooldownScale(_stageFireCooldownScale);
                    if (_stageBulletSpeed > 5f)
                    {
                        attack.SetBulletSpeedOverride(_stageBulletSpeed);
                    }
                }

                var fsm = enemy.GetComponent<EnemyLifecycleFSM>();
                if (fsm != null)
                {
                    fsm.enabled = true;
                    fsm.InitializeTrajectory(trajectory, anchor);
                }

                enemy.gameObject.SetActive(true);
                _activeWaveEnemies.Add(enemy);
            }
        }

        /// <summary>
        /// Kích hoạt một đợt bay đội hình theo Mẫu Data-Driven FormationPattern từ Timeline hoặc kịch bản.
        /// </summary>
        public bool SpawnScriptedFormation(FormationPattern pattern, float customLeadDist = -1f, float overrideAssemblyTime = -1f)
        {
            return SpawnScriptedFormation(pattern, default, customLeadDist, overrideAssemblyTime);
        }

        /// <summary>
        /// Kích hoạt một đợt bay đội hình theo Mẫu Data-Driven FormationPattern kèm tham số tùy biến (Endless DDA).
        /// </summary>
        public bool SpawnScriptedFormation(FormationPattern pattern, VirtualFormationAnchor.FormationCustomizationParams customParams, float customLeadDist = -1f, float overrideAssemblyTime = -1f)
        {
            if (pattern == null || pattern.slots == null || pattern.slots.Length == 0) return false;
            if (anchor == null) anchor = VirtualRailAnchor.Instance;
            if (anchor == null) return false;

            // Dọn sạch tàn dư đợt quái cũ trước khi kích hoạt đợt mới (0 Wave Overlap)
            ForceRetreatCurrentWave();

            VirtualFormationAnchor availableAnchor = GetAvailableFormationAnchor();
            if (availableAnchor == null) return false;

            int neededCount = pattern.slots.Length;
            List<EnemyMovement> enemies = new List<EnemyMovement>(neededCount);

            for (int i = 0; i < neededCount; i++)
            {
                var slot = pattern.slots[i];
                EnemyArchetype targetArch = (customParams.archetypeOverride.HasValue && !slot.isLeader)
                    ? customParams.archetypeOverride.Value
                    : slot.preferredArchetype;

                EnemyMovement enemy = GetAvailableEnemyByArchetype(targetArch);
                if (enemy == null) enemy = GetAvailableEnemy();
                if (enemy != null && !enemies.Contains(enemy))
                {
                    enemies.Add(enemy);
                }
            }

            if (enemies.Count < neededCount)
            {
                for (int i = 0; i < _pool.Count && enemies.Count < neededCount; i++)
                {
                    if (_pool[i] != null && !_pool[i].gameObject.activeSelf && !enemies.Contains(_pool[i]))
                    {
                        enemies.Add(_pool[i]);
                    }
                }
            }

            if (enemies.Count < 2) return false;

            float formDist = customLeadDist > 10f ? customLeadDist : ((_stageLeadDistance > 10f) ? _stageLeadDistance : 110f);
            Vector3 startPos = anchor.transform.position + anchor.ForwardTangent * formDist + anchor.transform.up * 4f;

            _activeWaveEnemies.Clear();
            int allowedShooters = Mathf.Max(1, Mathf.RoundToInt(enemies.Count * _stageShooterRatio));
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    _activeWaveEnemies.Add(enemies[i]);
                    var atk = enemies[i].GetComponent<EnemyAttack>();
                    if (atk != null)
                    {
                        atk.enabled = _allowShooting && (i < allowedShooters);
                        atk.SetFireCooldownScale(_stageFireCooldownScale);
                        if (_stageBulletSpeed > 5f)
                        {
                            atk.SetBulletSpeedOverride(_stageBulletSpeed);
                        }
                    }
                }
            }
            _activeWaveFormationAnchor = availableAnchor;
            _currentWaveTimer = 0f;
            _currentWaveMaxLifetime = pattern.maxLifetime > 1f ? pattern.maxLifetime : 14f;
            _isCurrentWaveTracking = true;

            availableAnchor.InitializeFormation(pattern, anchor, startPos, enemies, customParams, overrideAssemblyTime);
            return true;
        }

        /// <summary>
        /// Kiểm tra xem đợt quái gần nhất còn tàu nào đang hoạt động trên chiến trường không (O(1), 0 GC Alloc).
        /// </summary>
        public bool IsCurrentWaveActive()
        {
            if (_activeWaveFormationAnchor != null && _activeWaveFormationAnchor.gameObject.activeSelf)
            {
                return true;
            }
            for (int i = 0; i < _activeWaveEnemies.Count; i++)
            {
                if (_activeWaveEnemies[i] != null && _activeWaveEnemies[i].gameObject.activeSelf)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Đếm số lượng tàu còn sống trong đợt quái gần nhất (0 GC Alloc).
        /// </summary>
        public int GetActiveWaveEnemyCount()
        {
            int count = 0;
            for (int i = 0; i < _activeWaveEnemies.Count; i++)
            {
                if (_activeWaveEnemies[i] != null && _activeWaveEnemies[i].gameObject.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Ép buộc đợt quái hiện tại rút lui ngay lập tức để giải phóng sàn đấu cho đợt tiếp theo (0 GC Alloc).
        /// </summary>
        public void ForceRetreatCurrentWave()
        {
            if (_activeWaveFormationAnchor != null && _activeWaveFormationAnchor.gameObject.activeSelf)
            {
                _activeWaveFormationAnchor.ForceRetreat();
                _activeWaveFormationAnchor = null;
            }

            for (int i = 0; i < _activeWaveEnemies.Count; i++)
            {
                EnemyMovement enemy = _activeWaveEnemies[i];
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    var fsm = enemy.GetComponent<EnemyLifecycleFSM>();
                    if (fsm != null && fsm.enabled)
                    {
                        fsm.ForceExit();
                    }
                    else
                    {
                        enemy.ForceRetreat();
                    }
                }
            }

            _activeWaveEnemies.Clear();
            _isCurrentWaveTracking = false;
        }

        public void RecycleAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    _pool[i].Recycle();
                }
            }
            for (int i = 0; i < _formationPool.Count; i++)
            {
                if (_formationPool[i] != null && _formationPool[i].gameObject.activeSelf)
                {
                    _formationPool[i].Recycle();
                }
            }
        }

        /// <summary>
        /// Dịch chuyển toàn bộ tàu địch và đội hình đang hoạt động trong quá trình Tái định vị thế giới (Task 4.3).
        /// </summary>
        public void ShiftOrigin(Vector3 shift)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    _pool[i].transform.position -= shift;
                }
            }
            for (int i = 0; i < _formationPool.Count; i++)
            {
                if (_formationPool[i] != null && _formationPool[i].gameObject.activeSelf)
                {
                    _formationPool[i].ShiftOrigin(shift);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            RecycleAll();
            if (_poolRoot != null)
            {
                Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }
        }
    }
}
