using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    public enum UIViewState
    {
        MainMenu,
        Gameplay,
        Settings,
        HallOfFame,
        Pause,
        Debriefing
    }

    /// <summary>
    /// Bộ Điều Phối Giao Diện Trung Tâm (Complete Sci-Fi UI Manager).
    /// Quản lý toàn bộ vòng đời màn hình: Main Menu, Settings, Hall of Fame, In-Game Pause, Debriefing.
    /// Thiết kế theo kiến trúc 1 chiều, 0 GC Heap Allocation trong Game Loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Views / Panels")]
        public RectTransform mainMenuPanel;
        public RectTransform settingsModal;
        public RectTransform hallOfFameModal;
        public RectTransform pauseModal;
        public RectTransform debriefingModal;
        public RectTransform fpsDisplayRoot;

        [Header("Main Menu Controls")]
        public Button btnSinglePlayer;
        public Button btnMultiplayer;
        public Button btnOptions;
        public Button btnHallOfFame;
        public Button btnQuit;

        [Header("Settings Controls (Video Tab)")]
        public Button tabVideo;
        public Button tabAudio;
        public Button tabPlayers;
        public Button tabControls;
        public GameObject tabVideoContent;
        public GameObject tabAudioContent;
        public GameObject tabPlayersContent;
        public GameObject tabControlsContent;
        public Sprite tabActiveSprite;
        public Sprite tabNormalSprite;

        public Dropdown dropdownResolution;
        public Toggle toggleFullscreen;
        public Slider sliderLuminosity;
        public Text txtLuminosityVal;
        public Toggle toggleVSync;
        public Slider sliderFramerate;
        public Text txtFramerateVal;
        public Toggle toggleShowFPS;
        public Dropdown dropdownQuality;

        public Slider sliderVolMaster;
        public Slider sliderVolMusic;
        public Slider sliderVolSFX;

        public Button btnSettingsApply;
        public Button btnSettingsDefaults;
        public Button btnSettingsExit;
        public Button btnSettingsCloseX;

        [Header("Hall of Fame Controls")]
        public Button btnHallOfFameClose;
        public Button btnHallOfFameCloseX;
        public RectTransform hallOfFameTableContent;

        [Header("Pause Controls")]
        public Button btnPauseResume;
        public Button btnPauseOptions;
        public Button btnPauseRestart;
        public Button btnPauseMainMenu;

        [Header("Debriefing Controls")]
        public Text txtDebriefScore;
        public Text txtDebriefHits;
        public Text txtDebriefRank;
        public Button btnDebriefRestart;
        public Button btnDebriefMainMenu;

        private UIViewState _currentState = UIViewState.MainMenu;
        private UIViewState _previousState = UIViewState.MainMenu;

        // Cache CanvasGroups for smooth zero-allocation fading
        private CanvasGroup _cgMainMenu;
        private CanvasGroup _cgSettings;
        private CanvasGroup _cgHallOfFame;
        private CanvasGroup _cgPause;
        private CanvasGroup _cgDebriefing;

        private bool _isPaused = false;
        private static readonly string[] FLOAT_STRINGS = PrecomputeFloatStrings();

        private static string[] PrecomputeFloatStrings()
        {
            string[] table = new string[201];
            for (int i = 0; i <= 200; i++)
            {
                table[i] = (i / 100.0f).ToString("F2");
            }
            return table;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;

            _cgMainMenu = EnsureCanvasGroup(mainMenuPanel);
            _cgSettings = EnsureCanvasGroup(settingsModal);
            _cgHallOfFame = EnsureCanvasGroup(hallOfFameModal);
            _cgPause = EnsureCanvasGroup(pauseModal);
            _cgDebriefing = EnsureCanvasGroup(debriefingModal);
        }

        private void Start()
        {
            SetupButtonListeners();
            SetupSettingsBindings();
            PopulateHallOfFameRows();

            // Khởi đầu ở Main Menu
            ShowState(UIViewState.MainMenu);
        }

        private void OnEnable()
        {
            GameEventManager.OnStartGame += HandleGameStarted;
            GameEventManager.OnPlayerDestroyed += HandlePlayerDestroyed;
        }

        private void OnDisable()
        {
            GameEventManager.OnStartGame -= HandleGameStarted;
            GameEventManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
        }

        private void Update()
        {
            // Xử lý phím Escape / P để bật Pause Menu trong trận
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (_currentState == UIViewState.Gameplay)
                {
                    TogglePauseGame(true);
                }
                else if (_currentState == UIViewState.Pause)
                {
                    TogglePauseGame(false);
                }
                else if (_currentState == UIViewState.Settings)
                {
                    OnSettingsExitClicked();
                }
                else if (_currentState == UIViewState.HallOfFame)
                {
                    OnHallOfFameCloseClicked();
                }
            }
        }

        private CanvasGroup EnsureCanvasGroup(RectTransform rt)
        {
            if (rt == null) return null;
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        private void SetGroupVisible(CanvasGroup cg, bool visible)
        {
            if (cg == null) return;
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
            cg.gameObject.SetActive(visible);
        }

        public void ShowState(UIViewState newState)
        {
            _previousState = _currentState;
            _currentState = newState;

            SetGroupVisible(_cgMainMenu, newState == UIViewState.MainMenu);
            SetGroupVisible(_cgSettings, newState == UIViewState.Settings);
            SetGroupVisible(_cgHallOfFame, newState == UIViewState.HallOfFame);
            SetGroupVisible(_cgPause, newState == UIViewState.Pause);
            SetGroupVisible(_cgDebriefing, newState == UIViewState.Debriefing);

            if (newState == UIViewState.Settings)
            {
                SwitchSettingsTab(0);
            }
            else if (newState == UIViewState.HallOfFame)
            {
                PopulateHallOfFameRows();
            }

            // Tự động đồng bộ HUD buồng lái
            if (ModernCockpitHUD.Instance != null)
            {
                ModernCockpitHUD.Instance.SetHUDVisible(newState == UIViewState.Gameplay);
            }

            // Ẩn các UI phụ trợ chặng bay khi không ở Gameplay
            if (newState != UIViewState.Gameplay)
            {
                if (StageNotificationUI.Instance != null && StageNotificationUI.Instance.bannerCanvasGroup != null)
                {
                    StageNotificationUI.Instance.bannerCanvasGroup.alpha = 0f;
                }
                if (RadioCommsHUD.Instance != null && RadioCommsHUD.Instance.commsCanvasGroup != null)
                {
                    RadioCommsHUD.Instance.commsCanvasGroup.alpha = 0f;
                }
            }
        }

        #region EVENT LISTENERS & SETUP

        private void SetupButtonListeners()
        {
            if (btnSinglePlayer != null) btnSinglePlayer.onClick.AddListener(OnSinglePlayerClicked);
            if (btnMultiplayer != null) btnMultiplayer.onClick.AddListener(OnMultiplayerClicked);
            if (btnOptions != null) btnOptions.onClick.AddListener(OnOptionsClicked);
            if (btnHallOfFame != null) btnHallOfFame.onClick.AddListener(OnHallOfFameClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);

            if (btnSettingsApply != null) btnSettingsApply.onClick.AddListener(OnSettingsApplyClicked);
            if (btnSettingsDefaults != null) btnSettingsDefaults.onClick.AddListener(OnSettingsDefaultsClicked);
            if (btnSettingsExit != null) btnSettingsExit.onClick.AddListener(OnSettingsExitClicked);
            if (btnSettingsCloseX != null) btnSettingsCloseX.onClick.AddListener(OnSettingsExitClicked);

            if (btnHallOfFameClose != null) btnHallOfFameClose.onClick.AddListener(OnHallOfFameCloseClicked);
            if (btnHallOfFameCloseX != null) btnHallOfFameCloseX.onClick.AddListener(OnHallOfFameCloseClicked);

            if (btnPauseResume != null) btnPauseResume.onClick.AddListener(OnPauseResumeClicked);
            if (btnPauseOptions != null) btnPauseOptions.onClick.AddListener(OnPauseOptionsClicked);
            if (btnPauseRestart != null) btnPauseRestart.onClick.AddListener(OnPauseRestartClicked);
            if (btnPauseMainMenu != null) btnPauseMainMenu.onClick.AddListener(OnPauseMainMenuClicked);

            if (btnDebriefRestart != null) btnDebriefRestart.onClick.AddListener(OnSinglePlayerClicked);
            if (btnDebriefMainMenu != null) btnDebriefMainMenu.onClick.AddListener(() => ShowState(UIViewState.MainMenu));

            // Tabs
            if (tabVideo != null) tabVideo.onClick.AddListener(() => SwitchSettingsTab(0));
            if (tabAudio != null) tabAudio.onClick.AddListener(() => SwitchSettingsTab(1));
            if (tabPlayers != null) tabPlayers.onClick.AddListener(() => SwitchSettingsTab(2));
            if (tabControls != null) tabControls.onClick.AddListener(() => SwitchSettingsTab(3));
        }

        public void SwitchSettingsTab(int tabIndex)
        {
            if (tabVideoContent != null) tabVideoContent.SetActive(tabIndex == 0);
            if (tabAudioContent != null) tabAudioContent.SetActive(tabIndex == 1);
            if (tabPlayersContent != null) tabPlayersContent.SetActive(tabIndex == 2);
            if (tabControlsContent != null) tabControlsContent.SetActive(tabIndex == 3);

            UpdateTabVisual(tabVideo, tabIndex == 0);
            UpdateTabVisual(tabAudio, tabIndex == 1);
            UpdateTabVisual(tabPlayers, tabIndex == 2);
            UpdateTabVisual(tabControls, tabIndex == 3);
        }

        private void UpdateTabVisual(Button btn, bool isActive)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                if (isActive && tabActiveSprite != null) img.sprite = tabActiveSprite;
                else if (!isActive && tabNormalSprite != null) img.sprite = tabNormalSprite;
            }

            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.color = isActive ? Color.white : new Color(0f, 0.95f, 1f, 1f);
            }
        }

        private void SetupSettingsBindings()
        {
            var mgr = SettingsManager.Instance;
            if (mgr == null) return;

            // Resolution dropdown
            if (dropdownResolution != null)
            {
                dropdownResolution.ClearOptions();
                dropdownResolution.AddOptions(mgr.GetResolutionOptions());
                dropdownResolution.value = mgr.resolutionIndex;
                dropdownResolution.onValueChanged.AddListener(val => mgr.resolutionIndex = val);
            }

            // Fullscreen toggle
            if (toggleFullscreen != null)
            {
                toggleFullscreen.isOn = mgr.isFullscreen;
                toggleFullscreen.onValueChanged.AddListener(val => mgr.isFullscreen = val);
            }

            // Luminosity
            if (sliderLuminosity != null)
            {
                sliderLuminosity.value = mgr.luminosity;
                sliderLuminosity.onValueChanged.AddListener(val => {
                    mgr.luminosity = val;
                    if (txtLuminosityVal != null) txtLuminosityVal.text = FormatFloat(val);
                });
                if (txtLuminosityVal != null) txtLuminosityVal.text = FormatFloat(mgr.luminosity);
            }

            // VSync
            if (toggleVSync != null)
            {
                toggleVSync.isOn = mgr.isVSync;
                toggleVSync.onValueChanged.AddListener(val => mgr.isVSync = val);
            }

            // Framerate slider
            if (sliderFramerate != null)
            {
                sliderFramerate.value = mgr.targetFramerate;
                sliderFramerate.onValueChanged.AddListener(val => {
                    mgr.targetFramerate = Mathf.RoundToInt(val);
                    if (txtFramerateVal != null) txtFramerateVal.text = mgr.targetFramerate.ToString();
                });
                if (txtFramerateVal != null) txtFramerateVal.text = mgr.targetFramerate.ToString();
            }

            // Display FPS
            if (toggleShowFPS != null)
            {
                toggleShowFPS.isOn = mgr.showFPS;
                toggleShowFPS.onValueChanged.AddListener(val => mgr.showFPS = val);
            }

            // Quality
            if (dropdownQuality != null)
            {
                dropdownQuality.ClearOptions();
                List<string> qNames = new List<string>(QualitySettings.names);
                dropdownQuality.AddOptions(qNames);
                dropdownQuality.value = mgr.qualityLevel;
                dropdownQuality.onValueChanged.AddListener(val => mgr.qualityLevel = val);
            }

            // Audio sliders
            if (sliderVolMaster != null)
            {
                sliderVolMaster.value = mgr.volumeMaster;
                sliderVolMaster.onValueChanged.AddListener(val => mgr.volumeMaster = val);
            }
            if (sliderVolMusic != null)
            {
                sliderVolMusic.value = mgr.volumeMusic;
                sliderVolMusic.onValueChanged.AddListener(val => mgr.volumeMusic = val);
            }
            if (sliderVolSFX != null)
            {
                sliderVolSFX.value = mgr.volumeSFX;
                sliderVolSFX.onValueChanged.AddListener(val => mgr.volumeSFX = val);
            }
        }

        private string FormatFloat(float val)
        {
            int idx = Mathf.Clamp(Mathf.RoundToInt(val * 100f), 0, 200);
            return FLOAT_STRINGS[idx];
        }

        #endregion

        #region BUTTON ACTIONS

        public void OnSinglePlayerClicked()
        {
            Time.timeScale = 1.0f;
            _isPaused = false;
            ShowState(UIViewState.Gameplay);

            // Bắt đầu game thông qua hệ thống GameEventManager
            GameEventManager.StartGame();
        }

        public void OnMultiplayerClicked()
        {
            // Bắt đầu Arcade Star Fox Endless mode
            OnSinglePlayerClicked();
        }

        public void OnOptionsClicked()
        {
            ShowState(UIViewState.Settings);
        }

        public void OnHallOfFameClicked()
        {
            PopulateHallOfFameRows();
            ShowState(UIViewState.HallOfFame);
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnSettingsApplyClicked()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SaveSettings();
            }
            OnSettingsExitClicked();
        }

        private void OnSettingsDefaultsClicked()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetToDefaults();
                SetupSettingsBindings();
            }
        }

        private void OnSettingsExitClicked()
        {
            ShowState(_previousState == UIViewState.Gameplay || _previousState == UIViewState.Pause ? UIViewState.Pause : UIViewState.MainMenu);
        }

        private void OnHallOfFameCloseClicked()
        {
            ShowState(UIViewState.MainMenu);
        }

        public void TogglePauseGame(bool pause)
        {
            _isPaused = pause;
            Time.timeScale = pause ? 0.0f : 1.0f;
            ShowState(pause ? UIViewState.Pause : UIViewState.Gameplay);
        }

        private void OnPauseResumeClicked()
        {
            TogglePauseGame(false);
        }

        private void OnPauseOptionsClicked()
        {
            ShowState(UIViewState.Settings);
        }

        private void OnPauseRestartClicked()
        {
            Time.timeScale = 1.0f;
            _isPaused = false;
            OnSinglePlayerClicked();
        }

        private void OnPauseMainMenuClicked()
        {
            Time.timeScale = 1.0f;
            _isPaused = false;
            ShowState(UIViewState.MainMenu);
        }

        private void HandleGameStarted()
        {
            ShowState(UIViewState.Gameplay);
        }

        private void HandlePlayerDestroyed()
        {
            // Trì hoãn hiển thị Debriefing / Game Over
            Invoke("ShowDebriefingDelayed", 2.5f);
        }

        private void ShowDebriefingDelayed()
        {
            ShowState(UIViewState.Debriefing);
        }

        public void PopulateHallOfFameRows()
        {
            if (hallOfFameTableContent == null || LeaderboardManager.Instance == null) return;

            var entries = LeaderboardManager.Instance.GetEntries();
            for (int i = 0; i < entries.Count && i < hallOfFameTableContent.childCount; i++)
            {
                Transform row = hallOfFameTableContent.GetChild(i);
                Text rankText = row.Find("RankText")?.GetComponent<Text>();
                Text nameText = row.Find("NameText")?.GetComponent<Text>();
                Text scoreText = row.Find("ScoreText")?.GetComponent<Text>();
                Text stageText = row.Find("StageText")?.GetComponent<Text>();
                Text diffText = row.Find("DiffText")?.GetComponent<Text>();

                if (rankText != null) rankText.text = entries[i].rank.ToString();
                if (nameText != null) nameText.text = entries[i].callsign;
                if (scoreText != null) scoreText.text = entries[i].score.ToString();
                if (stageText != null) stageText.text = entries[i].stage.ToString();
                if (diffText != null) diffText.text = entries[i].difficulty;
            }
        }

        #endregion
    }
}
