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
        [Tooltip("Nút Bắn Laser & Tụ lực (Primary Fire / Charge)")]
        public VirtualTouchButton btnFire;
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
            if (btnFire != null)
            {
                if (_cachedAimAssist == null) CacheModules();
                float charge = (_cachedAimAssist != null && _cachedAimAssist.IsCharging) ? _cachedAimAssist.ChargeProgress : 0f;
                btnFire.SetChargeProgress(charge);
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
        /// Tự động tìm hoặc khởi tạo toàn bộ cụm điều khiển cảm ứng Android theo phong cách 'Vòng trắng tối giản'.
        /// Bao gồm Cần gạt trái và Cụm 5 nút hành động (Bắn/Tụ lực, Bom, Boost, Phanh, Barrel Roll L/R).
        /// </summary>
        public void EnsureMobileUI()
        {
            // Kiểm tra xem đã có đầy đủ UI chưa
            if (leftJoystick != null && btnFire != null && btnBomb != null && btnBoost != null && btnBrake != null && btnRollLeft != null && btnRollRight != null)
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
            Sprite boostIcon = Resources.Load<Sprite>("UI/ui_touch_icon_boost");
            Sprite brakeIcon = Resources.Load<Sprite>("UI/ui_touch_icon_brake");
            Sprite rollIcon = Resources.Load<Sprite>("UI/ui_touch_icon_roll");
            Sprite laserIcon = Resources.Load<Sprite>("HUD/hud_icon_twin_laser");
            Sprite bombIcon = Resources.Load<Sprite>("HUD/hud_icon_smartbomb");
            Font scifiFont = Resources.Load<Font>("Fonts/HUD_SciFi_Orbitron") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            float handleRange = (config != null) ? config.joystickHandleRange : 70f;
            float deadZone = (config != null) ? config.joystickDeadZone : 0.1f;

            // 3. Khởi tạo / Nâng cấp LEFT JOYSTICK (Cần gạt di chuyển thân tàu)
            if (leftJoystick == null)
            {
                Transform leftZoneTrans = mobileUIRoot.transform.Find("LeftTouchZone");
                GameObject leftZone = leftZoneTrans != null ? leftZoneTrans.gameObject : null;
                if (leftZone == null)
                {
                    leftZone = new GameObject("LeftTouchZone");
                    leftZone.transform.SetParent(mobileUIRoot.transform, false);
                    RectTransform leftZoneRt = leftZone.AddComponent<RectTransform>();
                    leftZoneRt.anchorMin = new Vector2(0f, 0f);
                    leftZoneRt.anchorMax = new Vector2(0.5f, 0.85f);
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
                    leftBgRt.anchoredPosition = new Vector2(180f, 180f);
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
                    if (leftJoystick != null)
                    {
                        if (leftJoystick.bgImage != null && baseRing != null) leftJoystick.bgImage.sprite = baseRing;
                        if (leftJoystick.handleImage != null && handleRing != null) leftJoystick.handleImage.sprite = handleRing;
                    }
                }
            }

            // 4. Khởi tạo RIGHT ACTION CLUSTER (Cụm nút bấm cảm ứng vòng trắng tối giản)
            Transform clusterTrans = mobileUIRoot.transform.Find("RightActionCluster");
            GameObject actionCluster = clusterTrans != null ? clusterTrans.gameObject : null;
            if (actionCluster == null)
            {
                // Nếu có RightTouchZone cũ từ phiên bản trước, ẩn đi để nhường chỗ cho cụm nút mới
                Transform oldRightZone = mobileUIRoot.transform.Find("RightTouchZone");
                if (oldRightZone != null)
                {
                    oldRightZone.gameObject.SetActive(false);
                }

                actionCluster = new GameObject("RightActionCluster");
                actionCluster.transform.SetParent(mobileUIRoot.transform, false);
                RectTransform clusterRt = actionCluster.AddComponent<RectTransform>();
                clusterRt.anchorMin = new Vector2(1f, 0f);
                clusterRt.anchorMax = new Vector2(1f, 0f);
                clusterRt.pivot = new Vector2(1f, 0f);
                clusterRt.anchoredPosition = Vector2.zero;
                clusterRt.sizeDelta = new Vector2(450f, 380f);
            }

            // 4.1. Nút Bắn Laser & Tụ lực (Primary Fire / Charge)
            if (btnFire == null)
            {
                Transform t = actionCluster.transform.Find("Btn_Fire");
                btnFire = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_Fire",
                    new Vector2(-135f, 145f),
                    new Vector2(100f, 100f),
                    baseRing,
                    laserIcon,
                    new Vector2(54f, 54f),
                    false,
                    true,
                    arcRing,
                    false,
                    null
                );
            }

            // 4.2. Nút Bom thông minh (Smart Bomb)
            if (btnBomb == null)
            {
                Transform t = actionCluster.transform.Find("Btn_Bomb");
                btnBomb = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_Bomb",
                    new Vector2(-245f, 190f),
                    new Vector2(68f, 68f),
                    baseRing,
                    bombIcon,
                    new Vector2(40f, 40f),
                    false,
                    false,
                    null,
                    true,
                    scifiFont
                );
            }

            // 4.3. Nút Tăng tốc (Boost)
            if (btnBoost == null)
            {
                Transform t = actionCluster.transform.Find("Btn_Boost");
                btnBoost = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_Boost",
                    new Vector2(-135f, 260f),
                    new Vector2(64f, 64f),
                    baseRing,
                    boostIcon,
                    new Vector2(34f, 34f),
                    false,
                    false,
                    null,
                    false,
                    null
                );
            }

            // 4.4. Nút Hãm phanh (Brake)
            if (btnBrake == null)
            {
                Transform t = actionCluster.transform.Find("Btn_Brake");
                btnBrake = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_Brake",
                    new Vector2(-135f, 45f),
                    new Vector2(64f, 64f),
                    baseRing,
                    brakeIcon,
                    new Vector2(34f, 34f),
                    false,
                    false,
                    null,
                    false,
                    null
                );
            }

            // 4.5. Nút Lộn vòng sang trái (Roll Left)
            if (btnRollLeft == null)
            {
                Transform t = actionCluster.transform.Find("Btn_RollLeft");
                btnRollLeft = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_RollLeft",
                    new Vector2(-245f, 95f),
                    new Vector2(58f, 58f),
                    baseRing,
                    rollIcon,
                    new Vector2(32f, 32f),
                    true, // Lật ngang icon hướng sang trái
                    false,
                    null,
                    false,
                    null
                );
            }

            // 4.6. Nút Lộn vòng sang phải (Roll Right)
            if (btnRollRight == null)
            {
                Transform t = actionCluster.transform.Find("Btn_RollRight");
                btnRollRight = (t != null) ? t.GetComponent<VirtualTouchButton>() : CreateTouchButton(
                    actionCluster.transform,
                    "Btn_RollRight",
                    new Vector2(-38f, 145f),
                    new Vector2(58f, 58f),
                    baseRing,
                    rollIcon,
                    new Vector2(32f, 32f),
                    false, // Giữ nguyên hướng sang phải
                    false,
                    null,
                    false,
                    null
                );
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
            btnRt.anchorMin = new Vector2(1f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
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
        /// </summary>
        public bool IsFireHeld()
        {
            if (btnFire != null && btnFire.IsHeld) return true;
            if (IsRightStickActive && rightJoystick.InputVector.sqrMagnitude > 0.01f) return true;
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
