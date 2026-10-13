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
        [Header("Dual Reticle (Giai đoạn 2 - Chuẩn Star Fox 64)")]
        public RectTransform nearCrosshairUI;
        public VirtualRailShip ship;
        private Image nearCrosshairImage;
        private Color originalNearCrosshairColor = new Color(0.2f, 1f, 0.4f, 0.90f);
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
        public void SetMouseTouching(bool state) => isMouseTouchingReticle = state;

        private DualReticleHUD hud;
        private AimAssistModule _aimAssist;
        private VirtualRailWeapon _cachedWeapon;
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
            if (ship == null && anchor != null)
            {
                ship = anchor.GetComponentInChildren<VirtualRailShip>();
            }
            if (ship == null)
            {
                ship = FindAnyObjectByType<VirtualRailShip>();
            }

            if (_cachedWeapon == null && ship != null) _cachedWeapon = ship.GetComponent<VirtualRailWeapon>();
            if (_cachedWeapon == null && anchor != null) _cachedWeapon = anchor.GetComponentInChildren<VirtualRailWeapon>();

            var cfg = anchor != null ? anchor.config : null;

            if (crosshairUI != null)
            {
                crosshairImage = crosshairUI.GetComponent<Image>();
                EnsureFarCrosshairStyling(cfg);

                // Tự động tìm hoặc sinh tâm ngắm gần nếu chưa được liên kết
                if (nearCrosshairUI == null && crosshairUI.parent != null)
                {
                    Transform existingNear = crosshairUI.parent.Find("ReticleNearCrosshair");
                    if (existingNear != null)
                    {
                        nearCrosshairUI = existingNear.GetComponent<RectTransform>();
                    }
                    else if (cfg != null && cfg.enableDualReticle)
                    {
                        nearCrosshairUI = CreateProceduralNearCrosshair(crosshairUI.parent, cfg);
                    }
                }

                if (nearCrosshairUI != null)
                {
                    nearCrosshairImage = nearCrosshairUI.GetComponent<Image>();
                    if (nearCrosshairImage != null) originalNearCrosshairColor = nearCrosshairImage.color;
                }

                // Khởi tạo hoặc liên kết DualReticleHUD (Presentation Layer)
                if (crosshairUI.parent != null)
                {
                    hud = crosshairUI.parent.GetComponent<DualReticleHUD>();
                    if (hud == null) hud = crosshairUI.parent.gameObject.AddComponent<DualReticleHUD>();
                    hud.anchor = anchor;
                    hud.reticle = this;
                    hud.ship = ship;
                    hud.farCrosshairUI = crosshairUI;
                    hud.nearCrosshairUI = nearCrosshairUI;
                }
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

                float baseSpeedX = (currentBounds.HalfWidth > 0.001f) ? (cfg.reticleSpeedX / currentBounds.HalfWidth) : 1.5f;
                float baseSpeedY = (currentBounds.HalfHeight > 0.001f) ? (cfg.reticleSpeedY / currentBounds.HalfHeight) : 1.5f;

                // Tăng tốc độ phản hồi Mobile tức thì (khử 100% độ trễ/delay ngắm bắn):
                // - Đẩy nhẹ (< 0.4): Hệ số ~2.5x hỗ trợ vi chỉnh mục tiêu mượt mà
                // - Đẩy mạnh (> 0.4): Gia tốc lũy tiến lên đến 5.5x để lia tâm ngắm nhạy bén ngang tốc độ thân tàu
                float stickMag = aimStick.magnitude;
                float responseMultiplier = Mathf.Lerp(2.5f, 5.5f, stickMag * stickMag);

                float speedNormX = baseSpeedX * responseMultiplier;
                float speedNormY = baseSpeedY * responseMultiplier;

                // Tích lũy vị trí ngắm theo độ lệch cần gạt (nhả tay đứng yên, phản hồi nhanh nhạy tức thì)
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
            else if (Application.isMobilePlatform)
            {
                // ==================== TRẠNG THÁI NGHỈ TRÊN MOBILE (NHẢ CẦN GẠT PHẢI) ====================
                // Giữ nguyên vị trí ngắm đã tích lũy (nhả tay đứng yên), ngừng xả đạn tự động
                float targetX = currentNormalizedInput.x >= 0f
                    ? Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.maxX, currentNormalizedInput.x)
                    : Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.minX, -currentNormalizedInput.x);

                float targetY = currentNormalizedInput.y >= 0f
                    ? Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.maxY, currentNormalizedInput.y)
                    : Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.minY, -currentNormalizedInput.y);

                localPos2D = currentBounds.Clamp(new Vector2(targetX, targetY));
                isAimingMoving = false;
            }
            else if (cfg.enableIndependentControls && cfg.enableMouseAim && mainCamera != null)
            {
                // ==================== CHẾ ĐỘ ĐIỀU KHIỂN TÂM NGẮM BẰNG CHUỘT (PC ONLY) ====================
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

            // Áp dụng lực hút từ tính của hồng tâm vào mục tiêu khi đang tụ lực (Reticle Magnetism - 0 GC Alloc)
            ApplyReticleMagnetism(cfg);

            // Cập nhật vị trí transform cục bộ
            transform.localPosition = LocalPosition3D;

            // Nếu chưa có DualReticleHUD (Presentation Layer), chạy fallback tương thích ngược
            if (hud == null && crosshairUI != null)
            {
                UpdateCrosshairUI();
                CheckMouseTouchReticle(cfg);
            }
        }

        /// <summary>
        /// Áp dụng lực hút từ tính mềm vào mục tiêu đang bị khóa (Reticle Magnetism - 0 GC Alloc).
        /// Hỗ trợ người chơi giữ tâm ngắm bám chặt vào kẻ địch trong pha tụ lực chuẩn Arcade.
        /// </summary>
        private void ApplyReticleMagnetism(VirtualRailConfig cfg)
        {
            if (cfg == null || !cfg.enableReticleMagnetism || anchor == null) return;

            if (_aimAssist == null)
            {
                _aimAssist = anchor.GetComponentInChildren<AimAssistModule>();
                if (_aimAssist == null) _aimAssist = FindAnyObjectByType<AimAssistModule>();
            }

            if (_aimAssist == null || !_aimAssist.IsCharging || _aimAssist.LockedTarget == null) return;

            Vector3 targetLocal = anchor.ToLocalPoint(_aimAssist.LockedTarget.position);
            // Chỉ hút nếu mục tiêu ở phía trước phi thuyền (tránh lỗi chia 0 hoặc hút ngược ra sau)
            if (targetLocal.z <= 10f) return;

            Vector2 targetPos2D = new Vector2(targetLocal.x, targetLocal.y);
            Vector2 diff = targetPos2D - localPos2D;
            float dist = diff.magnitude;

            // Kiểm tra góc nón từ tính tối đa
            float maxAngle = cfg.reticleMagnetismMaxAngle > 1f ? cfg.reticleMagnetismMaxAngle : 14f;
            float maxDist = Mathf.Tan(maxAngle * Mathf.Deg2Rad) * targetLocal.z;

            if (dist <= maxDist && dist > 0.01f)
            {
                // Lực hút tăng dần theo tiến trình khóa LockProgress (bắt dính êm ái)
                float progress = _aimAssist.LockProgress;
                float pullFactor = cfg.reticleMagnetismStrength * Mathf.Lerp(0.35f, 1.0f, progress);
                float lerpSpeed = pullFactor * 10f;

                localPos2D = Vector2.MoveTowards(localPos2D, targetPos2D, lerpSpeed * Time.deltaTime * Mathf.Min(dist, 20f));
                localPos2D = currentBounds.Clamp(localPos2D);
            }
        }

        private void UpdateCrosshairUI()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            var cfg = anchor != null ? anchor.config : null;

            // 1. Cập nhật Tâm ngắm xa (Far Reticle - Điều khiển bởi chuột / cần gạt tại Z = convergenceDistance)
            if (crosshairUI != null)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(WorldPosition);
                if (screenPos.z > 0)
                {
                    crosshairUI.position = screenPos;
                }
            }

            // 2. Cập nhật Tâm ngắm gần (Near Reticle - Neo theo mũi tàu tại Z = nearReticleDistance)
            if (cfg != null && cfg.enableDualReticle)
            {
                if (nearCrosshairUI != null)
                {
                    if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
                    if (_cachedWeapon == null)
                    {
                        if (ship != null) _cachedWeapon = ship.GetComponent<VirtualRailWeapon>();
                        else if (anchor != null) _cachedWeapon = anchor.GetComponentInChildren<VirtualRailWeapon>();
                    }

                    Vector3 nearWorldPos;
                    float targetDist = (cfg.convergenceDistance > 10f) ? cfg.convergenceDistance : 150f;
                    VirtualRailWeapon weapon = _cachedWeapon;

                    if (cfg.enableGimbalWeapon && weapon != null)
                    {
                        Vector3 origin = (ship != null) ? ship.transform.position : transform.position;
                        nearWorldPos = origin + weapon.CurrentGimbalDir * targetDist;
                        nearCrosshairUI.localEulerAngles = Vector3.zero;
                    }
                    else if (ship != null)
                    {
                        Transform meshT = (ship.VisualMesh != null) ? ship.VisualMesh : ship.transform;
                        nearWorldPos = ship.transform.position + meshT.forward * targetDist;
                        nearCrosshairUI.localEulerAngles = new Vector3(0, 0, meshT.localEulerAngles.z);
                    }
                    else if (anchor != null)
                    {
                        nearWorldPos = anchor.transform.position + anchor.transform.forward * targetDist;
                    }
                    else
                    {
                        nearWorldPos = transform.position + transform.forward * targetDist;
                    }

                    Vector3 nearScreenPos = mainCamera.WorldToScreenPoint(nearWorldPos);
                    if (nearScreenPos.z > 0)
                    {
                        nearCrosshairUI.position = nearScreenPos;
                        if (!nearCrosshairUI.gameObject.activeSelf) nearCrosshairUI.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if (nearCrosshairUI != null && nearCrosshairUI.gameObject.activeSelf)
                {
                    nearCrosshairUI.gameObject.SetActive(false);
                }
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

        private void EnsureFarCrosshairStyling(VirtualRailConfig cfg)
        {
            if (crosshairUI == null) return;
            float farSize = cfg != null && cfg.farReticleSize > 10f ? cfg.farReticleSize : 56f;
            crosshairUI.sizeDelta = new Vector2(farSize, farSize);

            if (crosshairImage != null)
            {
                // Màu kính ngắm Cyan trong suốt để nhìn rõ kẻ địch xuyên qua tâm
                originalCrosshairColor = new Color(0.1f, 0.95f, 1f, 0.20f);
                hoverCrosshairColor = new Color(1f, 1f, 1f, 0.55f);
                crosshairImage.color = originalCrosshairColor;
            }

            // Nếu chưa có viền khung, tạo 4 đường viền vi tính mảnh 2px
            if (crosshairUI.Find("BorderTop") == null)
            {
                CreateBorderLine(crosshairUI, "BorderTop", new Vector2(0, farSize * 0.5f - 1f), new Vector2(farSize, 2f), new Color(0.1f, 0.95f, 1f, 0.90f));
                CreateBorderLine(crosshairUI, "BorderBottom", new Vector2(0, -farSize * 0.5f + 1f), new Vector2(farSize, 2f), new Color(0.1f, 0.95f, 1f, 0.90f));
                CreateBorderLine(crosshairUI, "BorderLeft", new Vector2(-farSize * 0.5f + 1f, 0), new Vector2(2f, farSize), new Color(0.1f, 0.95f, 1f, 0.90f));
                CreateBorderLine(crosshairUI, "BorderRight", new Vector2(farSize * 0.5f - 1f, 0), new Vector2(2f, farSize), new Color(0.1f, 0.95f, 1f, 0.90f));
            }
        }

        private RectTransform CreateProceduralNearCrosshair(Transform parent, VirtualRailConfig cfg)
        {
            GameObject nearObj = new GameObject("ReticleNearCrosshair");
            nearObj.transform.SetParent(parent, false);
            RectTransform rt = nearObj.AddComponent<RectTransform>();
            float size = cfg != null && cfg.nearReticleSize > 10f ? cfg.nearReticleSize : 28f;
            rt.sizeDelta = new Vector2(size, size);

            // Boresight crosshair màu Emerald Green thanh mảnh
            Image img = nearObj.AddComponent<Image>();
            img.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            img.raycastTarget = false;

            // Chấm tâm nhỏ (Center Dot 4x4)
            GameObject dotObj = new GameObject("CenterDot");
            dotObj.transform.SetParent(nearObj.transform, false);
            RectTransform dotRt = dotObj.AddComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(4f, 4f);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(1f, 1f, 1f, 0.95f);
            dotImg.raycastTarget = false;

            // 4 vạch ngạnh chữ thập (Crosshair Ticks)
            CreateBorderLine(nearObj.transform, "NearTop", new Vector2(0, size * 0.35f), new Vector2(2f, size * 0.3f), new Color(0.2f, 1f, 0.4f, 0.95f));
            CreateBorderLine(nearObj.transform, "NearBottom", new Vector2(0, -size * 0.35f), new Vector2(2f, size * 0.3f), new Color(0.2f, 1f, 0.4f, 0.95f));
            CreateBorderLine(nearObj.transform, "NearLeft", new Vector2(-size * 0.35f, 0), new Vector2(size * 0.3f, 2f), new Color(0.2f, 1f, 0.4f, 0.95f));
            CreateBorderLine(nearObj.transform, "NearRight", new Vector2(size * 0.35f, 0), new Vector2(size * 0.3f, 2f), new Color(0.2f, 1f, 0.4f, 0.95f));

            return rt;
        }

        private static void CreateBorderLine(Transform parent, string name, Vector2 localPos, Vector2 size, Color color)
        {
            GameObject line = new GameObject(name);
            line.transform.SetParent(parent, false);
            RectTransform rt = line.AddComponent<RectTransform>();
            rt.anchoredPosition = localPos;
            rt.sizeDelta = size;
            Image img = line.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Khôi phục vị trí tâm ngắm về chính giữa màn hình khi tàu hồi sinh (0 GC Alloc).
        /// </summary>
        public void ResetPosition()
        {
            localPos2D = Vector2.zero;
            currentNormalizedInput = Vector2.zero;
            isAimingMoving = false;
            isMouseTouchingReticle = false;
            transform.localPosition = LocalPosition3D;
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
