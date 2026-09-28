using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Màn Chiếu Chân Trời Trực Diện Độ Nét Cao 1:1 (Front-Facing Horizon Backdrop).
    /// Gắn dưới [Camera Rig].
    /// Giải quyết triệt để 2 vấn đề lớn của Skybox 360 độ:
    /// 1. Khóa chuẩn 100% hình ảnh Vòm Trái Đất & Mặt Trăng chính diện trước mũi tàu theo đúng hướng bay (+Z).
    /// 2. Độ nét sắc bén Pixel-Perfect 1:1 (không bị loãng độ phân giải và méo góc như phép chiếu cầu Panoramic 360).
    /// 3. Tự động khớp theo Camera Frustum (FOV và Aspect Ratio).
    /// 4. 0 GC Allocation trong toàn bộ vòng lặp LateUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VirtualRailHorizonBackdrop : MonoBehaviour
    {
        [Header("References")]
        public Camera targetCamera;
        public Material backdropMaterial;

        [Header("Projection Settings")]
        [Tooltip("Khoảng cách Z đặt màn chiếu phía trước camera (m - trước Far Clip Plane 1000m)")]
        public float distanceZ = 850f;
        [Tooltip("Hệ số mở rộng biên (Overscan) để tránh hở viền màn hình khi camera di chuyển")]
        public float overscan = 1.05f;
        [Tooltip("Bật thị sai góc nhìn cực nhẹ theo vị trí tàu")]
        public bool enableSubtleParallax = true;
        [Tooltip("Hệ số thị sai góc nhìn (càng nhỏ càng giữ vững đường chân trời)")]
        public float parallaxFactor = 0.012f;

        private MeshFilter _mf;
        private MeshRenderer _mr;
        private Transform _camTransform;
        private float _lastFOV = -1f;
        private float _lastAspect = -1f;
        private bool _isInitialized = false;

        private void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
            Initialize();
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponentInParent<Camera>();
                if (targetCamera == null) targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                _camTransform = targetCamera.transform;
            }

            UpdateFrustumFit(true);
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();

            // 1. Tải vật liệu Backdrop nếu chưa gán
            if (backdropMaterial == null)
            {
#if UNITY_EDITOR
                backdropMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Backdrop_Image1_CosmicEarth.mat");
#endif
            }

            if (_mr != null && backdropMaterial != null)
            {
                _mr.sharedMaterial = backdropMaterial;
                _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _mr.receiveShadows = false;
                _mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                _mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            // 2. Tạo Quad Mesh chuẩn 2 tam giác (1x1 đơn vị)
            if (_mf != null && _mf.sharedMesh == null)
            {
                Mesh mesh = new Mesh();
                mesh.name = "HorizonBackdropQuad";
                mesh.vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3( 0.5f, -0.5f, 0f),
                    new Vector3(-0.5f,  0.5f, 0f),
                    new Vector3( 0.5f,  0.5f, 0f)
                };
                mesh.uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                };
                mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                _mf.sharedMesh = mesh;
            }

            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
                _camTransform = targetCamera.transform;
            }

            UpdateFrustumFit(false);
        }

        private void UpdateFrustumFit(bool force)
        {
            if (targetCamera == null) return;

            float currentFOV = targetCamera.fieldOfView;
            float currentAspect = targetCamera.aspect;

            // Chỉ cập nhật lại Scale khi FOV hoặc Aspect Ratio thay đổi để triệt tiêu overhead
            if (force || Mathf.Abs(currentFOV - _lastFOV) > 0.01f || Mathf.Abs(currentAspect - _lastAspect) > 0.005f)
            {
                _lastFOV = currentFOV;
                _lastAspect = currentAspect;

                float fovRad = currentFOV * Mathf.Deg2Rad;
                float frustumHeight = 2.0f * distanceZ * Mathf.Tan(fovRad * 0.5f);
                float frustumWidth = frustumHeight * currentAspect;

                transform.localScale = new Vector3(frustumWidth * overscan, frustumHeight * overscan, 1.0f);
            }

            // Neo màn chiếu thẳng tắp trước mặt Camera
            Vector3 baseLocalPos = new Vector3(0f, 0f, distanceZ);
            transform.localPosition = baseLocalPos;
            transform.localRotation = Quaternion.identity;
        }

        public void SetBackdropActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
