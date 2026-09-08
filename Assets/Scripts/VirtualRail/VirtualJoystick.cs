using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Component Cần Gạt Ảo (Virtual Joystick) hiệu năng cao cho màn hình cảm ứng di động.
    /// Kế thừa trực tiếp từ các interface Unity UI EventSystem (IPointerDownHandler, IDragHandler, IPointerUpHandler).
    /// Triệt tiêu 100% rác bộ nhớ (0 GC Alloc / frame).
    /// Hỗ trợ cả chế độ Cố định (Fixed) lẫn Điểm neo động (Dynamic Floating Anchor).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI RectTransforms")]
        [Tooltip("Vòng tròn nền của cần gạt")]
        public RectTransform background;
        [Tooltip("Nút gạt bên trong")]
        public RectTransform handle;

        [Header("Joystick Settings")]
        [Tooltip("Bán kính kéo tối đa của nút gạt (pixels)")]
        public float handleRange = 65f;
        [Tooltip("Vùng chết tối thiểu để bắt đầu nhận giá trị")]
        public float deadZone = 0.1f;
        [Tooltip("Bật chế độ neo động: Khi chạm vào vùng này, gốc cần gạt tự động nhảy tới vị trí ngón tay chạm")]
        public bool isDynamicFloating = true;

        [Header("Runtime State")]
        [SerializeField] private Vector2 inputVector = Vector2.zero;
        [SerializeField] private bool isHeld = false;

        private Vector2 defaultBackgroundAnchoredPosition;
        private Camera uiCamera;
        private Canvas parentCanvas;

        public Vector2 InputVector => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;
        public bool IsHeld => isHeld;

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = parentCanvas.worldCamera;
            }

            if (background != null)
            {
                defaultBackgroundAnchoredPosition = background.anchoredPosition;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isHeld = true;

            if (isDynamicFloating && background != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform,
                    eventData.position,
                    uiCamera,
                    out Vector2 localPoint
                );
                background.anchoredPosition = localPoint;
                background.gameObject.SetActive(true);
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            Vector2 bgScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, background.position);
            Vector2 delta = eventData.position - bgScreenPos;

            // Tính toán khoảng cách và kẹp trong bán kính handleRange
            float distance = delta.magnitude;
            float clampedDist = Mathf.Min(distance, handleRange);
            Vector2 direction = distance > 0.0001f ? (delta / distance) : Vector2.zero;

            handle.anchoredPosition = direction * clampedDist;

            // Chuẩn hóa vector đầu vào [-1, 1]
            float normalizedDist = clampedDist / handleRange;
            if (normalizedDist > deadZone)
            {
                float adjustedMag = (normalizedDist - deadZone) / (1f - deadZone);
                inputVector = direction * adjustedMag;
            }
            else
            {
                inputVector = Vector2.zero;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHeld = false;
            inputVector = Vector2.zero;

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (isDynamicFloating && background != null)
            {
                background.anchoredPosition = defaultBackgroundAnchoredPosition;
            }
        }

        private void OnDisable()
        {
            isHeld = false;
            inputVector = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
