using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using VirtualRail;

[InitializeOnLoad]
public class VirtualRailModeToggle
{
    private const string ORIGINAL_SHIP_PREFAB = "Assets/Prefabs/Player Ship (1).prefab";
    private const string VIRTUAL_RAIL_PREFAB = "Assets/Prefabs/VirtualRail_PlayerRig.prefab";
    private const string CONFIG_PATH = "Assets/Scripts/VirtualRail/DefaultVirtualRailConfig.asset";
    private const string MISSILE_PREFAB = "Assets/FreeSciFi/Prefabs/MinesBombs/Missile.prefab";
    private const string COSMIC_EARTH_TEXTURE_PATH = "Assets/Textures/Skybox/skybox_cosmic_earth.jpg";
    private const string COSMIC_EARTH_MATERIAL_PATH = "Assets/Materials/Skybox_Image1_CosmicEarth.mat";
    private const string ORIGINAL_SKYBOX_MATERIAL_PATH = "Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat";
    private const string SKYBOX_INIT_KEY = "VirtualRail_Skybox_Image1_Initialized";
    private const string BACKDROP_BAKED_KEY = "VirtualRail_Backdrop_Baked_v5";

    static VirtualRailModeToggle()
    {
        // Gỡ bỏ tự động cưỡng ép Cosmic Earth để cho phép Skybox gốc và StageData toàn quyền quyết định
        EditorApplication.delayCall += InitializeBackdropPrefabOnce;
    }

    private static void InitializeBackdropPrefabOnce()
    {
        if (!SessionState.GetBool(BACKDROP_BAKED_KEY, false))
        {
            SessionState.SetBool(BACKDROP_BAKED_KEY, true);
            RebuildPlayerRigPrefab();
        }
    }

    public static void RebuildPlayerRigPrefab()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config == null) return;

        GameObject railRig = BuildVirtualRailRig(config);
        PrefabUtility.SaveAsPrefabAsset(railRig, VIRTUAL_RAIL_PREFAB);
        Object.DestroyImmediate(railRig);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ TỰ ĐỘNG BAKE LẠI PREFAB VirtualRail_PlayerRig:</b></color> [Horizon Backdrop] đã được lưu trực tiếp vào cây phân cấp của Prefab!");
    }

    [MenuItem("Tools/Virtual Rail/1. Switch to Virtual Rail Mode (ACTIVE)")]
    public static void SwitchToVirtualRail()
    {
        // 1. Đảm bảo có Config
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<VirtualRailConfig>();
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();
        }

        // 2. Tạo Prefab VirtualRail_PlayerRig độc lập (Không đè file gốc)
        GameObject railRig = BuildVirtualRailRig(config);
        PrefabUtility.SaveAsPrefabAsset(railRig, VIRTUAL_RAIL_PREFAB);
        Object.DestroyImmediate(railRig);
        AssetDatabase.SaveAssets();

        // 3. Cập nhật GameUI trong Scene Game.unity
        GameObject virtualRailAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (virtualRailAsset == null)
        {
            Debug.LogError("[VirtualRail] Không tìm thấy VirtualRail_PlayerRig.prefab vừa tạo!");
            return;
        }

        GameUI gameUI = Object.FindAnyObjectByType<GameUI>();
        if (gameUI != null)
        {
            SerializedObject so = new SerializedObject(gameUI);
            SerializedProperty prop = so.FindProperty("_playerPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = virtualRailAsset;
                so.ApplyModifiedProperties();
            }

            // Vô hiệu hóa Follow Cam cũ trong Scene
            FollowCam followCam = Object.FindAnyObjectByType<FollowCam>();
            if (followCam != null)
            {
                followCam.enabled = false;
                EditorUtility.SetDirty(followCam);
            }

            // Đảm bảo có EventSystem trong Scene cho uGUI Touch Joysticks
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT THÀNH CÔNG CHẾ ĐỘ VIRTUAL RAIL!</b></color>\nGameUI._playerPrefab hiện đã trỏ tới: VirtualRail_PlayerRig.prefab. Hãy bấm PLAY và click 'Play Game' để trải nghiệm!");
        }
        else
        {
            Debug.LogWarning("[VirtualRail] Không tìm thấy GameUI trong Scene hiện tại. Hãy mở Scene 'Assets/Scenes/Game.unity' rồi chạy lại lệnh này.");
        }
    }

    [MenuItem("Tools/Virtual Rail/2. Revert to Classic System (UNDO)")]
    public static void RevertToClassic()
    {
        GameObject originalShip = AssetDatabase.LoadAssetAtPath<GameObject>(ORIGINAL_SHIP_PREFAB);
        if (originalShip == null)
        {
            Debug.LogError("[VirtualRail] Không tìm thấy Player Ship (1).prefab gốc!");
            return;
        }

        GameUI gameUI = Object.FindAnyObjectByType<GameUI>();
        if (gameUI != null)
        {
            SerializedObject so = new SerializedObject(gameUI);
            SerializedProperty prop = so.FindProperty("_playerPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = originalShip;
                so.ApplyModifiedProperties();
            }

            // Bật lại Follow Cam cũ trong Scene
            FollowCam followCam = Object.FindAnyObjectByType<FollowCam>();
            if (followCam != null)
            {
                followCam.enabled = true;
                EditorUtility.SetDirty(followCam);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ HỆ THỐNG GỐC!</b></color>\nGameUI._playerPrefab đã được trỏ lại: Player Ship (1).prefab nguyên bản.");
        }
        else
        {
            Debug.LogWarning("[VirtualRail] Không tìm thấy GameUI trong Scene hiện tại.");
        }
    }

    // =========================================================================
    // HORIZON BACKDROP CONTROLS (Man chieu chan troi do net cao khoa truoc mui tau)
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/3. Backdrop: Switch to Refined Horizon v2 (Fixed AI Flaws - ON)")]
    public static void EnableRefinedHorizonV2()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableHorizonBackdrop = true;
            config.backdropDistanceZ = 850f;
            config.backdropMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Backdrop_Image1_CosmicEarth_v2.mat");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] DA CHUYEN SANG MAN CHIEU CHAN TROI TINH CHINH V2:</b></color> Da khac phuc 5 loi AI: Mat trang co dia chat ho va cham chuan, may cuon xoay Coriolis, quang hoc thau kinh tu nhien, triet tieu vien halo!");
        }

        RebuildPlayerRigPrefab();

        var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bd in backdrops)
        {
            if (config != null && config.backdropMaterial != null) bd.backdropMaterial = config.backdropMaterial;
            bd.Initialize();
            bd.SetBackdropActive(true);
        }
    }

    [MenuItem("Tools/Virtual Rail/4. Backdrop: Undo to v1 Horizon (OFF - Revert to v1)")]
    public static void RevertToHorizonV1()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableHorizonBackdrop = true;
            config.backdropDistanceZ = 850f;
            config.backdropMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Backdrop_Image1_CosmicEarth.mat");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] DA HOAN NGUYEN (UNDO) VE MAN CHIEU V1 BAN DAU:</b></color> Su dung Backdrop_Image1_CosmicEarth.mat.");
        }

        RebuildPlayerRigPrefab();

        var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bd in backdrops)
        {
            if (config != null && config.backdropMaterial != null) bd.backdropMaterial = config.backdropMaterial;
            bd.Initialize();
            bd.SetBackdropActive(true);
        }
    }

    [MenuItem("Tools/Virtual Rail/5. Backdrop: Toggle Horizon Backdrop (ON/OFF)")]
    public static void ToggleHorizonBackdrop()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableHorizonBackdrop = !config.enableHorizonBackdrop;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=cyan><b>[VirtualRail] TRANG THAI HORIZON BACKDROP:</b></color> {(config.enableHorizonBackdrop ? "BAT (ON)" : "TAT (OFF)")}");
        }

        RebuildPlayerRigPrefab();

        var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bd in backdrops)
        {
            if (config != null && config.backdropMaterial != null) bd.backdropMaterial = config.backdropMaterial;
            bd.Initialize();
            bd.SetBackdropActive(config != null && config.enableHorizonBackdrop);
        }
    }

    // =========================================================================
    // STAGE 4K BACKDROP PREVIEWS & SKYBOX CONTROLS
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/Stage Backdrops/Preview Stage 1 Backdrop (4K)")]
    public static void PreviewStage1Backdrop()
    {
        ApplyBackdropPreview("Assets/Materials/Stage1_Backdrop.mat", "Stage 1 (4K)");
    }

    [MenuItem("Tools/Virtual Rail/Stage Backdrops/Preview Stage 2 Backdrop (4K)")]
    public static void PreviewStage2Backdrop()
    {
        ApplyBackdropPreview("Assets/Materials/Stage2_Backdrop.mat", "Stage 2 (4K)");
    }

    // =========================================================================
    // STAGE 2 CANDIDATE SELECTION (OPTION 2 vs OPTION 3 vs LEGACY)
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/Stage 2 Options/1. Apply Option 2 (3-Tier Parallax 4K)")]
    public static void ApplyStage2Option2()
    {
        SwitchStage2Texture("Assets/Textures/Skybox/stage2_option2_parallax_4k.png", "Phương án 2 (3-Tier Parallax 4K)");
    }

    [MenuItem("Tools/Virtual Rail/Stage 2 Options/2. Apply Option 3 (8K Remaster 4K)")]
    public static void ApplyStage2Option3()
    {
        SwitchStage2Texture("Assets/Textures/Skybox/stage2_option3_remaster_4k.png", "Phương án 3 (8K Remaster 4K)");
    }

    [MenuItem("Tools/Virtual Rail/Stage 2 Options/3. Restore Stage 2 (Legacy 768p Backup)")]
    public static void RestoreStage2Legacy()
    {
        SwitchStage2Texture("Assets/Textures/Skybox/stage2_backup_768p.png", "Bản gốc cũ (Legacy 768p)");
    }

    private static void SwitchStage2Texture(string sourcePath, string optionName)
    {
        string targetPath = "Assets/Textures/Skybox/stage2.png";
        if (!System.IO.File.Exists(sourcePath))
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy tệp nguồn: {sourcePath}");
            return;
        }

        System.IO.File.Copy(sourcePath, targetPath, true);
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        ApplyBackdropPreview("Assets/Materials/Stage2_Backdrop.mat", $"Stage 2 ({optionName})");
        Debug.Log($"<color=green><b>[VirtualRail] ĐÃ ÁP DỤNG THÀNH CÔNG CHO STAGE 2:</b></color> {optionName}");
    }

    // =========================================================================
    // STAGE 3 CANDIDATE SELECTION (MOTHERSHIP SAUCER 4K vs CLEAN SKY 5K vs CITADEL LEGACY)
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/Stage 3 Options/1. Apply Mothership Saucer (4K UHD)")]
    public static void ApplyStage3MothershipSaucer()
    {
        SwitchStage3Texture("Assets/Textures/Skybox/stage3_mothership_saucer_4k.png", "Tàu mẹ Saucer Dreadnought (4K UHD)");
    }

    [MenuItem("Tools/Virtual Rail/Stage 3 Options/2. Apply Stage 3 Pure Nebula (5K Clean Sky)")]
    public static void ApplyStage3PureNebula()
    {
        SwitchStage3Texture("Assets/Textures/Skybox/stage3_backup_green_nebula.png", "Tinh vân xanh thuần khiết (5K Clean Sky)");
    }

    [MenuItem("Tools/Virtual Rail/Stage 3 Options/3. Restore Stage 3 (Citadel Legacy 768p Backup)")]
    public static void RestoreStage3Legacy()
    {
        SwitchStage3Texture("Assets/Textures/Skybox/stage3_backup_citadel_768p.png", "Bản Citadel gốc cũ (Legacy 768p)");
    }

    [MenuItem("Tools/Virtual Rail/Stage 3 Options/4. Switch Stage 3 to Animated Mothership Vortex (Dynamic FX - ON)")]
    public static void EnableStage3AnimatedVortex()
    {
        var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_03.asset");
        var m3 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage3_Backdrop_Animated.mat");
        if (s3 != null && m3 != null)
        {
            s3.environmentSettings.backdropMaterial = m3;
            EditorUtility.SetDirty(s3);
            AssetDatabase.SaveAssets();
        }

        ApplyBackdropPreview("Assets/Materials/Stage3_Backdrop_Animated.mat", "Stage 3 (Animated Vortex Core)");
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT LÕI XOÁY ỐC ĐỘNG CHO TÀU MẸ STAGE 3 (Dynamic FX):</b></color> Shader GPU thời gian thực, 0% giật cục!");
    }

    [MenuItem("Tools/Virtual Rail/Stage 3 Options/5. Switch Stage 3 to Static Backdrop (Standard Image - OFF)")]
    public static void EnableStage3StaticBackdrop()
    {
        var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_03.asset");
        var m3 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage3_Backdrop.mat");
        if (s3 != null && m3 != null)
        {
            s3.environmentSettings.backdropMaterial = m3;
            EditorUtility.SetDirty(s3);
            AssetDatabase.SaveAssets();
        }

        ApplyBackdropPreview("Assets/Materials/Stage3_Backdrop.mat", "Stage 3 (Static Image)");
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ CHUYỂN STAGE 3 VỀ PHONG NỀN TĨNH TIÊU CHUẨN:</b></color> Stage3_Backdrop.mat");
    }

    private static void SwitchStage3Texture(string sourcePath, string optionName)
    {
        string targetPath = "Assets/Textures/Skybox/stage3.png";
        if (!System.IO.File.Exists(sourcePath))
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy tệp nguồn: {sourcePath}");
            return;
        }

        System.IO.File.Copy(sourcePath, targetPath, true);
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_03.asset");
        string matPath = (s3 != null && s3.environmentSettings.backdropMaterial != null) 
            ? AssetDatabase.GetAssetPath(s3.environmentSettings.backdropMaterial) 
            : "Assets/Materials/Stage3_Backdrop.mat";

        ApplyBackdropPreview(matPath, $"Stage 3 ({optionName})");
        Debug.Log($"<color=green><b>[VirtualRail] ĐÃ ÁP DỤNG THÀNH CÔNG CHO STAGE 3:</b></color> {optionName}");
    }

    [MenuItem("Tools/Virtual Rail/Stage Backdrops/Preview Stage 3 Backdrop (4K)")]
    public static void PreviewStage3Backdrop()
    {
        ApplyBackdropPreview("Assets/Materials/Stage3_Backdrop.mat", "Stage 3 (4K)");
    }

    [MenuItem("Tools/Virtual Rail/Stage Backdrops/Reset Skybox to Deep Space Stars (Starsonly)")]
    public static void ResetSkyboxToStarsOnly()
    {
        Material starsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat");
        if (starsMat != null)
        {
            RenderSettings.skybox = starsMat;
            DynamicGI.UpdateEnvironment();
            Debug.Log("<color=green><b>[VirtualRail] DA RESET SKYBOX VE KHONG GIAN SAO DEM:</b></color> skyboxv1_starsonly.mat");
        }
    }

    [MenuItem("Tools/Virtual Rail/Stage Backdrops/Switch All Stages to Static Backdrops (Standard Image)")]
    public static void SwitchAllStagesToStaticBackdrops()
    {
        var s1 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_01.asset");
        var s2 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_02.asset");
        var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_03.asset");

        var m1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage1_Backdrop.mat");
        var m2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage2_Backdrop.mat");
        var m3 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage3_Backdrop.mat");

        if (s1 != null && m1 != null) { s1.environmentSettings.backdropMaterial = m1; EditorUtility.SetDirty(s1); }
        if (s2 != null && m2 != null) { s2.environmentSettings.backdropMaterial = m2; EditorUtility.SetDirty(s2); }
        if (s3 != null && m3 != null) { s3.environmentSettings.backdropMaterial = m3; EditorUtility.SetDirty(s3); }

        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null && m1 != null)
        {
            config.backdropMaterial = m1;
            EditorUtility.SetDirty(config);
        }

        AssetDatabase.SaveAssets();
        RebuildPlayerRigPrefab();

        var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bd in backdrops)
        {
            if (m1 != null) bd.SetBackdropMaterial(m1);
            bd.Initialize();
            bd.SetBackdropActive(true);
        }

        Debug.Log("<color=green><b>[VirtualRail] DA CHUYEN TOAN BO MAN CHOI SANG PHONG NEN TINH TIEU CHUAN (Static Image)</b></color>");
    }

    [MenuItem("Tools/Virtual Rail/Stage Backdrops/Switch All Stages to Animated Backdrops (Dynamic FX)")]
    public static void SwitchAllStagesToAnimatedBackdrops()
    {
        var s1 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_01.asset");
        var s2 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_02.asset");
        var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_03.asset");

        var m1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage1_Backdrop_Animated.mat");
        var m2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage2_Backdrop_Animated.mat");
        var m3 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage3_Backdrop_Animated.mat");

        if (s1 != null && m1 != null) { s1.environmentSettings.backdropMaterial = m1; EditorUtility.SetDirty(s1); }
        if (s2 != null && m2 != null) { s2.environmentSettings.backdropMaterial = m2; EditorUtility.SetDirty(s2); }
        if (s3 != null && m3 != null) { s3.environmentSettings.backdropMaterial = m3; EditorUtility.SetDirty(s3); }

        AssetDatabase.SaveAssets();

        var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bd in backdrops)
        {
            if (m1 != null) bd.SetBackdropMaterial(m1);
            bd.Initialize();
            bd.SetBackdropActive(true);
        }

        Debug.Log("<color=cyan><b>[VirtualRail] DA CHUYEN TOAN BO MAN CHOI SANG PHONG NEN DONG (Dynamic FX Shader)</b></color>");
    }

    private static void ApplyBackdropPreview(string matPath, string stageName)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null && mat != null)
        {
            config.enableHorizonBackdrop = true;
            config.backdropDistanceZ = 850f;
            config.backdropMaterial = mat;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        RebuildPlayerRigPrefab();

        var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bd in backdrops)
        {
            if (mat != null) bd.SetBackdropMaterial(mat);
            bd.Initialize();
            bd.SetBackdropActive(true);
        }

        Debug.Log($"<color=cyan><b>[VirtualRail] DA XEM TRUOC BACKDROP 4K:</b></color> {stageName}");
    }

    // =========================================================================
    // CONFIG INSPECTOR SHORTCUT
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/6. Select Virtual Rail Config (Inspector)")]
    public static void SelectConfigInInspector()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            Debug.Log("<color=green><b>[VirtualRail] DA CHON CONFIG TRONG INSPECTOR:</b></color> Ban co the tinh chinh toan bo thong so gameplay (FOV, toc do, dan, corridor, khien...) truc quan!");
        }
        else
        {
            Debug.LogWarning("[VirtualRail] Khong tim thay config tai: " + CONFIG_PATH);
        }
    }

    // =========================================================================
    // MAINTENANCE & UTILITIES
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/Maintenance/Re-bake Player Rig Prefab")]
    public static void MenuRebakeRigPrefab()
    {
        RebuildPlayerRigPrefab();
        Debug.Log("<color=green><b>[VirtualRail] DA RE-BAKE THANH CONG PREFAB:</b></color> Assets/Prefabs/VirtualRail_PlayerRig.prefab.");
    }

    [MenuItem("Tools/Virtual Rail/Maintenance/Apply Cosmic Earth Skybox")]
    public static void ApplyCosmicEarthSkybox()
    {
        TextureImporter importer = AssetImporter.GetAtPath(COSMIC_EARTH_TEXTURE_PATH) as TextureImporter;
        if (importer != null)
        {
            bool needReimport = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                needReimport = true;
            }
            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                needReimport = true;
            }
            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                needReimport = true;
            }
            if (needReimport)
            {
                importer.SaveAndReimport();
            }
        }

        Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(COSMIC_EARTH_MATERIAL_PATH);
        Shader panoramicShader = Shader.Find("Skybox/Panoramic");
        if (panoramicShader == null)
        {
            Debug.LogError("[VirtualRail] Khong tim thay Shader 'Skybox/Panoramic' cua Unity!");
            return;
        }

        if (skyboxMat == null)
        {
            skyboxMat = new Material(panoramicShader);
            AssetDatabase.CreateAsset(skyboxMat, COSMIC_EARTH_MATERIAL_PATH);
        }
        else
        {
            skyboxMat.shader = panoramicShader;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(COSMIC_EARTH_TEXTURE_PATH);
        if (tex != null)
        {
            skyboxMat.mainTexture = tex;
        }
        skyboxMat.SetFloat("_Exposure", 1.25f);
        skyboxMat.SetFloat("_Rotation", 270f);
        skyboxMat.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        skyboxMat.SetFloat("_Mapping", 0);
        skyboxMat.SetFloat("_ImageType", 0);
        skyboxMat.SetFloat("_MirrorOnBack", 0);
        EditorUtility.SetDirty(skyboxMat);
        AssetDatabase.SaveAssets();

        RenderSettings.skybox = skyboxMat;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.18f, 0.24f, 0.32f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.14f);
        RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.03f);
        RenderSettings.ambientIntensity = 1.1f;

        VirtualRailHorizonBackdrop backdrop = Object.FindAnyObjectByType<VirtualRailHorizonBackdrop>();
        if (backdrop != null)
        {
            backdrop.SetBackdropMaterial(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Backdrop_Image1_CosmicEarth_v2.mat"));
        }

        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT SKYBOX ẢNH 1 (COSMIC EARTH):</b></color> Vòm Trái Đất neon & màn chiếu Backdrop 1440p!");
    }

    [MenuItem("Tools/Virtual Rail/38. Skybox: Apply Original Dark Space (Black)")]
    [MenuItem("Tools/Virtual Rail/Maintenance/Restore Original Skybox (Legacy)")]
    public static void RestoreOriginalSkybox()
    {
        Material originalMat = AssetDatabase.LoadAssetAtPath<Material>(ORIGINAL_SKYBOX_MATERIAL_PATH);
        if (originalMat == null)
        {
            Debug.LogError("[VirtualRail] Khong tim thay skyboxv1.mat goc!");
            return;
        }

        RenderSettings.skybox = originalMat;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
        RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
        RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
        RenderSettings.ambientIntensity = 1.0f;

        VirtualRailHorizonBackdrop backdrop = Object.FindAnyObjectByType<VirtualRailHorizonBackdrop>();
        if (backdrop != null)
        {
            backdrop.SetBackdropMaterial(null);
        }

        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỀ SKYBOX ĐEN VŨ TRỤ (STARS ONLY):</b></color> skyboxv1_starsonly.mat đã được gán và Backdrop đã được ẩn.");
    }

    [MenuItem("Tools/Virtual Rail/39. Skybox: Apply Cosmic Earth (Photo)")]
    [MenuItem("Tools/Virtual Rail/Maintenance/Apply Cosmic Earth Skybox")]
    public static void ApplyCosmicEarthSkyboxMenu()
    {
        ApplyCosmicEarthSkybox();
    }

    [MenuItem("Tools/Virtual Rail/40. Stages: Apply Stage 1 Skybox to Scene")]
    public static void ApplyStage1SkyboxToScene()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage1_Skybox.mat");
        if (mat != null)
        {
            RenderSettings.skybox = mat;
            VirtualRailHorizonBackdrop backdrop = Object.FindAnyObjectByType<VirtualRailHorizonBackdrop>();
            if (backdrop != null)
            {
                backdrop.SetBackdropMaterial(null);
            }
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ ÁP DỤNG SKYBOX STAGE 1:</b></color> stage1.png đã được gắn vào RenderSettings.skybox.");
        }
    }

    private static GameObject BuildVirtualRailRig(VirtualRailConfig config)
    {
        GameObject root = new GameObject("VirtualRail_PlayerRig");
        root.tag = "Player";

        // Gốc neo đường ray
        VirtualRailAnchor anchor = root.AddComponent<VirtualRailAnchor>();
        anchor.config = config;

        // Module Bom Thông Minh & Động cơ tính điểm Combo (TDD v1.0.0 Phần I)
        root.AddComponent<SmartBombModule>();
        root.AddComponent<ComboScoringEngine>();

        // Bộ đọc Input tập trung (Hòa trộn PC Keyboard/Mouse và Android Dual Joysticks)
        VirtualRailInputReader inputReader = root.AddComponent<VirtualRailInputReader>();
        inputReader.config = config;

        // 1. Camera Rig
        GameObject camRigObj = new GameObject("[Camera Rig]");
        camRigObj.transform.SetParent(root.transform, false);
        camRigObj.transform.localPosition = new Vector3(0, config.cameraHeight, -config.cameraDistance);
        VirtualRailCameraRig camRig = camRigObj.AddComponent<VirtualRailCameraRig>();
        camRig.anchor = anchor;

        // 1b. Space Dust (Bụi sao không gian giải quyết triệt để cảm giác tàu đứng im trên Skybox tĩnh)
        GameObject dustObj = new GameObject("[Space Dust]");
        dustObj.transform.SetParent(camRigObj.transform, false);
        dustObj.transform.localPosition = Vector3.zero;
        dustObj.transform.localRotation = Quaternion.identity;
        VirtualRailSpaceDust spaceDust = dustObj.AddComponent<VirtualRailSpaceDust>();
        spaceDust.anchor = anchor;
        spaceDust.config = config;
        spaceDust.InitializeSystem();

        // 1c. Horizon Backdrop (Màn chiếu chân trời độ nét cao 1:1 khóa chuẩn trước mũi tàu)
        GameObject backdropObj = new GameObject("[Horizon Backdrop]");
        backdropObj.transform.SetParent(camRigObj.transform, false);
        backdropObj.transform.localPosition = new Vector3(0, 0, config.backdropDistanceZ);
        backdropObj.transform.localRotation = Quaternion.identity;
        VirtualRailHorizonBackdrop backdrop = backdropObj.AddComponent<VirtualRailHorizonBackdrop>();
        backdrop.distanceZ = config.backdropDistanceZ;
        backdrop.backdropMaterial = config.backdropMaterial;
        backdrop.Initialize();

        // 2. Reticle Anchor
        GameObject reticleObj = new GameObject("[Reticle Anchor]");
        reticleObj.transform.SetParent(root.transform, false);
        reticleObj.transform.localPosition = new Vector3(0, 0, config.convergenceDistance);
        VirtualRailReticle reticle = reticleObj.AddComponent<VirtualRailReticle>();
        reticle.anchor = anchor;
        reticle.inputReader = inputReader;

        // UI Canvas cho Crosshair & Lock Marker
        GameObject canvasObj = new GameObject("VirtualRailCanvas");
        canvasObj.transform.SetParent(reticleObj.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 1. Far Reticle (Tâm ngắm xa - Outer Box Cyan 56x56)
        GameObject crossObj = new GameObject("ReticleFarCrosshair");
        crossObj.transform.SetParent(canvasObj.transform, false);
        Image crossImg = crossObj.AddComponent<Image>();
        crossImg.color = Color.clear; // Rỗng ruột hoàn toàn 100% để nhìn xuyên thấu không gian
        crossImg.raycastTarget = false;
        RectTransform crossRt = crossObj.GetComponent<RectTransform>();
        float farSize = config.farReticleSize > 10f ? config.farReticleSize : 56f;
        crossRt.sizeDelta = new Vector2(farSize, farSize);
        reticle.crosshairUI = crossRt;
        CreateHollowBoxBorders(crossObj.transform, crossRt.sizeDelta, new Color(0.1f, 0.95f, 1f, 0.90f));

        // Chấm tâm chính xác (FarCenterDot 4x4)
        GameObject farDotObj = new GameObject("FarCenterDot");
        farDotObj.transform.SetParent(crossObj.transform, false);
        RectTransform farDotRt = farDotObj.AddComponent<RectTransform>();
        farDotRt.sizeDelta = new Vector2(4f, 4f);
        Image farDotImg = farDotObj.AddComponent<Image>();
        farDotImg.color = new Color(0.1f, 0.95f, 1f, 0.95f);
        farDotImg.raycastTarget = false;

        // 2. Near Reticle (Tâm ngắm gần - Inner Crosshair Emerald Green 28x28)
        GameObject nearCrossObj = new GameObject("ReticleNearCrosshair");
        nearCrossObj.transform.SetParent(canvasObj.transform, false);
        Image nearCrossImg = nearCrossObj.AddComponent<Image>();
        nearCrossImg.color = Color.clear; // Rỗng ruột hoàn toàn 100% để nhìn xuyên thấu
        nearCrossImg.raycastTarget = false;
        RectTransform nearCrossRt = nearCrossObj.GetComponent<RectTransform>();
        float nearSize = config.nearReticleSize > 10f ? config.nearReticleSize : 28f;
        nearCrossRt.sizeDelta = new Vector2(nearSize, nearSize);
        reticle.nearCrosshairUI = nearCrossRt;

        // Chấm tâm nhỏ 4x4
        GameObject dotObj = new GameObject("CenterDot");
        dotObj.transform.SetParent(nearCrossObj.transform, false);
        RectTransform dotRt = dotObj.AddComponent<RectTransform>();
        dotRt.sizeDelta = new Vector2(4f, 4f);
        Image dotImg = dotObj.AddComponent<Image>();
        dotImg.color = new Color(1f, 1f, 1f, 0.95f);
        dotImg.raycastTarget = false;

        // 4 vạch ngạnh chữ thập
        CreateBorderLine(nearCrossObj.transform, "NearTop", new Vector2(0, nearSize * 0.35f), new Vector2(2f, nearSize * 0.3f), new Color(0.2f, 1f, 0.4f, 0.95f));
        CreateBorderLine(nearCrossObj.transform, "NearBottom", new Vector2(0, -nearSize * 0.35f), new Vector2(2f, nearSize * 0.3f), new Color(0.2f, 1f, 0.4f, 0.95f));
        CreateBorderLine(nearCrossObj.transform, "NearLeft", new Vector2(-nearSize * 0.35f, 0), new Vector2(nearSize * 0.3f, 2f), new Color(0.2f, 1f, 0.4f, 0.95f));
        CreateBorderLine(nearCrossObj.transform, "NearRight", new Vector2(nearSize * 0.35f, 0), new Vector2(nearSize * 0.3f, 2f), new Color(0.2f, 1f, 0.4f, 0.95f));

        // DualReticleHUD (Presentation Layer - Chuẩn hóa Điều 4 GEMINI.md)
        DualReticleHUD hud = canvasObj.AddComponent<DualReticleHUD>();
        hud.anchor = anchor;
        hud.reticle = reticle;
        hud.farCrosshairUI = crossRt;
        hud.nearCrosshairUI = nearCrossRt;

        // 1b. Distance Telemetry Text (0 GC Telemetry HUD - Hướng A)
        GameObject telemObj = new GameObject("DistanceTelemetry");
        telemObj.transform.SetParent(crossObj.transform, false);
        RectTransform telemRt = telemObj.AddComponent<RectTransform>();
        telemRt.anchoredPosition = new Vector2(0, -farSize * 0.5f - 11f);
        telemRt.sizeDelta = new Vector2(80f, 18f);
        Text telemText = telemObj.AddComponent<Text>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        telemText.font = defaultFont;
        telemText.fontSize = 11;
        telemText.fontStyle = FontStyle.Bold;
        telemText.alignment = TextAnchor.MiddleCenter;
        telemText.color = new Color(0.1f, 0.95f, 1f, 0.95f);
        telemText.raycastTarget = false;
        Outline telemOutline = telemObj.AddComponent<Outline>();
        telemOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        telemOutline.effectDistance = new Vector2(1f, -1f);
        hud.telemetryText = telemText;

        // Lock Marker
        GameObject lockObj = new GameObject("LockMarker");
        lockObj.transform.SetParent(canvasObj.transform, false);
        Image lockImg = lockObj.AddComponent<Image>();
        lockImg.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        RectTransform lockRt = lockObj.GetComponent<RectTransform>();
        lockRt.sizeDelta = new Vector2(60, 60);
        lockObj.SetActive(false);

        // ==================== CỤM GIAO DIỆN 2 CẦN GẠT ẢO CHO ANDROID ====================
        GameObject mobileRoot = new GameObject("MobileTouchControls");
        mobileRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform mobileRootRt = mobileRoot.AddComponent<RectTransform>();
        mobileRootRt.anchorMin = Vector2.zero;
        mobileRootRt.anchorMax = Vector2.one;
        mobileRootRt.offsetMin = Vector2.zero;
        mobileRootRt.offsetMax = Vector2.zero;
        inputReader.mobileUIRoot = mobileRoot;

        // --- CẦN GẠT TRÁI (LEFT JOYSTICK - ĐIỀU KHIỂN TÀU) ---
        GameObject leftZone = new GameObject("LeftTouchZone");
        leftZone.transform.SetParent(mobileRoot.transform, false);
        RectTransform leftZoneRt = leftZone.AddComponent<RectTransform>();
        leftZoneRt.anchorMin = new Vector2(0f, 0f);
        leftZoneRt.anchorMax = new Vector2(0.5f, 0.8f);
        leftZoneRt.offsetMin = Vector2.zero;
        leftZoneRt.offsetMax = Vector2.zero;
        Image leftZoneImg = leftZone.AddComponent<Image>();
        leftZoneImg.color = new Color(0f, 0f, 0f, 0.001f); // Vùng chạm trong suốt
        leftZoneImg.raycastTarget = true;
        VirtualJoystick leftJoy = leftZone.AddComponent<VirtualJoystick>();
        leftJoy.handleRange = config.joystickHandleRange;
        leftJoy.deadZone = config.joystickDeadZone;
        leftJoy.isDynamicFloating = false;

        GameObject leftBg = new GameObject("LeftJoyBackground");
        leftBg.transform.SetParent(leftZone.transform, false);
        RectTransform leftBgRt = leftBg.AddComponent<RectTransform>();
        leftBgRt.anchorMin = new Vector2(0f, 0f);
        leftBgRt.anchorMax = new Vector2(0f, 0f);
        leftBgRt.anchoredPosition = new Vector2(170f, 170f);
        leftBgRt.sizeDelta = new Vector2(160, 160);
        Image leftBgImg = leftBg.AddComponent<Image>();
        leftBgImg.color = new Color(0.1f, 0.7f, 1f, 0.45f);
        leftBgImg.raycastTarget = false;

        GameObject leftHandle = new GameObject("LeftJoyHandle");
        leftHandle.transform.SetParent(leftBg.transform, false);
        RectTransform leftHandleRt = leftHandle.AddComponent<RectTransform>();
        leftHandleRt.sizeDelta = new Vector2(70, 70);
        Image leftHandleImg = leftHandle.AddComponent<Image>();
        leftHandleImg.color = new Color(0.3f, 0.9f, 1f, 0.95f);
        leftHandleImg.raycastTarget = false;

        leftJoy.background = leftBgRt;
        leftJoy.handle = leftHandleRt;

        // --- CẦN GẠT PHẢI (RIGHT JOYSTICK - TÂM NGẮM & TỰ ĐỘNG BẮN) ---
        GameObject rightZone = new GameObject("RightTouchZone");
        rightZone.transform.SetParent(mobileRoot.transform, false);
        RectTransform rightZoneRt = rightZone.AddComponent<RectTransform>();
        rightZoneRt.anchorMin = new Vector2(0.5f, 0f);
        rightZoneRt.anchorMax = new Vector2(1f, 0.8f);
        rightZoneRt.offsetMin = Vector2.zero;
        rightZoneRt.offsetMax = Vector2.zero;
        Image rightZoneImg = rightZone.AddComponent<Image>();
        rightZoneImg.color = new Color(0f, 0f, 0f, 0.001f); // Vùng chạm trong suốt
        rightZoneImg.raycastTarget = true;
        VirtualJoystick rightJoy = rightZone.AddComponent<VirtualJoystick>();
        rightJoy.handleRange = config.joystickHandleRange;
        rightJoy.deadZone = config.joystickDeadZone;
        rightJoy.isDynamicFloating = false;

        GameObject rightBg = new GameObject("RightJoyBackground");
        rightBg.transform.SetParent(rightZone.transform, false);
        RectTransform rightBgRt = rightBg.AddComponent<RectTransform>();
        rightBgRt.anchorMin = new Vector2(1f, 0f);
        rightBgRt.anchorMax = new Vector2(1f, 0f);
        rightBgRt.anchoredPosition = new Vector2(-170f, 170f);
        rightBgRt.sizeDelta = new Vector2(160, 160);
        Image rightBgImg = rightBg.AddComponent<Image>();
        rightBgImg.color = new Color(1f, 0.3f, 0.3f, 0.45f);
        rightBgImg.raycastTarget = false;

        GameObject rightHandle = new GameObject("RightJoyHandle");
        rightHandle.transform.SetParent(rightBg.transform, false);
        RectTransform rightHandleRt = rightHandle.AddComponent<RectTransform>();
        rightHandleRt.sizeDelta = new Vector2(70, 70);
        Image rightHandleImg = rightHandle.AddComponent<Image>();
        rightHandleImg.color = new Color(1f, 0.5f, 0.4f, 0.95f);
        rightHandleImg.raycastTarget = false;

        rightJoy.background = rightBgRt;
        rightJoy.handle = rightHandleRt;

        // Nối 2 cần gạt vào InputReader
        inputReader.leftJoystick = leftJoy;
        inputReader.rightJoystick = rightJoy;

        // 3. Ship Container
        GameObject shipContainer = new GameObject("[Ship Container]");
        shipContainer.transform.SetParent(root.transform, false);
        shipContainer.transform.localPosition = Vector3.zero;

        // Nhân bản đồ họa và nòng súng từ Prefab gốc vào Container
        GameObject originalShipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ORIGINAL_SHIP_PREFAB);
        GameObject shipVisual = null;
        if (originalShipPrefab != null)
        {
            shipVisual = Object.Instantiate(originalShipPrefab, shipContainer.transform);
            shipVisual.name = "ShipVisual";
            shipVisual.transform.localPosition = Vector3.zero;
            shipVisual.transform.localRotation = Quaternion.identity;

            // Dọn dẹp sạch các script cũ khỏi bản sao này
            CleanLegacyComponents(shipVisual);
        }

        // Gắn các component mới lên Ship Container
        VirtualRailShip ship = shipContainer.AddComponent<VirtualRailShip>();
        ship.anchor = anchor;
        ship.reticle = reticle;
        ship.inputReader = inputReader;
        ship.shipVisualMesh = shipVisual != null ? shipVisual.transform : shipContainer.transform;

        // Vỏ bọc lướt cận kề (Outer Graze Box R = 1.8m - TDD v2.0.0 Phần III.2)
        GameObject grazeObj = new GameObject("[Graze Trigger]");
        grazeObj.transform.SetParent(shipContainer.transform, false);
        grazeObj.transform.localPosition = Vector3.zero;
        grazeObj.transform.localRotation = Quaternion.identity;
        VirtualRailGrazeTrigger grazeTrigger = grazeObj.AddComponent<VirtualRailGrazeTrigger>();
        grazeTrigger.Initialize(ship);
        ship.grazeTrigger = grazeTrigger;

        VirtualRailWeapon weapon = shipContainer.AddComponent<VirtualRailWeapon>();
        weapon.anchor = anchor;
        weapon.reticle = reticle;
        weapon.inputReader = inputReader;
        weapon.muzzles = shipContainer.GetComponentsInChildren<Laser>();

        if (config.laserBoltMaterial == null)
        {
            config.laserBoltMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
            if (config.laserBoltMaterial == null)
            {
                config.laserBoltMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Stretched/CFX_RayRounded.mat");
            }
            if (config.laserBoltMaterial == null)
            {
                config.laserBoltMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Player Laser.mat");
            }
            EditorUtility.SetDirty(config);
        }

        AimAssistModule aimAssist = shipContainer.AddComponent<AimAssistModule>();
        aimAssist.anchor = anchor;
        aimAssist.reticle = reticle;
        aimAssist.lockMarkerUI = lockRt;
        aimAssist.missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MISSILE_PREFAB);

        camRig.ship = ship;
        hud.ship = ship;
        hud.weapon = weapon;

        VirtualRailWaveSpawner waveSpawner = root.AddComponent<VirtualRailWaveSpawner>();
        waveSpawner.anchor = anchor;

        VirtualRailAsteroidSpawner asteroidSpawner = root.AddComponent<VirtualRailAsteroidSpawner>();
        asteroidSpawner.anchor = anchor;

        return root;
    }

    private static void CleanLegacyComponents(GameObject obj)
    {
        // Gỡ bỏ các script StarFox cũ
        var sfMove = obj.GetComponent<StarFoxMovement>();
        if (sfMove != null) Object.DestroyImmediate(sfMove, true);

        var sfInput = obj.GetComponent<StarFoxInput>();
        if (sfInput != null) Object.DestroyImmediate(sfInput, true);

        var sfCross = obj.GetComponent<StarFoxCrosshair>();
        if (sfCross != null) Object.DestroyImmediate(sfCross, true);

        var sfWeap = obj.GetComponent<StarFoxWeapons>();
        if (sfWeap != null) Object.DestroyImmediate(sfWeap, true);

        var sfCam = obj.GetComponent<StarFoxCameraRig>();
        if (sfCam != null) Object.DestroyImmediate(sfCam, true);

        var oldPlayer = obj.GetComponent<Player>();
        if (oldPlayer != null) Object.DestroyImmediate(oldPlayer, true);

        // Gỡ bỏ Canvas cũ nếu có
        Transform oldCanvas = obj.transform.Find("StarFoxCanvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject, true);
    }

    private static void CreateHollowBoxBorders(Transform parent, Vector2 size, Color borderColor)
    {
        float thickness = 2f;
        // Top border
        CreateBorderLine(parent, "BorderTop", new Vector2(0, size.y * 0.5f - thickness * 0.5f), new Vector2(size.x, thickness), borderColor);
        // Bottom border
        CreateBorderLine(parent, "BorderBottom", new Vector2(0, -size.y * 0.5f + thickness * 0.5f), new Vector2(size.x, thickness), borderColor);
        // Left border
        CreateBorderLine(parent, "BorderLeft", new Vector2(-size.x * 0.5f + thickness * 0.5f, 0), new Vector2(thickness, size.y), borderColor);
        // Right border
        CreateBorderLine(parent, "BorderRight", new Vector2(size.x * 0.5f - thickness * 0.5f, 0), new Vector2(thickness, size.y), borderColor);
    }

    private static void CreateBorderLine(Transform parent, string name, Vector2 localPos, Vector2 size, Color color)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent, false);
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta = size;
        Image img = line.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    [MenuItem("Tools/Virtual Rail/36. Stage Director: Setup Framework (ON)")]
    public static void SetupStageDirectorSystem()
    {
        VirtualRailStageDirector director = Object.FindAnyObjectByType<VirtualRailStageDirector>();
        if (director == null)
        {
            GameObject dirObj = new GameObject("[VirtualRail_StageDirector]");
            director = dirObj.AddComponent<VirtualRailStageDirector>();
        }

        StageData s1 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_01.asset");
        StageData s2 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_02.asset");
        StageData s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Scripts/VirtualRail/StageData_03.asset");

        var list = new System.Collections.Generic.List<StageData>();
        if (s1 != null) list.Add(s1);
        if (s2 != null) list.Add(s2);
        if (s3 != null) list.Add(s3);
        director.stages = list.ToArray();
        director.currentStageIndex = 0;
        director.defaultSkyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(ORIGINAL_SKYBOX_MATERIAL_PATH);

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && Object.FindAnyObjectByType<StageNotificationUI>() == null)
        {
            CreateStageNotificationUI(canvas.transform);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ THIẾT LẬP KHUNG KIẾN TRÚC STAGE DIRECTOR:</b></color> Đã gán 3 khung màn chơi (StageData_01, StageData_02, StageData_03).");
    }

    [MenuItem("Tools/Virtual Rail/37. Stage Director: Revert Framework (OFF)")]
    public static void RevertStageDirectorSystem()
    {
        VirtualRailStageDirector director = Object.FindAnyObjectByType<VirtualRailStageDirector>();
        if (director != null)
        {
            Object.DestroyImmediate(director.gameObject);
        }

        StageNotificationUI notif = Object.FindAnyObjectByType<StageNotificationUI>();
        if (notif != null)
        {
            Object.DestroyImmediate(notif.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN KHUNG STAGE DIRECTOR:</b></color> Đã gỡ bỏ StageDirector và UI thông báo.");
    }

    private static void CreateStageNotificationUI(Transform canvasParent)
    {
        GameObject notifObj = new GameObject("[StageNotificationUI]");
        notifObj.transform.SetParent(canvasParent, false);

        RectTransform rt = notifObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.6f);
        rt.anchorMax = new Vector2(1f, 0.85f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CanvasGroup cg = notifObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(notifObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.1f, 0.95f, 1.0f, 1.0f);

        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(notifObj.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.5f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;
        Text subText = subObj.AddComponent<Text>();
        subText.font = titleText.font;
        subText.fontSize = 20;
        subText.fontStyle = FontStyle.Normal;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(1.0f, 0.95f, 0.4f, 0.9f);

        StageNotificationUI notifUI = notifObj.AddComponent<StageNotificationUI>();
        notifUI.bannerCanvasGroup = cg;
        notifUI.titleText = titleText;
        notifUI.subtitleText = subText;
    }

    #region Enemy Projectile VFX (Core Option 1)
    private const string ENEMY_PROJECTILE_PREFAB_PATH = "Assets/Prefabs/EnemyProjectile_PlasmaOrb.prefab";
    private const string ENEMY_PLASMA_CORE_MAT_PATH = "Assets/Materials/EnemyPlasma_Core.mat";
    private const string CFX_ORANGE_HIT_PREFAB_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Hit B (Orange).prefab";
    private const string HOVL_FIRE_MAT_PATH = "Assets/Hovl Studio/Procedural fire/Materials/FireSphere1.mat";
    private const string HOVL_TRAIL_MAT_PATH = "Assets/Hovl Studio/Procedural fire/Materials/Trail67.mat";

    [MenuItem("Tools/Virtual Rail/Enemy Projectile VFX/1. Apply Enhanced Plasma Projectiles (Core - ON)")]
    public static void ApplyEnhancedPlasmaProjectiles()
    {
        // 1. Khởi tạo vật liệu Lõi trắng nóng (Inner Core)
        Material coreMat = AssetDatabase.LoadAssetAtPath<Material>(ENEMY_PLASMA_CORE_MAT_PATH);
        if (coreMat == null)
        {
            Shader coreShader = Shader.Find("VirtualRail/PlasmaOrbCore") ?? Shader.Find("Unlit/Color");
            coreMat = new Material(coreShader);
            coreMat.SetColor("_CoreColor", new Color(3.0f, 2.8f, 2.2f, 1.0f));
            coreMat.SetColor("_RimColor", new Color(2.5f, 1.0f, 0.1f, 1.0f));
            AssetDatabase.CreateAsset(coreMat, ENEMY_PLASMA_CORE_MAT_PATH);
        }

        // 2. Xây dựng Prefab đa tầng độc lập
        GameObject template = BuildEnemyPlasmaOrbGameObject(coreMat);
        PrefabUtility.SaveAsPrefabAsset(template, ENEMY_PROJECTILE_PREFAB_PATH);
        Object.DestroyImmediate(template);
        AssetDatabase.SaveAssets();

        // 3. Cập nhật cấu hình VirtualRailConfig
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useEnhancedPlasmaProjectiles = true;
            config.enemyProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PROJECTILE_PREFAB_PATH);
            config.enemyImpactVFXPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CFX_ORANGE_HIT_PREFAB_PATH);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        // 4. Cập nhật Pool trong Scene nếu có
        EnemyProjectilePool pool = Object.FindAnyObjectByType<EnemyProjectilePool>();
        if (pool != null)
        {
            pool.SetProjectilePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PROJECTILE_PREFAB_PATH));
            pool.SetImpactVFXPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CFX_ORANGE_HIT_PREFAB_PATH));
            EditorUtility.SetDirty(pool);
        }

        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT ĐẠN PLASMA ORB ĐA TẦNG CHO KẺ ĐỊCH:</b></color> Prefab đã lưu tại " + ENEMY_PROJECTILE_PREFAB_PATH);
    }

    [MenuItem("Tools/Virtual Rail/Enemy Projectile VFX/2. Revert to Legacy Projectiles (OFF)")]
    public static void RevertToLegacyProjectiles()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useEnhancedPlasmaProjectiles = false;
            config.enemyProjectilePrefab = null;
            config.enemyImpactVFXPrefab = null;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        EnemyProjectilePool pool = Object.FindAnyObjectByType<EnemyProjectilePool>();
        if (pool != null)
        {
            pool.SetProjectilePrefab(null);
            pool.SetImpactVFXPrefab(null);
            EditorUtility.SetDirty(pool);
        }

        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN ĐẠN KẺ ĐỊCH VỀ LEGACY PROJECTILES (OFF).</b></color>");
    }

    public static GameObject BuildEnemyPlasmaOrbGameObject(Material coreMat)
    {
        GameObject root = new GameObject("EnemyProjectile_PlasmaOrb");

        var sphereCol = root.AddComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 0.45f;

        GameObject visualRoot = new GameObject("[Visual_Root]");
        visualRoot.transform.SetParent(root.transform, false);

        // Quầng Plasma ngoài (Outer Corona)
        GameObject corona = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        corona.name = "[Corona]";
        corona.transform.SetParent(visualRoot.transform, false);
        corona.transform.localScale = Vector3.one * 1.2f;
        var coronaCol = corona.GetComponent<Collider>();
        if (coronaCol != null) Object.DestroyImmediate(coronaCol);

        Material coronaMat = AssetDatabase.LoadAssetAtPath<Material>(HOVL_FIRE_MAT_PATH);
        if (coronaMat == null)
        {
            coronaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Materials/PlasmaFire.mat");
        }
        Renderer coronaRend = corona.GetComponent<Renderer>();
        if (coronaRend != null && coronaMat != null)
        {
            coronaRend.sharedMaterial = coronaMat;
        }

        // Lõi trắng siêu nhiệt (Inner Core)
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "[Core]";
        core.transform.SetParent(visualRoot.transform, false);
        core.transform.localScale = Vector3.one * 0.55f;
        var coreCol = core.GetComponent<Collider>();
        if (coreCol != null) Object.DestroyImmediate(coreCol);

        Renderer coreRend = core.GetComponent<Renderer>();
        if (coreRend != null && coreMat != null)
        {
            coreRend.sharedMaterial = coreMat;
        }

        // Vệt đuôi Ribbon Trail
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.85f;
        trail.endWidth = 0.05f;

        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(HOVL_TRAIL_MAT_PATH);
        if (trailMat == null)
        {
            trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Fire/CFX3_FireBulk ADD.mat");
        }
        trail.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.98f, 0.75f), 0.0f),
                new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.5f),
                new GradientColorKey(new Color(0.9f, 0.05f, 0.0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.95f, 0.0f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;

        // Hạt tia lửa bay phía sau
        GameObject sparksObj = new GameObject("[Sparks]");
        sparksObj.transform.SetParent(root.transform, false);
        ParticleSystem ps = sparksObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.22f;
        main.startSpeed = 1.5f;
        main.startSize = 0.22f;
        main.startColor = new Color(1f, 0.7f, 0.2f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.loop = true;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        // Gán EnemyProjectile script
        EnemyProjectile proj = root.AddComponent<EnemyProjectile>();
        proj.visualTransform = visualRoot.transform;
        proj.trailRenderer = trail;
        proj.sphereCollider = sphereCol;
        proj.sparkParticles = ps;
        proj.visualBaseRadius = 1.0f;
        proj.hitboxRadius = 0.45f;
        proj.damage = 1f;
        proj.maxLifetime = 5.0f;

        GameObject impactVFX = AssetDatabase.LoadAssetAtPath<GameObject>(CFX_ORANGE_HIT_PREFAB_PATH);
        if (impactVFX != null)
        {
            proj.SetImpactVFXPrefab(impactVFX);
        }

        return root;
    }
    #endregion
}
