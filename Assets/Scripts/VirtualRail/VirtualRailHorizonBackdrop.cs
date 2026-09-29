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
    [ExecuteAlways]
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

        private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");
        private static readonly int ExposureProp = Shader.PropertyToID("_Exposure");
        private static readonly int TintProp = Shader.PropertyToID("_Tint");
        private static Material _sharedBaseMaterial;

        private MeshFilter _mf;
        private MeshRenderer _mr;
        private Transform _camTransform;
        private MaterialPropertyBlock _propBlock;
        private float _lastFOV = -1f;
        private float _lastAspect = -1f;
        private bool _isInitialized = false;

        public static Material GetOrCreateSharedBaseMaterial()
        {
            if (_sharedBaseMaterial == null)
            {
                Shader shader = Shader.Find("VirtualRail/HorizonBackdrop");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                _sharedBaseMaterial = new Material(shader)
                {
                    name = "VirtualRail_SharedBackdropBase",
                    hideFlags = HideFlags.DontSave
                };
            }
            return _sharedBaseMaterial;
        }

        private void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
            Initialize();
        }

        private void Start()
        {
            Initialize();
            FindTargetCamera();
            UpdateFrustumFit(true);
        }

        /// <summary>
        /// Áp dụng cấu hình phông nền màn chơi trực tiếp từ StageEnvironmentSettings.
        /// Tự động chuyển đổi giữa Texture2D trực tiếp (0 GC) và Material tùy biến.
        /// </summary>
        public void ApplyStageSettings(StageEnvironmentSettings env)
        {
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            if (env.backdropMaterial != null)
            {
                backdropMaterial = env.backdropMaterial;
                if (_mr != null)
                {
                    _mr.enabled = true;
                    _mr.sharedMaterial = env.backdropMaterial;
                    _mr.SetPropertyBlock(null);
                }
            }
            else if (env.backdropTexture != null)
            {
                Material baseMat = GetOrCreateSharedBaseMaterial();
                backdropMaterial = baseMat;
                if (_mr != null)
                {
                    _mr.enabled = true;
                    _mr.sharedMaterial = baseMat;

                    _propBlock.Clear();
                    _propBlock.SetTexture(MainTexProp, env.backdropTexture);
                    _propBlock.SetFloat(ExposureProp, env.backdropExposure > 0.01f ? env.backdropExposure : 1.0f);
                    Color tint = env.backdropTint.a > 0.01f ? env.backdropTint : Color.white;
                    _propBlock.SetColor(TintProp, tint);
                    _mr.SetPropertyBlock(_propBlock);
                }
            }
            else
            {
                backdropMaterial = null;
                if (_mr != null)
                {
                    _mr.enabled = false;
                }
            }
        }

        public void SetBackdropMaterial(Material mat)
        {
            backdropMaterial = mat;
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
            if (_mr != null)
            {
                if (mat != null)
                {
                    _mr.enabled = true;
                    _mr.sharedMaterial = mat;
                    _mr.SetPropertyBlock(null);
                }
                else
                {
                    _mr.enabled = false;
                }
            }
        }

        public void Initialize()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();

            if (_mr != null)
            {
                if (backdropMaterial != null)
                {
                    _mr.enabled = true;
                    _mr.sharedMaterial = backdropMaterial;
                    _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _mr.receiveShadows = false;
                    _mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    _mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
                else
                {
                    _mr.enabled = false;
                }
            }

            // 2. Gán Quad Mesh chuẩn Primitive của Unity Engine
            if (_mf != null && _mf.sharedMesh == null)
            {
                GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
                if (Application.isPlaying) Destroy(tempQuad);
                else DestroyImmediate(tempQuad);
            }

            // Khởi tạo kích thước ban đầu ngay khi Bake Prefab
            UpdateFrustumFit(true);
            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (_mr != null && !_mr.enabled) return;

            if (targetCamera == null)
            {
                FindTargetCamera();
            }

            UpdateFrustumFit(false);
        }

        private void FindTargetCamera()
        {
            if (targetCamera != null)
            {
                _camTransform = targetCamera.transform;
                return;
            }

            targetCamera = GetComponentInParent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

#if UNITY_EDITOR
            if (targetCamera == null && !Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
            {
                targetCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
            }
#endif

            if (targetCamera != null)
            {
                _camTransform = targetCamera.transform;
            }
        }

        private void UpdateFrustumFit(bool force)
        {
            float currentFOV = (targetCamera != null) ? targetCamera.fieldOfView : 40f;
            float currentAspect = (targetCamera != null && targetCamera.aspect > 0.01f) ? targetCamera.aspect : (16f / 9f);

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
            if (_camTransform != null && transform.parent != _camTransform && (transform.parent == null || transform.parent != _camTransform.parent))
            {
                transform.position = _camTransform.position + _camTransform.forward * distanceZ;
                transform.rotation = _camTransform.rotation;
            }
            else
            {
                transform.localPosition = new Vector3(0f, 0f, distanceZ);
                transform.localRotation = Quaternion.identity;
            }
        }

        public void SetBackdropActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
