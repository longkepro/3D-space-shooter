using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Bề mặt Vuốt Ngắm Cảm Ứng Di Động (Mobile Touch Drag Aim Pad).
    /// Thay thế cần gạt ảo cố định bằng cơ chế vuốt tự do (Touch Drag Delta) chuẩn mực cho game bắn súng mobile.
    /// - Không có quán tính trôi lố (Zero Overshoot): Kéo ngón tay đến đâu, tâm ngắm đi đến đó; dừng ngón tay thì tâm ngắm dừng lại.
    /// - Không kẹt vị trí: Chạm vào bất cứ đâu trong nửa phải màn hình để ngắm.
    /// - Cô lập Pointer ID: Tránh nuốt hoặc xung đột sự kiện với ngón tay bấm nút Sạc hoặc Bom.
    /// - 0 GC Alloc / frame: Tối ưu hiệu năng tuyệt đối.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualTouchAimPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Visual Feedback")]
        public RectTransform touchIndicator;
        public Image indicatorImage;
        [Range(0f, 1f)] public float idleAlpha = 0.0f;
        [Range(0f, 1f)] public float activeAlpha = 0.45f;

        [Header("Runtime State")]
        [SerializeField] private bool isHeld = false;
        [SerializeField] private Vector2 currentDelta = Vector2.zero;

        private int _activePointerId = -1;
        private Vector2 _lastPointerPosition;
        private Vector2 _accumulatedDelta;
        private float _currentAlpha = 0f;
        private Camera _uiCamera;
        private Canvas _parentCanvas;

        public bool IsHeld => isHeld;
        public Vector2 CurrentDelta => currentDelta;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _parentCanvas.worldCamera;
            }

            SyncVisualElements();
            ApplyAlpha(idleAlpha);
        }

        public void SyncVisualElements()
        {
            if (touchIndicator != null && indicatorImage == null)
            {
                indicatorImage = touchIndicator.GetComponent<Image>();
            }
        }

        private void Update()
        {
            float targetAlpha = isHeld ? activeAlpha : idleAlpha;
            if (Mathf.Abs(_currentAlpha - targetAlpha) > 0.001f)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, 8f * Time.unscaledDeltaTime);
                ApplyAlpha(_currentAlpha);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            _currentAlpha = alpha;
            if (indicatorImage != null)
            {
                Color c = indicatorImage.color;
                c.a = alpha;
                indicatorImage.color = c;
            }
            if (touchIndicator != null)
            {
                touchIndicator.gameObject.SetActive(alpha > 0.01f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isHeld) return; // Đã có 1 ngón tay đang ngắm, không cho ngón thứ 2 cướp quyền

            _activePointerId = eventData.pointerId;
            isHeld = true;
            _lastPointerPosition = eventData.position;
            _accumulatedDelta = Vector2.zero;
            currentDelta = Vector2.zero;

            if (touchIndicator != null)
            {
                RectTransform parentRt = touchIndicator.parent as RectTransform;
                if (parentRt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    eventData.position,
                    _uiCamera,
                    out Vector2 localPoint))
                {
                    touchIndicator.anchoredPosition = localPoint;
                }
            }

            ApplyAlpha(activeAlpha);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isHeld || eventData.pointerId != _activePointerId) return;

            Vector2 currentPos = eventData.position;
            Vector2 delta = currentPos - _lastPointerPosition;
            _accumulatedDelta += delta;
            _lastPointerPosition = currentPos;

            if (touchIndicator != null)
            {
                RectTransform parentRt = touchIndicator.parent as RectTransform;
                if (parentRt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    currentPos,
                    _uiCamera,
                    out Vector2 localPoint))
                {
                    touchIndicator.anchoredPosition = localPoint;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            isHeld = false;
            _activePointerId = -1;
            _accumulatedDelta = Vector2.zero;
            currentDelta = Vector2.zero;
        }

        /// <summary>
        /// Trả về và reset lượng delta di chuyển của ngón tay trong frame này (pixels).
        /// </summary>
        public Vector2 ConsumeDelta()
        {
            currentDelta = _accumulatedDelta;
            _accumulatedDelta = Vector2.zero;
            return currentDelta;
        }

        private void OnDisable()
        {
            isHeld = false;
            _activePointerId = -1;
            _accumulatedDelta = Vector2.zero;
            currentDelta = Vector2.zero;
            ApplyAlpha(idleAlpha);
        }
    }
}
