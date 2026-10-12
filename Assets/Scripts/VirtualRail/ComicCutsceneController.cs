using System;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Bộ Điều Khiển Trình Chiếu Truyện Tranh Dẫn Nhập Cốt Truyện (Comic Cutscene Controller).
    /// Quản lý 4 trang truyện tranh mở đầu nhiệm vụ, hỗ trợ các nút điều hướng và phím tắt (Space, Enter, Escape).
    /// Thiết kế theo kiến trúc 1 chiều, 0 GC Heap Allocation trong Game Loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class ComicCutsceneController : MonoBehaviour
    {
        public static ComicCutsceneController Instance { get; private set; }

        [Header("UI References")]
        public Image comicDisplayImage;
        public Sprite[] comicPages = new Sprite[4];
        public Button btnNext;
        public Button btnPrev;
        public Button btnSkip;
        public Text txtPageIndicator;
        public CanvasGroup cutsceneCanvasGroup;

        [Header("Audio SFX Per Page")]
        public AudioSFXType page1SFX = AudioSFXType.RadioChirp;
        public AudioSFXType page2SFX = AudioSFXType.ChargeHum;
        public AudioSFXType page3SFX = AudioSFXType.ChargeLockOn;
        public AudioSFXType page4SFX = AudioSFXType.EngineBoost;

        /// <summary>
        /// Sự kiện callback thông báo truyện tranh đã hoàn tất hoặc người chơi bấm Skip.
        /// </summary>
        public event Action OnComicCompleted;

        private int _currentPageIndex = 0;
        private bool _isActive = false;

        private static readonly string[] PAGE_INDICATOR_STRINGS = new string[]
        {
            "TRANG 1 / 4",
            "TRANG 2 / 4",
            "TRANG 3 / 4",
            "TRANG 4 / 4"
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (cutsceneCanvasGroup == null)
            {
                cutsceneCanvasGroup = GetComponent<CanvasGroup>();
            }

            // Tự động nạp Sprite từ Resources/Comic nếu mảng bị trống
            if (comicPages == null || comicPages.Length < 4 || comicPages[0] == null)
            {
                comicPages = new Sprite[4];
                for (int i = 0; i < 4; i++)
                {
                    comicPages[i] = Resources.Load<Sprite>("Comic/comic_page_" + (i + 1));
                }
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (btnNext != null)
            {
                btnNext.onClick.RemoveAllListeners();
                btnNext.onClick.AddListener(OnNextClicked);
            }

            if (btnPrev != null)
            {
                btnPrev.onClick.RemoveAllListeners();
                btnPrev.onClick.AddListener(OnPrevClicked);
            }

            if (btnSkip != null)
            {
                btnSkip.onClick.RemoveAllListeners();
                btnSkip.onClick.AddListener(OnSkipClicked);
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // Phím tắt điều hướng mượt mà: Next (Space, Enter, Mũi tên phải)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnNextClicked();
            }
            // Lùi trang (Backspace, Mũi tên trái)
            else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnPrevClicked();
            }
            // Bỏ qua (Escape)
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnSkipClicked();
            }
            // Click chuột trái vào màn hình (chỉ khi không trúng các nút UI riêng biệt)
            else if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    OnNextClicked();
                }
            }
        }

        /// <summary>
        /// Bắt đầu trình chiếu cutscene từ Trang 1.
        /// </summary>
        public void StartCutscene()
        {
            _isActive = true;
            _currentPageIndex = 0;

            if (cutsceneCanvasGroup != null)
            {
                cutsceneCanvasGroup.alpha = 1f;
                cutsceneCanvasGroup.interactable = true;
                cutsceneCanvasGroup.blocksRaycasts = true;
                cutsceneCanvasGroup.gameObject.SetActive(true);
            }

            DisplayPage(_currentPageIndex);
        }

        /// <summary>
        /// Hiển thị trang truyện tranh tương ứng và phát SFX đặc trưng.
        /// </summary>
        private void DisplayPage(int pageIndex)
        {
            if (comicPages == null || comicPages.Length == 0) return;

            pageIndex = Mathf.Clamp(pageIndex, 0, comicPages.Length - 1);
            _currentPageIndex = pageIndex;

            if (comicDisplayImage != null && _currentPageIndex < comicPages.Length)
            {
                comicDisplayImage.sprite = comicPages[_currentPageIndex];
            }

            if (txtPageIndicator != null && _currentPageIndex < PAGE_INDICATOR_STRINGS.Length)
            {
                txtPageIndicator.text = PAGE_INDICATOR_STRINGS[_currentPageIndex];
            }

            if (btnPrev != null)
            {
                btnPrev.gameObject.SetActive(_currentPageIndex > 0);
            }

            PlayPageAudio(_currentPageIndex);
        }

        private void PlayPageAudio(int pageIndex)
        {
            if (AudioManager.Instance == null) return;

            switch (pageIndex)
            {
                case 0:
                    AudioManager.Instance.PlaySFX2D(page1SFX, 0.85f, 1f);
                    break;
                case 1:
                    AudioManager.Instance.PlaySFX2D(page2SFX, 0.9f, 0.95f);
                    break;
                case 2:
                    AudioManager.Instance.PlaySFX2D(page3SFX, 0.95f, 1.1f);
                    break;
                case 3:
                    AudioManager.Instance.PlaySFX2D(page4SFX, 1.0f, 1f);
                    break;
            }
        }

        public void OnNextClicked()
        {
            if (!_isActive) return;

            if (_currentPageIndex < comicPages.Length - 1)
            {
                _currentPageIndex++;
                DisplayPage(_currentPageIndex);
            }
            else
            {
                FinishCutscene();
            }
        }

        public void OnPrevClicked()
        {
            if (!_isActive) return;

            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                DisplayPage(_currentPageIndex);
            }
        }

        public void OnSkipClicked()
        {
            if (!_isActive) return;

            FinishCutscene();
        }

        private void FinishCutscene()
        {
            _isActive = false;

            if (cutsceneCanvasGroup != null)
            {
                cutsceneCanvasGroup.alpha = 0f;
                cutsceneCanvasGroup.interactable = false;
                cutsceneCanvasGroup.blocksRaycasts = false;
                cutsceneCanvasGroup.gameObject.SetActive(false);
            }

            OnComicCompleted?.Invoke();
        }
    }
}
