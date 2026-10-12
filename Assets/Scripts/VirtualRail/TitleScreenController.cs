using System;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Bộ Điều Khiển Màn Hình Bìa Ngoài Cùng (Title Screen Controller).
    /// Quản lý giao diện Poster bìa Star Defender 3D, nút BẮT ĐẦU [START] và hiệu ứng nhấp nháy phát sáng (Pulsing Glow).
    /// Thiết kế theo kiến trúc 1 chiều, 0 GC Heap Allocation trong Game Loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class TitleScreenController : MonoBehaviour
    {
        public static TitleScreenController Instance { get; private set; }

        [Header("UI References")]
        public Image coverDisplayImage;
        public Sprite coverSprite;
        public Button btnStart;
        public Text txtPrompt;
        public CanvasGroup titleCanvasGroup;

        [Header("Audio SFX")]
        public AudioSFXType startSFX = AudioSFXType.EngineBoost;

        /// <summary>
        /// Sự kiện callback thông báo người chơi đã nhấn Start để vào Main Menu.
        /// </summary>
        public event Action OnTitleCompleted;

        private bool _isActive = false;
        private float _pulseTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (titleCanvasGroup == null)
            {
                titleCanvasGroup = GetComponent<CanvasGroup>();
            }

            // Tự động nạp Sprite từ Resources nếu chưa gán
            if (coverSprite == null)
            {
                coverSprite = Resources.Load<Sprite>("UI/title_screen_cover");
            }

            if (coverDisplayImage != null && coverSprite != null)
            {
                coverDisplayImage.sprite = coverSprite;
            }

            SetupButtons();
        }

        private void OnEnable()
        {
            _isActive = true;
            SetupButtons();
        }

        private void Start()
        {
            SetupButtons();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetupButtons()
        {
            if (btnStart != null)
            {
                btnStart.onClick.RemoveAllListeners();
                btnStart.onClick.AddListener(OnStartClicked);
            }
        }

        private void Update()
        {
            // Kiểm tra trạng thái hiển thị của Title Screen
            bool isVisible = (titleCanvasGroup != null && titleCanvasGroup.alpha > 0.01f) || _isActive;
            if (!isVisible) return;

            // Hiệu ứng nhấp nháy phát sáng chữ neon mượt mà (0 GC Allocation)
            _pulseTimer += Time.unscaledDeltaTime * 3.5f;
            if (txtPrompt != null)
            {
                float a = 0.5f + 0.5f * Mathf.Sin(_pulseTimer);
                Color c = txtPrompt.color;
                c.a = a;
                txtPrompt.color = c;
            }

            // Nhấn phím bất kỳ hoặc click chuột để BẮT ĐẦU
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                OnStartClicked();
            }
        }

        /// <summary>
        /// Kích hoạt màn hình Title Screen hiển thị lên trên cùng.
        /// </summary>
        public void StartTitleScreen()
        {
            _isActive = true;
            _pulseTimer = 0f;
            SetupButtons();

            if (coverDisplayImage != null && coverSprite != null)
            {
                coverDisplayImage.sprite = coverSprite;
            }

            if (titleCanvasGroup != null)
            {
                titleCanvasGroup.alpha = 1f;
                titleCanvasGroup.interactable = true;
                titleCanvasGroup.blocksRaycasts = true;
                titleCanvasGroup.gameObject.SetActive(true);
            }
        }

        public void OnStartClicked()
        {
            if (!_isActive && (titleCanvasGroup != null && titleCanvasGroup.alpha <= 0.01f)) return;
            _isActive = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(startSFX, 1.0f, 1.0f);
            }

            if (titleCanvasGroup != null)
            {
                titleCanvasGroup.alpha = 0f;
                titleCanvasGroup.interactable = false;
                titleCanvasGroup.blocksRaycasts = false;
                titleCanvasGroup.gameObject.SetActive(false);
            }

            OnTitleCompleted?.Invoke();
        }
    }
}
