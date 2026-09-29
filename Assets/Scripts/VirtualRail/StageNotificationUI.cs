using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Component hiển thị thông báo chuyển màn và tiến trình chặng bay (Stage Notification & Progress HUD).
    /// Phong cách Arcade Star Fox 64: MISSION START, WARP SPEED ACTIVATED, STAGE CLEAR.
    /// Tuân thủ 0 GC Allocation trong vòng lặp Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class StageNotificationUI : MonoBehaviour
    {
        public static StageNotificationUI Instance { get; private set; }

        [Header("Banner UI Elements")]
        public CanvasGroup bannerCanvasGroup;
        public Text titleText;
        public Text subtitleText;

        [Header("Progress Bar Elements")]
        public Image progressBarFill;
        public Text stageNumberText;

        [Header("Display Timing")]
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private float _displayTimer = 0f;
        private float _displayTotalDuration = 3f;
        private bool _isShowingBanner = false;

        private static readonly string[] STAGE_NUM_STRINGS = new string[]
        {
            "STAGE 0", "STAGE 1", "STAGE 2", "STAGE 3", "STAGE 4", "STAGE 5"
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            if (!_isShowingBanner || bannerCanvasGroup == null) return;

            _displayTimer += Time.deltaTime;

            if (_displayTimer < fadeInDuration)
            {
                bannerCanvasGroup.alpha = Mathf.Clamp01(_displayTimer / Mathf.Max(0.01f, fadeInDuration));
            }
            else if (_displayTimer < _displayTotalDuration - fadeOutDuration)
            {
                bannerCanvasGroup.alpha = 1f;
            }
            else if (_displayTimer < _displayTotalDuration)
            {
                float fadeOutProgress = (_displayTimer - (_displayTotalDuration - fadeOutDuration)) / Mathf.Max(0.01f, fadeOutDuration);
                bannerCanvasGroup.alpha = Mathf.Clamp01(1f - fadeOutProgress);
            }
            else
            {
                bannerCanvasGroup.alpha = 0f;
                _isShowingBanner = false;
            }
        }

        /// <summary>
        /// Kích hoạt thông báo banner lớn trên màn hình.
        /// </summary>
        public void ShowNotification(string title, string subtitle, float duration = 3.5f)
        {
            if (titleText != null) titleText.text = title;
            if (subtitleText != null) subtitleText.text = subtitle;

            _displayTotalDuration = Mathf.Max(1.0f, duration);
            _displayTimer = 0f;
            _isShowingBanner = true;

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Cập nhật tiến trình chặng bay theo cự ly Z (0.0 đến 1.0).
        /// </summary>
        public void UpdateProgressBar(float progress01)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(progress01);
            }
        }

        /// <summary>
        /// Cập nhật số thứ tự màn chơi (0 GC String).
        /// </summary>
        public void SetStageNumber(int stageIndex)
        {
            if (stageNumberText != null)
            {
                int idx = Mathf.Clamp(stageIndex, 0, STAGE_NUM_STRINGS.Length - 1);
                stageNumberText.text = STAGE_NUM_STRINGS[idx];
            }
        }
    }
}
