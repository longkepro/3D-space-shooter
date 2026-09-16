using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Tâm ngắm 3D dẫn đường (Reticle-Driven Core).
    /// Nhận trực tiếp Input người chơi với 0 độ trễ trên mặt phẳng Z = ConvergenceDistance.
    /// Giới hạn bởi Khung biên ngoài (Outer Box ~85% Camera Viewport).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailReticle : MonoBehaviour
    {
        public VirtualRailAnchor anchor;
        public VirtualRailInputReader inputReader;
        public RectTransform crosshairUI;
        private Camera mainCamera;

        [Header("Runtime Local Coordinates")]
        [SerializeField] private Vector2 localPos2D;
        [SerializeField] private VirtualRailConfig.FrustumBounds currentBounds;
        [SerializeField] private Vector2 currentNormalizedInput;
        [SerializeField] private bool isAimingMoving;

        [Header("Hover-to-Fire (Hướng 1)")]
        [SerializeField] private bool isMouseTouchingReticle;
        private Image crosshairImage;
        private Color originalCrosshairColor = new Color(0.2f, 1f, 0.4f, 0.85f);
        private Color hoverCrosshairColor = new Color(0.1f, 1f, 0.95f, 1f);

        public bool IsMouseTouchingReticle => isMouseTouchingReticle;

        private Vector2 prevMousePos;
        private float autoFireTimer;

        public Vector2 LocalPos2D => localPos2D;
        public float LocalX => localPos2D.x;
        public float LocalY => localPos2D.y;
        public VirtualRailConfig.FrustumBounds CurrentBounds => currentBounds;
        public Vector2 NormalizedPos => currentNormalizedInput;
        public float NormalizedX => currentNormalizedInput.x;
        public float NormalizedY => currentNormalizedInput.y;
        public Vector2 CurrentLimit => new Vector2(currentBounds.HalfWidth, currentBounds.HalfHeight);
        public bool IsAimingMoving => isAimingMoving;

        public Vector3 LocalPosition3D => new Vector3(localPos2D.x, localPos2D.y, anchor != null ? anchor.config.convergenceDistance : 150f);
        public Vector3 WorldPosition => anchor != null ? anchor.ToWorldPoint(LocalPosition3D) : transform.position;

        private void Start()
        {
            mainCamera = Camera.main;
            if (anchor == null)
            {
                anchor = GetComponentInParent<VirtualRailAnchor>();
            }
            if (inputReader == null && anchor != null)
            {
                inputReader = anchor.GetComponentInChildren<VirtualRailInputReader>();
            }
            if (crosshairUI != null)
            {
                crosshairImage = crosshairUI.GetComponent<Image>();
                if (crosshairImage != null) originalCrosshairColor = crosshairImage.color;
            }
            prevMousePos = Input.mousePosition;
            autoFireTimer = 0f;
        }

        private void Update()
        {
            if (anchor == null || anchor.config == null) return;

            var cfg = anchor.config;
            if (mainCamera == null) mainCamera = Camera.main;

            // 1. Tính toán biên ngoài Frustum chính xác (Outer Box: 80% - 85% Camera Frustum)
            currentBounds = cfg.CalculateFrustumBounds(mainCamera, anchor, cfg.convergenceDistance, cfg.reticleViewportRatio);

            if (inputReader != null && inputReader.IsRightStickActive)
            {
                // ==================== CHẾ ĐỘ CẦN GẠT ẢO PHẢI (ANDROID RIGHT JOYSTICK) ====================
                Vector2 aimStick = inputReader.GetAimMovement() * cfg.mobileAimSensitivity;

                float speedNormX = (currentBounds.HalfWidth > 0.001f) ? (cfg.reticleSpeedX / currentBounds.HalfWidth) : 1.5f;
                float speedNormY = (currentBounds.HalfHeight > 0.001f) ? (cfg.reticleSpeedY / currentBounds.HalfHeight) : 1.5f;

                // Hướng A: Tích lũy vị trí ngắm theo độ lệch cần gạt (nhả tay đứng yên)
                currentNormalizedInput.x = Mathf.Clamp(currentNormalizedInput.x + aimStick.x * speedNormX * Time.deltaTime, -1f, 1f);
                currentNormalizedInput.y = Mathf.Clamp(currentNormalizedInput.y + aimStick.y * speedNormY * Time.deltaTime, -1f, 1f);

                float targetX = currentNormalizedInput.x >= 0f
                    ? Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.maxX, currentNormalizedInput.x)
                    : Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.minX, -currentNormalizedInput.x);

                float targetY = currentNormalizedInput.y >= 0f
                    ? Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.maxY, currentNormalizedInput.y)
                    : Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.minY, -currentNormalizedInput.y);

                localPos2D = currentBounds.Clamp(new Vector2(targetX, targetY));

                // Tín hiệu gạt cần -> Tự động khai hỏa
                bool stickMoved = aimStick.sqrMagnitude > 0.01f;
                if (stickMoved)
                {
                    autoFireTimer = cfg.autoFireHoldTime;
                }
                else if (autoFireTimer > 0f)
                {
                    autoFireTimer -= Time.deltaTime;
                }
                isAimingMoving = autoFireTimer > 0f;
            }
            else if (cfg.enableIndependentControls && cfg.enableMouseAim && mainCamera != null)
            {
                // ==================== CHẾ ĐỘ ĐIỀU KHIỂN TÂM NGẮM BẰNG CHUỘT ====================
                Vector2 currentMousePos = Input.mousePosition;
                Vector2 mouseDelta = currentMousePos - prevMousePos;
                prevMousePos = currentMousePos;

                float rawMouseX = Input.GetAxisRaw("Mouse X");
                float rawMouseY = Input.GetAxisRaw("Mouse Y");
                bool mouseMoved = mouseDelta.sqrMagnitude > (cfg.aimMoveDeadzone * cfg.aimMoveDeadzone) ||
                                  Mathf.Abs(rawMouseX) > 0.01f || Mathf.Abs(rawMouseY) > 0.01f;

                if (mouseMoved)
                {
                    autoFireTimer = cfg.autoFireHoldTime;
                }
                else if (autoFireTimer > 0f)
                {
                    autoFireTimer -= Time.deltaTime;
                }
                isAimingMoving = autoFireTimer > 0f;

                // Chiếu Ray-Plane từ con trỏ chuột trên màn hình vào mặt phẳng hội tụ Z = convergenceDistance
                Vector3 planePoint = anchor.transform.position + anchor.transform.forward * cfg.convergenceDistance;
                Plane convPlane = new Plane(anchor.transform.forward, planePoint);
                Ray mouseRay = mainCamera.ScreenPointToRay(currentMousePos);

                if (convPlane.Raycast(mouseRay, out float enterDist))
                {
                    Vector3 hitPointWorld = mouseRay.GetPoint(enterDist);
                    Vector3 localHit = anchor.ToLocalPoint(hitPointWorld);

                    // Kẹp an toàn trong Khung biên ngoài (Outer Box: 85% Viewport)
                    localPos2D = currentBounds.Clamp(new Vector2(localHit.x, localHit.y));

                    // Cập nhật tọa độ chuẩn hóa Normalized [-1, 1] từ tâm quang học
                    float spanX = (localPos2D.x >= currentBounds.opticalCenter.x)
                        ? (currentBounds.maxX - currentBounds.opticalCenter.x)
                        : (currentBounds.opticalCenter.x - currentBounds.minX);
                    float spanY = (localPos2D.y >= currentBounds.opticalCenter.y)
                        ? (currentBounds.maxY - currentBounds.opticalCenter.y)
                        : (currentBounds.opticalCenter.y - currentBounds.minY);

                    currentNormalizedInput.x = spanX > 0.001f ? Mathf.Clamp((localPos2D.x - currentBounds.opticalCenter.x) / spanX, -1f, 1f) : 0f;
                    currentNormalizedInput.y = spanY > 0.001f ? Mathf.Clamp((localPos2D.y - currentBounds.opticalCenter.y) / spanY, -1f, 1f) : 0f;
                }
            }
            else
            {
                // ==================== CHẾ ĐỘ PHỤ THUỘC CŨ (PHÍM BÀN PHÍM) ====================
                float inputX = Input.GetAxis("Horizontal");
                float inputY = Input.GetAxis("Vertical");

                bool keysMoved = Mathf.Abs(inputX) > 0.05f || Mathf.Abs(inputY) > 0.05f;
                if (keysMoved)
                {
                    autoFireTimer = cfg.autoFireHoldTime;
                }
                else if (autoFireTimer > 0f)
                {
                    autoFireTimer -= Time.deltaTime;
                }
                isAimingMoving = autoFireTimer > 0f;

                if (cfg.useDirectAnalogMapping)
                {
                    // Ánh xạ trực tiếp từ analog cần gạt trong dải [-1, 1] (tự động hồi về tâm khi nhả phím)
                    currentNormalizedInput.x = Mathf.Clamp(inputX, -1f, 1f);
                    currentNormalizedInput.y = Mathf.Clamp(inputY, -1f, 1f);
                }
                else
                {
                    // Hướng A: Tích lũy vị trí chuẩn hóa (Free Roam - Không bao giờ bị kéo về trung tâm)
                    float speedNormX = (currentBounds.HalfWidth > 0.001f) ? (cfg.reticleSpeedX / currentBounds.HalfWidth) : 1.5f;
                    float speedNormY = (currentBounds.HalfHeight > 0.001f) ? (cfg.reticleSpeedY / currentBounds.HalfHeight) : 1.5f;

                    currentNormalizedInput.x = Mathf.Clamp(currentNormalizedInput.x + inputX * speedNormX * Time.deltaTime, -1f, 1f);
                    currentNormalizedInput.y = Mathf.Clamp(currentNormalizedInput.y + inputY * speedNormY * Time.deltaTime, -1f, 1f);

                    // Phím C: Giữ để chủ động đưa tàu & tâm ngắm lướt êm về lại trung tâm khi cần
                    if (Input.GetKey(KeyCode.C))
                    {
                        currentNormalizedInput = Vector2.MoveTowards(currentNormalizedInput, Vector2.zero, 2.5f * Time.deltaTime);
                    }
                }

                // Nội suy tọa độ 3D từ tâm quang học (opticalCenter) tới đúng các mép Frustum
                float targetX = currentNormalizedInput.x >= 0f
                    ? Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.maxX, currentNormalizedInput.x)
                    : Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.minX, -currentNormalizedInput.x);

                float targetY = currentNormalizedInput.y >= 0f
                    ? Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.maxY, currentNormalizedInput.y)
                    : Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.minY, -currentNormalizedInput.y);

                // Kẹp an toàn trong Khung biên ngoài (Outer Box: 85% Viewport)
                localPos2D = currentBounds.Clamp(new Vector2(targetX, targetY));
            }

            // Cập nhật vị trí transform cục bộ
            transform.localPosition = LocalPosition3D;

            // Cập nhật Crosshair UI
            UpdateCrosshairUI();

            // Kiểm tra con trỏ chuột chạm vào tâm ngắm (Hover-to-Fire Hướng 1)
            CheckMouseTouchReticle(cfg);
        }

        private void UpdateCrosshairUI()
        {
            if (crosshairUI == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(WorldPosition);
            if (screenPos.z > 0)
            {
                crosshairUI.position = screenPos;
            }
        }

        private void CheckMouseTouchReticle(VirtualRailConfig cfg)
        {
            if (cfg == null || !cfg.enableMouseHoverFire)
            {
                isMouseTouchingReticle = false;
                if (crosshairImage != null) crosshairImage.color = originalCrosshairColor;
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;

            Vector2 mousePos = Input.mousePosition;

            // Kiểm tra chuột có nằm trong cửa sổ game không
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            {
                isMouseTouchingReticle = false;
                if (crosshairImage != null) crosshairImage.color = originalCrosshairColor;
                return;
            }

            Vector3 reticleScreenPos;
            if (crosshairUI != null)
            {
                reticleScreenPos = crosshairUI.position;
            }
            else if (mainCamera != null)
            {
                reticleScreenPos = mainCamera.WorldToScreenPoint(WorldPosition);
            }
            else
            {
                isMouseTouchingReticle = false;
                return;
            }

            if (reticleScreenPos.z < 0)
            {
                isMouseTouchingReticle = false;
                if (crosshairImage != null) crosshairImage.color = originalCrosshairColor;
                return;
            }

            float effectiveRadius = cfg.reticleMouseTouchRadius;
            if (crosshairUI != null && crosshairUI.rect.width > 0)
            {
                effectiveRadius = Mathf.Max(effectiveRadius, crosshairUI.rect.width * 0.6f);
            }

            float dist = Vector2.Distance(mousePos, new Vector2(reticleScreenPos.x, reticleScreenPos.y));
            isMouseTouchingReticle = (dist <= effectiveRadius);

            // Phản hồi thị giác khi chuột chạm tâm ngắm (đổi màu Neon Cyan sáng rực)
            if (crosshairImage != null)
            {
                crosshairImage.color = isMouseTouchingReticle ? hoverCrosshairColor : originalCrosshairColor;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(WorldPosition, 1.5f);

            if (anchor != null && anchor.config != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
                Gizmos.matrix = Matrix4x4.TRS(anchor.transform.position, anchor.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(
                    new Vector3(currentBounds.Center.x, currentBounds.Center.y, anchor.config.convergenceDistance),
                    new Vector3(currentBounds.Width, currentBounds.Height, 0.1f)
                );
            }
        }
    }
}
