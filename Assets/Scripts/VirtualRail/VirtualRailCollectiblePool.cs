using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Object Pool quản lý Vòng Năng Lượng và Vật phẩm (0 GC Allocation)
    /// </summary>
    public class VirtualRailCollectiblePool : MonoBehaviour
    {
        public static VirtualRailCollectiblePool Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 15;
        [SerializeField] private GameObject ringPrefab;

        private readonly List<CollectibleRing> _pool = new List<CollectibleRing>();
        private Transform _poolRoot;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void InitializePool()
        {
            if (_isInitialized) return;

            if (_poolRoot == null)
            {
                GameObject root = GameObject.Find("[VirtualRail_CollectiblePool]");
                if (root != null)
                {
                    _poolRoot = root.transform;
                }
                else
                {
                    GameObject newRoot = new GameObject("[VirtualRail_CollectiblePool]");
                    _poolRoot = newRoot.transform;
                    _poolRoot.position = Vector3.zero;
                    _poolRoot.rotation = Quaternion.identity;
                }
            }

            // Tự động tìm nạp model từ Gems Ultimate Pack nếu chưa có prefab
            if (ringPrefab == null)
            {
#if UNITY_EDITOR
                ringPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset Store/AurynSky/Gems Ultimate Pack/Prefabs/SphereGemLarge.prefab");
                if (ringPrefab == null)
                {
                    ringPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pickup (1).prefab");
                }
#endif
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj;
                if (ringPrefab != null)
                {
                    obj = Instantiate(ringPrefab, _poolRoot);
                }
                else
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.transform.SetParent(_poolRoot);
                }

                obj.name = "CollectibleRing_Pooled_" + i;
                obj.tag = "Pickup";
                obj.SetActive(false);

                // Loại bỏ script cũ nếu có để tránh xung đột
                var oldPickup = obj.GetComponent<Pickup>();
                if (oldPickup != null) Destroy(oldPickup);

                var ring = obj.GetComponent<CollectibleRing>();
                if (ring == null) ring = obj.AddComponent<CollectibleRing>();

                ring.OnRecycle = OnRingRecycled;
                _pool.Add(ring);
            }

            _isInitialized = true;
        }

        public CollectibleRing SpawnRing(RingType type, Vector3 position)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    _pool[i].Initialize(type, position);
                    return _pool[i];
                }
            }

            return null;
        }

        private void OnRingRecycled(CollectibleRing ring)
        {
            // Được gọi khi ring thu hồi
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

        /// <summary>
        /// Dịch chuyển toàn bộ vật phẩm/vòng năng lượng đang xuất hiện trong quá trình Tái định vị thế giới (Task 4.3).
        /// </summary>
        public void ShiftOrigin(Vector3 shift)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    _pool[i].ShiftOrigin(shift);
                }
            }
        }
    }
}
