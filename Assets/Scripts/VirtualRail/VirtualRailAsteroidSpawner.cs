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
        [SerializeField] private float spawnZInterval = 18f;
        [Tooltip("Cự ly thu hồi thiên thạch phía sau tàu (m)")]
        [SerializeField] private float despawnBehindDistance = 45f;

        private readonly List<Asteroid> _pool = new List<Asteroid>();
        private float _lastSpawnZ = 0f;
        private bool _isSpawning = false;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializePool();
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

            if (asteroidPrefab != null)
            {
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject obj = Instantiate(asteroidPrefab, transform);
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

            // 1. Sinh thêm thiên thạch đón đầu khi tàu tiến lên
            while (playerZ + effSpawnDist > _lastSpawnZ)
            {
                _lastSpawnZ += effZInterval;
                SpawnSingleAsteroid(_lastSpawnZ);
            }

            // 2. Thu hồi thiên thạch trôi về sau lưng tàu
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

        private void SpawnSingleAsteroid(float targetZ)
        {
            Asteroid ast = GetAvailableAsteroid();
            if (ast == null) return;

            // Vị trí rải ngẫu nhiên trong hành lang bay
            float x = Random.Range(-20f, 20f);
            float y = Random.Range(1f, 16f);

            ast.transform.position = new Vector3(x, y, targetZ);
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
    }
}
