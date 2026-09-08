using UnityEngine;

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

        private void Start()
        {
            if (config == null)
            {
                var anchor = GetComponentInParent<VirtualRailAnchor>();
                if (anchor != null) config = anchor.config;
            }

            if (mobileUIRoot != null)
            {
                bool showMobile = Application.isMobilePlatform || (config != null && config.forceShowMobileUI);
                mobileUIRoot.SetActive(showMobile);
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
