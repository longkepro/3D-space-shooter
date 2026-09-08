using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Module Đọc Input Tập Trung (Centralized Input Provider / Hub).
    /// Phân tách hoàn toàn tầng thiết bị phần cứng (Phím, Chuột, Cần gạt ảo Android)
    /// khỏi tầng logic gameplay của Tàu (VirtualRailShip), Tâm ngắm (VirtualRailReticle) và Vũ khí (VirtualRailWeapon).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailInputReader : MonoBehaviour
    {
        [Header("Mobile Joysticks")]
        [Tooltip("Cần gạt ảo bên trái (Điều khiển thân tàu)")]
        public VirtualJoystick leftJoystick;
        [Tooltip("Cần gạt ảo bên phải (Điều khiển tâm ngắm & Tự động bắn)")]
        public VirtualJoystick rightJoystick;

        [Header("Config & UI References")]
        public VirtualRailConfig config;
        [Tooltip("Root GameObject chứa UI cảm ứng của 2 cần gạt")]
        public GameObject mobileUIRoot;

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
            UpdateUIVisibility();
        }

        private void Update()
        {
            UpdateUIVisibility();
        }

        private void UpdateUIVisibility()
        {
            if (mobileUIRoot != null)
            {
                bool showMobile = Application.isMobilePlatform || (config != null && config.forceShowMobileUI);
                if (mobileUIRoot.activeSelf != showMobile)
                {
                    mobileUIRoot.SetActive(showMobile);
                }
            }
        }

        /// <summary>
        /// Tự động tìm hoặc khởi tạo cụm 2 cần gạt ảo ngay tại runtime nếu prefab chưa có.
        /// </summary>
        public void EnsureMobileUI()
        {
            if (leftJoystick != null && rightJoystick != null) return;

            // 1. Thử tìm trong các đối tượng con
            var joysticks = GetComponentsInChildren<VirtualJoystick>(true);
            if (joysticks != null && joysticks.Length >= 2)
            {
                leftJoystick = joysticks[0];
                rightJoystick = joysticks[1];
                if (mobileUIRoot == null && leftJoystick.transform.parent != null)
                {
                    mobileUIRoot = leftJoystick.transform.parent.gameObject;
                }
                return;
            }

            // 2. Tìm Canvas trong rig hoặc ngoài scene
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Đảm bảo canvas hiển thị đè lên trên cùng
            canvas.sortingOrder = 100;

            // 3. Khởi tạo cụm MobileTouchControls tại runtime
            GameObject mobileRoot = new GameObject("MobileTouchControls");
            mobileRoot.transform.SetParent(canvas.transform, false);
            RectTransform mobileRootRt = mobileRoot.AddComponent<RectTransform>();
            mobileRootRt.anchorMin = Vector2.zero;
            mobileRootRt.anchorMax = Vector2.one;
            mobileRootRt.offsetMin = Vector2.zero;
            mobileRootRt.offsetMax = Vector2.zero;
            mobileUIRoot = mobileRoot;

            float handleRange = (config != null) ? config.joystickHandleRange : 65f;
            float deadZone = (config != null) ? config.joystickDeadZone : 0.1f;

            // --- LEFT JOYSTICK ---
            GameObject leftZone = new GameObject("LeftTouchZone");
            leftZone.transform.SetParent(mobileRoot.transform, false);
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
            leftJoystick.isDynamicFloating = false;

            GameObject leftBg = new GameObject("LeftJoyBackground");
            leftBg.transform.SetParent(leftZone.transform, false);
            RectTransform leftBgRt = leftBg.AddComponent<RectTransform>();
            leftBgRt.anchorMin = new Vector2(0f, 0f);
            leftBgRt.anchorMax = new Vector2(0f, 0f);
            leftBgRt.anchoredPosition = new Vector2(170f, 170f);
            leftBgRt.sizeDelta = new Vector2(160f, 160f);
            Image leftBgImg = leftBg.AddComponent<Image>();
            leftBgImg.color = new Color(0.1f, 0.7f, 1f, 0.5f);
            leftBgImg.raycastTarget = false;

            GameObject leftHandle = new GameObject("LeftJoyHandle");
            leftHandle.transform.SetParent(leftBg.transform, false);
            RectTransform leftHandleRt = leftHandle.AddComponent<RectTransform>();
            leftHandleRt.sizeDelta = new Vector2(70f, 70f);
            Image leftHandleImg = leftHandle.AddComponent<Image>();
            leftHandleImg.color = new Color(0.3f, 0.9f, 1f, 0.95f);
            leftHandleImg.raycastTarget = false;

            leftJoystick.background = leftBgRt;
            leftJoystick.handle = leftHandleRt;

            // --- RIGHT JOYSTICK ---
            GameObject rightZone = new GameObject("RightTouchZone");
            rightZone.transform.SetParent(mobileRoot.transform, false);
            RectTransform rightZoneRt = rightZone.AddComponent<RectTransform>();
            rightZoneRt.anchorMin = new Vector2(0.5f, 0f);
            rightZoneRt.anchorMax = new Vector2(1f, 0.85f);
            rightZoneRt.offsetMin = Vector2.zero;
            rightZoneRt.offsetMax = Vector2.zero;
            Image rightZoneImg = rightZone.AddComponent<Image>();
            rightZoneImg.color = new Color(0f, 0f, 0f, 0.001f);
            rightZoneImg.raycastTarget = true;
            rightJoystick = rightZone.AddComponent<VirtualJoystick>();
            rightJoystick.handleRange = handleRange;
            rightJoystick.deadZone = deadZone;
            rightJoystick.isDynamicFloating = false;

            GameObject rightBg = new GameObject("RightJoyBackground");
            rightBg.transform.SetParent(rightZone.transform, false);
            RectTransform rightBgRt = rightBg.AddComponent<RectTransform>();
            rightBgRt.anchorMin = new Vector2(1f, 0f);
            rightBgRt.anchorMax = new Vector2(1f, 0f);
            rightBgRt.anchoredPosition = new Vector2(-170f, 170f);
            rightBgRt.sizeDelta = new Vector2(160f, 160f);
            Image rightBgImg = rightBg.AddComponent<Image>();
            rightBgImg.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            rightBgImg.raycastTarget = false;

            GameObject rightHandle = new GameObject("RightJoyHandle");
            rightHandle.transform.SetParent(rightBg.transform, false);
            RectTransform rightHandleRt = rightHandle.AddComponent<RectTransform>();
            rightHandleRt.sizeDelta = new Vector2(70f, 70f);
            Image rightHandleImg = rightHandle.AddComponent<Image>();
            rightHandleImg.color = new Color(1f, 0.5f, 0.4f, 0.95f);
            rightHandleImg.raycastTarget = false;

            rightJoystick.background = rightBgRt;
            rightJoystick.handle = rightHandleRt;

            // Đảm bảo có EventSystem trong scene
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
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
        /// Lấy vector di chuyển của tâm ngắm 3D từ Cần gạt phải.
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
        /// Kiểm tra tín hiệu bắn đạn (Auto-fire khi gạt cần ngắm hoặc Bấm nút thủ công).
        /// </summary>
        public bool IsFiring()
        {
            // 1. Tự động khai hỏa khi gạt cần phải (Right Joystick)
            if (IsRightStickActive && rightJoystick.InputVector.sqrMagnitude > 0.01f)
            {
                return true;
            }

            // 2. Khai hỏa thủ công qua bàn phím / chuột / gamepad
            if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem có đang nhận tín hiệu điều khiển tâm ngắm từ Cần gạt phải hay không.
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
