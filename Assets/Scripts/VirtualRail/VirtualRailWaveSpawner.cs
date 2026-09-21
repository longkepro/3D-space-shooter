using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
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
            bool useStarSparrow = cfg != null ? cfg.useStarSparrowFleet : true;

            // Tự động tìm nạp các prefab Enemy theo cấu hình (StarSparrow Fleet vs Legacy)
            if (useStarSparrow)
            {
#if UNITY_EDITOR
                var ss1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VirtualRail_Enemy_StarSparrow1.prefab");
                if (ss1 == null) ss1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarSparrow/Prefabs/Examples/StarSparrow1.prefab");

                var ss4 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VirtualRail_Enemy_StarSparrow4.prefab");
                if (ss4 == null) ss4 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarSparrow/Prefabs/Examples/StarSparrow4.prefab");

                var ss6 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VirtualRail_Enemy_StarSparrow6.prefab");
                if (ss6 == null) ss6 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StarSparrow/Prefabs/Examples/StarSparrow6.prefab");

                var list = new List<GameObject>();
                if (ss1 != null) list.Add(ss1);
                if (ss4 != null) list.Add(ss4);
                if (ss6 != null) list.Add(ss6);
                if (list.Count > 0)
                {
                    enemyPrefabs = list.ToArray();
                }
#endif
            }
            else
            {
#if UNITY_EDITOR
                var e0 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (0).prefab");
                var e1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (1).prefab");
                var e2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (2).prefab");
                var e3 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (3) - Squad Leader.prefab");
                if (e3 == null) e3 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (3).prefab");
                var e4 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (4) - Shield Bearer.prefab");
                if (e4 == null) e4 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (4).prefab");

                var list = new List<GameObject>();
                if (e0 != null) list.Add(e0);
                if (e1 != null) list.Add(e1);
                if (e2 != null) list.Add(e2);
                if (e3 != null) list.Add(e3);
                if (e4 != null) list.Add(e4);
                enemyPrefabs = list.ToArray();
#endif
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
            _nextWaveTime = Time.time + 1.5f; // Đợi 1.5s sau khi vào game rồi bắt đầu đợt 1
        }

        public void StopSpawning()
        {
            _isSpawning = false;
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

            float effectiveInterval = cfg != null ? cfg.waveSpawnInterval : waveInterval;
            float effectiveLead = cfg != null ? cfg.waveSpawnDistance : spawnLeadDistance;

            // 1. Quét sinh đợt quái mới
            if (Time.time >= _nextWaveTime)
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
        }

        private void SpawnWave(float leadDist)
        {
            if (anchor == null) return;

            var cfg = anchor.config;
            bool useSpatial = (cfg != null) ? cfg.useSpatialVectors : useSpatialVectors;
            bool useFSM = (cfg != null) ? cfg.enable4PhaseLifecycle : enable4PhaseFSM;
            bool useFormations = (cfg != null) ? cfg.enableFormations : enableFormations;
            int interval = (cfg != null) ? cfg.formationIntervalWaves : formationWaveInterval;

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

            for (int i = 0; i < countInWave; i++)
            {
                EnemyMovement availableEnemy = GetAvailableStandardEnemy();
                if (availableEnemy == null) break;

                // Chọn vector tiếp theo theo chu kỳ phân bổ đa hướng 10 Vector
                SpatialVectorType vectorType = ACTIVE_SPATIAL_VECTORS[_vectorCycleIndex % ACTIVE_SPATIAL_VECTORS.Length];
                _vectorCycleIndex++;

                SpatialTrajectoryData trajectory = SpatialVectorFactory.GenerateTrajectory(vectorType, anchor, mainCam, leadDist);

                availableEnemy.ResetState();
                availableEnemy.transform.localScale = Vector3.one;
                availableEnemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : anchor.transform);

                if (useFSM)
                {
                    var fsm = availableEnemy.GetComponent<EnemyLifecycleFSM>();
                    if (fsm == null) fsm = availableEnemy.gameObject.AddComponent<EnemyLifecycleFSM>();
                    fsm.InitializeTrajectory(trajectory, anchor);
                }
                else
                {
                    availableEnemy.transform.position = trajectory.spawnPosition;
                    availableEnemy.transform.rotation = trajectory.spawnRotation;
                }

                availableEnemy.gameObject.SetActive(true);
            }
        }

        private bool TrySpawnFormationWave(float leadDist, VirtualRailConfig cfg)
        {
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

            float formDist = (cfg != null) ? cfg.formationSpawnDistance : 110f;
            Vector3 startPos = anchor.transform.position + anchor.ForwardTangent * formDist + anchor.transform.up * 4f;
            float assemblyTime = (cfg != null) ? cfg.formationAssemblyTime : 1.2f;

            if (cfg != null && cfg.enableModularEnemies)
            {
                // Nếu tàu chỉ huy chưa mang archetype SquadLeader thì gán chỉ số
                if (enemies[0].ArchetypeData == null || enemies[0].ArchetypeData.archetype != EnemyArchetype.SquadLeader)
                {
                    enemies[0].ApplyArchetype(EnemyData.CreateDefaultPreset(EnemyArchetype.SquadLeader));
                    enemies[0].transform.localScale = Vector3.one * 1.35f;
                }
                else
                {
                    enemies[0].transform.localScale = Vector3.one;
                }

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
            Camera mainCam = _mainCam != null ? _mainCam : Camera.main;
            int fragmentCount = Random.Range(3, 5);

            for (int i = 0; i < fragmentCount; i++)
            {
                EnemyMovement fragment = GetAvailableEnemy();
                if (fragment == null) break;

                SpatialTrajectoryData trajectory = SpatialVectorFactory.GenerateTrajectory(
                    SpatialVectorType.ClusterFracture, anchor, mainCam, 60f);
                trajectory.spawnPosition = explosionCenter + Random.insideUnitSphere * 2.5f;

                fragment.ResetState();
                fragment.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : anchor.transform);

                var fsm = fragment.GetComponent<EnemyLifecycleFSM>();
                if (fsm == null) fsm = fragment.gameObject.AddComponent<EnemyLifecycleFSM>();
                fsm.InitializeTrajectory(trajectory, anchor);

                fragment.gameObject.SetActive(true);
            }

            Debug.Log($"<color=yellow><b>[CLUSTER FRACTURE]</b></color> Nổ văng {fragmentCount} mảnh vỡ quán tính từ tâm {explosionCenter}!");
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
