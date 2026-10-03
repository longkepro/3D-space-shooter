using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Component chuyên trách hiển thị HUD và hệ tâm ngắm Sci-Fi phản hồi độ sâu (Depth-Responsive Reticle Presentation).
    /// Hướng A: Tâm ngắm đơn Sci-Fi rỗng ruột (Hollow Box) tích hợp đo đạc cự ly số hóa (0 GC Telemetry) và co giãn quang học theo độ sâu 3D.
    /// Tuân thủ nguyên tắc Unidirectional Data Flow (Điều 4 GEMINI.md) và 0 GC Allocation.
    /// Toàn bộ phép chiếu màn hình thực thi trong LateUpdate() để đồng bộ 100% sau CameraRig và Ship.
    /// </summary>
    [DisallowMultipleComponent]
    public class DualReticleHUD : MonoBehaviour
    {
        [Header("References")]
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public VirtualRailShip ship;
        public VirtualRailWeapon weapon;
        public RectTransform farCrosshairUI;
        public RectTransform nearCrosshairUI;
        public Image farCrosshairImage;
        public Image nearCrosshairImage;
        public Text telemetryText;

        [Header("Multiplier & Graze HUD (TDD v2.0.0 Phần III)")]
        public RectTransform multiplierBadgeRoot;
        public Text multiplierBadgeText;
        public Image multiplierDecayFill;
        public Text grazePopupText;

        // 0 GC String Cache: Bảng chuỗi cự ly tĩnh từ 0m đến 350m để triệt tiêu hoàn toàn Heap Allocation trong LateUpdate
        private static readonly string[] DIST_STRINGS = PrecomputeDistanceStrings();

        private static string[] PrecomputeDistanceStrings()
        {
            string[] table = new string[351];
            for (int i = 0; i <= 350; i++)
            {
                table[i] = i + "m";
            }
            return table;
        }

        // Bộ đệm quét độ sâu không cấp phát rác (0 GC Alloc)
        private readonly RaycastHit[] _depthHits = new RaycastHit[8];
        private Image[] _farBorderImages;
        private Image _farCenterDotImage;
        private Image[] _nearTickImages;
        private Image _nearCenterDotImage;

        private Camera mainCamera;
        private Color originalBorderColor = new Color(0.1f, 0.95f, 1f, 0.90f);
        private Color hoverBorderColor = new Color(1f, 1f, 1f, 1.0f);
        private Color warningBorderColor = new Color(1f, 0.35f, 0.2f, 0.95f);
        private Color mediumBorderColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        private readonly Color normalNearColor = new Color(0.2f, 1f, 0.4f, 0.95f);
        private readonly Color lockedNearColor = new Color(1f, 0.95f, 0.35f, 1.0f);

        private bool isMouseHovering = false;
        private float currentScale = 1.0f;
        private int lastDistInt = -1;
        private int _lastMultiplierIndex = -1;
        private float _grazeFlashTimer = 0f;

        public bool IsMouseHovering => isMouseHovering;
        public float CurrentScale => currentScale;

        private void OnEnable()
        {
            ComboScoringEngine.OnGrazeTriggered += HandleGrazeTriggered;
        }

        private void OnDisable()
        {
            ComboScoringEngine.OnGrazeTriggered -= HandleGrazeTriggered;
        }

        private void HandleGrazeTriggered(Vector3 worldPos, int points)
        {
            _grazeFlashTimer = 0.65f;
            if (grazePopupText != null)
            {
                grazePopupText.gameObject.SetActive(true);
            }
        }

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (ship == null) ship = FindAnyObjectByType<VirtualRailShip>();
            if (weapon == null && anchor != null) weapon = anchor.GetComponentInChildren<VirtualRailWeapon>();
            if (weapon == null) weapon = FindAnyObjectByType<VirtualRailWeapon>();
            if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
            {
                var camRig = anchor != null ? anchor.GetComponentInChildren<VirtualRailCameraRig>() : null;
                mainCamera = (camRig != null && camRig.targetCamera != null) ? camRig.targetCamera : Camera.main;
            }

            var cfg = anchor != null ? anchor.config : null;
            EnsureCrosshairStyling(cfg);
        }

        private void LateUpdate()
        {
            if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
            {
                var camRig = anchor != null ? anchor.GetComponentInChildren<VirtualRailCameraRig>() : null;
                mainCamera = (camRig != null && camRig.targetCamera != null) ? camRig.targetCamera : Camera.main;
            }
            if (mainCamera == null) return;

            var cfg = anchor != null ? anchor.config : null;
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (weapon == null && anchor != null) weapon = anchor.GetComponentInChildren<VirtualRailWeapon>();

            // 1. Tâm ngắm chính (Far Reticle) - Chiếu từ reticle.WorldPosition lên Screen Space
            if (farCrosshairUI != null && reticle != null)
            {
                Vector3 farScreenPos = mainCamera.WorldToScreenPoint(reticle.WorldPosition);
                if (farScreenPos.z > 0)
                {
                    farCrosshairUI.position = farScreenPos;
                }
            }

            // 2. Phản hồi độ sâu quang học & Đo đạc cự ly số hóa (Depth-Responsive Reticle & Distance Telemetry)
            if (cfg != null && cfg.enableDepthResponsiveReticle)
            {
                UpdateDepthResponsiveHUD(cfg);
            }
            else
            {
                // Trạng thái mặc định (1.0x, tắt Telemetry) khi không bật phản hồi độ sâu
                if (farCrosshairUI != null && farCrosshairUI.localScale != Vector3.one)
                {
                    farCrosshairUI.localScale = Vector3.one;
                }
                if (telemetryText != null && telemetryText.gameObject.activeSelf)
                {
                    telemetryText.gameObject.SetActive(false);
                }
                SetBorderColors(originalBorderColor);
            }

            // 3. Tâm ngắm gần (Dual Reticle Mode - Chỉ bật khi được kích hoạt rõ ràng)
            if (cfg != null && cfg.enableDualReticle)
            {
                UpdateDualReticlePresentation(cfg);
            }
            else
            {
                if (nearCrosshairUI != null && nearCrosshairUI.gameObject.activeSelf)
                {
                    nearCrosshairUI.gameObject.SetActive(false);
                }
            }

            // 4. Cập nhật trạng thái chuột chạm tâm ngắm (Hover-to-fire - Chỉ dùng khi KHÔNG dùng Mouse Aim)
            UpdateMouseHoverState(cfg);

            // 5. Cập nhật Telemetry Hệ số nhân Combo & Lướt cận kề Graze (0 GC Alloc)
            UpdateMultiplierAndGrazeHUD();
        }

        /// <summary>
        /// Quét độ sâu mục tiêu dọc theo trục ngắm Camera và cập nhật tỷ lệ co giãn + cự ly Telemetry (0 GC Alloc).
        /// </summary>
        private void UpdateDepthResponsiveHUD(VirtualRailConfig cfg)
        {
            float targetDistance = (cfg.convergenceDistance > 10f) ? cfg.convergenceDistance : 150f;
            Vector3 aimWorld = (reticle != null) ? reticle.WorldPosition : (anchor != null ? anchor.ToWorldPoint(new Vector3(0, 0, targetDistance)) : transform.position + transform.forward * targetDistance);
            Vector3 camPos = mainCamera.transform.position;
            Vector3 rayDir = (aimWorld - camPos).normalized;

            Vector3 shipPos = (ship != null) ? ship.transform.position : (anchor != null ? anchor.transform.position : transform.position);
            float camToShipProj = Vector3.Dot(shipPos - camPos, rayDir);
            Vector3 rayStart = camPos + rayDir * Mathf.Max(0f, camToShipProj + 1.5f);

            float maxScanDist = (cfg.maxConvergenceScanDistance > 0f) ? cfg.maxConvergenceScanDistance : 250f;
            float scanDist = Mathf.Max(50f, maxScanDist - camToShipProj);
            float scanRadius = (cfg.dynamicConvergenceRadius > 0.05f) ? cfg.dynamicConvergenceRadius : 2.5f;
            LayerMask mask = (cfg.enemyLayer.value != 0) ? cfg.enemyLayer : ~0;

            int hitCount = Physics.SphereCastNonAlloc(
                rayStart,
                scanRadius,
                rayDir,
                _depthHits,
                scanDist,
                mask,
                QueryTriggerInteraction.Ignore
            );

            float closestDist = float.MaxValue;
            bool hasTarget = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = _depthHits[i];
                if (h.collider == null) continue;

                // Loại trừ chính thân tàu người chơi
                if (h.transform == transform || h.transform.IsChildOf(transform) || h.transform.root == transform.root || h.transform.CompareTag("Player")) continue;
                if (ship != null && (h.transform == ship.transform || h.transform.IsChildOf(ship.transform))) continue;

                // Đảm bảo mục tiêu nằm ở phía trước tàu theo trục tiến
                Vector3 toHit = h.point - shipPos;
                if (Vector3.Dot(toHit, anchor != null ? anchor.transform.forward : Vector3.forward) <= 1.0f) continue;

                float d = Vector3.Distance(shipPos, h.point);
                if (d < closestDist)
                {
                    closestDist = d;
                    targetDistance = d;
                    hasTarget = true;
                }
            }

            // A. Dynamic Reticle Scaling: Phóng to khi gần (1.4x tại 25m), Thu nhỏ khi xa (0.7x tại 250m)
            float minScale = (cfg.minReticleScale > 0.1f) ? cfg.minReticleScale : 0.7f;
            float maxScale = (cfg.maxReticleScale > 0.1f) ? cfg.maxReticleScale : 1.4f;
            float normDepth = Mathf.Clamp01((targetDistance - 25f) / (maxScanDist - 25f));
            float targetScale = Mathf.Lerp(maxScale, minScale, normDepth);
            float lerpSpeed = (cfg.reticleScaleLerpSpeed > 0.1f) ? cfg.reticleScaleLerpSpeed : 12f;

            currentScale = Mathf.Lerp(currentScale, targetScale, lerpSpeed * Time.deltaTime);
            if (farCrosshairUI != null)
            {
                farCrosshairUI.localScale = new Vector3(currentScale, currentScale, 1f);
            }

            // B. Distance Telemetry HUD & Border Glow (0 GC Alloc)
            Color activeColor;
            if (weapon != null && weapon.IsGimbalClamped)
            {
                activeColor = mediumBorderColor;  // Vàng hổ phách: Cảnh báo nòng súng kịch biên góc quay 35°
            }
            else if (targetDistance < 50f)
            {
                activeColor = warningBorderColor; // Đỏ báo động: Mục tiêu/thiên thạch sát mặt (< 50m)
            }
            else if (targetDistance < 100f)
            {
                activeColor = lockedNearColor;    // Vàng sáng rực: Khóa mục tiêu cự ly gần - trung bình (chuẩn Star Fox / Sin & Punishment)
            }
            else if (hasTarget)
            {
                activeColor = hoverBorderColor;   // Trắng sáng: Khóa mục tiêu ở xa (> 100m)
            }
            else
            {
                activeColor = originalBorderColor; // Xanh neon Cyan chuẩn (không có mục tiêu, hiển thị 150m)
            }

            SetBorderColors(activeColor);

            if (cfg.enableDistanceTelemetry && telemetryText != null)
            {
                if (!telemetryText.gameObject.activeSelf) telemetryText.gameObject.SetActive(true);

                int distInt = Mathf.Clamp(Mathf.RoundToInt(targetDistance), 0, 350);
                if (distInt != lastDistInt)
                {
                    lastDistInt = distInt;
                    telemetryText.text = DIST_STRINGS[distInt];
                    telemetryText.color = activeColor;
                }
            }
            else if (telemetryText != null && telemetryText.gameObject.activeSelf)
            {
                telemetryText.gameObject.SetActive(false);
            }
        }

        private void SetBorderColors(Color color)
        {
            if (_farBorderImages != null)
            {
                for (int i = 0; i < _farBorderImages.Length; i++)
                {
                    if (_farBorderImages[i] != null) _farBorderImages[i].color = color;
                }
            }
            if (_farCenterDotImage != null)
            {
                _farCenterDotImage.color = color;
            }
        }

        /// <summary>
        /// Cập nhật tâm ngắm gần theo trục nòng súng (Boresight) khi bật Dual Reticle mode.
        /// </summary>
        private void UpdateDualReticlePresentation(VirtualRailConfig cfg)
        {
            if (nearCrosshairUI == null) return;

            Vector3 boresightPoint;
            float targetDist = (cfg != null && cfg.convergenceDistance > 10f) ? cfg.convergenceDistance : 150f;

            if (cfg != null && cfg.enableGimbalWeapon)
            {
                Vector3 origin = (ship != null) ? ship.transform.position : transform.position;
                Vector3 gunDir = (weapon != null) ? weapon.CurrentGimbalDir : ((ship != null) ? ship.ShipForward : transform.forward);
                boresightPoint = origin + gunDir * targetDist;
                // Giữ nẹp ngang màn hình, không bị chao nghiêng theo roll của tàu
                nearCrosshairUI.localEulerAngles = Vector3.zero;
            }
            else if (ship != null)
            {
                Transform meshT = (ship.VisualMesh != null) ? ship.VisualMesh : ship.transform;
                boresightPoint = ship.transform.position + meshT.forward * targetDist;
                nearCrosshairUI.localEulerAngles = new Vector3(0, 0, meshT.localEulerAngles.z);
            }
            else if (anchor != null)
            {
                boresightPoint = anchor.transform.position + anchor.transform.forward * targetDist;
            }
            else
            {
                boresightPoint = transform.position + transform.forward * targetDist;
            }

            Vector3 nearScreenPos = mainCamera.WorldToScreenPoint(boresightPoint);
            if (nearScreenPos.z > 0)
            {
                nearCrosshairUI.position = nearScreenPos;
                if (!nearCrosshairUI.gameObject.activeSelf) nearCrosshairUI.gameObject.SetActive(true);
            }

            // Phản hồi thị giác Locked-on khi Ụ súng xoay bắt kịp chuột (0 GC Alloc)
            if (cfg != null && cfg.enableGimbalWeapon && farCrosshairUI != null)
            {
                float screenDist = Vector2.Distance(
                    new Vector2(nearCrosshairUI.position.x, nearCrosshairUI.position.y),
                    new Vector2(farCrosshairUI.position.x, farCrosshairUI.position.y)
                );
                float lockThreshold = (cfg.gimbalLockThreshold > 1f) ? cfg.gimbalLockThreshold : 12f;
                bool isLocked = screenDist <= lockThreshold;
                UpdateGimbalLockFeedback(isLocked);
            }
            else
            {
                UpdateGimbalLockFeedback(false);
            }
        }

        private void UpdateGimbalLockFeedback(bool isLocked)
        {
            Color col = isLocked ? lockedNearColor : normalNearColor;
            if (_nearTickImages != null)
            {
                for (int i = 0; i < _nearTickImages.Length; i++)
                {
                    if (_nearTickImages[i] != null) _nearTickImages[i].color = col;
                }
            }
            if (_nearCenterDotImage != null)
            {
                _nearCenterDotImage.color = col;
            }
        }

        private void UpdateMouseHoverState(VirtualRailConfig cfg)
        {
            // Trong chế độ điều khiển độc lập bằng chuột, chuột chính là tâm ngắm nên không dùng hover-touch
            if (cfg == null || !cfg.enableMouseHoverFire || cfg.enableMouseAim || farCrosshairUI == null)
            {
                isMouseHovering = false;
                if (reticle != null) reticle.SetMouseTouching(false);
                return;
            }

            Vector2 mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            {
                isMouseHovering = false;
                if (reticle != null) reticle.SetMouseTouching(false);
                return;
            }

            Vector3 reticleScreenPos = farCrosshairUI.position;
            float dist = Vector2.Distance(mousePos, new Vector2(reticleScreenPos.x, reticleScreenPos.y));
            float effectiveRadius = cfg.reticleMouseTouchRadius > 5f ? cfg.reticleMouseTouchRadius : 45f;

            isMouseHovering = (dist <= effectiveRadius);
            if (reticle != null) reticle.SetMouseTouching(isMouseHovering);

            if (isMouseHovering)
            {
                SetBorderColors(hoverBorderColor);
            }
        }

        /// <summary>
        /// Cập nhật hệ thống hiển thị Multiplier Badge và thanh Multiplier Decay (TDD v2.0.0 Phần III).
        /// Hoàn toàn 0 GC Allocation trong LateUpdate.
        /// </summary>
        private void UpdateMultiplierAndGrazeHUD()
        {
            var engine = ComboScoringEngine.Instance;
            if (engine == null) return;

            int multIndex = engine.MultiplierIndex;
            if (multIndex != _lastMultiplierIndex)
            {
                _lastMultiplierIndex = multIndex;
                if (multiplierBadgeText != null && multIndex >= 0 && multIndex < ComboScoringEngine.MULTIPLIER_TEXTS.Length)
                {
                    multiplierBadgeText.text = ComboScoringEngine.MULTIPLIER_TEXTS[multIndex];
                    multiplierBadgeText.color = ComboScoringEngine.MULTIPLIER_COLORS[multIndex];
                }
            }

            if (multiplierDecayFill != null)
            {
                float ratio = engine.DecayRatio;
                multiplierDecayFill.fillAmount = ratio;
                if (multIndex >= 0 && multIndex < ComboScoringEngine.MULTIPLIER_COLORS.Length)
                {
                    multiplierDecayFill.color = ComboScoringEngine.MULTIPLIER_COLORS[multIndex];
                }
                multiplierDecayFill.gameObject.SetActive(ratio > 0f);
            }

            if (_grazeFlashTimer > 0f)
            {
                _grazeFlashTimer -= Time.deltaTime;
                if (grazePopupText != null)
                {
                    float alpha = Mathf.Clamp01(_grazeFlashTimer / 0.65f);
                    Color col = grazePopupText.color;
                    col.a = alpha;
                    grazePopupText.color = col;

                    if (_grazeFlashTimer <= 0f)
                    {
                        grazePopupText.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void EnsureCrosshairStyling(VirtualRailConfig cfg)
        {
            float farSize = cfg != null && cfg.farReticleSize > 10f ? cfg.farReticleSize : 56f;
            float nearSize = cfg != null && cfg.nearReticleSize > 10f ? cfg.nearReticleSize : 28f;

            if (farCrosshairUI != null)
            {
                farCrosshairUI.sizeDelta = new Vector2(farSize, farSize);
                if (farCrosshairImage == null) farCrosshairImage = farCrosshairUI.GetComponent<Image>();
                // Tâm ngắm rỗng ruột 100% để nhìn xuyên thấu không gian và kẻ địch
                if (farCrosshairImage != null)
                {
                    farCrosshairImage.color = Color.clear;
                    farCrosshairImage.raycastTarget = false;
                }

                if (farCrosshairUI.Find("BorderTop") == null)
                {
                    CreateBorderLine(farCrosshairUI, "BorderTop", new Vector2(0, farSize * 0.5f - 1f), new Vector2(farSize, 2f), originalBorderColor);
                    CreateBorderLine(farCrosshairUI, "BorderBottom", new Vector2(0, -farSize * 0.5f + 1f), new Vector2(farSize, 2f), originalBorderColor);
                    CreateBorderLine(farCrosshairUI, "BorderLeft", new Vector2(-farSize * 0.5f + 1f, 0), new Vector2(2f, farSize), originalBorderColor);
                    CreateBorderLine(farCrosshairUI, "BorderRight", new Vector2(farSize * 0.5f - 1f, 0), new Vector2(2f, farSize), originalBorderColor);
                }

                // Chấm tâm nhỏ (FarCenterDot 4x4)
                Transform existingDot = farCrosshairUI.Find("FarCenterDot");
                if (existingDot == null)
                {
                    GameObject dotObj = new GameObject("FarCenterDot");
                    dotObj.transform.SetParent(farCrosshairUI, false);
                    RectTransform dotRt = dotObj.AddComponent<RectTransform>();
                    dotRt.sizeDelta = new Vector2(4f, 4f);
                    _farCenterDotImage = dotObj.AddComponent<Image>();
                    _farCenterDotImage.color = originalBorderColor;
                    _farCenterDotImage.raycastTarget = false;
                }
                else
                {
                    _farCenterDotImage = existingDot.GetComponent<Image>();
                }

                // Cache 4 đường viền
                CacheBorderImages();

                // Tự động kiểm tra hoặc sinh Text Telemetry nếu chưa có
                if (telemetryText == null)
                {
                    Transform existingTelem = farCrosshairUI.Find("DistanceTelemetry");
                    if (existingTelem != null)
                    {
                        telemetryText = existingTelem.GetComponent<Text>();
                    }
                    else
                    {
                        telemetryText = CreateProceduralTelemetryText(farCrosshairUI, farSize);
                    }
                }
            }

            if (nearCrosshairUI == null && cfg != null && cfg.enableDualReticle)
            {
                Transform existing = transform.Find("ReticleNearCrosshair");
                if (existing != null)
                {
                    nearCrosshairUI = existing.GetComponent<RectTransform>();
                }
                else
                {
                    nearCrosshairUI = CreateProceduralNearCrosshair(transform, nearSize);
                }
            }

            if (nearCrosshairUI != null)
            {
                nearCrosshairUI.sizeDelta = new Vector2(nearSize, nearSize);
                if (nearCrosshairImage == null) nearCrosshairImage = nearCrosshairUI.GetComponent<Image>();
                CacheNearTickImages();
            }

            // Tự động kiểm tra hoặc sinh Multiplier Badge & Decay Gauge UI
            EnsureMultiplierBadgeHUD();
        }

        private void EnsureMultiplierBadgeHUD()
        {
            if (multiplierBadgeRoot != null) return;

            Transform existingBadge = transform.Find("MultiplierBadge");
            if (existingBadge != null)
            {
                multiplierBadgeRoot = existingBadge.GetComponent<RectTransform>();
                multiplierBadgeText = existingBadge.Find("MultiplierValue")?.GetComponent<Text>();
                multiplierDecayFill = existingBadge.Find("DecayTrack/DecayFill")?.GetComponent<Image>();
                grazePopupText = existingBadge.Find("GrazePopupBanner")?.GetComponent<Text>();
                return;
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Badge Root Panel (Góc trên bên trái, dưới thanh máu/điểm)
            GameObject badgeObj = new GameObject("MultiplierBadge");
            badgeObj.transform.SetParent(transform, false);
            RectTransform rt = badgeObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(25f, -80f);
            rt.sizeDelta = new Vector2(130f, 52f);

            Image bgImg = badgeObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.12f, 0.82f);
            bgImg.raycastTarget = false;

            Outline badgeOutline = badgeObj.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.15f, 0.6f, 0.9f, 0.5f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);
            multiplierBadgeRoot = rt;

            // 2. Sub-Label ("COMBO MULTIPLIER")
            GameObject labelObj = new GameObject("ComboLabel");
            labelObj.transform.SetParent(badgeObj.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -4f);
            labelRt.sizeDelta = new Vector2(120f, 14f);
            Text lbl = labelObj.AddComponent<Text>();
            lbl.font = defaultFont;
            lbl.fontSize = 10;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = new Color(0.6f, 0.8f, 0.9f, 0.8f);
            lbl.text = "COMBO MULTIPLIER";
            lbl.raycastTarget = false;

            // 3. Multiplier Value Text
            GameObject valObj = new GameObject("MultiplierValue");
            valObj.transform.SetParent(badgeObj.transform, false);
            RectTransform valRt = valObj.AddComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0f, 0.5f);
            valRt.anchorMax = new Vector2(1f, 0.5f);
            valRt.pivot = new Vector2(0.5f, 0.5f);
            valRt.anchoredPosition = new Vector2(0f, 2f);
            valRt.sizeDelta = new Vector2(120f, 26f);
            multiplierBadgeText = valObj.AddComponent<Text>();
            multiplierBadgeText.font = defaultFont;
            multiplierBadgeText.fontSize = 22;
            multiplierBadgeText.fontStyle = FontStyle.Bold;
            multiplierBadgeText.alignment = TextAnchor.MiddleCenter;
            multiplierBadgeText.color = ComboScoringEngine.MULTIPLIER_COLORS[0];
            multiplierBadgeText.text = ComboScoringEngine.MULTIPLIER_TEXTS[0];
            multiplierBadgeText.raycastTarget = false;

            Outline valOutline = valObj.AddComponent<Outline>();
            valOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            valOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // 4. Decay Bar Track
            GameObject trackObj = new GameObject("DecayTrack");
            trackObj.transform.SetParent(badgeObj.transform, false);
            RectTransform trackRt = trackObj.AddComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.5f, 0f);
            trackRt.anchorMax = new Vector2(0.5f, 0f);
            trackRt.pivot = new Vector2(0.5f, 0f);
            trackRt.anchoredPosition = new Vector2(0f, 5f);
            trackRt.sizeDelta = new Vector2(116f, 5f);
            Image trackImg = trackObj.AddComponent<Image>();
            trackImg.color = new Color(0.1f, 0.15f, 0.22f, 0.85f);
            trackImg.raycastTarget = false;

            // 5. Decay Bar Fill
            GameObject fillObj = new GameObject("DecayFill");
            fillObj.transform.SetParent(trackObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            multiplierDecayFill = fillObj.AddComponent<Image>();
            multiplierDecayFill.type = Image.Type.Filled;
            multiplierDecayFill.fillMethod = Image.FillMethod.Horizontal;
            multiplierDecayFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            multiplierDecayFill.color = ComboScoringEngine.MULTIPLIER_COLORS[0];
            multiplierDecayFill.fillAmount = 0f;
            multiplierDecayFill.raycastTarget = false;

            // 6. Graze Banner Popup
            GameObject grazeObj = new GameObject("GrazePopupBanner");
            grazeObj.transform.SetParent(badgeObj.transform, false);
            RectTransform grazeRt = grazeObj.AddComponent<RectTransform>();
            grazeRt.anchorMin = new Vector2(0.5f, 1f);
            grazeRt.anchorMax = new Vector2(0.5f, 1f);
            grazeRt.pivot = new Vector2(0.5f, 0f);
            grazeRt.anchoredPosition = new Vector2(0f, 4f);
            grazeRt.sizeDelta = new Vector2(140f, 20f);
            grazePopupText = grazeObj.AddComponent<Text>();
            grazePopupText.font = defaultFont;
            grazePopupText.fontSize = 13;
            grazePopupText.fontStyle = FontStyle.Bold;
            grazePopupText.alignment = TextAnchor.MiddleCenter;
            grazePopupText.color = new Color(0.2f, 1f, 0.85f, 1.0f);
            grazePopupText.text = "GRAZE +500!";
            grazePopupText.raycastTarget = false;

            Outline gOutline = grazeObj.AddComponent<Outline>();
            gOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            gOutline.effectDistance = new Vector2(1f, -1f);
            grazeObj.SetActive(false);
        }

        private void CacheBorderImages()
        {
            if (farCrosshairUI == null) return;
            string[] names = { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" };
            _farBorderImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                Transform t = farCrosshairUI.Find(names[i]);
                if (t != null) _farBorderImages[i] = t.GetComponent<Image>();
            }
        }

        private void CacheNearTickImages()
        {
            if (nearCrosshairUI == null) return;
            string[] names = { "NearTop", "NearBottom", "NearLeft", "NearRight" };
            _nearTickImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                Transform t = nearCrosshairUI.Find(names[i]);
                if (t != null) _nearTickImages[i] = t.GetComponent<Image>();
            }
            Transform dotT = nearCrosshairUI.Find("CenterDot");
            if (dotT != null) _nearCenterDotImage = dotT.GetComponent<Image>();
        }

        private Text CreateProceduralTelemetryText(Transform parent, float farSize)
        {
            GameObject telemObj = new GameObject("DistanceTelemetry");
            telemObj.transform.SetParent(parent, false);
            RectTransform rt = telemObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -farSize * 0.5f - 11f);
            rt.sizeDelta = new Vector2(80f, 18f);

            Text txt = telemObj.AddComponent<Text>();
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.font = defaultFont;
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = originalBorderColor;
            txt.raycastTarget = false;

            Outline outline = telemObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            return txt;
        }

        private RectTransform CreateProceduralNearCrosshair(Transform parent, float size)
        {
            GameObject nearObj = new GameObject("ReticleNearCrosshair");
            nearObj.transform.SetParent(parent, false);
            RectTransform rt = nearObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            Image img = nearObj.AddComponent<Image>();
            img.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            img.raycastTarget = false;

            // Center Dot 4x4
            GameObject dotObj = new GameObject("CenterDot");
            dotObj.transform.SetParent(nearObj.transform, false);
            RectTransform dotRt = dotObj.AddComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(4f, 4f);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(1f, 1f, 1f, 0.95f);
            dotImg.raycastTarget = false;

            // 4 Ticks
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
    }
}
