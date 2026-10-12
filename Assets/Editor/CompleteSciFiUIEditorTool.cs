using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VirtualRail;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ Editor 1-Click xây dựng và cấu hình Hệ Thống UI Hoàn Chỉnh (Complete Sci-Fi UI System).
    /// Bám sát 100% bản thiết kế mẫu (UI COMPONENTS 01 & 02):
    /// - Menu Chính dạng cột kim loại (Pill Beveled Buttons).
    /// - Cửa sổ Cài Đặt toàn diện (Settings Modal với Tab Video, Audio, Controls, Slider, Checkbox, Dropdown).
    /// - Bảng Vinh Danh Kỷ Lục (Hall of Fame Modal với Banner và Top 10 High Scores).
    /// - Menu Tạm Dừng trong trận (In-Game Pause Menu).
    /// - Bảng Tổng Kết Chiến Dịch (Mission Debriefing).
    /// 
    /// Tuân thủ Quy Tắc Bảo Tồn An Toàn 3 Tầng (3-Layer Undo Protocol):
    /// Cung cấp MenuItem 1-Click đối xứng Bật / Hoàn nguyên.
    /// </summary>
    [InitializeOnLoad]
    public static class CompleteSciFiUIEditorTool
    {
        private const string SCENE_PATH = "Assets/Scenes/Game.unity";
        private const string COMPLETE_UI_ROOT_NAME = "[CompleteSciFiUISystem]";
        private const string PREF_AUTO_SETUP = "VirtualRail_CompleteUISystem_Enabled";

        static CompleteSciFiUIEditorTool()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (EditorPrefs.GetBool(PREF_AUTO_SETUP, true))
                {
                    Canvas canvas = Object.FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        Transform existing = canvas.transform.Find(COMPLETE_UI_ROOT_NAME);
                        if (existing == null || existing.Find("FullScreenMenuBackdrop") == null)
                        {
                            SwitchToCompleteSciFiUI();
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/Virtual Rail/UI System/1. Switch to Complete Sci-Fi UI System (ON)")]
        [MenuItem("Tools/Virtual Rail/50. Switch to Complete Sci-Fi UI System (ON)")]
        public static void SwitchToCompleteSciFiUI()
        {
            EditorPrefs.SetBool(PREF_AUTO_SETUP, true);
            EnsureManagersExist();

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject cObj = new GameObject("Canvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = cObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                cObj.AddComponent<GraphicRaycaster>();
            }

            // Gỡ bỏ hệ thống cũ nếu có
            Transform existing = canvas.transform.Find(COMPLETE_UI_ROOT_NAME);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // Ẩn Main Menu và Game UI gốc của scene cũ
            GameObject oldMainMenu = GameObject.Find("Main Menu");
            if (oldMainMenu != null) oldMainMenu.SetActive(false);
            GameObject oldGameUI = GameObject.Find("Game UI");
            if (oldGameUI != null) oldGameUI.SetActive(false);

            // Vô hiệu hóa các component UI legacy chạy ngầm
            GameTimer oldTimer = canvas.GetComponent<GameTimer>();
            if (oldTimer != null) oldTimer.enabled = false;
            GameScore oldScore = canvas.GetComponent<GameScore>();
            if (oldScore != null) oldScore.enabled = false;

            // Xây dựng cây phân cấp mới
            BuildCompleteUIHierarchy(canvas.transform);

            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ KÍCH HOẠT HỆ THỐNG GIAO DIỆN SCI-FI HOÀN CHỈNH (ON)!</b></color>");
        }

        [MenuItem("Tools/Virtual Rail/UI System/2. Revert to Legacy UI (OFF - UNDO)")]
        [MenuItem("Tools/Virtual Rail/51. Revert to Legacy UI (OFF - UNDO)")]
        public static void RevertToLegacyUI()
        {
            EditorPrefs.SetBool(PREF_AUTO_SETUP, false);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find(COMPLETE_UI_ROOT_NAME);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                GameTimer oldTimer = canvas.GetComponent<GameTimer>();
                if (oldTimer != null) oldTimer.enabled = true;
                GameScore oldScore = canvas.GetComponent<GameScore>();
                if (oldScore != null) oldScore.enabled = true;
            }

            // Bật lại Main Menu và Game UI gốc
            GameObject oldMainMenu = GameObject.Find("Main Menu");
            if (oldMainMenu != null) oldMainMenu.SetActive(true);
            GameObject oldGameUI = GameObject.Find("Game UI");
            if (oldGameUI != null) oldGameUI.SetActive(true);

            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN GIAO DIỆN CŨ (UNDO)!</b></color>");
        }

        private static void EnsureManagersExist()
        {
            if (Object.FindAnyObjectByType<SettingsManager>() == null)
            {
                GameObject sm = new GameObject("[SettingsManager]");
                sm.AddComponent<SettingsManager>();
            }
            if (Object.FindAnyObjectByType<LeaderboardManager>() == null)
            {
                GameObject lm = new GameObject("[LeaderboardManager]");
                lm.AddComponent<LeaderboardManager>();
            }
        }

        private static void BuildCompleteUIHierarchy(Transform canvasParent)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Orbitron.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Load Sprites
            Sprite spWindow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_panel_mech_window.png");
            Sprite spMenuCol = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_panel_menu_column.png");
            Sprite spBtn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic.png");
            Sprite spBtnHover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic_hover.png");
            Sprite spBtnPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic_pressed.png");
            Sprite spTabNorm = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_tab_btn_normal.png");
            Sprite spTabAct = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_tab_btn_active.png");
            Sprite spDropdown = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_dropdown_frame.png");
            Sprite spBanner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_banner_halloffame.png");
            Sprite spSliderTrack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_slider_track.png");
            Sprite spSliderFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_slider_fill.png");
            Sprite spSliderThumb = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_slider_thumb.png");
            Sprite spCheckFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_checkbox_frame.png");
            Sprite spCheckX = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_checkbox_x.png");
            Sprite spToggleSq = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_toggle_square.png");
            Sprite spCloseX = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_icon_close_x.png");

            Color cyanColor = new Color(0f, 0.95f, 1f, 1f);
            Color amberColor = new Color(1f, 0.75f, 0.1f, 1f);

            // Root System
            GameObject root = new GameObject(COMPLETE_UI_ROOT_NAME);
            root.transform.SetParent(canvasParent, false);
            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            UIManager uiMgr = root.AddComponent<UIManager>();

            // -------------------------------------------------------------
            // FULL-SCREEN BACKDROP (Che kín 100% demo 3D khi ở Menu, fade out mượt mà khi vào Gameplay)
            // -------------------------------------------------------------
            GameObject backdropObj = new GameObject("FullScreenMenuBackdrop");
            backdropObj.transform.SetParent(root.transform, false);
            RectTransform bdRt = backdropObj.AddComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;

            Image bdImg = backdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.015f, 0.025f, 0.045f, 1f); // Deep Space Dark Navy (#04060C)
            bdImg.raycastTarget = false;

            CanvasGroup bdCg = backdropObj.AddComponent<CanvasGroup>();
            bdCg.alpha = 1f;
            bdCg.interactable = false;
            bdCg.blocksRaycasts = false;

            // Lớp phủ vân lưới công nghệ scanline mờ tạo chiều sâu Sci-Fi sang trọng
            Sprite spScanline = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_scanline_overlay.png");
            if (spScanline != null)
            {
                GameObject scanlineObj = new GameObject("ScanlineOverlay");
                scanlineObj.transform.SetParent(backdropObj.transform, false);
                RectTransform slRt = scanlineObj.AddComponent<RectTransform>();
                slRt.anchorMin = Vector2.zero;
                slRt.anchorMax = Vector2.one;
                slRt.offsetMin = Vector2.zero;
                slRt.offsetMax = Vector2.zero;
                Image slImg = scanlineObj.AddComponent<Image>();
                slImg.sprite = spScanline;
                slImg.type = Image.Type.Tiled;
                slImg.color = new Color(0f, 0.85f, 1f, 0.035f);
                slImg.raycastTarget = false;
            }

            uiMgr.fullScreenBackdrop = bdCg;
            uiMgr.backdropImage = bdImg;

            // -------------------------------------------------------------
            // 0. FPS DISPLAY (Top-Center)
            // -------------------------------------------------------------
            GameObject fpsObj = CreateText("FPSDisplay", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(160f, 26f), "FPS: 60", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleCenter);
            FPSDisplay fpsComp = fpsObj.AddComponent<FPSDisplay>();
            fpsComp.fpsText = fpsObj.GetComponent<Text>();
            uiMgr.fpsDisplayRoot = fpsObj.GetComponent<RectTransform>();

            // -------------------------------------------------------------
            // 1. MAIN MENU PANEL (Matching UI COMPONENTS 02)
            // -------------------------------------------------------------
            GameObject mmObj = CreatePanel("MainMenuPanel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(300f, 440f), spMenuCol, Image.Type.Sliced);
            uiMgr.mainMenuPanel = mmObj.GetComponent<RectTransform>();

            // Header Title
            CreateText("HeaderTitle", mmObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(260f, 32f), "MISSION COMMAND", font, 16, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);

            // 5 Pill Buttons Stack
            uiMgr.btnSinglePlayer = CreatePillButton("Btn_SinglePlayer", mmObj.transform, new Vector2(0f, -80f), "SINGLE PLAYER", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnMultiplayer = CreatePillButton("Btn_Multiplayer", mmObj.transform, new Vector2(0f, -145f), "MULTIPLAYER", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnOptions = CreatePillButton("Btn_Options", mmObj.transform, new Vector2(0f, -210f), "OPTIONS", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnHallOfFame = CreatePillButton("Btn_HallOfFame", mmObj.transform, new Vector2(0f, -275f), "HALL OF FAME", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnQuit = CreatePillButton("Btn_Quit", mmObj.transform, new Vector2(0f, -340f), "QUIT GAME", font, spBtn, spBtnHover, spBtnPressed);

            // -------------------------------------------------------------
            // 2. SETTINGS / OPTIONS MODAL (Matching UI COMPONENTS 01)
            // -------------------------------------------------------------
            GameObject setModal = CreatePanel("SettingsModal", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 520f), spWindow, Image.Type.Sliced);
            uiMgr.settingsModal = setModal.GetComponent<RectTransform>();

            // Close [X] Button on top-right
            uiMgr.btnSettingsCloseX = CreateIconButton("Btn_CloseX", setModal.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(44f, 44f), spCloseX);

            // Top Tabs: VIDEO (Green Active), AUDIO, PLAYERS, CONTROLS
            float tabXStart = 32f;
            float tabSpacing = 145f;
            uiMgr.tabVideo = CreateTabButton("Tab_Video", setModal.transform, new Vector2(tabXStart, 22f), "VIDEO", font, spTabAct, Color.white);
            uiMgr.tabAudio = CreateTabButton("Tab_Audio", setModal.transform, new Vector2(tabXStart + tabSpacing, 22f), "AUDIO", font, spTabNorm, cyanColor);
            uiMgr.tabPlayers = CreateTabButton("Tab_Players", setModal.transform, new Vector2(tabXStart + tabSpacing * 2, 22f), "PLAYERS", font, spTabNorm, cyanColor);
            uiMgr.tabControls = CreateTabButton("Tab_Controls", setModal.transform, new Vector2(tabXStart + tabSpacing * 3, 22f), "CONTROLS", font, spTabNorm, cyanColor);
            uiMgr.tabActiveSprite = spTabAct;
            uiMgr.tabNormalSprite = spTabNorm;

            // --- TAB CONTENT: VIDEO ---
            GameObject videoContent = new GameObject("TabContent_Video");
            videoContent.transform.SetParent(setModal.transform, false);
            RectTransform vcRt = videoContent.AddComponent<RectTransform>();
            vcRt.anchorMin = new Vector2(0f, 0f);
            vcRt.anchorMax = new Vector2(1f, 1f);
            vcRt.offsetMin = new Vector2(40f, 75f);
            vcRt.offsetMax = new Vector2(-40f, -50f);
            uiMgr.tabVideoContent = videoContent;

            float rowY = -25f;
            float rowStep = 45f;

            // 1. SCREEN RESOLUTION
            CreateText("Label_Res", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "SCREEN RESOLUTION", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.dropdownResolution = CreateDropdown("Dropdown_Res", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(300f, 36f), font, spDropdown);

            // 2. FULLSCREEN
            rowY -= rowStep;
            CreateText("Label_Fullscreen", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "FULLSCREEN", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.toggleFullscreen = CreateCheckbox("Toggle_Fullscreen", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), spCheckFrame, spCheckX);

            // 3. LUMINOSITY
            rowY -= rowStep;
            CreateText("Label_Luminosity", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "LUMINOSITY", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            var lumSlider = CreateSlider("Slider_Luminosity", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(240f, 20f), 0.5f, 2.0f, spSliderTrack, spSliderFill, spSliderThumb);
            uiMgr.sliderLuminosity = lumSlider.slider;
            uiMgr.txtLuminosityVal = CreateText("Val_Luminosity", videoContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, rowY), new Vector2(50f, 32f), "1.00", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleRight).GetComponent<Text>();

            // 4. VSYNC
            rowY -= rowStep;
            CreateText("Label_VSync", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "VSYNC", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.toggleVSync = CreateCheckbox("Toggle_VSync", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), spCheckFrame, spToggleSq);

            // 5. TARGET FRAMERATE
            rowY -= rowStep;
            CreateText("Label_Framerate", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "TARGET FRAMERATE", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            var fpsSlider = CreateSlider("Slider_Framerate", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(240f, 20f), 30f, 144f, spSliderTrack, spSliderFill, spSliderThumb);
            uiMgr.sliderFramerate = fpsSlider.slider;
            uiMgr.txtFramerateVal = CreateText("Val_Framerate", videoContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, rowY), new Vector2(50f, 32f), "60", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleRight).GetComponent<Text>();

            // 6. DISPLAY FPS
            rowY -= rowStep;
            CreateText("Label_ShowFPS", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "DISPLAY FPS", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.toggleShowFPS = CreateCheckbox("Toggle_ShowFPS", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), spCheckFrame, spCheckX);

            // 7. OVERALL IMAGE QUALITY
            rowY -= rowStep;
            CreateText("Label_Quality", videoContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "OVERALL IMAGE QUALITY", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.dropdownQuality = CreateDropdown("Dropdown_Quality", videoContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(300f, 36f), font, spDropdown);

            // --- TAB CONTENT: AUDIO ---
            GameObject audioContent = new GameObject("TabContent_Audio");
            audioContent.transform.SetParent(setModal.transform, false);
            RectTransform acRt = audioContent.AddComponent<RectTransform>();
            acRt.anchorMin = new Vector2(0f, 0f);
            acRt.anchorMax = new Vector2(1f, 1f);
            acRt.offsetMin = new Vector2(40f, 75f);
            acRt.offsetMax = new Vector2(-40f, -50f);
            uiMgr.tabAudioContent = audioContent;
            audioContent.SetActive(false);

            rowY = -40f;
            CreateText("Label_VolMaster", audioContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "MASTER VOLUME", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.sliderVolMaster = CreateSlider("Slider_Master", audioContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(300f, 20f), 0f, 1f, spSliderTrack, spSliderFill, spSliderThumb).slider;

            rowY -= 60f;
            CreateText("Label_VolMusic", audioContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "MUSIC VOLUME", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.sliderVolMusic = CreateSlider("Slider_Music", audioContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(300f, 20f), 0f, 1f, spSliderTrack, spSliderFill, spSliderThumb).slider;

            rowY -= 60f;
            CreateText("Label_VolSFX", audioContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(260f, 32f), "SFX VOLUME", font, 14, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            uiMgr.sliderVolSFX = CreateSlider("Slider_SFX", audioContent.transform, new Vector2(1f, 1f), new Vector2(-150f, rowY), new Vector2(300f, 20f), 0f, 1f, spSliderTrack, spSliderFill, spSliderThumb).slider;

            // --- TAB CONTENT: PLAYERS ---
            GameObject playersContent = new GameObject("TabContent_Players");
            playersContent.transform.SetParent(setModal.transform, false);
            RectTransform pcRt = playersContent.AddComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0f, 0f);
            pcRt.anchorMax = new Vector2(1f, 1f);
            pcRt.offsetMin = new Vector2(40f, 75f);
            pcRt.offsetMax = new Vector2(-40f, -50f);
            uiMgr.tabPlayersContent = playersContent;
            playersContent.SetActive(false);

            CreateText("Players_Title", playersContent.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(450f, 32f), "PILOT PROFILE & TACTICAL LOADOUT", font, 15, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);

            float pY = -70f;
            float pStep = 36f;
            CreateText("P_Callsign", playersContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, pY), new Vector2(300f, 28f), "PILOT CALLSIGN:", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            CreateText("P_Callsign_Val", playersContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, pY), new Vector2(300f, 28f), "FOX McCLOUD [LEADER]", font, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

            pY -= pStep;
            CreateText("P_Craft", playersContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, pY), new Vector2(300f, 28f), "ASSIGNED CRAFT:", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            CreateText("P_Craft_Val", playersContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, pY), new Vector2(300f, 28f), "ARWING MK-II INTERCEPTOR", font, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

            pY -= pStep;
            CreateText("P_Propulsion", playersContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, pY), new Vector2(300f, 28f), "PROPULSION SYSTEM:", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            CreateText("P_Propulsion_Val", playersContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, pY), new Vector2(300f, 28f), "G-DIFFUSER + PLASMA TURBO", font, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

            pY -= pStep;
            CreateText("P_Shield", playersContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, pY), new Vector2(300f, 28f), "DEFENSE MATRIX:", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            CreateText("P_Shield_Val", playersContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, pY), new Vector2(300f, 28f), "HEXAGONAL TITANIUM 100 HP", font, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

            pY -= pStep;
            CreateText("P_Weapons", playersContent.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, pY), new Vector2(300f, 28f), "WEAPON SUITE:", font, 13, FontStyle.Bold, cyanColor, TextAnchor.MiddleLeft);
            CreateText("P_Weapons_Val", playersContent.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, pY), new Vector2(300f, 28f), "TWIN HYPER LASER + 3 BOMBS", font, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

            // --- TAB CONTENT: CONTROLS ---
            GameObject ctrlContent = new GameObject("TabContent_Controls");
            ctrlContent.transform.SetParent(setModal.transform, false);
            RectTransform ccRt = ctrlContent.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0f, 0f);
            ccRt.anchorMax = new Vector2(1f, 1f);
            ccRt.offsetMin = new Vector2(40f, 75f);
            ccRt.offsetMax = new Vector2(-40f, -50f);
            uiMgr.tabControlsContent = ctrlContent;
            ctrlContent.SetActive(false);

            CreateText("Ctrl_Guide", ctrlContent.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 200f),
                "FLIGHT CONTROLS:\n\n[W / A / S / D] or [ARROWS] : Pitch & Yaw Steering\n[SPACE] or [LEFT CLICK] : Fire Plasma Blaster\n[HOLD SPACE] : Plasma Charge Shot\n[B] or [RIGHT CLICK] : Launch Smart Bomb\n[SHIFT] : Turbo Boost\n[CTRL] : Airbrake\n[Q / E] : Barrel Roll\n[ESC] : In-Game Pause",
                font, 13, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

            // Bottom Buttons (APPLY, DEFAULTS, EXIT)
            float bY = 28f;
            uiMgr.btnSettingsApply = CreateSmallButton("Btn_Apply", setModal.transform, new Vector2(0.5f, 0f), new Vector2(-150f, bY), new Vector2(130f, 40f), "APPLY", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnSettingsDefaults = CreateSmallButton("Btn_Defaults", setModal.transform, new Vector2(0.5f, 0f), new Vector2(0f, bY), new Vector2(140f, 40f), "DEFAULTS", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnSettingsExit = CreateSmallButton("Btn_Exit", setModal.transform, new Vector2(0.5f, 0f), new Vector2(150f, bY), new Vector2(130f, 40f), "EXIT", font, spBtn, spBtnHover, spBtnPressed);

            // -------------------------------------------------------------
            // 3. HALL OF FAME MODAL (Matching UI COMPONENTS 02)
            // -------------------------------------------------------------
            GameObject hofModal = CreatePanel("HallOfFameModal", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 540f), spWindow, Image.Type.Sliced);
            uiMgr.hallOfFameModal = hofModal.GetComponent<RectTransform>();

            // Extruded Header Banner: HALL OF FAME
            CreatePanel("Banner_HallOfFame", hofModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(480f, 72f), spBanner, Image.Type.Sliced);
            CreateText("BannerText", hofModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(400f, 36f), "HALL OF FAME", font, 22, FontStyle.Bold, cyanColor, TextAnchor.MiddleCenter);

            uiMgr.btnHallOfFameCloseX = CreateIconButton("Btn_CloseX", hofModal.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(44f, 44f), spCloseX);

            // Table Header: RANK, CALLSIGN, SCORE, STAGE, DIFFICULTY
            float tY = -65f;
            CreateText("TH_Rank", hofModal.transform, new Vector2(0.12f, 1f), new Vector2(0.12f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, tY), new Vector2(80f, 26f), "RANK", font, 14, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);
            CreateText("TH_Name", hofModal.transform, new Vector2(0.30f, 1f), new Vector2(0.30f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, tY), new Vector2(140f, 26f), "CALLSIGN", font, 14, FontStyle.Bold, amberColor, TextAnchor.MiddleLeft);
            CreateText("TH_Score", hofModal.transform, new Vector2(0.55f, 1f), new Vector2(0.55f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, tY), new Vector2(120f, 26f), "SCORE", font, 14, FontStyle.Bold, amberColor, TextAnchor.MiddleRight);
            CreateText("TH_Stage", hofModal.transform, new Vector2(0.72f, 1f), new Vector2(0.72f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, tY), new Vector2(80f, 26f), "STAGE", font, 14, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);
            CreateText("TH_Diff", hofModal.transform, new Vector2(0.88f, 1f), new Vector2(0.88f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, tY), new Vector2(100f, 26f), "DIFFICULTY", font, 14, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);

            // Table Content Container (10 Rows)
            GameObject tableContainer = new GameObject("TableContent");
            tableContainer.transform.SetParent(hofModal.transform, false);
            RectTransform tcRt = tableContainer.AddComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0f, 0f);
            tcRt.anchorMax = new Vector2(1f, 1f);
            tcRt.offsetMin = new Vector2(40f, 75f);
            tcRt.offsetMax = new Vector2(-40f, -95f);
            uiMgr.hallOfFameTableContent = tcRt;

            for (int r = 0; r < 10; r++)
            {
                GameObject row = new GameObject("Row_" + (r + 1));
                row.transform.SetParent(tableContainer.transform, false);
                RectTransform rRt = row.AddComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0f, 1f);
                rRt.anchorMax = new Vector2(1f, 1f);
                rRt.pivot = new Vector2(0.5f, 1f);
                rRt.anchoredPosition = new Vector2(0f, -(r * 32f));
                rRt.sizeDelta = new Vector2(0f, 30f);

                Color rowCol = (r == 0) ? cyanColor : Color.white;
                CreateText("RankText", row.transform, new Vector2(0.12f, 0.5f), new Vector2(0.12f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 28f), (r + 1).ToString(), font, 13, FontStyle.Bold, rowCol, TextAnchor.MiddleCenter);
                CreateText("NameText", row.transform, new Vector2(0.30f, 0.5f), new Vector2(0.30f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 28f), "MATT", font, 13, FontStyle.Bold, rowCol, TextAnchor.MiddleLeft);
                CreateText("ScoreText", row.transform, new Vector2(0.55f, 0.5f), new Vector2(0.55f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 28f), "123939", font, 13, FontStyle.Bold, rowCol, TextAnchor.MiddleRight);
                CreateText("StageText", row.transform, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 28f), "6", font, 13, FontStyle.Bold, rowCol, TextAnchor.MiddleCenter);
                CreateText("DiffText", row.transform, new Vector2(0.88f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 28f), "EASY", font, 13, FontStyle.Bold, rowCol, TextAnchor.MiddleCenter);
            }

            uiMgr.btnHallOfFameClose = CreateSmallButton("Btn_Close", hofModal.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(160f, 40f), "BACK TO MENU", font, spBtn, spBtnHover, spBtnPressed);

            // -------------------------------------------------------------
            // 4. IN-GAME PAUSE MODAL
            // -------------------------------------------------------------
            GameObject pauseObj = CreatePanel("PauseModal", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 380f), spWindow, Image.Type.Sliced);
            uiMgr.pauseModal = pauseObj.GetComponent<RectTransform>();

            CreateText("PauseTitle", pauseObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(300f, 36f), "TACTICAL PAUSE", font, 18, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);
            uiMgr.btnPauseResume = CreatePillButton("Btn_Resume", pauseObj.transform, new Vector2(0f, -90f), "RESUME", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnPauseOptions = CreatePillButton("Btn_Options", pauseObj.transform, new Vector2(0f, -155f), "OPTIONS", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnPauseRestart = CreatePillButton("Btn_Restart", pauseObj.transform, new Vector2(0f, -220f), "RESTART MISSION", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnPauseMainMenu = CreatePillButton("Btn_MainMenu", pauseObj.transform, new Vector2(0f, -285f), "ABORT TO MENU", font, spBtn, spBtnHover, spBtnPressed);

            // -------------------------------------------------------------
            // 5. MISSION DEBRIEFING / GAME OVER MODAL
            // -------------------------------------------------------------
            GameObject debriefObj = CreatePanel("DebriefingModal", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 400f), spWindow, Image.Type.Sliced);
            uiMgr.debriefingModal = debriefObj.GetComponent<RectTransform>();

            CreateText("DebriefTitle", debriefObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(400f, 36f), "MISSION DEBRIEFING", font, 18, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);

            uiMgr.txtDebriefScore = CreateText("DebriefScore", debriefObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(400f, 32f), "FINAL SCORE: 000,000", font, 16, FontStyle.Bold, cyanColor, TextAnchor.MiddleCenter).GetComponent<Text>();
            uiMgr.txtDebriefHits = CreateText("DebriefHits", debriefObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(400f, 32f), "TOTAL HITS: 000", font, 16, FontStyle.Bold, cyanColor, TextAnchor.MiddleCenter).GetComponent<Text>();
            uiMgr.txtDebriefRank = CreateText("DebriefRank", debriefObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(400f, 36f), "RANK: CLASS S (GOLD MEDAL)", font, 18, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter).GetComponent<Text>();

            uiMgr.btnDebriefRestart = CreatePillButton("Btn_Retry", debriefObj.transform, new Vector2(0f, -270f), "RETRY MISSION", font, spBtn, spBtnHover, spBtnPressed);
            uiMgr.btnDebriefMainMenu = CreatePillButton("Btn_MainMenu", debriefObj.transform, new Vector2(0f, -335f), "MAIN MENU", font, spBtn, spBtnHover, spBtnPressed);
        }

        #region HELPER BUILDERS

        private static GameObject CreatePanel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, Sprite sp, Image.Type imgType = Image.Type.Simple)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = sp;
            img.type = imgType;
            img.color = Color.white;
            return obj;
        }

        private static GameObject CreateText(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, string text, Font font, int fontSize, FontStyle fontStyle, Color color, TextAnchor align)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.color = color;
            txt.alignment = align;
            return obj;
        }

        private static Button CreatePillButton(string name, Transform parent, Vector2 pos, string label, Font font, Sprite spNorm, Sprite spHover, Sprite spPressed)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240f, 52f);

            Image img = obj.AddComponent<Image>();
            img.sprite = spNorm;
            img.type = Image.Type.Sliced;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            var cb = btn.colors;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;

            CreateText("Label", obj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, font, 14, FontStyle.Bold, new Color(0f, 0.95f, 1f, 1f), TextAnchor.MiddleCenter);

            return btn;
        }

        private static Button CreateSmallButton(string name, Transform parent, Vector2 aMin, Vector2 pos, Vector2 size, string label, Font font, Sprite spNorm, Sprite spHover, Sprite spPressed)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = spNorm;
            img.type = Image.Type.Sliced;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            CreateText("Label", obj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, font, 13, FontStyle.Bold, new Color(0f, 0.95f, 1f, 1f), TextAnchor.MiddleCenter);

            return btn;
        }

        private static Button CreateTabButton(string name, Transform parent, Vector2 pos, string label, Font font, Sprite sp, Color textColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(135f, 42f);

            Image img = obj.AddComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Sliced;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            CreateText("Label", obj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, font, 13, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);

            return btn;
        }

        private static Button CreateIconButton(string name, Transform parent, Vector2 aMin, Vector2 pos, Vector2 size, Sprite sp)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static Toggle CreateCheckbox(string name, Transform parent, Vector2 aMin, Vector2 pos, Sprite frameSp, Sprite checkSp)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(36f, 36f);

            Image bgImg = obj.AddComponent<Image>();
            bgImg.sprite = frameSp;

            Toggle toggle = obj.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;

            // Checkmark
            GameObject cm = new GameObject("Checkmark");
            cm.transform.SetParent(obj.transform, false);
            RectTransform cmRt = cm.AddComponent<RectTransform>();
            cmRt.anchorMin = Vector2.zero;
            cmRt.anchorMax = Vector2.one;
            cmRt.offsetMin = Vector2.zero;
            cmRt.offsetMax = Vector2.zero;

            Image cmImg = cm.AddComponent<Image>();
            cmImg.sprite = checkSp;
            cmImg.preserveAspect = true;

            toggle.graphic = cmImg;
            return toggle;
        }

        private static (Slider slider, RectTransform handle) CreateSlider(string name, Transform parent, Vector2 aMin, Vector2 pos, Vector2 size, float minVal, float maxVal, Sprite spTrack, Sprite spFill, Sprite spThumb)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Background Track
            Image trackImg = obj.AddComponent<Image>();
            trackImg.sprite = spTrack;
            trackImg.type = Image.Type.Sliced;

            Slider slider = obj.AddComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = minVal;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(obj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(4f, 0f);
            faRt.offsetMax = new Vector2(-4f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.AddComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            Image fImg = fill.AddComponent<Image>();
            fImg.sprite = spFill;
            fImg.type = Image.Type.Sliced;
            slider.fillRect = fRt;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(obj.transform, false);
            RectTransform haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10f, 0f);
            haRt.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRt = handle.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(20f, 28f);
            Image hImg = handle.AddComponent<Image>();
            hImg.sprite = spThumb;
            hImg.preserveAspect = true;
            slider.handleRect = hRt;
            slider.targetGraphic = hImg;

            return (slider, hRt);
        }

        private static Dropdown CreateDropdown(string name, Transform parent, Vector2 aMin, Vector2 pos, Vector2 size, Font font, Sprite spFrame)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = spFrame;
            img.type = Image.Type.Sliced;

            Dropdown dropdown = obj.AddComponent<Dropdown>();
            dropdown.targetGraphic = img;

            // Caption Text
            GameObject caption = CreateText("Label", obj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, 0f), "Dropdown", font, 12, FontStyle.Bold, new Color(0f, 0.95f, 1f, 1f), TextAnchor.MiddleLeft);
            dropdown.captionText = caption.GetComponent<Text>();

            // Template
            GameObject template = new GameObject("Template");
            template.transform.SetParent(obj.transform, false);
            RectTransform tRt = template.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, 2f);
            tRt.sizeDelta = new Vector2(0f, 150f);

            Image tImg = template.AddComponent<Image>();
            tImg.color = new Color(0.04f, 0.10f, 0.16f, 0.98f);
            ScrollRect sr = template.AddComponent<ScrollRect>();

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            RectTransform vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.white;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 28f);

            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            RectTransform itemRt = item.AddComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 26f);

            Toggle itemToggle = item.AddComponent<Toggle>();
            GameObject itemLabel = CreateText("Item Label", item.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f), "Option", font, 11, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);

            dropdown.template = tRt;
            dropdown.itemText = itemLabel.GetComponent<Text>();
            sr.content = cRt;
            sr.viewport = vpRt;
            template.SetActive(false);

            return dropdown;
        }

        #endregion
    }
}
