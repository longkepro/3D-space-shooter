using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Component chuyên trách hiển thị Khung Đàm Thoại Tác Chiến Bộ Đàm (Tactical Radio Comms HUD).
    /// Đặt tại góc dưới bên trái buồng lái, tuân thủ 100% Unidirectional Data Flow và 0 GC Allocation.
    /// Tự động lắng nghe sự kiện từ VirtualRailStageDirector và hiển thị Avatar phi công + phụ đề chiến thuật.
    /// </summary>
    [DisallowMultipleComponent]
    public class RadioCommsHUD : MonoBehaviour
    {
        public static RadioCommsHUD Instance { get; private set; }

        [Header("UI References")]
        public CanvasGroup commsCanvasGroup;
        public RectTransform commsRoot;
        public Image commsBackground;
        public Image avatarImage;
        public Text speakerText;
        public Text messageText;
        public Image signalWaveImage;

        [Header("Timing & Animation")]
        public float fadeInSpeed = 6.0f;
        public float fadeOutSpeed = 3.5f;

        private float _displayTimer = 0f;
        private float _targetAlpha = 0f;
        private bool _isDisplaying = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (commsCanvasGroup != null)
            {
                commsCanvasGroup.alpha = 0f;
                commsCanvasGroup.blocksRaycasts = false;
                commsCanvasGroup.interactable = false;
            }
        }

        private void OnEnable()
        {
            VirtualRailStageDirector.OnRadioTransmission += HandleRadioTransmission;
        }

        private void OnDisable()
        {
            VirtualRailStageDirector.OnRadioTransmission -= HandleRadioTransmission;
        }

        private void Update()
        {
            if (commsCanvasGroup == null) return;

            if (_isDisplaying)
            {
                _displayTimer -= Time.deltaTime;
                if (_displayTimer <= 0f)
                {
                    _isDisplaying = false;
                    _targetAlpha = 0f;
                }
                else
                {
                    _targetAlpha = 1f;
                }
            }
            else
            {
                _targetAlpha = 0f;
            }

            // Alpha Lerp mượt mà (0 GC Allocation)
            float speed = (_targetAlpha > commsCanvasGroup.alpha) ? fadeInSpeed : fadeOutSpeed;
            commsCanvasGroup.alpha = Mathf.MoveTowards(commsCanvasGroup.alpha, _targetAlpha, Time.deltaTime * speed);

            // Hiệu ứng sóng radio nhấp nháy khi đang phát đàm thoại
            if (signalWaveImage != null && commsCanvasGroup.alpha > 0.05f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f);
                Color c = signalWaveImage.color;
                c.a = pulse;
                signalWaveImage.color = c;
            }
        }

        private void HandleRadioTransmission(string speaker, string message, float duration)
        {
            var anchor = VirtualRailAnchor.Instance;
            var cfg = (anchor != null) ? anchor.config : null;
            if (cfg != null && !cfg.enableRadioComms) return;

            ShowTransmission(speaker, message, duration);
        }

        public void ShowTransmission(string speaker, string message, float duration = 3.5f)
        {
            if (speakerText != null)
            {
                speakerText.text = string.IsNullOrEmpty(speaker) ? "[WING COMMAND]" : speaker;
            }

            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }

            _displayTimer = Mathf.Max(1.0f, duration);
            _isDisplaying = true;
            _targetAlpha = 1f;
        }

        public void HideImmediately()
        {
            _isDisplaying = false;
            _targetAlpha = 0f;
            if (commsCanvasGroup != null)
            {
                commsCanvasGroup.alpha = 0f;
            }
        }
    }
}
