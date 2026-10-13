using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Component tự động co giãn UI theo Vùng An Toàn của màn hình điện thoại (Screen Safe Area).
    /// Triệt tiêu 100% hiện tượng tai thỏ (Notch), camera đục lỗ (Punch-hole) và các góc bo tròn che khuất UI.
    /// Hoạt động 0 GC Heap Allocation, tự động phản hồi tức thì khi xoay màn hình (Landscape Left / Right).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // Kiểm tra xem Safe Area hoặc độ phân giải / hướng xoay có thay đổi không
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height ||
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            // Nếu chạy trên PC Editor hoặc màn hình chuẩn không có tai thỏ, bung đầy màn hình
            if (!Application.isMobilePlatform || Screen.width <= 0 || Screen.height <= 0)
            {
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.one;
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
