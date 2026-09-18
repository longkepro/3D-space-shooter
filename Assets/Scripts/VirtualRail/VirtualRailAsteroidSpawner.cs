using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bộ điều phối thiên thạch đón đầu theo trục Z của đường ray (Streaming Asteroid Spawner).
    /// Quản lý Object Pool thiên thạch tái sử dụng 100% (0 GC Alloc),
    /// liên tục sinh thiên thạch phía trước mũi tàu trong suốt hành trình và tự thu hồi khi bay qua.
    /// </summary>
    public class VirtualRailAsteroidSpawner : MonoBehaviour
    {
        public static VirtualRailAsteroidSpawner Instance { get; private set; }

        [Header("References")]
        public VirtualRailAnchor anchor;
        [SerializeField] private GameObject asteroidPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 32;

        [Header("Streaming Spawning Settings")]
        [Tooltip("Cự ly sinh thiên thạch phía trước tàu (m)")]
        [SerializeField] private float spawnDistance = 250f;
        [Tooltip("Khoảng cách bước Z giữa các cụm thiên thạch (m)")]
        [SerializeField] private float spawnZInterval = 50f;
        [Tooltip("Cự ly thu hồi thiên thạch phía sau tàu (m)")]
        [SerializeField] private float despawnBehindDistance = 45f;

        private readonly List<Asteroid> _pool = new List<Asteroid>();
        private readonly ICorridorPathStrategy _pathStrategy = new PerlinSpinePathStrategy();
        private Transform _poolRoot;
        private float _lastSpawnZ = 0f;
        private bool _isSpawning = false;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializePool();
        }

        private void Start()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();
            if (!_isSpawning && anchor != null)
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

            if (asteroidPrefab == null)
            {
#if UNITY_EDITOR
                asteroidPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Asteroid (1).prefab");
#endif
            }

            if (_poolRoot == null)
            {
                GameObject existingRoot = GameObject.Find("[VirtualRail_AsteroidPool]");
                if (existingRoot != null)
                {
                    _poolRoot = existingRoot.transform;
                }
                else
                {
                    GameObject rootObj = new GameObject("[VirtualRail_AsteroidPool]");
                    _poolRoot = rootObj.transform;
                    _poolRoot.position = Vector3.zero;
                    _poolRoot.rotation = Quaternion.identity;
                }
            }

            if (asteroidPrefab != null)
            {
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject obj = Instantiate(asteroidPrefab, _poolRoot);
                    obj.name = "Asteroid_Pooled_" + i;
                    obj.SetActive(false);

                    var ast = obj.GetComponent<Asteroid>();
                    if (ast != null)
                    {
                        _pool.Add(ast);
                    }
                }
            }

            _isInitialized = true;
        }

        public void StartSpawning()
        {
            if (_isSpawning) return;
            _isSpawning = true;
            if (anchor != null)
            {
                _lastSpawnZ = anchor.transform.position.z;
                // Tiền tạo (pre-warm) một đoạn thiên thạch phía trước khi bắt đầu chơi
                PrewarmCorridor(anchor.transform.position.z);
            }
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            RecycleAll();
        }

        private void PrewarmCorridor(float currentZ)
        {
            var cfg = anchor != null ? anchor.config : null;
            float effSpawnDist = cfg != null ? cfg.asteroidSpawnDistance : spawnDistance;
            float effZInterval = cfg != null ? cfg.asteroidZInterval : spawnZInterval;

            for (float z = currentZ + 80f; z < currentZ + effSpawnDist; z += effZInterval)
            {
                SpawnSingleAsteroid(z);
            }
            _lastSpawnZ = currentZ + effSpawnDist;
        }

        private void Update()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance;
            if (anchor == null) return;

            var cfg = anchor.config;
            bool enabledByConfig = cfg == null || cfg.useRailAsteroidSpawner;
            if (!enabledByConfig) return;

            // Tự động kích hoạt khi tàu bắt đầu di chuyển nếu chưa nhận được sự kiện UI
            if (!_isSpawning && anchor.CurrentSpeed > 0.5f)
            {
                StartSpawning();
            }

            if (!_isSpawning) return;

            float effSpawnDist = cfg != null ? cfg.asteroidSpawnDistance : spawnDistance;
            float effZInterval = cfg != null ? cfg.asteroidZInterval : spawnZInterval;

            float playerZ = anchor.transform.position.z;

            // 1. Thu hồi thiên thạch trôi về sau lưng tàu TRƯỚC để hoàn trả vào pool
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

            // 2. Sinh thêm thiên thạch đón đầu khi tàu tiến lên
            while (playerZ + effSpawnDist > _lastSpawnZ)
            {
                _lastSpawnZ += effZInterval;
                SpawnSingleAsteroid(_lastSpawnZ);
            }
        }

        private void SpawnSingleAsteroid(float targetZ)
        {
            Asteroid ast = GetAvailableAsteroid();
            if (ast == null) return;

            var cfg = anchor != null ? anchor.config : null;
            Vector3 spawnPos;

            if (cfg != null && cfg.enableSafeCorridorPath)
            {
                // Áp dụng thuật toán luồng an toàn Perlin Safe Spine (0 GC Alloc)
                spawnPos = _pathStrategy.CalculateSpawnPosition(targetZ, 40f, 15f, cfg);
            }
            else
            {
                // Hoàn nguyên về ngẫu nhiên thuần túy cũ (Fallback/Undo)
                float x = Random.Range(-20f, 20f);
                float y = Random.Range(1f, 16f);
                spawnPos = new Vector3(x, y, targetZ);
            }

            ast.transform.position = spawnPos;
            ast.transform.rotation = Random.rotation;
            ast.ResetScale();
            ast.gameObject.SetActive(true);
        }

        private Asteroid GetAvailableAsteroid()
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

        private void OnDrawGizmosSelected()
        {
            var cfg = anchor != null ? anchor.config : null;
            if (cfg == null || !cfg.enableSafeCorridorPath) return;

            float startZ = anchor != null ? anchor.transform.position.z : transform.position.z;
            float endZ = startZ + (cfg != null ? cfg.asteroidSpawnDistance : 250f);
            float clearR = cfg.safeCorridorRadius;

            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.65f);
            Vector3 prevCenter = Vector3.zero;
            bool hasPrev = false;

            for (float z = startZ; z <= endZ; z += 12f)
            {
                Vector2 center2D = _pathStrategy.SampleSafeCenter(z, cfg);
                Vector3 center3D = new Vector3(center2D.x, center2D.y, z);

                if (hasPrev)
                {
                    Gizmos.DrawLine(prevCenter, center3D);
                }

                Gizmos.DrawWireSphere(center3D, clearR);
                prevCenter = center3D;
                hasPrev = true;
            }
        }
    }
}
