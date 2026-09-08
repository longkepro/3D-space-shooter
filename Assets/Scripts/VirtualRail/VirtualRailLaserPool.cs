using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bộ đệm đạn Laser (Object Pool) hiệu năng cao, triệt tiêu 100% rác bộ nhớ (0 GC Alloc).
    /// Tự động khởi tạo và tái sử dụng các đoạn đạn laser trong suốt màn chơi.
    /// </summary>
    public class VirtualRailLaserPool : MonoBehaviour
    {
        private static VirtualRailLaserPool _instance;
        public static VirtualRailLaserPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<VirtualRailLaserPool>();
                    if (_instance == null)
                    {
                        GameObject poolObj = new GameObject("[VirtualRail_LaserPool]");
                        _instance = poolObj.AddComponent<VirtualRailLaserPool>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private int initialPoolSize = 36;
        private readonly List<VirtualRailLaserBolt> _pool = new List<VirtualRailLaserBolt>();
        private Material _defaultMaterial;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            InitializePool();
        }

        public void InitializePool(Material boltMaterial = null)
        {
            if (_isInitialized && boltMaterial == null) return;

            if (boltMaterial != null)
            {
                _defaultMaterial = boltMaterial;
            }
            else if (_defaultMaterial == null)
            {
#if UNITY_EDITOR
                _defaultMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
#endif
                if (_defaultMaterial == null)
                {
                    // 1. Thử lấy từ LineRenderer của súng Laser có sẵn trên tàu
                    var existingLaser = FindAnyObjectByType<Laser>();
                    if (existingLaser != null)
                    {
                        var lr = existingLaser.GetComponent<LineRenderer>();
                        if (lr != null && lr.sharedMaterial != null)
                        {
                            _defaultMaterial = lr.sharedMaterial;
                        }
                    }
                }

                // 2. Fallback tìm shader Particles Additive
                if (_defaultMaterial == null)
                {
                    Shader shader = Shader.Find("Mobile/Particles/Additive");
                    if (shader == null) shader = Shader.Find("Particles/Additive");
                    if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    if (shader != null) _defaultMaterial = new Material(shader);
                }
            }

            // Sinh sẵn các viên đạn trong pool
            while (_pool.Count < initialPoolSize)
            {
                CreateNewBolt();
            }

            _isInitialized = true;
        }

        private VirtualRailLaserBolt CreateNewBolt()
        {
            GameObject boltObj = new GameObject("LaserBolt_" + _pool.Count);
            boltObj.transform.SetParent(transform, false);
            boltObj.SetActive(false);

            VirtualRailLaserBolt bolt = boltObj.AddComponent<VirtualRailLaserBolt>();
            _pool.Add(bolt);
            return bolt;
        }

        public VirtualRailLaserBolt FireBolt(
            Vector3 startPos,
            Vector3 direction,
            float speed,
            float length,
            float lifetime,
            Color color,
            float width,
            Material customMat,
            Transform source,
            LayerMask hitMask)
        {
            if (!_isInitialized) InitializePool(customMat);

            VirtualRailLaserBolt selectedBolt = null;

            // 1. Quét tìm viên đạn đang rảnh (Inactive)
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    selectedBolt = _pool[i];
                    break;
                }
            }

            // 2. Nếu cường độ bắn cực cao và hết đạn rảnh, sinh thêm hoặc tái sử dụng
            if (selectedBolt == null)
            {
                selectedBolt = CreateNewBolt();
            }

            // 3. Cấu hình vật liệu và màu sắc
            Material matToUse = customMat != null ? customMat : _defaultMaterial;
            selectedBolt.SetupVisual(matToUse, color, width);

            // 4. Kích hoạt phóng đạn
            selectedBolt.Spawn(startPos, direction, speed, length, lifetime, source, hitMask);

            return selectedBolt;
        }
    }
}
