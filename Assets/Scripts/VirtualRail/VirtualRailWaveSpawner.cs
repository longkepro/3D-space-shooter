using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bộ điều phối đợt tấn công đường ray (Rail Wave Spawner - Hướng Star Fox 64).
    /// Sinh quái vật đón đầu phía trước người chơi theo tiến trình trục Z,
    /// quản lý Object Pool tái sử dụng 100% (0 GC Alloc), và tự động thu hồi khi trôi về sau lưng.
    /// </summary>
    public class VirtualRailWaveSpawner : MonoBehaviour
    {
        public static VirtualRailWaveSpawner Instance { get; private set; }

        [Header("Configuration")]
        public VirtualRailAnchor anchor;
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private int poolSizePerType = 5;

        [Header("Wave Spawning Settings")]
        [Tooltip("Cự ly sinh quái vật phía trước người chơi theo trục Z (m)")]
        [SerializeField] private float spawnLeadDistance = 220f;
        [Tooltip("Chu kỳ sinh đợt tấn công mới (giây)")]
        [SerializeField] private float waveInterval = 3.5f;
        [Tooltip("Cự ly thu hồi quái vật khi người chơi bay vượt qua phía sau (m)")]
        [SerializeField] private float despawnBehindDistance = 50f;

        private readonly List<EnemyMovement> _pool = new List<EnemyMovement>();
        private float _nextWaveTime = 0f;
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

            // Tự động tìm nạp các prefab Enemy nếu chưa gán
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
#if UNITY_EDITOR
                var e0 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (0).prefab");
                var e1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (1).prefab");
                var e2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy (2).prefab");
                var list = new List<GameObject>();
                if (e0 != null) list.Add(e0);
                if (e1 != null) list.Add(e1);
                if (e2 != null) list.Add(e2);
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
                        GameObject enemyObj = Instantiate(enemyPrefabs[p], transform);
                        enemyObj.name = enemyPrefabs[p].name + "_Pooled_" + i;
                        enemyObj.SetActive(false);

                        var movement = enemyObj.GetComponent<EnemyMovement>();
                        if (movement != null)
                        {
                            _pool.Add(movement);
                        }
                    }
                }
            }

            _isInitialized = true;
        }

        public void StartSpawning()
        {
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

            Vector3 playerPos = anchor.transform.position;
            float targetZ = playerPos.z + leadDist;

            // Mỗi đợt sinh ngẫu nhiên 1 đến 3 quái vật dàn trận
            int countInWave = Random.Range(1, 4);
            float baseOffsetX = Random.Range(-14f, 14f);
            float baseOffsetY = Random.Range(3f, 13f);

            for (int i = 0; i < countInWave; i++)
            {
                EnemyMovement availableEnemy = GetAvailableEnemy();
                if (availableEnemy == null) break;

                // Tọa độ dàn đội hình đón đầu tàu người chơi
                Vector3 spawnPos = new Vector3(
                    baseOffsetX + (i - 1) * 6f,
                    baseOffsetY + (i % 2 == 0 ? 2f : -2f),
                    targetZ + (i * 15f)
                );

                availableEnemy.transform.position = spawnPos;
                // Quay mặt về hướng tàu người chơi
                Vector3 dirToPlayer = (playerPos - spawnPos).normalized;
                availableEnemy.transform.rotation = Quaternion.LookRotation(dirToPlayer);

                availableEnemy.ResetState();
                availableEnemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : anchor.transform);
                availableEnemy.gameObject.SetActive(true);
            }
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
        }
    }
}
