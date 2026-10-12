using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Module Đọc Input Tập Trung (Centralized Input Provider / Hub).
    /// Phân tách hoàn toàn tầng thiết bị phần cứng (Phím, Chuột, Cảm ứng di động Android)
    /// khỏi tầng logic gameplay của Tàu (VirtualRailShip), Tâm ngắm (VirtualRailReticle) và Vũ khí (VirtualRailWeapon).
    /// Hỗ trợ hệ thống nút cảm ứng 'Vòng trắng tối giản' (Minimalist White Ring) với 0 GC Alloc.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailInputReader : MonoBehaviour
    {
        [Header("Mobile Joysticks")]
        [Tooltip("Cần gạt ảo bên trái (Điều khiển thân tàu)")]
        public VirtualJoystick leftJoystick;
        [Tooltip("Cần gạt ảo bên phải (Fallback/Legacy)")]
        public VirtualJoystick rightJoystick;

        [Header("Mobile Touch Buttons (Minimalist White Rings)")]
        [Tooltip("Nút Bắn Laser & Tụ lực (Fallback/Legacy)")]
        public VirtualTouchButton btnFire;
        [Tooltip("Nút Sạc Hỏa Lực Tụ Lực Riêng Biệt (Dedicated Charge Shot Button)")]
        public VirtualTouchButton btnCharge;
        [Tooltip("Nút Bom nguyên tử Smart Bomb")]
        public VirtualTouchButton btnBomb;
        [Tooltip("Nút Tăng tốc hành trình (Boost)")]
        public VirtualTouchButton btnBoost;
        [Tooltip("Nút Hãm phanh (Brake)")]
        public VirtualTouchButton btnBrake;
        [Tooltip("Nút Lộn vòng sang trái phản xạ đạn (Barrel Roll Left)")]
        public VirtualTouchButton btnRollLeft;
        [Tooltip("Nút Lộn vòng sang phải phản xạ đạn (Barrel Roll Right)")]
        public VirtualTouchButton btnRollRight;

        [Header("Config & UI References")]
        public VirtualRailConfig config;
        [Tooltip("Root GameObject chứa UI cảm ứng")]
        public GameObject mobileUIRoot;

        // Bộ nhớ đệm không cấp phát rác (0 GC Alloc Cache)
        private SmartBombModule _cachedBombModule;
        private AimAssistModule _cachedAimAssist;
        private int _lastBombCount = -999;
        private static readonly string[] BombBadgeStrings = new string[]
        {
            "x0", "x1", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9", "x10"
        };

        private void Awake()
        {
            // Ép hướng màn hình ngang cố định trên thiết bị di động (Mobile Landscape Lock)
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            if (config == null)
            {
                var anchor = GetComponentInParent<VirtualRailAnchor>();
                if (anchor != null) config = anchor.config;
            }

            EnsureMobileUI();
        }

        private void Start()
        {
            EnsureMobileUI();
            CacheModules();
            UpdateUIVisibility();
        }

        private void CacheModules()
        {
            if (_cachedBombModule == null)
            {
                _cachedBombModule = GetComponentInParent<SmartBombModule>();
                if (_cachedBombModule == null) _cachedBombModule = FindAnyObjectByType<SmartBombModule>();
            }

            if (_cachedAimAssist == null)
            {
                _cachedAimAssist = GetComponentInParent<AimAssistModule>();
                if (_cachedAimAssist == null) _cachedAimAssist = FindAnyObjectByType<AimAssistModule>();
            }
        }

        private void Update()
        {
            HandleHotkeyToggle();
            UpdateUIVisibility();
            UpdateTouchUIFeedback();
        }

        private void UpdateTouchUIFeedback()
        {
            if (mobileUIRoot == null || !mobileUIRoot.activeInHierarchy) return;

            // Đồng bộ tiến trình nạp tụ lực Laser lên vòng tròn nạp (Charge Arc Image - 0 GC Alloc)
            VirtualTouchButton chargeTarget = btnCharge != null ? btnCharge : btnFire;
            if (chargeTarget != null)
            {
                if (_cachedAimAssist == null) CacheModules();
                float charge = (_cachedAimAssist != null && _cachedAimAssist.IsCharging) ? _cachedAimAssist.ChargeProgress : 0f;
                chargeTarget.SetChargeProgress(charge);
            }

            // Đồng bộ số lượng bom Smart Bomb còn lại lên nhãn huy hiệu (0 GC Alloc)
            if (btnBomb != null)
            {
                if (_cachedBombModule == null) CacheModules();
                if (_cachedBombModule != null)
                {
                    int bombs = _cachedBombModule.CurrentBombs;
                    if (bombs != _lastBombCount)
                    {
                        _lastBombCount = bombs;
                        if (bombs >= 0 && bombs < BombBadgeStrings.Length)
                        {
                            btnBomb.SetBadge(BombBadgeStrings[bombs]);
                        }
                        else
                        {
                            btnBomb.SetBadge(bombs > 0 ? "x" + bombs.ToString() : "");
                        }
                    }
                }
            }
        }

        private void HandleHotkeyToggle()
        {
            KeyCode key = (config != null && config.toggleMobileUIKey != KeyCode.None) ? config.toggleMobileUIKey : KeyCode.F1;
            if (Input.GetKeyDown(key))
            {
                if (config != null)
                {
                    if (Application.isMobilePlatform)
                    {
                        config.enableMobileUI = !config.enableMobileUI;
                        Debug.Log($"<color=cyan><b>[VirtualRail] Phím {key}:</b> Đã chuyển đổi hiển thị Mobile UI -> {(config.enableMobileUI ? "HIỆN" : "ẨN")}</color>");
                    }
                    else
                    {
                        config.forceShowMobileUI = !config.forceShowMobileUI;
                        Debug.Log($"<color=cyan><b>[VirtualRail] Phím {key}:</b> Đã chuyển đổi hiển thị Mobile UI trên PC -> {(config.forceShowMobileUI ? "HIỆN" : "ẨN")}</color>");
                    }
                }
            }
        }

        private void UpdateUIVisibility()
        {
            if (mobileUIRoot != null)
            {
                bool allowMobile = (config == null) || config.enableMobileUI;
                bool showMobile = allowMobile && (Application.isMobilePlatform || (config != null && config.forceShowMobileUI));
                if (mobileUIRoot.activeSelf != showMobile)
                {
                    mobileUIRoot.SetActive(showMobile);
                }
            }
        }

        /// <summary>
        /// Tự động tìm hoặc khởi tạo toàn bộ cụm điều khiển cảm ứng Android theo Bố cục 4 ngón công thái học Tinh gọn (Refined 4-Finger Claw).
        /// Bao gồm:
        /// - Góc dưới trái (Bottom-Left): Cần gạt di chuyển thân tàu (Left Joystick).
        /// - Góc trên trái (Top-Left Shoulder): Cụm 2 nút lộn vòng phản đạn (Roll Left & Roll Right).
        /// - Góc dưới phải (Bottom-Right): Cần gạt ngắm 3D độc lập (Right Aim Stick) kiêm Auto-Fire xả đạn liên thanh khi kéo cần.
        /// - Góc trên phải (Top-Right Shoulder): Nút Sạc hỏa lực tụ lực riêng biệt (Charge Shot) & Nút Bom nguyên tử (Smart Bomb).
        /// </summary>
        public void EnsureMobileUI()
        {
            // Kiểm tra xem đã có đầy đủ UI và Sprite chuẩn vòng trắng hay chưa
            if (leftJoystick != null && rightJoystick != null && btnCharge != null && btnBomb != null && btnRollLeft != null && btnRollRight != null &&
                leftJoystick.bgImage != null && leftJoystick.bgImage.sprite != null &&
                rightJoystick.bgImage != null && rightJoystick.bgImage.sprite != null)
            {
                return;
            }

            // 1. Tìm Canvas trong rig hoặc ngoài scene
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Đảm bảo canvas hiển thị đè lên trên cùng HUD
            canvas.sortingOrder = 100;

            // 2. Tìm hoặc khởi tạo Root GameObject
            if (mobileUIRoot == null)
            {
                Transform existingRoot = canvas.transform.Find("MobileTouchControls");
                if (existingRoot != null)
                {
                    mobileUIRoot = existingRoot.gameObject;
                }
                else
                {
                    GameObject mobileRoot = new GameObject("MobileTouchControls");
                    mobileRoot.transform.SetParent(canvas.transform, false);
                    RectTransform mobileRootRt = mobileRoot.AddComponent<RectTransform>();
                    mobileRootRt.anchorMin = Vector2.zero;
                    mobileRootRt.anchorMax = Vector2.one;
                    mobileRootRt.offsetMin = Vector2.zero;
                    mobileRootRt.offsetMax = Vector2.zero;
                    mobileUIRoot = mobileRoot;
                }
            }

            // Tải Sprites và Fonts từ Resources
            Sprite baseRing = Resources.Load<Sprite>("UI/ui_touch_ring_base");
            Sprite handleRing = Resources.Load<Sprite>("UI/ui_touch_ring_handle");
            Sprite arcRing = Resources.Load<Sprite>("UI/ui_touch_ring_arc");
            Sprite rollIcon = Resources.Load<Sprite>("UI/ui_touch_icon_roll");
            Sprite laserIcon = Resources.Load<Sprite>("HUD/hud_icon_twin_laser");
            Sprite bombIcon = Resources.Load<Sprite>("HUD/hud_icon_smartbomb");
            Font scifiFont = Resources.Load<Font>("Fonts/HUD_SciFi_Orbitron") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            float handleRange = (config != null) ? config.joystickHandleRange : 70f;
            float deadZone = (config != null) ? config.joystickDeadZone : 0.1f;

            // Ẩn cụm nút cũ (RightActionCluster) nếu tồn tại để tránh xung đột
            Transform oldActionCluster = mobileUIRoot.transform.Find("RightActionCluster");
            if (oldActionCluster != null)
            {
                oldActionCluster.gameObject.SetActive(false);
            }

            // 3. Khởi tạo / Nâng cấp LEFT JOYSTICK (Cần gạt di chuyển thân tàu - Góc dưới trái)
            Transform leftZoneTrans = mobileUIRoot.transform.Find("LeftTouchZone");
            GameObject leftZone = leftZoneTrans != null ? leftZoneTrans.gameObject : null;
            if (leftZone == null)
            {
                leftZone = new GameObject("LeftTouchZone");
                leftZone.transform.SetParent(mobileUIRoot.transform, false);
                RectTransform leftZoneRt = leftZone.AddComponent<RectTransform>();
                leftZoneRt.anchorMin = new Vector2(0f, 0f);
                leftZoneRt.anchorMax = new Vector2(0.5f, 0.75f);
                leftZoneRt.offsetMin = Vector2.zero;
                leftZoneRt.offsetMax = Vector2.zero;

                Image leftZoneImg = leftZone.AddComponent<Image>();
                leftZoneImg.color = new Color(0f, 0f, 0f, 0.001f);
                leftZoneImg.raycastTarget = true;

                leftJoystick = leftZone.AddComponent<VirtualJoystick>();
                leftJoystick.handleRange = handleRange;
                leftJoystick.deadZone = deadZone;
                leftJoystick.isDynamicFloating = true;

                // Background
                GameObject leftBg = new GameObject("LeftJoyBackground");
                leftBg.transform.SetParent(leftZone.transform, false);
                RectTransform leftBgRt = leftBg.AddComponent<RectTransform>();
                leftBgRt.anchorMin = new Vector2(0f, 0f);
                leftBgRt.anchorMax = new Vector2(0f, 0f);
                leftBgRt.anchoredPosition = new Vector2(170f, 170f);
                leftBgRt.sizeDelta = new Vector2(180f, 180f);
                Image leftBgImg = leftBg.AddComponent<Image>();
                leftBgImg.sprite = baseRing;
                leftBgImg.color = Color.white;
                leftBgImg.raycastTarget = false;

                // Handle
                GameObject leftHandle = new GameObject("LeftJoyHandle");
                leftHandle.transform.SetParent(leftBg.transform, false);
                RectTransform leftHandleRt = leftHandle.AddComponent<RectTransform>();
                leftHandleRt.sizeDelta = new Vector2(76f, 76f);
                Image leftHandleImg = leftHandle.AddComponent<Image>();
                leftHandleImg.sprite = handleRing;
                leftHandleImg.color = Color.white;
                leftHandleImg.raycastTarget = false;

                leftJoystick.background = leftBgRt;
                leftJoystick.handle = leftHandleRt;
                leftJoystick.bgImage = leftBgImg;
                leftJoystick.handleImage = leftHandleImg;
            }
            else
            {
                leftJoystick = leftZone.GetComponent<VirtualJoystick>();
                if (leftJoystick == null) leftJoystick = leftZone.AddComponent<VirtualJoystick>();

                Transform leftBgTrans = leftZone.transform.Find("LeftJoyBackground");
                if (leftBgTrans != null)
                {
                    RectTransform rt = (RectTransform)leftBgTrans;
                    rt.sizeDelta = new Vector2(180f, 180f);
                    Image img = leftBgTrans.GetComponent<Image>();
                    if (img != null)
                    {
                        if (baseRing != null) img.sprite = baseRing;
                        img.color = Color.white;
                    }
                    leftJoystick.background = rt;
                    leftJoystick.bgImage = img;

                    Transform leftHandleTrans = leftBgTrans.Find("LeftJoyHandle");
                    if (leftHandleTrans != null)
                    {
                        RectTransform hRt = (RectTransform)leftHandleTrans;
                        hRt.sizeDelta = new Vector2(76f, 76f);
                        Image hImg = leftHandleTrans.GetComponent<Image>();
                        if (hImg != null)
                        {
                            if (handleRing != null) hImg.sprite = handleRing;
                            hImg.color = Color.white;
                        }
                        leftJoystick.handle = hRt;
                        leftJoystick.handleImage = hImg;
                    }
                }
            }
            if (leftJoystick != null)
            {
                leftJoystick.SyncVisualElements();
                leftJoystick.ApplyAlpha(0.35f);
            }

            // 4. Khởi tạo MID-LEFT ROLL CLUSTER (Cụm 2 nút lộn vòng phản đạn - Giữa mép trái 50% màn hình)
            Transform rollClusterTrans = mobileUIRoot.transform.Find("MidLeftRollCluster");
            if (rollClusterTrans == null) rollClusterTrans = mobileUIRoot.transform.Find("TopLeftRollCluster");
            GameObject rollCluster = rollClusterTrans != null ? rollClusterTrans.gameObject : null;
            if (rollCluster == null)
            {
                rollCluster = new GameObject("MidLeftRollCluster");
                rollCluster.transform.SetParent(mobileUIRoot.transform, false);
                RectTransform rollRt = rollCluster.AddComponent<RectTransform>();
                rollRt.anchorMin = new Vector2(0f, 0.5f);
                rollRt.anchorMax = new Vector2(0f, 0.5f);
                rollRt.pivot = new Vector2(0f, 0.5f);
                rollRt.anchoredPosition = Vector2.zero;
                rollRt.sizeDelta = new Vector2(240f, 130f);
            }
            else
            {
                rollCluster.name = "MidLeftRollCluster";
                RectTransform rollRt = rollCluster.GetComponent<RectTransform>();
                if (rollRt != null)
                {
                    rollRt.anchorMin = new Vector2(0f, 0.5f);
                    rollRt.anchorMax = new Vector2(0f, 0.5f);
                    rollRt.pivot = new Vector2(0f, 0.5f);
                    rollRt.anchoredPosition = Vector2.zero;
                    rollRt.sizeDelta = new Vector2(240f, 130f);
                }
            }

            if (btnRollLeft == null)
            {
                Transform t = rollCluster.transform.Find("Btn_RollLeft");
                btnRollLeft = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    rollCluster.transform,
                    "Btn_RollLeft",
                    new Vector2(0f, 0.5f),
                    new Vector2(65f, 0f),
                    new Vector2(64f, 64f),
                    baseRing,
                    rollIcon,
                    new Vector2(36f, 36f),
                    true, // Lật ngang icon hướng sang trái
                    false,
                    null,
                    false,
                    null
                );
            }
            if (btnRollLeft != null)
            {
                RectTransform rt = btnRollLeft.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(65f, 0f);
                }
            }

            if (btnRollRight == null)
            {
                Transform t = rollCluster.transform.Find("Btn_RollRight");
                btnRollRight = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    rollCluster.transform,
                    "Btn_RollRight",
                    new Vector2(0f, 0.5f),
                    new Vector2(145f, 0f),
                    new Vector2(64f, 64f),
                    baseRing,
                    rollIcon,
                    new Vector2(36f, 36f),
                    false,
                    false,
                    null,
                    false,
                    null
                );
            }
            if (btnRollRight != null)
            {
                RectTransform rt = btnRollRight.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(145f, 0f);
                }
            }

            // 5. Khởi tạo RIGHT AIM JOYSTICK (Cần gạt ngắm 3D độc lập kiêm Auto-Fire - Góc dưới phải)
            Transform rightAimTrans = mobileUIRoot.transform.Find("RightAimZone");
            GameObject rightAimZone = rightAimTrans != null ? rightAimTrans.gameObject : null;
            if (rightAimZone == null)
            {
                Transform oldRightTouch = mobileUIRoot.transform.Find("RightTouchZone");
                if (oldRightTouch != null) oldRightTouch.gameObject.SetActive(false);

                rightAimZone = new GameObject("RightAimZone");
                rightAimZone.transform.SetParent(mobileUIRoot.transform, false);
                RectTransform rightAimRt = rightAimZone.AddComponent<RectTransform>();
                rightAimRt.anchorMin = new Vector2(0.5f, 0f);
                rightAimRt.anchorMax = new Vector2(1f, 0.75f);
                rightAimRt.offsetMin = Vector2.zero;
                rightAimRt.offsetMax = Vector2.zero;

                Image rightAimImg = rightAimZone.AddComponent<Image>();
                rightAimImg.color = new Color(0f, 0f, 0f, 0.001f);
                rightAimImg.raycastTarget = true;

                rightJoystick = rightAimZone.AddComponent<VirtualJoystick>();
                rightJoystick.handleRange = handleRange;
                rightJoystick.deadZone = deadZone;
                rightJoystick.isDynamicFloating = true;

                // Background
                GameObject rightBg = new GameObject("RightJoyBackground");
                rightBg.transform.SetParent(rightAimZone.transform, false);
                RectTransform rightBgRt = rightBg.AddComponent<RectTransform>();
                rightBgRt.anchorMin = new Vector2(1f, 0f);
                rightBgRt.anchorMax = new Vector2(1f, 0f);
                rightBgRt.anchoredPosition = new Vector2(-170f, 170f);
                rightBgRt.sizeDelta = new Vector2(180f, 180f);
                Image rightBgImg = rightBg.AddComponent<Image>();
                rightBgImg.sprite = baseRing;
                rightBgImg.color = Color.white;
                rightBgImg.raycastTarget = false;

                // Handle
                GameObject rightHandle = new GameObject("RightJoyHandle");
                rightHandle.transform.SetParent(rightBg.transform, false);
                RectTransform rightHandleRt = rightHandle.AddComponent<RectTransform>();
                rightHandleRt.sizeDelta = new Vector2(76f, 76f);
                Image rightHandleImg = rightHandle.AddComponent<Image>();
                rightHandleImg.sprite = handleRing;
                rightHandleImg.color = Color.white;
                rightHandleImg.raycastTarget = false;

                rightJoystick.background = rightBgRt;
                rightJoystick.handle = rightHandleRt;
                rightJoystick.bgImage = rightBgImg;
                rightJoystick.handleImage = rightHandleImg;
            }
            else
            {
                rightJoystick = rightAimZone.GetComponent<VirtualJoystick>();
                if (rightJoystick == null) rightJoystick = rightAimZone.AddComponent<VirtualJoystick>();

                Transform rightBgTrans = rightAimZone.transform.Find("RightJoyBackground");
                if (rightBgTrans != null)
                {
                    RectTransform rt = (RectTransform)rightBgTrans;
                    rt.sizeDelta = new Vector2(180f, 180f);
                    Image img = rightBgTrans.GetComponent<Image>();
                    if (img != null)
                    {
                        if (baseRing != null) img.sprite = baseRing;
                        img.color = Color.white;
                    }
                    rightJoystick.background = rt;
                    rightJoystick.bgImage = img;

                    Transform rightHandleTrans = rightBgTrans.Find("RightJoyHandle");
                    if (rightHandleTrans != null)
                    {
                        RectTransform hRt = (RectTransform)rightHandleTrans;
                        hRt.sizeDelta = new Vector2(76f, 76f);
                        Image hImg = rightHandleTrans.GetComponent<Image>();
                        if (hImg != null)
                        {
                            if (handleRing != null) hImg.sprite = handleRing;
                            hImg.color = Color.white;
                        }
                        rightJoystick.handle = hRt;
                        rightJoystick.handleImage = hImg;
                    }
                }
            }
            if (rightJoystick != null)
            {
                rightJoystick.SyncVisualElements();
                rightJoystick.ApplyAlpha(0.35f);
            }

            // 6. Khởi tạo MID-RIGHT ACTION CLUSTER (Cụm Nút Sạc Hỏa Lực & Bom - Giữa mép phải 50% màn hình)
            Transform actionClusterTrans = mobileUIRoot.transform.Find("MidRightActionCluster");
            if (actionClusterTrans == null) actionClusterTrans = mobileUIRoot.transform.Find("TopRightActionCluster");
            GameObject actionCluster = actionClusterTrans != null ? actionClusterTrans.gameObject : null;
            if (actionCluster == null)
            {
                actionCluster = new GameObject("MidRightActionCluster");
                actionCluster.transform.SetParent(mobileUIRoot.transform, false);
                RectTransform clusterRt = actionCluster.AddComponent<RectTransform>();
                clusterRt.anchorMin = new Vector2(1f, 0.5f);
                clusterRt.anchorMax = new Vector2(1f, 0.5f);
                clusterRt.pivot = new Vector2(1f, 0.5f);
                clusterRt.anchoredPosition = Vector2.zero;
                clusterRt.sizeDelta = new Vector2(270f, 130f);
            }
            else
            {
                actionCluster.name = "MidRightActionCluster";
                RectTransform clusterRt = actionCluster.GetComponent<RectTransform>();
                if (clusterRt != null)
                {
                    clusterRt.anchorMin = new Vector2(1f, 0.5f);
                    clusterRt.anchorMax = new Vector2(1f, 0.5f);
                    clusterRt.pivot = new Vector2(1f, 0.5f);
                    clusterRt.anchoredPosition = Vector2.zero;
                    clusterRt.sizeDelta = new Vector2(270f, 130f);
                }
            }

            // Nút Sạc Tụ Lực Riêng Biệt (Dedicated Charge Shot Button)
            if (btnCharge == null)
            {
                Transform t = actionCluster.transform.Find("Btn_Charge");
                btnCharge = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_Charge",
                    new Vector2(1f, 0.5f),
                    new Vector2(-165f, 0f),
                    new Vector2(78f, 78f),
                    baseRing,
                    laserIcon,
                    new Vector2(44f, 44f),
                    false,
                    true, // Có vòng tiến trình nạp Arc Image
                    arcRing,
                    false,
                    null
                );
            }
            if (btnCharge != null)
            {
                RectTransform rt = btnCharge.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-165f, 0f);
                }
            }

            // Nút Bom thông minh (Smart Bomb)
            if (btnBomb == null)
            {
                Transform t = actionCluster.transform.Find("Btn_Bomb");
                btnBomb = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_Bomb",
                    new Vector2(1f, 0.5f),
                    new Vector2(-70f, 0f),
                    new Vector2(68f, 68f),
                    baseRing,
                    bombIcon,
                    new Vector2(40f, 40f),
                    false,
                    false,
                    null,
                    true, // Có huy hiệu số lượng bom
                    scifiFont
                );
            }
            if (btnBomb != null)
            {
                RectTransform rt = btnBomb.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-70f, 0f);
                }
            }

            // Đảm bảo có EventSystem trong scene
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private VirtualTouchButton CreateTouchButton(
            Transform parent,
            string name,
            Vector2 anchorMinMax,
            Vector2 anchoredPos,
            Vector2 size,
            Sprite ringSprite,
            Sprite iconSprite,
            Vector2 iconSize,
            bool flipIconX,
            bool hasChargeArc,
            Sprite arcSprite,
            bool hasBadge,
            Font badgeFont)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = anchorMinMax;
            btnRt.anchorMax = anchorMinMax;
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = anchoredPos;
            btnRt.sizeDelta = size;

            Image ringImg = btnObj.AddComponent<Image>();
            ringImg.sprite = ringSprite;
            ringImg.color = Color.white;
            ringImg.raycastTarget = true;

            VirtualTouchButton touchBtn = btnObj.AddComponent<VirtualTouchButton>();
            touchBtn.ringImage = ringImg;

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);

                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = iconSize;
                if (flipIconX)
                {
                    iconRt.localScale = new Vector3(-1f, 1f, 1f);
                }

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;

                touchBtn.iconImage = iconImg;
            }

            if (hasChargeArc && arcSprite != null)
            {
                GameObject arcObj = new GameObject("ChargeArc");
                arcObj.transform.SetParent(btnObj.transform, false);

                RectTransform arcRt = arcObj.AddComponent<RectTransform>();
                arcRt.anchorMin = new Vector2(0.5f, 0.5f);
                arcRt.anchorMax = new Vector2(0.5f, 0.5f);
                arcRt.anchoredPosition = Vector2.zero;
                arcRt.sizeDelta = size * 1.14f;

                Image arcImg = arcObj.AddComponent<Image>();
                arcImg.sprite = arcSprite;
                arcImg.type = Image.Type.Filled;
                arcImg.fillMethod = Image.FillMethod.Radial360;
                arcImg.fillOrigin = (int)Image.Origin360.Top;
                arcImg.fillClockwise = true;
                arcImg.fillAmount = 0f;
                arcImg.color = new Color(0.2f, 1f, 0.45f, 0.95f);
                arcImg.raycastTarget = false;

                touchBtn.chargeArcImage = arcImg;
                arcObj.SetActive(false);
            }

            if (hasBadge)
            {
                GameObject badgeObj = new GameObject("BadgeText");
                badgeObj.transform.SetParent(btnObj.transform, false);

                RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
                badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(0f, -size.y * 0.32f);
                badgeRt.sizeDelta = new Vector2(36f, 18f);

                Text badge = badgeObj.AddComponent<Text>();
                badge.font = badgeFont != null ? badgeFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
                badge.fontSize = 12;
                badge.fontStyle = FontStyle.Bold;
                badge.alignment = TextAnchor.MiddleCenter;
                badge.color = new Color(1f, 0.88f, 0.2f, 1f);
                badge.raycastTarget = false;
                badge.text = "x3";

                Outline outline = badgeObj.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1f, -1f);

                touchBtn.badgeText = badge;
            }

            return touchBtn;
        }

        public bool IsLeftStickActive => leftJoystick != null && leftJoystick.IsHeld;
        public bool IsRightStickActive => rightJoystick != null && rightJoystick.IsHeld;

        /// <summary>
        /// Lấy vector di chuyển của thân tàu [-1, 1].
        /// Ưu tiên Cần gạt trái (Mobile Touch), fallback về Bàn phím/Gamepad PC (Horizontal/Vertical).
        /// </summary>
        public Vector2 GetShipMovement()
        {
            if (IsLeftStickActive)
            {
                return leftJoystick.InputVector;
            }

            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        /// <summary>
        /// Lấy vector di chuyển của tâm ngắm 3D từ Cần gạt phải (Legacy).
        /// </summary>
        public Vector2 GetAimMovement()
        {
            if (IsRightStickActive)
            {
                return rightJoystick.InputVector;
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Tín hiệu bắt đầu nhấn nút Bắn Laser trong frame này (Trigger nhấp tap tức thời).
        /// </summary>
        public bool IsFireDown()
        {
            return (btnFire != null && btnFire.WasPressedThisFrame);
        }

        /// <summary>
        /// Tín hiệu đang nhấn giữ nút Bắn Laser (Duy trì nạp tụ lực Hold-to-Charge).
        /// Tự động tắt nếu đang nhấn giữ nút Sạc Hỏa Lực riêng biệt.
        /// </summary>
        public bool IsFireHeld()
        {
            if (IsChargeHeld()) return false;
            if (btnFire != null && btnFire.IsHeld) return true;
            if (IsRightStickActive) return true;
            return false;
        }

        /// <summary>
        /// Tín hiệu nhả nút Bắn Laser trong frame này (Phóng đạn tụ lực Plasma).
        /// </summary>
        public bool IsFireUp()
        {
            return (btnFire != null && btnFire.WasReleasedThisFrame);
        }

        /// <summary>
        /// Tín hiệu bắt đầu nhấn nút Sạc Tụ Lực Riêng Biệt (Dedicated Charge Shot) trong frame này.
        /// </summary>
        public bool IsChargeDown()
        {
            return (btnCharge != null && btnCharge.WasPressedThisFrame);
        }

        /// <summary>
        /// Tín hiệu đang nhấn giữ nút Sạc Tụ Lực Riêng Biệt (Duy trì nạp tụ lực Hold-to-Charge).
        /// </summary>
        public bool IsChargeHeld()
        {
            return (btnCharge != null && btnCharge.IsHeld);
        }

        /// <summary>
        /// Tín hiệu nhả nút Sạc Tụ Lực Riêng Biệt trong frame này (Phóng đạn Plasma tầm nhiệt).
        /// </summary>
        public bool IsChargeUp()
        {
            return (btnCharge != null && btnCharge.WasReleasedThisFrame);
        }

        /// <summary>
        /// Tương thích ngược: Kiểm tra tín hiệu bắn đạn.
        /// </summary>
        public bool IsFiring()
        {
            return IsFireHeld();
        }

        /// <summary>
        /// Kiểm tra tín hiệu Tăng tốc (Boost).
        /// </summary>
        public bool IsBoosting()
        {
            return (btnBoost != null && btnBoost.IsHeld);
        }

        /// <summary>
        /// Kiểm tra tín hiệu Hãm phanh (Brake).
        /// </summary>
        public bool IsBraking()
        {
            return (btnBrake != null && btnBrake.IsHeld);
        }

        /// <summary>
        /// Kiểm tra tín hiệu Phóng Bom Nguyên Tử (Smart Bomb) trong frame này.
        /// </summary>
        public bool IsBombTriggered()
        {
            return (btnBomb != null && btnBomb.WasPressedThisFrame);
        }

        /// <summary>
        /// Kiểm tra tín hiệu Lộn vòng (Barrel Roll): Trả về -1f (Trái), 1f (Phải), 0f (Không có).
        /// </summary>
        public float GetBarrelRollTrigger()
        {
            if (btnRollLeft != null && btnRollLeft.WasPressedThisFrame) return -1f;
            if (btnRollRight != null && btnRollRight.WasPressedThisFrame) return 1f;
            return 0f;
        }

        /// <summary>
        /// Kiểm tra xem có đang nhận tín hiệu điều khiển tâm ngắm từ Cần gạt phải hay không (Legacy).
        /// </summary>
        public bool IsAimingMoving()
        {
            if (IsRightStickActive)
            {
                return rightJoystick.InputVector.sqrMagnitude > 0.01f;
            }

            return false;
        }
    }
}
