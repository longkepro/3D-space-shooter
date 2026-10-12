using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using VirtualRail;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ Editor 1-Click thiết lập và quản lý Hệ Thống Truyện Tranh Dẫn Nhập (Comic Cutscene System).
    /// Tuân thủ nghiêm ngặt Quy tắc An toàn 3 Tầng (AGENTS.md / GEMINI.md Điều 3):
    /// Cung cấp cặp MenuItem 1-Click đối xứng Bật [ON] / Hoàn nguyên [OFF].
    /// </summary>
    [InitializeOnLoad]
    public static class ComicCutsceneEditorTool
    {
        public const string COMIC_P1_PATH = "Assets/Textures/Comic/comic_page_1.png";
        public const string COMIC_P2_PATH = "Assets/Textures/Comic/comic_page_2.png";
        public const string COMIC_P3_PATH = "Assets/Textures/Comic/comic_page_3.png";
        public const string COMIC_P4_PATH = "Assets/Textures/Comic/comic_page_4.png";

        public const string COMIC_PANEL_NAME = "ComicCutscenePanel";
        private const string PREF_AUTO_SETUP = "VirtualRail_ComicCutscene_Enabled";

        static ComicCutsceneEditorTool()
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
                        Transform existing = uiMgr.transform.Find(COMIC_PANEL_NAME);
                        if (existing == null || uiMgr.comicCutscenePanel == null)
                        {
                            SetupComicCutsceneSystem();
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/Virtual Rail/Cutscene/1. Setup Comic Cutscene System (ON)", false, 60)]
        [MenuItem("Tools/Virtual Rail/60. Setup Comic Cutscene System (ON)", false, 60)]
        public static void SetupComicCutsceneSystem()
        {
            EditorPrefs.SetBool(PREF_AUTO_SETUP, true);

            // Đảm bảo các Sprite đã được cấu hình TextureImporter sang SpriteMode.Single
            ConfigureComicTextureImporter(COMIC_P1_PATH);
            ConfigureComicTextureImporter(COMIC_P2_PATH);
            ConfigureComicTextureImporter(COMIC_P3_PATH);
            ConfigureComicTextureImporter(COMIC_P4_PATH);
            AssetDatabase.Refresh();

            Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>(COMIC_P1_PATH);
            Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>(COMIC_P2_PATH);
            Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>(COMIC_P3_PATH);
            Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>(COMIC_P4_PATH);

            if (s1 == null || s2 == null || s3 == null || s4 == null)
            {
                Debug.LogError("[ComicCutscene] Không tìm thấy đủ 4 Sprite truyện tranh tại Assets/Textures/Comic!");
                return;
            }

            UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
            if (uiMgr == null)
            {
                Debug.LogError("[ComicCutscene] Không tìm thấy UIManager trong Scene hiện tại!");
                return;
            }

            Transform uiRoot = uiMgr.transform;
            Transform existing = uiRoot.Find(COMIC_PANEL_NAME);
            GameObject panelObj;

            if (existing != null)
            {
                panelObj = existing.gameObject;
            }
            else
            {
                panelObj = new GameObject(COMIC_PANEL_NAME, typeof(RectTransform), typeof(CanvasGroup));
                panelObj.transform.SetParent(uiRoot, false);
            }

            // Đặt Panel lên trên cùng của UIManager nhưng dưới Settings Modal
            panelObj.transform.SetSiblingIndex(Mathf.Max(0, uiRoot.childCount - 3));

            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            CanvasGroup panelCg = panelObj.GetComponent<CanvasGroup>();
            panelCg.alpha = 0f;
            panelCg.interactable = false;
            panelCg.blocksRaycasts = false;
            panelObj.SetActive(false);

            ComicCutsceneController controller = panelObj.GetComponent<ComicCutsceneController>();
            if (controller == null) controller = panelObj.AddComponent<ComicCutsceneController>();

            controller.comicPages = new Sprite[] { s1, s2, s3, s4 };
            controller.cutsceneCanvasGroup = panelCg;

            // 1. Nền mờ Backdrop màu đen thẫm
            Transform bgTr = panelRt.Find("BackdropBackground");
            GameObject bgObj = bgTr != null ? bgTr.gameObject : new GameObject("BackdropBackground", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(panelRt, false);
            bgObj.transform.SetAsFirstSibling();
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
            bgImg.raycastTarget = true;

            // 2. Khung ảnh truyện tranh chính (ComicPageDisplay)
            Transform displayTr = panelRt.Find("ComicPageDisplay");
            GameObject displayObj = displayTr != null ? displayTr.gameObject : new GameObject("ComicPageDisplay", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            displayObj.transform.SetParent(panelRt, false);
            RectTransform displayRt = displayObj.GetComponent<RectTransform>();
            displayRt.anchorMin = new Vector2(0.02f, 0.02f);
            displayRt.anchorMax = new Vector2(0.98f, 0.98f);
            displayRt.offsetMin = Vector2.zero;
            displayRt.offsetMax = Vector2.zero;
            Image displayImg = displayObj.GetComponent<Image>();
            displayImg.sprite = s1;
            displayImg.preserveAspect = true;
            displayImg.raycastTarget = false;

            AspectRatioFitter arf = displayObj.GetComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1376f / 768f;

            controller.comicDisplayImage = displayImg;

            // 3. Nút TIẾP THEO  ▶▶ (BtnNext)
            controller.btnNext = CreateCutsceneButton(panelRt, "BtnNext", "TIẾP THEO  ▶▶", new Vector2(1f, 0.05f), new Vector2(1f, 0.05f), new Vector2(-140f, 45f), new Vector2(220f, 52f), isSkip: false);

            // 4. Nút ◀◀  TRƯỚC (BtnPrev)
            controller.btnPrev = CreateCutsceneButton(panelRt, "BtnPrev", "◀◀  TRƯỚC", new Vector2(0f, 0.05f), new Vector2(0f, 0.05f), new Vector2(140f, 45f), new Vector2(200f, 52f), isSkip: false);

            // 5. Nút BỎ QUA [ESC]  ▶▶ (BtnSkip)
            controller.btnSkip = CreateCutsceneButton(panelRt, "BtnSkip", "BỎ QUA [ESC]  ▶▶", new Vector2(1f, 0.95f), new Vector2(1f, 0.95f), new Vector2(-115f, -32f), new Vector2(185f, 42f), isSkip: true);

            // 6. Text Chỉ Số Trang (TxtPageIndicator)
            Transform indTr = panelRt.Find("TxtPageIndicator");
            GameObject indObj = indTr != null ? indTr.gameObject : new GameObject("TxtPageIndicator", typeof(RectTransform), typeof(Image));
            indObj.transform.SetParent(panelRt, false);
            RectTransform indRt = indObj.GetComponent<RectTransform>();
            indRt.anchorMin = new Vector2(0.5f, 0.05f);
            indRt.anchorMax = new Vector2(0.5f, 0.05f);
            indRt.pivot = new Vector2(0.5f, 0.5f);
            indRt.anchoredPosition = new Vector2(0f, 45f);
            indRt.sizeDelta = new Vector2(200f, 42f);

            Image indBg = indObj.GetComponent<Image>();
            if (indBg == null) indBg = indObj.AddComponent<Image>();
            Sprite spPill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic.png");
            if (spPill != null)
            {
                indBg.sprite = spPill;
                indBg.type = Image.Type.Sliced;
                indBg.color = new Color(0.04f, 0.08f, 0.15f, 0.88f);
            }
            indBg.raycastTarget = false;

            Transform indTxtTr = indRt.Find("Text");
            GameObject indTxtObj = indTxtTr != null ? indTxtTr.gameObject : new GameObject("Text", typeof(RectTransform), typeof(Text));
            indTxtObj.transform.SetParent(indRt, false);
            RectTransform indTxtRt = indTxtObj.GetComponent<RectTransform>();
            indTxtRt.anchorMin = Vector2.zero;
            indTxtRt.anchorMax = Vector2.one;
            indTxtRt.offsetMin = Vector2.zero;
            indTxtRt.offsetMax = Vector2.zero;

            Text indTxt = indTxtObj.GetComponent<Text>();
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Orbitron.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            indTxt.font = font;
            indTxt.fontSize = 14;
            indTxt.fontStyle = FontStyle.Bold;
            indTxt.alignment = TextAnchor.MiddleCenter;
            indTxt.color = new Color(0.2f, 0.95f, 1f, 1f);
            indTxt.text = "TRANG 1 / 4";
            indTxt.raycastTarget = false;
            controller.txtPageIndicator = indTxt;
            controller.txtNextLabel = controller.btnNext != null ? controller.btnNext.GetComponentInChildren<Text>() : null;

            // Liên kết vào UIManager
            uiMgr.comicCutscenePanel = panelRt;
            uiMgr.comicCutsceneController = controller;

            EditorUtility.SetDirty(uiMgr);
            EditorUtility.SetDirty(panelObj);
            EditorSceneManager.MarkSceneDirty(uiMgr.gameObject.scene);
            EditorSceneManager.SaveScene(uiMgr.gameObject.scene);
            AssetDatabase.SaveAssets();

            if (!System.IO.Directory.Exists("Assets/Prefabs")) System.IO.Directory.CreateDirectory("Assets/Prefabs");
            if (!System.IO.Directory.Exists("Assets/Resources")) System.IO.Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(panelObj, "Assets/Prefabs/ComicCutscenePanel.prefab");
            PrefabUtility.SaveAsPrefabAsset(panelObj, "Assets/Resources/ComicCutscenePanel.prefab");

            Debug.Log("<color=#00FF99><b>[ComicCutscene] Đã thiết lập hoàn tất Hệ Thống Truyện Tranh Dẫn Nhập vào UIManager và lưu Scene/Prefab!</b></color>");
        }

        [MenuItem("Tools/Virtual Rail/Cutscene/2. Remove Comic Cutscene System (OFF - Undo)", false, 61)]
        [MenuItem("Tools/Virtual Rail/61. Remove Comic Cutscene System (OFF - Undo)", false, 61)]
        public static void RemoveComicCutsceneSystem()
        {
            EditorPrefs.SetBool(PREF_AUTO_SETUP, false);

            UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null)
            {
                uiMgr.comicCutscenePanel = null;
                uiMgr.comicCutsceneController = null;
                EditorUtility.SetDirty(uiMgr);

                Transform panel = uiMgr.transform.Find(COMIC_PANEL_NAME);
                if (panel != null)
                {
                    Undo.DestroyObjectImmediate(panel.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(uiMgr.gameObject.scene);
                EditorSceneManager.SaveScene(uiMgr.gameObject.scene);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=#FF9900><b>[ComicCutscene] Đã hoàn nguyên loại bỏ Comic Cutscene System khỏi UIManager.</b></color>");
            }
        }

        private static void ConfigureComicTextureImporter(string path)
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

        private static Button CreateCutsceneButton(RectTransform parent, string name, string textLabel, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, bool isSkip = false)
        {
            Transform existing = parent.Find(name);
            GameObject btnObj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Sprite spNorm = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic.png");
            Sprite spHover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic_hover.png");
            Sprite spPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_btn_pill_metallic_pressed.png");

            Image img = btnObj.GetComponent<Image>();
            if (spNorm != null)
            {
                img.sprite = spNorm;
                img.type = Image.Type.Sliced;
                img.color = isSkip ? new Color(1f, 0.82f, 0.82f, 0.92f) : Color.white;
            }
            else
            {
                img.color = isSkip ? new Color(0.85f, 0.25f, 0.25f, 0.9f) : new Color(0.08f, 0.45f, 0.85f, 0.95f);
            }

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;

            if (spHover != null && spPressed != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState ss = new SpriteState();
                ss.highlightedSprite = spHover;
                ss.pressedSprite = spPressed;
                ss.selectedSprite = spHover;
                btn.spriteState = ss;
            }
            else
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                btn.colors = cb;
            }

            Transform txtTr = rt.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(rt, false);

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Text txt = txtObj.GetComponent<Text>();
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Orbitron.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.font = font;
            txt.fontSize = isSkip ? 13 : 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = isSkip ? new Color(1f, 0.45f, 0.45f, 1f) : new Color(0f, 0.95f, 1f, 1f);
            txt.text = textLabel;
            txt.raycastTarget = false;

            return btn;
        }
    }
}
