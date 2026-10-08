using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Camera Dynamic Framing.
    /// Nằm trong phân cấp của Rail Anchor.
    /// Giữ góc nhìn ổn định, cố định đường chân trời (Fixed Horizon) giống phong cách Star Fox gốc,
    /// tránh việc camera bị lắc lư hoặc xoay liếc theo tàu gây chóng mặt.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailCameraRig : MonoBehaviour
    {
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public VirtualRailShip ship;
        public Camera targetCamera;

        public static VirtualRailCameraRig Instance { get; private set; }

        [Header("Camera Control Mode")]
        [Tooltip("Bật chế độ dùng Camera riêng biệt tự do: Script sẽ KHÔNG reparent, KHÔNG ghi đè tọa độ/góc xoay/FOV của Camera")]
        public bool useExternalCamera = false;

        [Header("Camera Impulse Shake (0 GC Alloc Traumatic Stress Model)")]
        [SerializeField] private float maxShakeTranslationX = 0.45f;
        [SerializeField] private float maxShakeTranslationY = 0.35f;
        [SerializeField] private float maxShakeRotationZ = 2.5f;
        [SerializeField] private float traumaDecayRate = 1.4f;
        [SerializeField] private float shakeFrequency = 24f;

        private float _trauma = 0f;
        private float _shakeTime = 0f;

        public static void TriggerShake(float amount)
        {
            if (Instance != null)
            {
                Instance.AddTrauma(amount);
            }
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    // Tự phục hồi: Tìm bất kỳ Camera nào kể cả khi đang bị Inactive trong Scene
                    var cam = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
                    if (cam != null)
                    {
                        cam.gameObject.SetActive(true);
                        cam.tag = "MainCamera";
                        targetCamera = cam;
                    }
                    else
                    {
                        // Fallback an toàn tuyệt đối: Tự tạo mới Camera nếu Scene bị mất sạch
                        GameObject camObj = new GameObject("Main Camera");
                        camObj.tag = "MainCamera";
                        targetCamera = camObj.AddComponent<Camera>();
                        camObj.AddComponent<AudioListener>();
                    }
                }
            }

            // Tiếp quản Camera nếu không bật chế độ Camera riêng biệt tự do
            if (targetCamera != null)
            {
                var oldFollow = targetCamera.GetComponent<FollowCam>();
                if (oldFollow != null) oldFollow.enabled = false;

                if (!useExternalCamera)
                {
                    targetCamera.transform.SetParent(transform, false);
                    targetCamera.transform.localPosition = Vector3.zero;
                    targetCamera.transform.localRotation = Quaternion.identity;

                    if (anchor != null && anchor.config != null)
                    {
                        targetCamera.fieldOfView = anchor.config.cameraFOV;
                    }
                }
                else
                {
                    // Tự động gắn kết Camera riêng biệt vào tàu nhưng GIỮ NGUYÊN 100% góc nhìn World
                    // Giúp Camera tự động bay bám theo con tàu với đúng cự ly và góc độ người dùng đã đặt trong Scene!
                    targetCamera.transform.SetParent(transform, true);
                }
            }

            // Khởi tạo Bụi Sao Không Gian (Space Dust Motion Parallax) nếu chưa có
            if (anchor != null && anchor.config != null && anchor.config.enableSpaceDust)
            {
                var existingDust = GetComponentInChildren<VirtualRailSpaceDust>();
                if (existingDust == null)
                {
                    GameObject dustObj = new GameObject("[Space Dust]");
                    dustObj.transform.SetParent(transform, false);
                    dustObj.transform.localPosition = Vector3.zero;
                    dustObj.transform.localRotation = Quaternion.identity;
                    var dust = dustObj.AddComponent<VirtualRailSpaceDust>();
                    dust.anchor = anchor;
                    dust.config = anchor.config;
                }
            }

            // Khởi tạo Màn Chiếu Chân Trời Độ Nét Cao 1:1 (Front-Facing Horizon Backdrop)
            if (anchor != null && anchor.config != null && anchor.config.enableHorizonBackdrop)
            {
                var existingBackdrop = GetComponentInChildren<VirtualRailHorizonBackdrop>();
                if (existingBackdrop == null)
                {
                    GameObject backdropObj = new GameObject("[Horizon Backdrop]");
                    backdropObj.transform.SetParent(transform, false);
                    backdropObj.transform.localPosition = new Vector3(0f, 0f, anchor.config.backdropDistanceZ);
                    backdropObj.transform.localRotation = Quaternion.identity;
                    var backdrop = backdropObj.AddComponent<VirtualRailHorizonBackdrop>();
                    backdrop.targetCamera = targetCamera;
                    backdrop.distanceZ = anchor.config.backdropDistanceZ;
                    if (anchor.config.backdropMaterial != null) backdrop.backdropMaterial = anchor.config.backdropMaterial;
                    backdrop.Initialize();
                }
            }
        }

        private void LateUpdate()
        {
            if (useExternalCamera) return; // Tuyệt đối không can thiệp vị trí, góc xoay hay FOV khi dùng Camera riêng biệt tự do!
            if (anchor == null || anchor.config == null) return;

            var cfg = anchor.config;

            // Đồng bộ góc FOV của ống kính theo cấu hình (Hỗ trợ đổi FOV thời gian thực)
            if (targetCamera != null && Mathf.Abs(targetCamera.fieldOfView - cfg.cameraFOV) > 0.01f)
            {
                targetCamera.fieldOfView = cfg.cameraFOV;
            }

            // 1. Vị trí Camera (Lùi xa phía sau và đặt cao để tầm nhìn thoáng đãng)
            float offsetX = (reticle != null) ? (reticle.LocalX - reticle.CurrentBounds.opticalCenter.x) : 0f;
            float offsetY = (reticle != null) ? (reticle.LocalY - reticle.CurrentBounds.opticalCenter.y) : 0f;

            float targetLocalX = offsetX * cfg.cameraPanFactor;
            float targetLocalY = (offsetY * cfg.cameraPanFactor) + cfg.cameraHeight;
            float targetLocalZ = -cfg.cameraDistance;

            float curX = Mathf.Lerp(transform.localPosition.x, targetLocalX, cfg.cameraSmoothSpeed * Time.deltaTime);
            float curY = Mathf.Lerp(transform.localPosition.y, targetLocalY, cfg.cameraSmoothSpeed * Time.deltaTime);
            // Khóa cứng 100% cự ly trục Z (Zero Z-Lag)
            transform.localPosition = new Vector3(curX, curY, targetLocalZ);

            // 2. Góc xoay Camera (Cố định đường chân trời)
            if (!cfg.enableDynamicLookAt)
            {
                // Giữ góc nhìn chúc nhẹ cố định dọc theo đường ray (5 độ) - Tuyệt đối không rung lắc
                transform.localRotation = Quaternion.Euler(cfg.fixedCameraPitch, 0f, 0f);
            }
            else
            {
                // Chỉ kích hoạt nếu người chơi chủ động bật Look-At trong Config
                Vector3 aimWorld = (reticle != null) ? reticle.WorldPosition : transform.position + transform.forward * 100f;
                Vector3 shipWorld = (ship != null) ? ship.WorldPosition : transform.position + transform.forward * 20f;
                Vector3 lookTargetWorld = Vector3.Lerp(shipWorld, aimWorld, cfg.cameraLookAtWeight);

                Vector3 direction = (lookTargetWorld - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction, anchor.transform.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, cfg.cameraSmoothSpeed * Time.deltaTime);
                }
            }

            // 3. Phản hồi Rung Chấn Camera Impulse (Traumatic Stress Model - 0 GC Alloc)
            if (targetCamera != null && !useExternalCamera)
            {
                if (_trauma > 0.001f)
                {
                    _shakeTime += Time.deltaTime * shakeFrequency;
                    float shake = _trauma * _trauma; // Phi tuyến tính bậc hai

                    float shakeX = (Mathf.PerlinNoise(_shakeTime, 0.35f) * 2f - 1f) * maxShakeTranslationX * shake;
                    float shakeY = (Mathf.PerlinNoise(0.35f, _shakeTime) * 2f - 1f) * maxShakeTranslationY * shake;
                    float shakeRotZ = (Mathf.PerlinNoise(_shakeTime, _shakeTime) * 2f - 1f) * maxShakeRotationZ * shake;

                    targetCamera.transform.localPosition = new Vector3(shakeX, shakeY, 0f);
                    targetCamera.transform.localRotation = Quaternion.Euler(0f, 0f, shakeRotZ);

                    _trauma = Mathf.Max(0f, _trauma - traumaDecayRate * Time.deltaTime);
                }
                else if (targetCamera.transform.localPosition != Vector3.zero || targetCamera.transform.localRotation != Quaternion.identity)
                {
                    targetCamera.transform.localPosition = Vector3.zero;
                    targetCamera.transform.localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Căn chỉnh tức thì Camera về vị trí và góc nhìn mặc định khi hồi sinh (triệt tiêu 1-frame Lerp Jitter).
        /// </summary>
        public void SnapToDefault()
        {
            if (useExternalCamera) return;
            if (anchor == null || anchor.config == null) return;
            var cfg = anchor.config;
            transform.localPosition = new Vector3(0f, cfg.cameraHeight, -cfg.cameraDistance);
            transform.localRotation = Quaternion.Euler(cfg.fixedCameraPitch, 0f, 0f);
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = cfg.cameraFOV;
                targetCamera.transform.localPosition = Vector3.zero;
                targetCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (targetCamera != null)
            {
                targetCamera.transform.SetParent(null);
                if (!useExternalCamera)
                {
                    var oldFollow = targetCamera.GetComponent<FollowCam>();
                    if (oldFollow != null) oldFollow.enabled = true;
                }
            }
        }
    }
}
