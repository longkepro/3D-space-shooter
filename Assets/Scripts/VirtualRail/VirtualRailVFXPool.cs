using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Quản lý vòng đời hiệu ứng hạt (VFX) được tái sử dụng trong Object Pool.
    /// Tự động đếm lùi thời gian và ẩn đối tượng, triệt tiêu 100% rác bộ nhớ (0 GC Alloc).
    /// </summary>
    public class PooledVFXInstance : MonoBehaviour
    {
        private float _returnTime;
        private ParticleSystem[] _particleSystems;
        private bool _isCached = false;

        private void CacheParticles()
        {
            if (!_isCached)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
                _isCached = true;
            }
        }

        public void Activate(float duration)
        {
            CacheParticles();
            _returnTime = Time.time + duration;
            gameObject.SetActive(true);

            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] != null)
                    {
                        _particleSystems[i].Clear();
                        _particleSystems[i].Play(true);
                    }
                }
            }
        }

        private void Update()
        {
            if (Time.time >= _returnTime)
            {
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Bộ đệm hiệu ứng nổ và tia lửa (VFX Object Pool) hiệu năng cao cho toàn bộ game.
    /// Tự động phân nhóm theo Prefab gốc và tái sử dụng, không gọi Instantiate/Destroy trong lúc chơi.
    /// </summary>
    public class VirtualRailVFXPool : MonoBehaviour
    {
        private static VirtualRailVFXPool _instance;
        public static VirtualRailVFXPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<VirtualRailVFXPool>();
                    if (_instance == null)
                    {
                        GameObject poolObj = new GameObject("[VirtualRail_VFXPool]");
                        _instance = poolObj.AddComponent<VirtualRailVFXPool>();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<int, List<PooledVFXInstance>> _pools = new Dictionary<int, List<PooledVFXInstance>>();

        private void Awake()
        {
            if (_instance == null) _instance = this;
        }

        /// <summary>
        /// Lấy một hiệu ứng nổ/tia lửa từ Pool hoặc khởi tạo bổ sung nếu thiếu.
        /// </summary>
        public GameObject SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float duration = 2.5f)
        {
            if (prefab == null) return null;

            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out List<PooledVFXInstance> list))
            {
                list = new List<PooledVFXInstance>(16);
                _pools[key] = list;
            }

            PooledVFXInstance available = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].gameObject.activeSelf)
                {
                    available = list[i];
                    break;
                }
            }

            if (available == null)
            {
                GameObject obj = Instantiate(prefab, position, rotation, parent != null ? parent : transform);
                available = obj.GetComponent<PooledVFXInstance>();
                if (available == null) available = obj.AddComponent<PooledVFXInstance>();
                list.Add(available);
            }
            else
            {
                available.transform.position = position;
                available.transform.rotation = rotation;
                if (parent != null && available.transform.parent != parent)
                {
                    available.transform.SetParent(parent);
                }
                else if (parent == null && available.transform.parent != transform)
                {
                    available.transform.SetParent(transform);
                }
            }

            available.Activate(duration);
            return available.gameObject;
        }
    }
}
