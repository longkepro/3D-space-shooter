using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using VirtualRail;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ Editor 1-Click thiết lập và quản lý Màn Hình Bìa Ngoài Cùng (Title Screen System).
    /// Tuân thủ nghiêm ngặt Quy tắc An toàn 3 Tầng (AGENTS.md / GEMINI.md Điều 3):
    /// Cung cấp cặp MenuItem 1-Click đối xứng Bật [ON] / Hoàn nguyên [OFF].
    /// </summary>
    [InitializeOnLoad]
    public static class TitleScreenEditorTool
    {
        public const string TITLE_COVER_PATH = "Assets/Textures/UI/title_screen_cover.png";
        public const string TITLE_PANEL_NAME = "TitleScreenPanel";
        private const string PREF_AUTO_SETUP = "VirtualRail_TitleScreen_Enabled";

        static TitleScreenEditorTool()
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
                    UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
                    if (uiMgr != null)
                    {
                        Transform existing = uiMgr.transform.Find(TITLE_PANEL_NAME);
                        if (existing == null || uiMgr.titleScreenPanel == null)
                        {
                            SetupTitleScreenSystem();
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/Virtual Rail/Title Screen/1. Setup Title Screen System (ON)", false, 70)]
        [MenuItem("Tools/Virtual Rail/70. Setup Title Screen System (ON)", false, 70)]
        public static void SetupTitleScreenSystem()
        {
            EditorPrefs.SetBool(PREF_AUTO_SETUP, true);

            // Cấu hình TextureImporter sang SpriteMode.Single
            ConfigureSpriteTextureImporter(TITLE_COVER_PATH);
            AssetDatabase.Refresh();

            Sprite coverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TITLE_COVER_PATH);
            if (coverSprite == null)
            {
                Debug.LogError($"[TitleScreen] Không tìm thấy Sprite ảnh bìa tại {TITLE_COVER_PATH}!");
                return;
            }

            UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
            if (uiMgr == null)
            {
                Debug.LogError("[TitleScreen] Không tìm thấy UIManager trong Scene hiện tại!");
                return;
            }

            Transform uiRoot = uiMgr.transform;
            Transform existing = uiRoot.Find(TITLE_PANEL_NAME);
            GameObject panelObj;

            if (existing != null)
            {
                panelObj = existing.gameObject;
            }
            else
            {
                panelObj = new GameObject(TITLE_PANEL_NAME, typeof(RectTransform), typeof(CanvasGroup));
                panelObj.transform.SetParent(uiRoot, false);
            }

            panelObj.transform.SetAsLastSibling(); // Luôn hiển thị trên cùng

            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            CanvasGroup panelCg = panelObj.GetComponent<CanvasGroup>();
            panelCg.alpha = 1f;
            panelCg.interactable = true;
            panelCg.blocksRaycasts = true;
            panelObj.SetActive(true);

            TitleScreenController controller = panelObj.GetComponent<TitleScreenController>();
            if (controller == null) controller = panelObj.AddComponent<TitleScreenController>();

            controller.titleCanvasGroup = panelCg;
            controller.coverSprite = coverSprite;

            // 1. Ảnh bìa toàn màn hình (TitleCoverDisplay)
            Transform coverTr = panelRt.Find("TitleCoverDisplay");
            GameObject coverObj = coverTr != null ? coverTr.gameObject : new GameObject("TitleCoverDisplay", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            coverObj.transform.SetParent(panelRt, false);
            coverObj.transform.SetAsFirstSibling();

            RectTransform coverRt = coverObj.GetComponent<RectTransform>();
            coverRt.anchorMin = Vector2.zero;
            coverRt.anchorMax = Vector2.one;
            coverRt.offsetMin = Vector2.zero;
            coverRt.offsetMax = Vector2.zero;

            Image coverImg = coverObj.GetComponent<Image>();
            coverImg.sprite = coverSprite;
            coverImg.preserveAspect = true;
            coverImg.raycastTarget = false;
            controller.coverDisplayImage = coverImg;

            AspectRatioFitter arf = coverObj.GetComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1376f / 768f;

            // 2. Nút tương tác bấm toàn màn hình (BtnStartTrigger)
            Transform btnTr = panelRt.Find("BtnStartTrigger");
            GameObject btnObj = btnTr != null ? btnTr.gameObject : new GameObject("BtnStartTrigger", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(panelRt, false);

            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0f, 0f, 0f, 0f); // Trong suốt
            btnImg.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();
            controller.btnStart = btn;

            // 3. Dòng chữ nhấp nháy phát sáng phía dưới (TxtPressStartPrompt)
            Transform promptTr = panelRt.Find("TxtPressStartPrompt");
            GameObject promptObj = promptTr != null ? promptTr.gameObject : new GameObject("TxtPressStartPrompt", typeof(RectTransform), typeof(Text));
            promptObj.transform.SetParent(panelRt, false);

            RectTransform promptRt = promptObj.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 0.16f);
            promptRt.anchorMax = new Vector2(0.5f, 0.16f);
            promptRt.anchoredPosition = Vector2.zero;
            promptRt.sizeDelta = new Vector2(650f, 40f);

            Text promptTxt = promptObj.GetComponent<Text>();
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Orbitron.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptTxt.font = font;
            promptTxt.fontSize = 21;
            promptTxt.fontStyle = FontStyle.Bold;
            promptTxt.alignment = TextAnchor.MiddleCenter;
            promptTxt.color = new Color(0.2f, 0.95f, 1f, 1f);
            promptTxt.text = "PRESS ANY KEY OR CLICK TO LAUNCH";
            promptTxt.raycastTarget = false; // Triệt tiêu raycast target để không cản trở click chuột vào nút bấm
            controller.txtPrompt = promptTxt;

            // Đăng ký sự kiện Click cho Button và khởi tạo
            controller.SetupButtons();
            controller.StartTitleScreen();

            // Gán vào UIManager
            uiMgr.titleScreenPanel = panelRt;
            uiMgr.titleScreenController = controller;

            EditorUtility.SetDirty(uiMgr);
            EditorUtility.SetDirty(panelObj);
            EditorSceneManager.MarkSceneDirty(uiMgr.gameObject.scene);
            EditorSceneManager.SaveScene(uiMgr.gameObject.scene);
            AssetDatabase.SaveAssets();

            if (!System.IO.Directory.Exists("Assets/Prefabs")) System.IO.Directory.CreateDirectory("Assets/Prefabs");
            if (!System.IO.Directory.Exists("Assets/Resources")) System.IO.Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(panelObj, "Assets/Prefabs/TitleScreenPanel.prefab");
            PrefabUtility.SaveAsPrefabAsset(panelObj, "Assets/Resources/TitleScreenPanel.prefab");

            Debug.Log("<color=#00FF99><b>[TitleScreen] Đã thiết lập hoàn tất Hệ Thống Màn Hình Bìa Ngoài Cùng vào UIManager và lưu Scene/Prefab!</b></color>");
        }

        [MenuItem("Tools/Virtual Rail/Title Screen/2. Remove Title Screen System (OFF - Undo)", false, 71)]
        [MenuItem("Tools/Virtual Rail/71. Remove Title Screen System (OFF - Undo)", false, 71)]
        public static void RemoveTitleScreenSystem()
        {
            EditorPrefs.SetBool(PREF_AUTO_SETUP, false);

            UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null)
            {
                uiMgr.titleScreenPanel = null;
                uiMgr.titleScreenController = null;
                EditorUtility.SetDirty(uiMgr);

                Transform panel = uiMgr.transform.Find(TITLE_PANEL_NAME);
                if (panel != null)
                {
                    Undo.DestroyObjectImmediate(panel.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(uiMgr.gameObject.scene);
                EditorSceneManager.SaveScene(uiMgr.gameObject.scene);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=#FF9900><b>[TitleScreen] Đã hoàn nguyên loại bỏ Title Screen System khỏi UIManager.</b></color>");
            }
        }

        private static void ConfigureSpriteTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
