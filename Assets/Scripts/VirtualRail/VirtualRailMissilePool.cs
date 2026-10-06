using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bộ đệm Tên lửa / Đạn tụ lực Homing Missile (Object Pool) hiệu năng cao, 0 GC Alloc.
    /// Triệt tiêu 100% việc gọi Instantiate / Destroy trong vòng lặp chiến đấu.
    /// </summary>
    public class VirtualRailMissilePool : MonoBehaviour
    {
        private static VirtualRailMissilePool _instance;
        public static VirtualRailMissilePool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<VirtualRailMissilePool>();
                    if (_instance == null)
                    {
                        GameObject poolObj = new GameObject("[VirtualRail_MissilePool]");
                        _instance = poolObj.AddComponent<VirtualRailMissilePool>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private int initialPoolSize = 6;
        private readonly Dictionary<GameObject, List<VirtualRailHomingMissile>> _pools = new Dictionary<GameObject, List<VirtualRailHomingMissile>>();

        private void Awake()
        {
            if (_instance == null) _instance = this;
        }

        /// <summary>
        /// Lấy hoặc sinh mới đạn tên lửa từ Pool (0 GC Alloc khi tái sử dụng).
        /// Hỗ trợ đồng thời nhiều Prefab khác nhau (Đạn tụ lực Charge Shot & Tên lửa Bom thông minh).
        /// </summary>
        public VirtualRailHomingMissile SpawnMissile(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out var poolList))
            {
                poolList = new List<VirtualRailHomingMissile>(initialPoolSize);
                _pools[prefab] = poolList;
                for (int p = 0; p < initialPoolSize; p++)
                {
                    CreatePooledInstance(prefab, poolList);
                }
            }

            // Tìm đạn đang nhàn rỗi trong pool
            for (int i = 0; i < poolList.Count; i++)
            {
                var m = poolList[i];
                if (m != null && !m.gameObject.activeSelf)
                {
                    m.transform.position = position;
                    m.transform.rotation = rotation;
                    m.gameObject.SetActive(true);
                    m.OnSpawnFromPool();
                    return m;
                }
            }

            // Nếu pool chưa đủ hoặc đã dùng hết, khởi tạo thêm
            var newMissile = CreatePooledInstance(prefab, poolList);
            newMissile.transform.position = position;
            newMissile.transform.rotation = rotation;
            newMissile.gameObject.SetActive(true);
            newMissile.OnSpawnFromPool();
            return newMissile;
        }

        private VirtualRailHomingMissile CreatePooledInstance(GameObject prefab, List<VirtualRailHomingMissile> poolList)
        {
            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            obj.name = prefab.name + "_Pooled_" + poolList.Count;
            obj.SetActive(false);
            var missile = obj.GetComponent<VirtualRailHomingMissile>();
            if (missile == null) missile = obj.AddComponent<VirtualRailHomingMissile>();
            poolList.Add(missile);
            return missile;
        }

        public void ClearPool()
        {
            foreach (var kvp in _pools)
            {
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null) Destroy(list[i].gameObject);
                }
                list.Clear();
            }
            _pools.Clear();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
