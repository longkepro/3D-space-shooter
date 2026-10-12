using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Component Nút Bấm Cảm Ứng Di Động (Virtual Touch Button) chuẩn AAA.
    /// Thiết kế theo phong cách 'Vòng trắng tối giản' (Minimalist White Ring).
    /// Hỗ trợ cả thao tác Nhấn nhấp (Tap) lẫn Nhấn giữ (Hold to Charge/Boost).
    /// Đảm bảo 100% 0 GC Heap Allocation trong toàn bộ vòng đời Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("UI Visual Elements")]
        [Tooltip("Ảnh vòng viền ngoài chính")]
        public Image ringImage;
        [Tooltip("Ảnh icon bên trong nút")]
        public Image iconImage;
        [Tooltip("Vòng tiến trình nạp tụ lực (dành cho nút Bắn Laser)")]
        public Image chargeArcImage;
        [Tooltip("Nhãn hiển thị số lượng (dành cho nút Smart Bomb x3)")]
        public Text badgeText;

        [Header("Appearance & Alpha")]
        [Range(0f, 1f)] public float idleAlpha = 0.35f;
        [Range(0f, 1f)] public float pressedAlpha = 0.85f;
        public float animSpeed = 14f;

        [Header("Runtime State")]
        [SerializeField] private bool _isHeld = false;
        private bool _wasPressedThisFrame = false;
        private bool _wasReleasedThisFrame = false;
        private float _pressStartTime = 0f;
        private float _currentAlpha = 0.35f;
        private Vector3 _defaultScale = Vector3.one;

        public bool IsHeld => _isHeld;
        public bool WasPressedThisFrame => _wasPressedThisFrame;
        public bool WasReleasedThisFrame => _wasReleasedThisFrame;
        public float HoldDuration => _isHeld ? (Time.time - _pressStartTime) : 0f;

        private void Awake()
        {
            _currentAlpha = idleAlpha;
            _defaultScale = transform.localScale;
            ApplyAlpha(_currentAlpha);
        }

        private void Update()
        {
            // Chuyển đổi mượt mà độ mờ (Alpha) và tỉ lệ kích thước (Scale) khi nhấn (0 GC Alloc)
            float targetAlpha = _isHeld ? pressedAlpha : idleAlpha;
            if (Mathf.Abs(_currentAlpha - targetAlpha) > 0.001f)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, animSpeed * Time.unscaledDeltaTime);
                ApplyAlpha(_currentAlpha);
            }

            Vector3 targetScale = _isHeld ? (_defaultScale * 0.92f) : _defaultScale;
            if ((transform.localScale - targetScale).sqrMagnitude > 0.0001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, animSpeed * Time.unscaledDeltaTime);
            }
        }

        private void LateUpdate()
        {
            // Reset các cờ 1-frame ở cuối frame (sau khi toàn bộ các hàm Update() của mọi script trong game đã đọc xong)
            _wasPressedThisFrame = false;
            _wasReleasedThisFrame = false;
        }

        /// <summary>
        /// Tiêu thụ sự kiện nhấn tức thì (0ms input lag) - trả về true nếu vừa nhấn trong frame này và tự động xóa cờ.
        /// </summary>
        public bool ConsumePress()
        {
            if (_wasPressedThisFrame)
            {
                _wasPressedThisFrame = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tiêu thụ sự kiện nhả nút tức thì - trả về true nếu vừa nhả trong frame này và tự động xóa cờ.
        /// </summary>
        public bool ConsumeRelease()
        {
            if (_wasReleasedThisFrame)
            {
                _wasReleasedThisFrame = false;
                return true;
            }
            return false;
        }

        private void ApplyAlpha(float alpha)
        {
            if (ringImage != null)
            {
                Color c = ringImage.color;
                c.a = alpha;
                ringImage.color = c;
            }

            if (iconImage != null)
            {
                Color c = iconImage.color;
                c.a = alpha * 1.15f; // Icon hiển thị rõ nét hơn viền một chút
                iconImage.color = c;
            }
        }

        public void SetChargeProgress(float progress01)
        {
            if (chargeArcImage != null)
            {
                chargeArcImage.fillAmount = Mathf.Clamp01(progress01);
                chargeArcImage.gameObject.SetActive(progress01 > 0.01f);
            }
        }

        public void SetBadge(string text)
        {
            if (badgeText != null)
            {
                badgeText.text = text;
                badgeText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isHeld = true;
            _wasPressedThisFrame = true;
            _pressStartTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isHeld)
            {
                _isHeld = false;
                _wasReleasedThisFrame = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Khi ngón tay trượt ra ngoài vùng nút thì hủy nhấn giữ
            if (_isHeld)
            {
                _isHeld = false;
                _wasReleasedThisFrame = true;
            }
        }
    }
}
