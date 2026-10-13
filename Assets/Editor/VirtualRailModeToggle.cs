using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    private const string GREEN_ORB_INIT_KEY = "VirtualRail_GreenOrb_ChargeShot_v1";
    private const string MODERN_HUD_APPLIED_KEY = "VirtualRail_ModernHUD_Applied_Demo_v7_ScaledNoClutter";

    static VirtualRailModeToggle()
    {
        // Gỡ bỏ tự động cưỡng ép Cosmic Earth để cho phép Skybox gốc và StageData toàn quyền quyết định
        EditorApplication.delayCall += InitializeBackdropPrefabOnce;
        EditorApplication.delayCall += InitializeGreenOrbChargeShotOnce;
        EditorApplication.delayCall += AutoApplyModernHUDToGameSceneOnce;
    }

    private static void AutoApplyModernHUDToGameSceneOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool(MODERN_HUD_APPLIED_KEY, false)) return;

        string gameScenePath = "Assets/Scenes/Game.unity";
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != gameScenePath)
        {
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }
            activeScene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Single);
        }

        SwitchToModernCockpitHUD();
        Debug.Log("<color=lime><b>[VirtualRail] ĐÃ TỰ ĐỘNG ÁP DỤNG VÀ LƯU MODERN COCKPIT HUD VÀO SCENE GAME.UNITY THÀNH CÔNG!</b></color>");
    }

    private static void InitializeBackdropPrefabOnce()
    {
        if (!SessionState.GetBool(BACKDROP_BAKED_KEY, false))
        {
            SessionState.SetBool(BACKDROP_BAKED_KEY, true);
            RebuildPlayerRigPrefab();
        }
    }

    private static void InitializeGreenOrbChargeShotOnce()
    {
        if (!SessionState.GetBool(GREEN_ORB_INIT_KEY, false))
        {
            SessionState.SetBool(GREEN_ORB_INIT_KEY, true);
            ApplyHouidisoftGreenPlasmaOrb();
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

            // Vô hiệu hóa Spawner di sản (Enemy Spawner) trong Scene để tránh sinh quái ngẫu nhiên tại gốc (0,0,0)
            EnemySpawner legacyEnemySpawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (legacyEnemySpawner != null)
            {
                legacyEnemySpawner.gameObject.SetActive(false);
                EditorUtility.SetDirty(legacyEnemySpawner.gameObject);
            }

            // Đảm bảo có EventSystem trong Scene cho uGUI Touch Joysticks
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // Tự động thiết lập Stage Director và UI thông báo kịch bản
            SetupStageDirectorSystem();

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

            // Bật lại Spawner di sản cũ trong Scene
            EnemySpawner legacyEnemySpawner = Object.FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
            if (legacyEnemySpawner != null)
            {
                legacyEnemySpawner.gameObject.SetActive(true);
                EditorUtility.SetDirty(legacyEnemySpawner.gameObject);
            }

            // Hoàn nguyên Stage Director và UI thông báo
            RevertStageDirectorSystem();

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
    // RETICLE & AIMING SYSTEM CONTROLS (Tâm ngắm đơn vs Tâm ngắm kép)
    // =========================================================================

    [MenuItem("Tools/Virtual Rail/6. Aiming: Single Reticle Mode (Disable Boresight Reticle - ON)")]
    public static void EnableSingleReticleMode()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableDualReticle = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT CHẾ ĐỘ TÂM NGẮM ĐƠN (SINGLE RETICLE):</b></color> Đã vô hiệu hóa tâm ngắm mũi tàu (Boresight Near Reticle). Chỉ sử dụng tâm ngắm chính Sci-Fi.");
        }

        RebuildPlayerRigPrefab();

        var huds = Object.FindObjectsByType<DualReticleHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var hud in huds)
        {
            if (hud.nearCrosshairUI != null) hud.nearCrosshairUI.gameObject.SetActive(false);
        }
    }

    [MenuItem("Tools/Virtual Rail/7. Aiming: Undo / Restore Dual Reticle (Boresight Reticle - OFF)")]
    public static void RevertToDualReticleMode()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableDualReticle = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ TÂM NGẮM KÉP (DUAL RETICLE):</b></color> Đã bật lại tâm ngắm mũi tàu (Boresight Near Reticle).");
        }

        RebuildPlayerRigPrefab();

        var huds = Object.FindObjectsByType<DualReticleHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var hud in huds)
        {
            if (hud.nearCrosshairUI != null) hud.nearCrosshairUI.gameObject.SetActive(true);
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

    [MenuItem("Tools/Virtual Rail/41. Spawner Mode: Scripted Director Only (ON - Production)")]
    public static void SetSpawnerModeScriptedDirectorOnly()
    {
        VirtualRailWaveSpawner[] spawners = Object.FindObjectsByType<VirtualRailWaveSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i].ControlMode = SpawnerControlMode.ScriptedDirectorOnly;
            EditorUtility.SetDirty(spawners[i]);
        }
        RebuildPlayerRigPrefab();
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT CHẾ ĐỘ SCRIPTED DIRECTOR ONLY:</b></color> 100% quái vật do StageDirector điều phối qua Timeline kịch bản. Triệt tiêu hoàn toàn quái ngẫu nhiên/demo!");
    }

    [MenuItem("Tools/Virtual Rail/42. Spawner Mode: Endless Sandbox Demo (OFF - Test Only)")]
    public static void SetSpawnerModeEndlessSandboxDemo()
    {
        VirtualRailWaveSpawner[] spawners = Object.FindObjectsByType<VirtualRailWaveSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i].ControlMode = SpawnerControlMode.EndlessSandboxDemo;
            EditorUtility.SetDirty(spawners[i]);
        }
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ KÍCH HOẠT CHẾ ĐỘ ENDLESS SANDBOX DEMO:</b></color> Quái vật tự động sinh tuần hoàn ngẫu nhiên để kiểm thử tải!");
    }

    public const string BGM_VISION_PATH = "Assets/Audio/Music/(No Copyright Music) Sci-Fi Background [Techno Music] by MokkaMusic - Vision.mp3";
    public const string BGM_PUSHER_PATH = "Assets/Audio/Music/Electro House Techno in Space -no copyright music- by MokkaMusic - Pusher.mp3";
    public const string BGM_CRYOSLEEP_PATH = "Assets/Audio/Music/Futuristic Sci-Fi Interstellar Synthwave -no copyright music- by MokkaMusic - Cryosleep.mp3";
    public const string BGM_SECRECY_PATH = "Assets/Audio/Music/Synthwave Futuristic Neon Electro (No Copyright Music) by MokkaMusic - Secrecy.mp3";
    public const string STAGE_DATA_01_PATH = "Assets/Scripts/VirtualRail/StageData_01.asset";
    public const string STAGE_DATA_02_PATH = "Assets/Scripts/VirtualRail/StageData_02.asset";
    public const string STAGE_DATA_03_PATH = "Assets/Scripts/VirtualRail/StageData_03.asset";

    [MenuItem("Tools/Virtual Rail/43. Audio: Setup & Select AudioManager (Inspector)")]
    public static void SelectAudioManager()
    {
        ApplyBackgroundMusicToStagesAndManager();
    }

    [MenuItem("Tools/Virtual Rail/44. Audio: Apply Background Music to Stage & AudioManager")]
    public static void ApplyBackgroundMusicToStagesAndManager()
    {
        AudioClip clipVision = AssetDatabase.LoadAssetAtPath<AudioClip>(BGM_VISION_PATH);
        AudioClip clipPusher = AssetDatabase.LoadAssetAtPath<AudioClip>(BGM_PUSHER_PATH);
        AudioClip clipCryosleep = AssetDatabase.LoadAssetAtPath<AudioClip>(BGM_CRYOSLEEP_PATH);
        AudioClip clipSecrecy = AssetDatabase.LoadAssetAtPath<AudioClip>(BGM_SECRECY_PATH);

        // 1. Cấu hình vào AudioManager trong Scene
        AudioManager audioMgr = Object.FindAnyObjectByType<AudioManager>();
        if (audioMgr == null)
        {
            GameObject audioObj = new GameObject("[VirtualRail_AudioManager]");
            audioMgr = audioObj.AddComponent<AudioManager>();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // Stage Playlist: 4 bài nhạc không gian của MokkaMusic tự động Shuffle
        audioMgr.stageMusicPlaylist = new AudioClip[] { clipCryosleep, clipPusher, clipVision, clipSecrecy };
        audioMgr.shuffleStageMusic = true;

        // Boss Playlist: Danh sách riêng cho Boss, sẵn sàng tiếp nhận khi người dùng thêm nhạc Boss
        if (audioMgr.bossMusicPlaylist == null)
        {
            audioMgr.bossMusicPlaylist = new AudioClip[0];
        }
        audioMgr.shuffleBossMusic = true;
        audioMgr.crossfadeLeadTime = 2.0f;

        if (clipCryosleep != null) audioMgr.defaultStageBgm = clipCryosleep;
        audioMgr.defaultBossBgm = null; // Chưa có nhạc boss riêng
        if (clipSecrecy != null) audioMgr.defaultMenuBgm = clipSecrecy;
        audioMgr.musicPlaylist = new AudioClip[] { clipCryosleep, clipPusher, clipVision, clipSecrecy };
        EditorUtility.SetDirty(audioMgr);

        // 2. Cấu hình vào StageData_01 (Sector Alpha)
        StageData stage1 = AssetDatabase.LoadAssetAtPath<StageData>(STAGE_DATA_01_PATH);
        if (stage1 != null)
        {
            if (clipCryosleep != null) stage1.audioSettings.stageBgm = clipCryosleep;
            stage1.audioSettings.bossBgm = null;
            stage1.audioSettings.musicVolume = 0.85f;
            EditorUtility.SetDirty(stage1);
        }

        // 3. Cấu hình vào StageData_02 (Volcano)
        StageData stage2 = AssetDatabase.LoadAssetAtPath<StageData>(STAGE_DATA_02_PATH);
        if (stage2 != null)
        {
            if (clipVision != null) stage2.audioSettings.stageBgm = clipVision;
            stage2.audioSettings.bossBgm = null;
            stage2.audioSettings.musicVolume = 0.85f;
            EditorUtility.SetDirty(stage2);
        }

        // 4. Cấu hình vào StageData_03 (Citadel)
        StageData stage3 = AssetDatabase.LoadAssetAtPath<StageData>(STAGE_DATA_03_PATH);
        if (stage3 != null)
        {
            if (clipSecrecy != null) stage3.audioSettings.stageBgm = clipSecrecy;
            stage3.audioSettings.bossBgm = null;
            stage3.audioSettings.musicVolume = 0.85f;
            EditorUtility.SetDirty(stage3);
        }

        AssetDatabase.SaveAssets();

        Selection.activeGameObject = audioMgr.gameObject;
        EditorGUIUtility.PingObject(audioMgr.gameObject);
        Debug.Log("<color=green><b>[VirtualRail Audio] ĐÃ CẤU HÌNH XONG CƠ CHẾ DUAL-POOL SHUFFLE!</b></color>\n" +
            "• <b>Stage BGM Shuffle (4 bài):</b> Cryosleep, Pusher, Vision, Secrecy (Tự động xáo trộn Deck Shuffle, hòa âm trượt 2.0s).\n" +
            "• <b>Boss BGM Shuffle:</b> Đã tạo danh sách riêng biệt bossMusicPlaylist (Sẵn sàng nhận các bài nhạc Boss mới khi bạn thêm vào).\n" +
            "• <b>Menu BGM:</b> Secrecy (Neon Electro Synthwave).");
    }

    private static GameObject BuildVirtualRailRig(VirtualRailConfig config)
    {
        GameObject root = new GameObject("VirtualRail_PlayerRig");
        root.tag = "Player";

        // Gốc neo đường ray
        VirtualRailAnchor anchor = root.AddComponent<VirtualRailAnchor>();
        anchor.config = config;

        // Module Bom Thông Minh & Động cơ tính điểm Combo (TDD v1.0.0 Phần I)
        var smartBomb = root.AddComponent<SmartBombModule>();
        smartBomb.BombPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerSmartBomb.prefab");
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

        if (!config.enableDualReticle)
        {
            nearCrossObj.SetActive(false);
        }

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
        Sprite baseRing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_touch_ring_base.png");
        Sprite handleRing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_touch_ring_handle.png");
        Sprite arcRing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_touch_ring_arc.png");
        Sprite boostIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_touch_icon_boost.png");
        Sprite brakeIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_touch_icon_brake.png");
        Sprite rollIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/ui_touch_icon_roll.png");
        Sprite laserIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_icon_twin_laser.png");
        Sprite bombIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_icon_smartbomb.png");
        Font scifiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Orbitron.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        leftJoy.handleRange = config.joystickHandleRange;
        leftJoy.deadZone = config.joystickDeadZone;
        leftJoy.isDynamicFloating = true;

        GameObject leftBg = new GameObject("LeftJoyBackground");
        leftBg.transform.SetParent(leftZone.transform, false);
        RectTransform leftBgRt = leftBg.AddComponent<RectTransform>();
        leftBgRt.anchorMin = new Vector2(0f, 0f);
        leftBgRt.anchorMax = new Vector2(0f, 0f);
        leftBgRt.anchoredPosition = new Vector2(180f, 180f);
        leftBgRt.sizeDelta = new Vector2(180f, 180f);
        Image leftBgImg = leftBg.AddComponent<Image>();
        leftBgImg.sprite = baseRing;
        leftBgImg.color = Color.white;
        leftBgImg.raycastTarget = false;

        GameObject leftHandle = new GameObject("LeftJoyHandle");
        leftHandle.transform.SetParent(leftBg.transform, false);
        RectTransform leftHandleRt = leftHandle.AddComponent<RectTransform>();
        leftHandleRt.sizeDelta = new Vector2(76f, 76f);
        Image leftHandleImg = leftHandle.AddComponent<Image>();
        leftHandleImg.sprite = handleRing;
        leftHandleImg.color = Color.white;
        leftHandleImg.raycastTarget = false;

        leftJoy.background = leftBgRt;
        leftJoy.handle = leftHandleRt;
        leftJoy.bgImage = leftBgImg;
        leftJoy.handleImage = leftHandleImg;

        // --- HỆ THỐNG GIAO DIỆN CẢM ỨNG 4 NGÓN (4-FINGER CLAW MOBILE CONTROLS) ---
        inputReader.leftJoystick = leftJoy;
        inputReader.EnsureMobileUI();

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
        waveSpawner.ControlMode = SpawnerControlMode.ScriptedDirectorOnly;

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

        AudioManager audioMgr = Object.FindAnyObjectByType<AudioManager>();
        if (audioMgr == null)
        {
            GameObject audioObj = new GameObject("[VirtualRail_AudioManager]");
            audioMgr = audioObj.AddComponent<AudioManager>();
            EditorUtility.SetDirty(audioMgr);
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

        director.notificationUI = Object.FindAnyObjectByType<StageNotificationUI>();
        EditorUtility.SetDirty(director);

        VirtualRailWaveSpawner[] spawners = Object.FindObjectsByType<VirtualRailWaveSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i].ControlMode = SpawnerControlMode.ScriptedDirectorOnly;
            EditorUtility.SetDirty(spawners[i]);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ THIẾT LẬP KHUNG KIẾN TRÚC STAGE DIRECTOR:</b></color> Đã gán 3 khung màn chơi (StageData_01, StageData_02, StageData_03) và khóa Spawner ở chế độ ScriptedDirectorOnly.");
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

    #region Enemy Projectile VFX (Bright Red & Bright Purple Geodesic Orbs)
    private const string ENEMY_PROJECTILE_PREFAB_PATH = "Assets/Prefabs/EnemyProjectile_PlasmaOrb.prefab";
    private const string ENEMY_PROJECTILE_RED_PREFAB_PATH = "Assets/Prefabs/EnemyProjectile_BrightRed.prefab";
    private const string ENEMY_PROJECTILE_PURPLE_PREFAB_PATH = "Assets/Prefabs/EnemyProjectile_BrightPurple.prefab";
    private const string ENEMY_PLASMA_CORE_MAT_PATH = "Assets/Materials/EnemyPlasma_Core.mat";
    private const string ENEMY_PLASMA_RED_MAT_PATH = "Assets/Materials/EnemyPlasma_BrightRed.mat";
    private const string ENEMY_PLASMA_PURPLE_MAT_PATH = "Assets/Materials/EnemyPlasma_BrightPurple.mat";
    private const string CFX_ORANGE_HIT_PREFAB_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Hit B (Orange).prefab";
    private const string HOVL_FIRE_MAT_PATH = "Assets/Hovl Studio/Procedural fire/Materials/FireSphere1.mat";
    private const string HOVL_TRAIL_MAT_PATH = "Assets/Hovl Studio/Procedural fire/Materials/Trail67.mat";
    private const string ICOSPHERE_MESH_PATH = "Assets/Models/Projectiles/EnemyPlasmaOrb_Mesh.obj";

    [MenuItem("Tools/Virtual Rail/Enemy Projectile VFX/1. Switch to Bright Red Plasma (ON)")]
    public static void ApplyBrightRedPlasmaProjectiles()
    {
        Material redMat = AssetDatabase.LoadAssetAtPath<Material>(ENEMY_PLASMA_RED_MAT_PATH);
        if (redMat == null)
        {
            ApplyEnhancedPlasmaProjectiles();
            redMat = AssetDatabase.LoadAssetAtPath<Material>(ENEMY_PLASMA_RED_MAT_PATH);
        }

        GameObject redObj = BuildEnemyPlasmaOrbGameObject(
            redMat,
            new Color(1f, 0.98f, 0.75f),
            new Color(1f, 0.15f, 0.05f),
            new Color(0.85f, 0.02f, 0.0f),
            "EnemyProjectile_BrightRed"
        );
        PrefabUtility.SaveAsPrefabAsset(redObj, ENEMY_PROJECTILE_RED_PREFAB_PATH);
        Object.DestroyImmediate(redObj);

        // Đồng thời cập nhật cả prefab mặc định
        AssetDatabase.CopyAsset(ENEMY_PROJECTILE_RED_PREFAB_PATH, ENEMY_PROJECTILE_PREFAB_PATH);
        AssetDatabase.SaveAssets();

        UpdateConfigAndPool(ENEMY_PROJECTILE_RED_PREFAB_PATH);
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT ĐẠN ĐỎ SÁNG (BRIGHT RED PLASMA):</b></color> " + ENEMY_PROJECTILE_RED_PREFAB_PATH);
    }

    [MenuItem("Tools/Virtual Rail/Enemy Projectile VFX/2. Switch to Bright Purple Plasma (ON)")]
    public static void ApplyBrightPurplePlasmaProjectiles()
    {
        Material purpleMat = AssetDatabase.LoadAssetAtPath<Material>(ENEMY_PLASMA_PURPLE_MAT_PATH);
        if (purpleMat == null)
        {
            ApplyEnhancedPlasmaProjectiles();
            purpleMat = AssetDatabase.LoadAssetAtPath<Material>(ENEMY_PLASMA_PURPLE_MAT_PATH);
        }

        GameObject purpleObj = BuildEnemyPlasmaOrbGameObject(
            purpleMat,
            new Color(1f, 0.9f, 1f),
            new Color(0.85f, 0.05f, 1f),
            new Color(0.55f, 0.0f, 0.85f),
            "EnemyProjectile_BrightPurple"
        );
        PrefabUtility.SaveAsPrefabAsset(purpleObj, ENEMY_PROJECTILE_PURPLE_PREFAB_PATH);
        Object.DestroyImmediate(purpleObj);

        AssetDatabase.CopyAsset(ENEMY_PROJECTILE_PURPLE_PREFAB_PATH, ENEMY_PROJECTILE_PREFAB_PATH);
        AssetDatabase.SaveAssets();

        UpdateConfigAndPool(ENEMY_PROJECTILE_PURPLE_PREFAB_PATH);
        Debug.Log("<color=magenta><b>[VirtualRail] ĐÃ KÍCH HOẠT ĐẠN TÍM SÁNG (BRIGHT PURPLE PLASMA):</b></color> " + ENEMY_PROJECTILE_PURPLE_PREFAB_PATH);
    }

    [MenuItem("Tools/Virtual Rail/Enemy Projectile VFX/3. Revert to Legacy Projectiles (OFF)")]
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

    private static void UpdateConfigAndPool(string prefabPath)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject hitAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CFX_ORANGE_HIT_PREFAB_PATH);

        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useEnhancedPlasmaProjectiles = true;
            config.enemyProjectilePrefab = prefabAsset;
            config.enemyImpactVFXPrefab = hitAsset;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        EnemyProjectilePool pool = Object.FindAnyObjectByType<EnemyProjectilePool>();
        if (pool != null)
        {
            pool.SetProjectilePrefab(prefabAsset);
            pool.SetImpactVFXPrefab(hitAsset);
            EditorUtility.SetDirty(pool);
        }
    }

    public static void ApplyEnhancedPlasmaProjectiles()
    {
        ApplyBrightRedPlasmaProjectiles();
    }

    public static GameObject BuildEnemyPlasmaOrbGameObject(Material coreMat, Color trailHead, Color trailMid, Color trailEnd, string prefabName = "EnemyProjectile_PlasmaOrb")
    {
        GameObject root = new GameObject(prefabName);

        var sphereCol = root.AddComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 0.45f;

        GameObject visualRoot = new GameObject("[Visual_Root]");
        visualRoot.transform.SetParent(root.transform, false);

        // Tải Mesh IcoSphere nếu có
        Mesh icoMesh = null;
        GameObject meshObj = AssetDatabase.LoadAssetAtPath<GameObject>(ICOSPHERE_MESH_PATH);
        if (meshObj != null)
        {
            MeshFilter mf = meshObj.GetComponentInChildren<MeshFilter>();
            if (mf != null) icoMesh = mf.sharedMesh;
        }

        // Quầng Plasma ngoài (Outer Corona)
        GameObject corona = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        corona.name = "[Corona]";
        corona.transform.SetParent(visualRoot.transform, false);
        corona.transform.localScale = Vector3.one * 1.2f;
        var coronaCol = corona.GetComponent<Collider>();
        if (coronaCol != null) Object.DestroyImmediate(coronaCol);

        if (icoMesh != null)
        {
            MeshFilter cmf = corona.GetComponent<MeshFilter>();
            if (cmf != null) cmf.sharedMesh = icoMesh;
        }

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

        if (icoMesh != null)
        {
            MeshFilter kmf = core.GetComponent<MeshFilter>();
            if (kmf != null) kmf.sharedMesh = icoMesh;
        }

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
                new GradientColorKey(trailHead, 0.0f),
                new GradientColorKey(trailMid, 0.5f),
                new GradientColorKey(trailEnd, 1.0f)
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
        main.startColor = trailMid;
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

    #region 51 & 52: User Custom Camera Setup & Undo
    private static GameObject FindGameObjectInScene(string name)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == name) return t.gameObject;
            }
        }
        return null;
    }

    [MenuItem("Tools/Virtual Rail/51. Setup User Custom Camera (Tạo Camera Riêng Biệt Tự Do)")]
    public static void SetupUserCustomCamera()
    {
        // 1. Tìm hoặc tạo GameObject UserCustomCamera trong Scene
        GameObject customCamObj = FindGameObjectInScene("UserCustomCamera");
        if (customCamObj == null)
        {
            customCamObj = new GameObject("UserCustomCamera");
            customCamObj.tag = "MainCamera";
            Camera cam = customCamObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 2000f;
            cam.depth = 0;

            customCamObj.AddComponent<AudioListener>();

            // Đặt vị trí khởi đầu thuận tiện để người dùng quan sát và tự kéo
            customCamObj.transform.position = new Vector3(0f, 6.5f, -30f);
            customCamObj.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            Undo.RegisterCreatedObjectUndo(customCamObj, "Create UserCustomCamera");
        }
        else
        {
            customCamObj.SetActive(true);
            customCamObj.tag = "MainCamera";
        }

        // 2. Vô hiệu hóa Camera cũ (Follow Cam) trong Scene để tránh 2 Camera cùng tag MainCamera
        GameObject oldCam = FindGameObjectInScene("Follow Cam");
        if (oldCam != null)
        {
            oldCam.SetActive(false);
            EditorUtility.SetDirty(oldCam);
        }

        // 3. Bật cờ useExternalCamera trên Prefab VirtualRail_PlayerRig
        GameObject playerRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (playerRigPrefab != null)
        {
            VirtualRailCameraRig camRig = playerRigPrefab.GetComponentInChildren<VirtualRailCameraRig>(true);
            if (camRig != null)
            {
                SerializedObject so = new SerializedObject(camRig);
                SerializedProperty prop = so.FindProperty("useExternalCamera");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(playerRigPrefab);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        // 4. Chọn ngay UserCustomCamera để người dùng dùng phím W/E kéo thả
        Selection.activeGameObject = customCamObj;
        EditorGUIUtility.PingObject(customCamObj);

        Debug.Log("<color=green><b>[VirtualRail] ĐÃ TẠO VÀ KÍCH HOẠT THÀNH CÔNG CAMERA RIÊNG BIỆT: UserCustomCamera!</b></color>\nBạn có thể tự do dùng công cụ Move/Rotate (W/E) kéo Camera này đến bất kỳ vị trí và góc nhìn nào trong Scene View!");
    }

    [MenuItem("Tools/Virtual Rail/52. Revert to Default Rail Camera (UNDO - Trở Về Camera Tự Động)")]
    public static void RevertToDefaultRailCamera()
    {
        // 1. Tắt UserCustomCamera trong Scene
        GameObject customCamObj = FindGameObjectInScene("UserCustomCamera");
        if (customCamObj != null)
        {
            customCamObj.SetActive(false);
            customCamObj.tag = "Untagged";
            EditorUtility.SetDirty(customCamObj);
        }

        // 2. Bật lại Follow Cam cũ trong Scene (kể cả khi đang bị Inactive)
        GameObject oldCam = FindGameObjectInScene("Follow Cam");
        if (oldCam != null)
        {
            oldCam.SetActive(true);
            oldCam.tag = "MainCamera";
            EditorUtility.SetDirty(oldCam);
        }

        // 3. Tắt cờ useExternalCamera trên Prefab VirtualRail_PlayerRig
        GameObject playerRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (playerRigPrefab != null)
        {
            VirtualRailCameraRig camRig = playerRigPrefab.GetComponentInChildren<VirtualRailCameraRig>(true);
            if (camRig != null)
            {
                SerializedObject so = new SerializedObject(camRig);
                SerializedProperty prop = so.FindProperty("useExternalCamera");
                if (prop != null)
                {
                    prop.boolValue = false;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(playerRigPrefab);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ CAMERA TỰ ĐỘNG MẶC ĐỊNH!</b></color>");
    }
    #endregion

    #region Charge Shot VFX (Houidisoft Green Plasma Orb & Star Fox Cyan Plasma Orb)
    public const string HOUIDI_PLASMA_GREEN_PREFAB_PATH = "Assets/Houidisoft technology/Plasma Shader/Prefabs/Plasma Sphere (3).prefab";
    public const string PLAYER_CHARGE_HOUIDI_ORB_PREFAB_PATH = "Assets/Prefabs/PlayerChargeShot_GreenPlasmaOrb.prefab";
    public const string PLAYER_CHARGE_ORB_PREFAB_PATH = "Assets/Prefabs/PlayerChargeShot_PlasmaOrb.prefab";
    public const string PLAYER_CHARGE_GREEN_ORB_PREFAB_PATH = "Assets/Prefabs/PlayerChargeShot_GreenOrb.prefab";
    public const string LEGACY_MISSILE_PREFAB_PATH = "Assets/FreeSciFi/Prefabs/MinesBombs/Missile.prefab";
    public const string PLAYER_CHARGE_CORE_MAT_PATH = "Assets/Materials/PlayerChargeShot_Core.mat";
    public const string HOVL_GREEN_FIRE_PREFAB_PATH = "Assets/Hovl Studio/Procedural fire/Prefabs/Magic fire pro green.prefab";
    public const string HOUIDI_PLASMA_BUILTIN_SHADER = "VirtualRail/HouidiPlasmaBuiltin";
    public const string HOUIDI_CORE_BUILTIN_MAT_PATH = "Assets/Materials/PlayerChargeShot_HouidiCore_Builtin.mat";
    public const string HOUIDI_GREEN_BUILTIN_MAT_PATH = "Assets/Materials/PlayerChargeShot_HouidiGreen_Builtin.mat";
    public const string HOUIDI_MAT_PLSM2_2 = "Assets/Houidisoft technology/Plasma Shader/Materials/plsm2 2.mat";
    public const string HOUIDI_MAT_PLSM_2 = "Assets/Houidisoft technology/Plasma Shader/Materials/plsm 2.mat";

    [MenuItem("Tools/Virtual Rail/Charge Shot VFX/1. Apply Houidisoft Green Plasma Orb (NEW - ON)")]
    public static void ApplyHouidisoftGreenPlasmaOrb()
    {
        // 1. Đảm bảo file Prefab Quả cầu Plasma Xanh Lá Houidisoft đã được dựng với Material Built-in
        GameObject orbObj = BuildPlayerChargeHouidiGreenOrbGameObject();
        PrefabUtility.SaveAsPrefabAsset(orbObj, PLAYER_CHARGE_HOUIDI_ORB_PREFAB_PATH);
        Object.DestroyImmediate(orbObj);
        AssetDatabase.SaveAssets();

        // 2. Gán prefab mới vào AimAssistModule trên VirtualRail_PlayerRig.prefab
        SetAimAssistMissilePrefab(PLAYER_CHARGE_HOUIDI_ORB_PREFAB_PATH);

        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT QUẢ CẦU PLASMA NĂNG LƯỢNG XANH LÁ HOUIDISOFT (NEW - ON):</b></color> " + PLAYER_CHARGE_HOUIDI_ORB_PREFAB_PATH);
    }

    [MenuItem("Tools/Virtual Rail/Charge Shot VFX/Inspect & Adjust Plasma Orb Size (Open Inspector)")]
    public static void SelectAndAdjustPlasmaOrbInInspector()
    {
        GameObject orbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_CHARGE_HOUIDI_ORB_PREFAB_PATH);
        if (orbAsset != null)
        {
            Selection.activeObject = orbAsset;
            EditorGUIUtility.PingObject(orbAsset);
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ CHỌN PREFAB QUẢ CẦU PLASMA:</b></color> Hãy xem cửa sổ Inspector bên phải để kéo thanh trượt điều chỉnh kích thước trực quan!");
        }
        else
        {
            Debug.LogError("[VirtualRail] Không tìm thấy prefab: " + PLAYER_CHARGE_HOUIDI_ORB_PREFAB_PATH);
        }
    }

    [MenuItem("Tools/Virtual Rail/Charge Shot VFX/Fix Houidisoft Asset Folder Materials for Built-in RP (NO PINK)")]
    public static void FixHouidisoftPlasmaForBuiltIn()
    {
        // 1. Cập nhật material plsm2 2.mat (vỏ ngoài xanh lá của Houidisoft) sang shader Built-in
        Material outerMat = AssetDatabase.LoadAssetAtPath<Material>(HOUIDI_MAT_PLSM2_2);
        Shader builtinShader = Shader.Find(HOUIDI_PLASMA_BUILTIN_SHADER);
        if (outerMat != null && builtinShader != null)
        {
            outerMat.shader = builtinShader;
            outerMat.SetColor("_BoarderColor", new Color(0f, 3f, 0.2f, 1f));
            outerMat.SetColor("_Boarder_Color", new Color(0.35f, 2.6f, 0.85f, 1f));
            outerMat.SetColor("_Color", new Color(0.07f, 0.95f, 0.15f, 1f));
            Texture2D caustics = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Houidisoft technology/Plasma Shader/Texture/caustics.png");
            if (caustics != null) outerMat.SetTexture("_Texture2D", caustics);
            EditorUtility.SetDirty(outerMat);
        }

        // 2. Cập nhật material plsm 2.mat (lõi trong xanh lá của Houidisoft) sang shader Core Built-in
        Material coreMat = AssetDatabase.LoadAssetAtPath<Material>(HOUIDI_MAT_PLSM_2);
        Shader coreShader = Shader.Find("VirtualRail/PlasmaOrbCore");
        if (coreMat != null && coreShader != null)
        {
            coreMat.shader = coreShader;
            coreMat.SetColor("_CoreColor", new Color(1.5f, 3.5f, 1.8f, 1f));
            coreMat.SetColor("_RimColor", new Color(0.1f, 3.0f, 0.3f, 1f));
            EditorUtility.SetDirty(coreMat);
        }

        // 3. Đảm bảo PlayerChargeShot_GreenPlasmaOrb.prefab sử dụng material Built-in
        ApplyHouidisoftGreenPlasmaOrb();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ FIX TRIỆT ĐỂ LỖI TÍM CHO HOUIDISOFT PLASMA TRÊN BUILT-IN RP!</b></color>");
    }

    [MenuItem("Tools/Virtual Rail/Charge Shot VFX/Revert Houidisoft Asset Folder to Original URP (OFF)")]
    public static void RevertHouidisoftPlasmaToOriginalURP()
    {
        Material outerMat = AssetDatabase.LoadAssetAtPath<Material>(HOUIDI_MAT_PLSM2_2);
        Shader sg = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Houidisoft technology/Plasma Shader/Shader/Plasma.shadergraph");
        if (outerMat != null && sg != null)
        {
            outerMat.shader = sg;
            EditorUtility.SetDirty(outerMat);
        }

        Material coreMat = AssetDatabase.LoadAssetAtPath<Material>(HOUIDI_MAT_PLSM_2);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (coreMat != null && urpLit != null)
        {
            coreMat.shader = urpLit;
            EditorUtility.SetDirty(coreMat);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN HOUIDISOFT VỀ URP GỐC!</b></color>");
    }

    [MenuItem("Tools/Virtual Rail/Charge Shot VFX/2. Apply Star Fox Cyan Plasma Orb")]
    public static void ApplyStarFoxChargePlasmaOrb()
    {
        // 1. Tạo Material lõi phát quang nếu chưa có
        Material coreMat = AssetDatabase.LoadAssetAtPath<Material>(PLAYER_CHARGE_CORE_MAT_PATH);
        if (coreMat == null)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");

            coreMat = new Material(standardShader);
            coreMat.name = "PlayerChargeShot_Core";
            coreMat.color = new Color(0.1f, 1f, 0.9f, 1f);
            if (coreMat.HasProperty("_EmissionColor"))
            {
                coreMat.EnableKeyword("_EMISSION");
                coreMat.SetColor("_EmissionColor", new Color(0.15f, 2.2f, 1.8f) * 1.5f);
            }
            AssetDatabase.CreateAsset(coreMat, PLAYER_CHARGE_CORE_MAT_PATH);
            AssetDatabase.SaveAssets();
        }

        // 2. Dựng GameObject Quả cầu Plasma tụ lực chuẩn Star Fox 64
        GameObject orbObj = BuildPlayerChargeOrbGameObject(coreMat);
        PrefabUtility.SaveAsPrefabAsset(orbObj, PLAYER_CHARGE_ORB_PREFAB_PATH);
        Object.DestroyImmediate(orbObj);
        AssetDatabase.SaveAssets();

        // 3. Gán prefab mới vào AimAssistModule trên VirtualRail_PlayerRig.prefab
        SetAimAssistMissilePrefab(PLAYER_CHARGE_ORB_PREFAB_PATH);

        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ KÍCH HOẠT QUẢ CẦU PLASMA TỤ LỰC CHUẨN STAR FOX 64 (ON):</b></color> " + PLAYER_CHARGE_ORB_PREFAB_PATH);
    }

    [MenuItem("Tools/Virtual Rail/Charge Shot VFX/3. Revert to Legacy Military Missile (OFF)")]
    public static void RevertToLegacyMilitaryMissile()
    {
        SetAimAssistMissilePrefab(LEGACY_MISSILE_PREFAB_PATH);
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ TÊN LỬA QUÂN SỰ CŨ (OFF):</b></color> " + LEGACY_MISSILE_PREFAB_PATH);
    }

    private static void SetAimAssistMissilePrefab(string prefabPath)
    {
        GameObject newMissilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (newMissilePrefab == null)
        {
            Debug.LogError("[VirtualRail] Không tìm thấy prefab: " + prefabPath);
            return;
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (rigPrefab != null)
        {
            AimAssistModule aimAssist = rigPrefab.GetComponentInChildren<AimAssistModule>(true);
            if (aimAssist != null)
            {
                SerializedObject so = new SerializedObject(aimAssist);
                SerializedProperty prop = so.FindProperty("missilePrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = newMissilePrefab;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(rigPrefab);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        // Cập nhật cả instance trong scene đang mở nếu có
        AimAssistModule sceneAimAssist = Object.FindAnyObjectByType<AimAssistModule>();
        if (sceneAimAssist != null)
        {
            sceneAimAssist.missilePrefab = newMissilePrefab;
            EditorUtility.SetDirty(sceneAimAssist);
        }
    }

    public static GameObject BuildPlayerChargeOrbGameObject(Material coreMat)
    {
        GameObject root = new GameObject("PlayerChargeShot_PlasmaOrb");

        // 1. Collider & Rigidbody
        var sphereCol = root.AddComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 1.1f;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 2. Logic bay tự dẫn Homing Missile
        var homing = root.AddComponent<VirtualRailHomingMissile>();
        homing.speed = 95f;
        homing.turnRate = 180f;
        homing.lifeTime = 5.0f;
        homing.detonationDistance = 2.0f;
        homing.splashRadius = 10f;

        GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/BigExplosion.prefab");
        if (expPrefab != null) homing.explosionPrefab = expPrefab;

        // 3. Visual Root & Child spheres
        GameObject visualRoot = new GameObject("[Visual_Root]");
        visualRoot.transform.SetParent(root.transform, false);

        Mesh icoMesh = null;
        GameObject meshObj = AssetDatabase.LoadAssetAtPath<GameObject>(ICOSPHERE_MESH_PATH);
        if (meshObj != null)
        {
            MeshFilter mf = meshObj.GetComponentInChildren<MeshFilter>();
            if (mf != null) icoMesh = mf.sharedMesh;
        }

        // Corona (Outer Plasma Glow Sphere)
        GameObject corona = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        corona.name = "[Corona]";
        corona.transform.SetParent(visualRoot.transform, false);
        corona.transform.localScale = Vector3.one * 1.8f;
        var cCol = corona.GetComponent<Collider>();
        if (cCol != null) Object.DestroyImmediate(cCol);

        if (icoMesh != null)
        {
            MeshFilter cmf = corona.GetComponent<MeshFilter>();
            if (cmf != null) cmf.sharedMesh = icoMesh;
        }

        Material coronaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexShield_ElectricCyan.mat");
        if (coronaMat == null) coronaMat = AssetDatabase.LoadAssetAtPath<Material>(HOVL_FIRE_MAT_PATH);
        Renderer cRend = corona.GetComponent<Renderer>();
        if (cRend != null && coronaMat != null) cRend.sharedMaterial = coronaMat;

        // Core (Inner Hot Plasma Sphere)
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "[Core]";
        core.transform.SetParent(visualRoot.transform, false);
        core.transform.localScale = Vector3.one * 1.0f;
        var kCol = core.GetComponent<Collider>();
        if (kCol != null) Object.DestroyImmediate(kCol);

        if (icoMesh != null)
        {
            MeshFilter kmf = core.GetComponent<MeshFilter>();
            if (kmf != null) kmf.sharedMesh = icoMesh;
        }

        Renderer kRend = core.GetComponent<Renderer>();
        if (kRend != null && coreMat != null) kRend.sharedMaterial = coreMat;

        // Dynamic Point Light
        GameObject lightObj = new GameObject("[PointLight]");
        lightObj.transform.SetParent(root.transform, false);
        Light pl = lightObj.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.15f, 0.95f, 1f);
        pl.range = 16f;
        pl.intensity = 4.0f;

        // Ribbon Trail Renderer
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 1.4f;
        trail.endWidth = 0.05f;

        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(HOVL_TRAIL_MAT_PATH);
        if (trailMat == null) trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
        if (trailMat != null) trail.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.2f, 1f, 0.95f), 0.0f),
                new GradientColorKey(new Color(0.1f, 0.85f, 1f), 0.5f),
                new GradientColorKey(new Color(0.0f, 0.4f, 0.85f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;

        // Controller Visual Animation
        root.AddComponent<VirtualRailChargeShotVisual>();

        return root;
    }

    public static GameObject BuildPlayerChargeGreenOrbGameObject()
    {
        GameObject root = new GameObject("PlayerChargeShot_GreenOrb");

        // 1. Collider & Rigidbody (Gameplay / Physics)
        var sphereCol = root.AddComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 1.2f;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 2. Logic bay tự dẫn Homing Missile & Splash AoE Star Fox 64
        var homing = root.AddComponent<VirtualRailHomingMissile>();
        homing.speed = 240f;
        homing.turnRate = 420f;
        homing.lifeTime = 5.0f;
        homing.detonationDistance = 2.0f;
        homing.splashRadius = 10f;

        GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/BigExplosion.prefab");
        if (expPrefab != null) homing.explosionPrefab = expPrefab;

        // 3. Visual Child: Load Magic fire pro green prefab
        GameObject hovlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HOVL_GREEN_FIRE_PREFAB_PATH);
        if (hovlPrefab != null)
        {
            GameObject vfx = Object.Instantiate(hovlPrefab);
            vfx.name = "[Visual_MagicFireGreen]";
            vfx.transform.SetParent(root.transform, false);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.identity;
            vfx.transform.localScale = Vector3.one * 1.0f;

            // Đảm bảo không có Collider nào sót lại trong VFX gây va quệt thân tàu
            Collider[] colliders = vfx.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }

            // Tinh chỉnh Particle Systems: World Simulation Space cho vệt khói và tia lửa
            ParticleSystem[] pss = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < pss.Length; i++)
            {
                var main = pss[i].main;
                if (pss[i].name.Contains("Sparks") || pss[i].name.Contains("Smoke"))
                {
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                }
            }
        }

        // 4. Dynamic Point Light (Màu xanh lục phát quang trong không gian)
        GameObject lightObj = new GameObject("[PointLight]");
        lightObj.transform.SetParent(root.transform, false);
        Light pl = lightObj.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.2f, 1f, 0.35f);
        pl.range = 16f;
        pl.intensity = 3.5f;

        // 5. Ribbon Trail Renderer (Vệt đuôi xanh lục phát sáng)
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 1.2f;
        trail.endWidth = 0.05f;

        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(HOVL_TRAIL_MAT_PATH);
        if (trailMat == null) trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
        if (trailMat != null) trail.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 1f, 0.4f), 0.0f),
                new GradientColorKey(new Color(0.1f, 0.9f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.0f, 0.5f, 0.1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;

        return root;
    }

    public static GameObject BuildPlayerChargeHouidiGreenOrbGameObject()
    {
        GameObject root = new GameObject("PlayerChargeShot_GreenPlasmaOrb");

        // 1. Collider & Rigidbody trên Root (Gameplay / Physics)
        var sphereCol = root.AddComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 2.85f;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 2. Logic bay tự dẫn & Splash AoE Star Fox 64
        var homing = root.AddComponent<VirtualRailHomingMissile>();
        homing.speed = 240f;
        homing.turnRate = 420f;
        homing.lifeTime = 5.0f;
        homing.detonationDistance = 2.0f;
        homing.splashRadius = 25f;

        GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/BigExplosion.prefab");
        if (expPrefab != null) homing.explosionPrefab = expPrefab;

        // 3. Visual Child: Nạp model Plasma Sphere (3) từ Houidisoft
        GameObject houidiSource = AssetDatabase.LoadAssetAtPath<GameObject>(HOUIDI_PLASMA_GREEN_PREFAB_PATH);
        Transform coronaT = null;
        Transform coreT = null;

        if (houidiSource != null)
        {
            GameObject visual = Object.Instantiate(houidiSource);
            visual.name = "[Visual_PlasmaSphere3]";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            // Kích thước chuẩn gốc Houidisoft: 3.85m
            visual.transform.localScale = Vector3.one * 3.85f;

            // Bóc tách toàn bộ Collider đặc của Houidisoft để triệt tiêu va chạm cứng với thân tàu
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }

            coronaT = visual.transform;
            Renderer outRend = visual.GetComponent<Renderer>();
            Material outMat = AssetDatabase.LoadAssetAtPath<Material>(HOUIDI_GREEN_BUILTIN_MAT_PATH);
            if (outRend != null && outMat != null) outRend.sharedMaterial = outMat;

            Transform childSphere = visual.transform.Find("Sphere");
            if (childSphere != null)
            {
                coreT = childSphere;
                Renderer inRend = childSphere.GetComponent<Renderer>();
                Material inMat = AssetDatabase.LoadAssetAtPath<Material>(HOUIDI_CORE_BUILTIN_MAT_PATH);
                if (inRend != null && inMat != null) inRend.sharedMaterial = inMat;
            }
        }

        // 4. Dynamic Point Light (Màu xanh lục phát quang trong không gian)
        GameObject lightObj = new GameObject("[PointLight]");
        lightObj.transform.SetParent(root.transform, false);
        Light pl = lightObj.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.15f, 1f, 0.35f);
        pl.range = 32f;
        pl.intensity = 4.5f;

        // 5. Ribbon Trail Renderer (Tắt vệt đuôi theo yêu cầu để quả cầu năng lượng thuần khiết)
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.enabled = false;
        trail.time = 0.35f;
        trail.startWidth = 1.2f;
        trail.endWidth = 0.05f;

        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(HOVL_TRAIL_MAT_PATH);
        if (trailMat == null) trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
        if (trailMat != null) trail.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.2f, 1f, 0.4f), 0.0f),
                new GradientColorKey(new Color(0.1f, 0.9f, 0.25f), 0.5f),
                new GradientColorKey(new Color(0.0f, 0.5f, 0.1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;

        // 6. Xoay tròn tự nhiên quanh trục đạn và hỗ trợ chỉnh kích thước trực quan
        var visualAnim = root.AddComponent<VirtualRailChargeShotVisual>();
        visualAnim.outerDiameter = 3.85f;
        visualAnim.innerCoreRatio = 0.54f;
        visualAnim.enableTrail = false;
        visualAnim.lightRange = 32f;
        visualAnim.collisionRadius = 2.85f;
        visualAnim.splashRadius = 25f;
        visualAnim.AutoBindReferences();
        visualAnim.ApplyDimensions();

        return root;
    }
    #endregion

    #region Modern Cockpit HUD System (Level 3 Undo Toggle)
    [MenuItem("Tools/Virtual Rail/45. Switch to Modern Cockpit HUD (ON)")]
    [MenuItem("Tools/Virtual Rail/HUD/1. Switch to Modern Cockpit HUD (ON)")]
    public static void SwitchToModernCockpitHUD()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        Transform existingHUD = canvas.transform.Find("[ModernCockpitHUD]");
        if (existingHUD != null)
        {
            Object.DestroyImmediate(existingHUD.gameObject);
        }

        BuildModernCockpitHUDHierarchy(canvas.transform);
        ToggleLegacyGameUI(false);

        Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        EditorPrefs.SetBool(MODERN_HUD_APPLIED_KEY, true);
        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ KÍCH HOẠT MODERN COCKPIT HUD (ON):</b></color> 5 Cụm giao diện Hologram Sci-Fi đã sẵn sàng trên Canvas và đã lưu Scene!");
    }

    [MenuItem("Tools/Virtual Rail/46. Revert Modern HUD (OFF - UNDO)")]
    [MenuItem("Tools/Virtual Rail/HUD/2. Revert to Legacy UI (OFF - UNDO)")]
    public static void RevertModernCockpitHUD()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform existingHUD = canvas.transform.Find("[ModernCockpitHUD]");
            if (existingHUD != null)
            {
                Object.DestroyImmediate(existingHUD.gameObject);
            }
        }

        ToggleLegacyGameUI(true);

        Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        EditorPrefs.DeleteKey(MODERN_HUD_APPLIED_KEY);
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN LEGACY UI (UNDO):</b></color> Đã gỡ bỏ ModernCockpitHUD, khôi phục giao diện nguyên bản và đã lưu Scene!");
    }

    private static void ToggleLegacyGameUI(bool enable)
    {
        GameObject gameUI = GameObject.Find("Game UI");
        if (gameUI != null)
        {
            gameUI.SetActive(enable);

            Transform healthBar = gameUI.transform.Find("Health Bar");
            if (healthBar != null) healthBar.gameObject.SetActive(enable);

            Transform shieldUI = gameUI.transform.Find("Shield UI");
            if (shieldUI != null)
            {
                shieldUI.gameObject.SetActive(enable);
                Transform hb = shieldUI.Find("Health Bar");
                if (hb != null) hb.gameObject.SetActive(enable);
            }

            Transform score = gameUI.transform.Find("Score");
            if (score != null) score.gameObject.SetActive(enable);

            Transform highScore = gameUI.transform.Find("High Score");
            if (highScore != null) highScore.gameObject.SetActive(enable);

            Transform timer = gameUI.transform.Find("Game Timer");
            if (timer != null) timer.gameObject.SetActive(enable);
        }
    }

    private static void EnsureHUDSpritesConfigured()
    {
        string hudDir = "Assets/Textures/HUD";
        string[] files = new string[]
        {
            "hud_frame_bracket.png",
            "hud_bar_segmented_bg.png",
            "hud_bar_segmented_fill.png",
            "hud_icon_smartbomb.png",
            "hud_icon_shield.png",
            "hud_icon_twin_laser.png",
            "hud_avatar_pilot.png",
            "hud_speed_gauge_arc.png",
            "hud_scanline_overlay.png",
            "hud_panel_left_chamfered.png",
            "hud_panel_score_red.png",
            "hud_panel_hits_cyan.png",
            "hud_bar_capsule_track.png",
            "hud_bar_capsule_fill.png",
            "hud_icon_classic_bomb.png",
            "hud_combo_radial_dial.png",
            "hud_combo_radial_fill.png"
        };

        bool reimportedAny = false;
        foreach (string file in files)
        {
            string path = $"{hudDir}/{file}";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                Vector4 targetBorder = Vector4.zero;
                if (file == "hud_frame_bracket.png") targetBorder = new Vector4(36, 36, 36, 36);
                else if (file == "hud_bar_segmented_bg.png" || file == "hud_bar_segmented_fill.png") targetBorder = new Vector4(16, 6, 16, 6);
                else if (file == "hud_panel_score_red.png" || file == "hud_panel_hits_cyan.png") targetBorder = new Vector4(24, 24, 24, 24);
                else if (file == "hud_bar_capsule_track.png" || file == "hud_bar_capsule_fill.png") targetBorder = new Vector4(16, 12, 16, 12);

                if (importer.spriteBorder != targetBorder)
                {
                    importer.spriteBorder = targetBorder;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    reimportedAny = true;
                }
            }
        }

        if (reimportedAny)
        {
            AssetDatabase.Refresh();
        }
    }

    private static GameObject BuildModernCockpitHUDHierarchy(Transform canvasParent)
    {
        EnsureHUDSpritesConfigured();

        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Orbitron.ttf");
        if (font == null) font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/HUD_SciFi_Bahnschrift.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Load Pixel-Perfect Sci-Fi Sprites matching 100% User Reference
        Sprite leftChamferedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_panel_left_chamfered.png");
        Sprite scoreRedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_panel_score_red.png");
        Sprite hitsCyanSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_panel_hits_cyan.png");
        Sprite capsuleTrackSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_bar_capsule_track.png");
        Sprite capsuleFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_bar_capsule_fill.png");
        Sprite classicBombSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_icon_classic_bomb.png");
        Sprite comboDialSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_combo_radial_dial.png");
        Sprite comboFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_combo_radial_fill.png");

        // Legacy / Secondary Clusters Sprites
        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_frame_bracket.png");
        Sprite barFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_bar_segmented_fill.png");
        Sprite barBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_bar_segmented_bg.png");
        Sprite laserIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_icon_twin_laser.png");
        Sprite pilotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_avatar_pilot.png");

        GameObject hudRoot = new GameObject("[ModernCockpitHUD]");
        hudRoot.transform.SetParent(canvasParent, false);

        RectTransform rootRt = hudRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        CanvasGroup hudCg = hudRoot.AddComponent<CanvasGroup>();
        ModernCockpitHUD hud = hudRoot.AddComponent<ModernCockpitHUD>();

        Color bgPanelColor = new Color(0.02f, 0.08f, 0.14f, 0.88f);
        Color borderCyan = new Color(0f, 0.95f, 1f, 1f);
        Color amberColor = new Color(1f, 0.65f, 0.05f, 1f);
        Color pinkScoreColor = new Color(1f, 0.15f, 0.45f, 1f);
        Color goldComboColor = new Color(1f, 0.84f, 0f, 1f);

        // -------------------------------------------------------------
        // CỤM 1: TOP-LEFT (CHAMFERED PANEL: SHIELD & BOOST METER & 3 BOMBS)
        // -------------------------------------------------------------
        // Tỷ lệ chuẩn Full HD 1080p (Scale 1.45x) chiếm ~21.3% chiều rộng màn hình (410x206px)
        GameObject tlCluster = CreateUIPanel("Cluster_TopLeft", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -40f), new Vector2(410f, 206f), bgPanelColor, leftChamferedSprite, Image.Type.Simple);
        hud.clusterTopLeftRoot = tlCluster.GetComponent<RectTransform>();

        // Row 1: Shield (Title above bar + Progress Bar + 85% on right)
        CreateUIText("ShieldLabel", tlCluster.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -20f), new Vector2(110f, 24f), "SHIELD", font, 17, FontStyle.Bold, borderCyan, TextAnchor.MiddleLeft);
        GameObject shieldBarObj = CreateUIProgressBar("ShieldBar", tlCluster.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -50f), new Vector2(254f, 26f), borderCyan, capsuleTrackSprite, capsuleFillSprite);
        hud.shieldBarFill = shieldBarObj.transform.Find("Fill").GetComponent<Image>();
        hud.shieldPercentText = CreateUIText("ShieldPercent", tlCluster.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(298f, -50f), new Vector2(95f, 26f), "85%", font, 22, FontStyle.Bold, borderCyan, TextAnchor.MiddleLeft);

        // Row 2: Boost Bar (Rectangular Bar with "BOOST" label inside)
        GameObject chargeBarObj = CreateUIProgressBar("BoostBar", tlCluster.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -92f), new Vector2(284f, 26f), amberColor, capsuleTrackSprite, capsuleFillSprite);
        hud.chargeBarFill = chargeBarObj.transform.Find("Fill").GetComponent<Image>();
        CreateUIText("BoostLabel", chargeBarObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(90f, 22f), "BOOST", font, 15, FontStyle.Bold, new Color(1f, 0.88f, 0.45f, 1f), TextAnchor.MiddleLeft);
        hud.chargeTimerText = null;
        hud.chargeStatusText = null;

        // Row 3: Bombs (3 Glowing Hologram Warhead Icons)
        hud.bombIconImages = new Image[3];
        for (int b = 0; b < 3; b++)
        {
            GameObject bIcon = CreateUIImage("Bomb_" + b, tlCluster.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f + (b * 48f), -142f), new Vector2(38f, 38f), Color.white, classicBombSprite);
            hud.bombIconImages[b] = bIcon.GetComponent<Image>();
        }
        hud.bombCountText = null;

        // -------------------------------------------------------------
        // CỤM 2: TOP-RIGHT (STACKED PANELS: SCORE BOX + HITS BOX + COMBO DIAL)
        // -------------------------------------------------------------
        // Tỷ lệ chuẩn Full HD 1080p (Scale 1.45x) kích thước 355x320px
        GameObject trCluster = new GameObject("Cluster_TopRight");
        trCluster.transform.SetParent(hudRoot.transform, false);
        RectTransform trRt = trCluster.AddComponent<RectTransform>();
        trRt.anchorMin = new Vector2(1f, 1f);
        trRt.anchorMax = new Vector2(1f, 1f);
        trRt.pivot = new Vector2(1f, 1f);
        trRt.anchoredPosition = new Vector2(-50f, -40f);
        trRt.sizeDelta = new Vector2(355f, 335f);
        hud.clusterTopRightRoot = trRt;

        // Box 1: Score Box (Neon Pink/Red Border Frame + Cyan Value)
        GameObject scoreBox = CreateUIPanel("ScoreBox", trCluster.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(355f, 64f), Color.clear, scoreRedSprite, Image.Type.Sliced);
        CreateUIText("ScoreLabel", scoreBox.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(90f, 32f), "SCORE", font, 18, FontStyle.Bold, pinkScoreColor, TextAnchor.MiddleLeft);
        hud.scoreValueText = CreateUIText("ScoreValue", scoreBox.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(210f, 38f), "024,500", font, 30, FontStyle.Bold, borderCyan, TextAnchor.MiddleRight);

        // Box 2: Hits Box (Neon Cyan Border Frame + Cyan Value)
        GameObject hitsBox = CreateUIPanel("HitsBox", trCluster.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -76f), new Vector2(355f, 64f), Color.clear, hitsCyanSprite, Image.Type.Sliced);
        CreateUIText("HitsLabel", hitsBox.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(90f, 32f), "HITS", font, 18, FontStyle.Bold, borderCyan, TextAnchor.MiddleLeft);
        hud.hitCountValueText = CreateUIText("HitsValue", hitsBox.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(210f, 38f), "156", font, 30, FontStyle.Bold, borderCyan, TextAnchor.MiddleRight);

        // Box 3: Endless Telemetry Box (Sector Threat Level & Distance Tracker)
        // High-tech Sci-Fi Bracket Frame with 9-Slice and glowing corner chamfers
        GameObject telemetryBox = CreateUIPanel("TelemetryBox", trCluster.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -148f), new Vector2(355f, 48f), new Color(0.02f, 0.08f, 0.14f, 0.88f), frameSprite, Image.Type.Sliced);
        
        // Sub 3a: Threat Badge
        GameObject threatBadge = new GameObject("ThreatBadge");
        threatBadge.transform.SetParent(telemetryBox.transform, false);
        RectTransform tbRt = threatBadge.AddComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0f);
        tbRt.anchorMax = new Vector2(0.52f, 1f);
        tbRt.offsetMin = Vector2.zero;
        tbRt.offsetMax = Vector2.zero;
        hud.threatLevelText = CreateUIText("ThreatLabel", threatBadge.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 7f), new Vector2(-20f, 20f), "THREAT LV.01", font, 13, FontStyle.Bold, borderCyan, TextAnchor.MiddleLeft);

        GameObject tpObj = new GameObject("ThreatProgress");
        tpObj.transform.SetParent(threatBadge.transform, false);
        RectTransform tpRt = tpObj.AddComponent<RectTransform>();
        tpRt.anchorMin = new Vector2(0f, 0.5f);
        tpRt.anchorMax = new Vector2(1f, 0.5f);
        tpRt.pivot = new Vector2(0f, 0.5f);
        tpRt.anchoredPosition = new Vector2(16f, -10f);
        tpRt.sizeDelta = new Vector2(-32f, 6f);
        Image tpTrack = tpObj.AddComponent<Image>();
        tpTrack.sprite = barBgSprite;
        tpTrack.type = Image.Type.Sliced;
        tpTrack.color = new Color(0.08f, 0.22f, 0.32f, 0.95f);

        GameObject tpFillObj = new GameObject("Fill");
        tpFillObj.transform.SetParent(tpObj.transform, false);
        RectTransform tpfRt = tpFillObj.AddComponent<RectTransform>();
        tpfRt.anchorMin = Vector2.zero;
        tpfRt.anchorMax = Vector2.one;
        tpfRt.offsetMin = Vector2.zero;
        tpfRt.offsetMax = Vector2.zero;
        hud.threatProgressFill = tpFillObj.AddComponent<Image>();
        hud.threatProgressFill.sprite = barFillSprite;
        hud.threatProgressFill.type = Image.Type.Filled;
        hud.threatProgressFill.fillMethod = Image.FillMethod.Horizontal;
        hud.threatProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.threatProgressFill.fillAmount = 0.25f;
        hud.threatProgressFill.color = borderCyan;

        // Sub 3b: Distance Tracker
        GameObject distTracker = new GameObject("DistanceTracker");
        distTracker.transform.SetParent(telemetryBox.transform, false);
        RectTransform dtRt = distTracker.AddComponent<RectTransform>();
        dtRt.anchorMin = new Vector2(0.52f, 0f);
        dtRt.anchorMax = new Vector2(1f, 1f);
        dtRt.offsetMin = Vector2.zero;
        dtRt.offsetMax = Vector2.zero;
        CreateUIText("DistLabel", distTracker.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(45f, 22f), "DIST:", font, 12, FontStyle.Bold, new Color(0.6f, 0.78f, 0.88f, 0.85f), TextAnchor.MiddleLeft);
        hud.distanceTrackerText = CreateUIText("DistanceValue", distTracker.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(115f, 24f), "00,000 m", font, 14, FontStyle.Bold, borderCyan, TextAnchor.MiddleRight);

        // Box 4: Combo Radial Dial (Circular Golden Gauge with 360 Arc Decay) positioned directly under right side of Telemetry box
        GameObject comboDialObj = new GameObject("ComboRadialDial");
        comboDialObj.transform.SetParent(trCluster.transform, false);
        RectTransform comboRt = comboDialObj.AddComponent<RectTransform>();
        comboRt.anchorMin = new Vector2(1f, 1f);
        comboRt.anchorMax = new Vector2(1f, 1f);
        comboRt.pivot = new Vector2(1f, 1f);
        comboRt.anchoredPosition = new Vector2(0f, -200f);
        comboRt.sizeDelta = new Vector2(118f, 118f);
        hud.comboBadgeRoot = comboRt;

        AspectRatioFitter arf = comboDialObj.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        arf.aspectRatio = 1.0f;

        GameObject dialFrame = CreateUIImage("DialFrame", comboDialObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white, comboDialSprite);
        dialFrame.GetComponent<Image>().preserveAspect = true;
        RectTransform dialFrameRt = dialFrame.GetComponent<RectTransform>();
        dialFrameRt.offsetMin = Vector2.zero;
        dialFrameRt.offsetMax = Vector2.zero;

        GameObject dialFill = CreateUIImage("DecayFill", comboDialObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, goldComboColor, comboFillSprite);
        RectTransform dialFillRt = dialFill.GetComponent<RectTransform>();
        dialFillRt.offsetMin = Vector2.zero;
        dialFillRt.offsetMax = Vector2.zero;
        Image decayImg = dialFill.GetComponent<Image>();
        decayImg.type = Image.Type.Filled;
        decayImg.fillMethod = Image.FillMethod.Radial360;
        decayImg.fillOrigin = (int)Image.Origin360.Top;
        decayImg.fillClockwise = true;
        decayImg.fillAmount = 1f;
        hud.comboDecayRadial = decayImg;

        CreateUIText("ComboTag", comboDialObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(90f, 24f), "COMBO", font, 13, FontStyle.Bold, goldComboColor, TextAnchor.MiddleCenter);
        hud.comboMultiplierText = CreateUIText("ComboMultiplier", comboDialObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(90f, 38f), "x3", font, 30, FontStyle.Bold, new Color(1f, 0.95f, 0.45f, 1f), TextAnchor.MiddleCenter);

        // -------------------------------------------------------------
        // CỤM 4: BOTTOM-LEFT (RADIO COMMS TACTICAL BOX) - TẮT MẶC ĐỊNH ĐỂ GIỮ VIEWPORT TINH GỌN CHUẨN MẪU
        // -------------------------------------------------------------
        GameObject blCluster = CreateUIPanel("Cluster_BottomLeft_Comms", hudRoot.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(35f, 35f), new Vector2(460f, 125f), bgPanelColor, frameSprite);
        hud.clusterBottomLeftRoot = blCluster.GetComponent<RectTransform>();

        CanvasGroup commsCg = blCluster.AddComponent<CanvasGroup>();
        commsCg.alpha = 0f;

        RadioCommsHUD radio = blCluster.AddComponent<RadioCommsHUD>();
        radio.commsCanvasGroup = commsCg;
        radio.commsRoot = hud.clusterBottomLeftRoot;
        hud.radioComms = radio;

        // Avatar Box (Pilot Hologram Portrait)
        GameObject avatarObj = CreateUIImage("AvatarBox", blCluster.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(90f, 95f), Color.white, pilotSprite);
        radio.avatarImage = avatarObj.GetComponent<Image>();

        // Speaker Title & Message Box
        radio.speakerText = CreateUIText("SpeakerText", blCluster.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(116f, -14f), new Vector2(-20f, 22f), "[WING COMMAND - PEPPY]", font, 13, FontStyle.Bold, amberColor, TextAnchor.MiddleLeft);
        radio.messageText = CreateUIText("MessageText", blCluster.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(116f, 16f), new Vector2(-20f, -44f), "Plasma weapons online. Good hunting, Arwing team!", font, 12, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        radio.messageText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // Signal Wave bar
        GameObject waveObj = CreateUIImage("SignalWave", blCluster.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(116f, 8f), new Vector2(-20f, 3f), borderCyan);
        radio.signalWaveImage = waveObj.GetComponent<Image>();

        // TẮT MẶC ĐỊNH cụm Comms để không xuất hiện HUD thừa trên màn hình
        blCluster.SetActive(false);

        // -------------------------------------------------------------
        // CỤM 5: BOTTOM-RIGHT (FLIGHT TELEMETRY & WEAPON TIER) - TẮT MẶC ĐỊNH ĐỂ GIỮ VIEWPORT TINH GỌN CHUẨN MẪU
        // -------------------------------------------------------------
        GameObject brCluster = CreateUIPanel("Cluster_BottomRight", hudRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-35f, 35f), new Vector2(340f, 110f), bgPanelColor, frameSprite);
        hud.clusterBottomRightRoot = brCluster.GetComponent<RectTransform>();

        // Weapon Tier (Icon + Text)
        if (laserIconSprite != null)
        {
            CreateUIImage("LaserIcon", brCluster.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(22f, 22f), Color.white, laserIconSprite);
        }
        hud.weaponTierText = CreateUIText("WeaponTier", brCluster.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(46f, -14f), new Vector2(-20f, 22f), "TWIN LASER MK-II", font, 13, FontStyle.Bold, borderCyan, TextAnchor.MiddleLeft);

        // Speedometer (Gauge Arc + Value)
        CreateUIText("SpeedLabel", brCluster.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(65f, 24f), "SPEED", font, 11, FontStyle.Bold, borderCyan, TextAnchor.MiddleLeft);
        hud.speedometerValueText = CreateUIText("SpeedValue", brCluster.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(120f, 32f), "140 m/s", font, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
        GameObject speedGaugeObj = CreateUIProgressBar("SpeedGauge", brCluster.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(18f, 52f), new Vector2(-36f, 10f), borderCyan, null, barFillSprite);
        hud.speedometerGaugeFill = speedGaugeObj.transform.Find("Fill").GetComponent<Image>();

        // TẮT MẶC ĐỊNH cụm Flight Telemetry để không xuất hiện HUD thừa trên màn hình
        brCluster.SetActive(false);

        // -------------------------------------------------------------
        // CỤM 6: TOP-CENTER (CLIMAX BOSS HEALTH BAR & PHASE SUBTITLE)
        // -------------------------------------------------------------
        GameObject bossCluster = CreateUIPanel("Cluster_Boss", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(540f, 74f), bgPanelColor, frameSprite);
        hud.clusterBossRoot = bossCluster.GetComponent<RectTransform>();

        // Boss Name
        hud.bossNameText = CreateUIText("BossName", bossCluster.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(-24f, 16f), "FEDERATION CORVETTE F3 // CRIMSON TITAN", font, 12, FontStyle.Bold, amberColor, TextAnchor.MiddleCenter);

        // 1. Boss Shield Bar (Tầng trên)
        GameObject bossShieldObj = new GameObject("ShieldBar");
        bossShieldObj.transform.SetParent(bossCluster.transform, false);
        RectTransform sRt = bossShieldObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0.5f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = new Vector2(0f, 6f);
        sRt.sizeDelta = new Vector2(-28f, 9f);

        Image sTrack = bossShieldObj.AddComponent<Image>();
        sTrack.color = new Color(0.04f, 0.10f, 0.16f, 0.95f);

        GameObject sGhost = new GameObject("GhostFill");
        sGhost.transform.SetParent(bossShieldObj.transform, false);
        RectTransform sGhostRt = sGhost.AddComponent<RectTransform>();
        sGhostRt.anchorMin = Vector2.zero;
        sGhostRt.anchorMax = Vector2.one;
        sGhostRt.offsetMin = Vector2.zero;
        sGhostRt.offsetMax = Vector2.zero;
        hud.bossShieldGhostBarFill = sGhost.AddComponent<Image>();
        hud.bossShieldGhostBarFill.sprite = capsuleFillSprite;
        hud.bossShieldGhostBarFill.type = Image.Type.Filled;
        hud.bossShieldGhostBarFill.fillMethod = Image.FillMethod.Horizontal;
        hud.bossShieldGhostBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.bossShieldGhostBarFill.fillAmount = 1f;
        hud.bossShieldGhostBarFill.color = new Color(0.2f, 0.85f, 1f, 0.55f);

        GameObject sFill = new GameObject("Fill");
        sFill.transform.SetParent(bossShieldObj.transform, false);
        RectTransform sFillRt = sFill.AddComponent<RectTransform>();
        sFillRt.anchorMin = Vector2.zero;
        sFillRt.anchorMax = Vector2.one;
        sFillRt.offsetMin = Vector2.zero;
        sFillRt.offsetMax = Vector2.zero;
        hud.bossShieldBarFill = sFill.AddComponent<Image>();
        hud.bossShieldBarFill.sprite = capsuleFillSprite;
        hud.bossShieldBarFill.type = Image.Type.Filled;
        hud.bossShieldBarFill.fillMethod = Image.FillMethod.Horizontal;
        hud.bossShieldBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.bossShieldBarFill.fillAmount = 1f;
        hud.bossShieldBarFill.color = borderCyan;

        // 2. Boss Health Bar (Tầng dưới)
        GameObject bossBarObj = new GameObject("HealthBar");
        bossBarObj.transform.SetParent(bossCluster.transform, false);
        RectTransform barRt = bossBarObj.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0.5f);
        barRt.anchorMax = new Vector2(1f, 0.5f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.anchoredPosition = new Vector2(0f, -7f);
        barRt.sizeDelta = new Vector2(-28f, 11f);

        Image trackImg = bossBarObj.AddComponent<Image>();
        trackImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        GameObject ghostObj = new GameObject("GhostFill");
        ghostObj.transform.SetParent(bossBarObj.transform, false);
        RectTransform ghostRt = ghostObj.AddComponent<RectTransform>();
        ghostRt.anchorMin = Vector2.zero;
        ghostRt.anchorMax = Vector2.one;
        ghostRt.offsetMin = Vector2.zero;
        ghostRt.offsetMax = Vector2.zero;
        hud.bossGhostBarFill = ghostObj.AddComponent<Image>();
        hud.bossGhostBarFill.sprite = capsuleFillSprite;
        hud.bossGhostBarFill.type = Image.Type.Filled;
        hud.bossGhostBarFill.fillMethod = Image.FillMethod.Horizontal;
        hud.bossGhostBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.bossGhostBarFill.fillAmount = 1f;
        hud.bossGhostBarFill.color = new Color(1f, 0.85f, 0.35f, 0.7f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bossBarObj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        hud.bossHealthBarFill = fillObj.AddComponent<Image>();
        hud.bossHealthBarFill.sprite = capsuleFillSprite;
        hud.bossHealthBarFill.type = Image.Type.Filled;
        hud.bossHealthBarFill.fillMethod = Image.FillMethod.Horizontal;
        hud.bossHealthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.bossHealthBarFill.fillAmount = 1f;
        hud.bossHealthBarFill.color = new Color(1f, 0.22f, 0.15f, 1f);

        // 3. Phase Text
        hud.bossPhaseText = CreateUIText("PhaseText", bossCluster.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(-24f, 14f), "PHASE 1: DORSAL SHIELD ONLINE", font, 10, FontStyle.Normal, new Color(0.85f, 0.88f, 0.95f, 0.9f), TextAnchor.MiddleCenter);

        bossCluster.SetActive(false);

        // -------------------------------------------------------------
        // CỤM GRAZE FLOATING COMBAT TEXT (0 GC ALLOC OBJECT POOL)
        // -------------------------------------------------------------
        GameObject grazeContainer = new GameObject("GrazeFloatingContainer");
        grazeContainer.transform.SetParent(hudRoot.transform, false);
        RectTransform grazeRt = grazeContainer.AddComponent<RectTransform>();
        grazeRt.anchorMin = new Vector2(0.5f, 0.5f);
        grazeRt.anchorMax = new Vector2(0.5f, 0.5f);
        grazeRt.pivot = new Vector2(0.5f, 0.5f);
        grazeRt.anchoredPosition = Vector2.zero;
        grazeRt.sizeDelta = new Vector2(400f, 300f);
        hud.grazeFloatingRoot = grazeRt;

        hud.grazePopupTexts = new Text[3];
        for (int gp = 0; gp < 3; gp++)
        {
            GameObject gTextObj = new GameObject($"GrazePopup_{gp}");
            gTextObj.transform.SetParent(grazeContainer.transform, false);
            RectTransform gtRt = gTextObj.AddComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0.5f, 0.5f);
            gtRt.anchorMax = new Vector2(0.5f, 0.5f);
            gtRt.pivot = new Vector2(0.5f, 0.5f);
            gtRt.sizeDelta = new Vector2(250f, 40f);

            Text gTxt = gTextObj.AddComponent<Text>();
            gTxt.font = font;
            gTxt.fontSize = 24;
            gTxt.fontStyle = FontStyle.Bold;
            gTxt.alignment = TextAnchor.MiddleCenter;
            gTxt.color = borderCyan;
            gTxt.text = "+500 [GRAZE!]";

            Outline gOutline = gTextObj.AddComponent<Outline>();
            gOutline.effectColor = new Color(0f, 0.35f, 0.55f, 0.85f);
            gOutline.effectDistance = new Vector2(1.5f, -1.5f);

            gTextObj.SetActive(false);
            hud.grazePopupTexts[gp] = gTxt;
        }

        return hudRoot;
    }

    private static GameObject CreateUIPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color bgColor, Sprite sprite = null, Image.Type imageType = Image.Type.Sliced)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = panel.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = imageType;
            img.color = Color.white;
        }
        else
        {
            img.color = bgColor;
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.95f, 1f, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        return panel;
    }


    private static GameObject CreateUIImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = color;
        }
        else
        {
            img.color = color;
        }
        return obj;
    }

    private static GameObject CreateUIProgressBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color fillColor, Sprite bgSprite = null, Sprite fillSprite = null)
    {
        GameObject bgObj = new GameObject(name);
        bgObj.transform.SetParent(parent, false);

        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = anchorMin;
        bgRt.anchorMax = anchorMax;
        bgRt.pivot = pivot;
        bgRt.anchoredPosition = pos;
        bgRt.sizeDelta = size;

        Image bgImg = bgObj.AddComponent<Image>();
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
        }
        else
        {
            bgImg.color = new Color(0.08f, 0.15f, 0.2f, 0.85f);
        }

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);

        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        Image fillImg = fillObj.AddComponent<Image>();
        if (fillSprite != null)
        {
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
            fillImg.color = fillColor;
        }
        else
        {
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
        }

        return bgObj;
    }

    private static Text CreateUIText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, string text, Font font, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment, bool addShadow = true)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Text t = obj.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.fontStyle = fontStyle;
        t.color = color;
        t.alignment = alignment;
        t.text = text;

        if (addShadow)
        {
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
            shadow.useGraphicAlpha = true;
        }

        return t;
    }
    #endregion

    #region DDA Escalation Controls (ON / OFF - UNDO)
    [MenuItem("Tools/Virtual Rail/49. DDA Escalation: Enable Full Hazard, Bullet & Speed Scaling (ON)")]
    public static void EnableFullDDAEscalation()
    {
        var endlessDirectors = Object.FindObjectsByType<VirtualRailEndlessDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < endlessDirectors.Length; i++)
        {
            endlessDirectors[i].ScaleRailSpeed = true;
            EditorUtility.SetDirty(endlessDirectors[i]);
        }

        var asteroidSpawners = Object.FindObjectsByType<VirtualRailAsteroidSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < asteroidSpawners.Length; i++)
        {
            asteroidSpawners[i].EnableDDAScaling = true;
            EditorUtility.SetDirty(asteroidSpawners[i]);
        }

        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT DDA ESCALATION ĐẦY ĐỦ (ON):</b></color> Tốc độ đạn quái, mật độ thiên thạch và vận tốc ray tự động tăng tiến theo thời gian sống sót!");
    }

    [MenuItem("Tools/Virtual Rail/50. DDA Escalation: Revert to Flat Baseline (OFF - UNDO)")]
    public static void RevertFullDDAEscalation()
    {
        var endlessDirectors = Object.FindObjectsByType<VirtualRailEndlessDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < endlessDirectors.Length; i++)
        {
            endlessDirectors[i].ScaleRailSpeed = false;
            EditorUtility.SetDirty(endlessDirectors[i]);
        }

        var asteroidSpawners = Object.FindObjectsByType<VirtualRailAsteroidSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < asteroidSpawners.Length; i++)
        {
            asteroidSpawners[i].EnableDDAScaling = false;
            asteroidSpawners[i].ResetThreatLevelScaling();
            EditorUtility.SetDirty(asteroidSpawners[i]);
        }

        var anchor = Object.FindAnyObjectByType<VirtualRailAnchor>();
        if (anchor != null)
        {
            anchor.SetSpeedOverride(70f);
        }

        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN DDA VỀ FLAT BASELINE (OFF - UNDO):</b></color> Tốc độ ray và mật độ thiên thạch giữ cố định ở mức cơ sở ban đầu.");
    }

    [MenuItem("Tools/Virtual Rail/51. Evasion Controls: Decouple from Z-Speed (Independent Speeds - ON)")]
    public static void EnableIndependentEvasionSpeeds()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useGoldenRatioMotion = false;
            config.shipSpeedX = 60f;
            config.shipSpeedY = 42f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            RebuildPlayerRigPrefab();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT TỐC ĐỘ NÉ ĐỘC LẬP (ON):</b></color> Tốc độ né ngang (60 m/s) và né dọc (42 m/s) hoàn toàn độc lập với tốc độ tiến Z!");
        }
    }

    [MenuItem("Tools/Virtual Rail/52. Evasion Controls: Revert to Golden Ratio Coupled (OFF - Legacy)")]
    public static void RevertToGoldenRatioCoupled()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useGoldenRatioMotion = true;
            config.goldenRatioX = 0.50f;
            config.goldenRatioY = 0.35f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            RebuildPlayerRigPrefab();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỀ ĐỘNG HỌC TỶ LỆ VÀNG (OFF - UNDO):</b></color> Tốc độ né trở lại gắn liền với tốc độ tiến Z (Vx = 50% Vz, Vy = 35% Vz).");
        }
    }

    [MenuItem("Tools/Virtual Rail/53. Boss Corvette: Synchronize Phase Logic & Visual Telegraphs (ON)")]
    public static void EnableBossPhaseVisualSync()
    {
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT ĐỒNG BỘ LOGIC & THỊ GIÁC BOSS CORVETTE F3 (ON):</b></color>\n" +
                  "1. Triệt tiêu rò rỉ _isChargingTrench khi chuyển pha.\n" +
                  "2. HUD đồng bộ theo đúng CurrentPhase khi Boss bị Choáng (Stun).\n" +
                  "3. Dải rãnh nhiệt lò phản ứng reset sạch _chargeProgress khi vào Pha 3 Meltdown.\n" +
                  "4. Shader hiển thị xung điện Xanh Neon (Electric Cyan Flare) cảnh báo Hyper Laser cực kỳ nổi bật.\n" +
                  "5. Thân tàu dâng cao -22m -> -18m qua MoveTowards 3.5 m/s, triệt tiêu hoàn toàn giật nhảy 1-frame!");
    }

    [MenuItem("Tools/Virtual Rail/54. Boss Corvette: Revert Phase Visual Sync (OFF - Legacy)")]
    public static void RevertBossPhaseVisualSync()
    {
        Debug.Log("<color=yellow><b>[VirtualRail] THÔNG BÁO HOÀN NGUYÊN (OFF - UNDO):</b></color> Để hoàn nguyên mã nguồn Boss F3 về trạng thái cũ, bạn có thể checkout nhánh backup/fix-boss-phase-conflicts.");
    }

    [MenuItem("Tools/Virtual Rail/55. Endless Boss Incursions & DDA Scaling (ON)")]
    public static void EnableEndlessBossIncursions()
    {
        var directors = Object.FindObjectsByType<VirtualRailEndlessDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss_F3_Corvette.prefab");

        for (int i = 0; i < directors.Length; i++)
        {
            directors[i].EnableBossIncursions = true;
            directors[i].BossIncursionIntervalWaves = 8;
            if (bossPrefab != null)
            {
                directors[i].BossPrefab = bossPrefab;
            }
            EditorUtility.SetDirty(directors[i]);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT ENDLESS BOSS INCURSIONS & DDA SCALING (ON):</b></color>\n" +
                  "1. Tự động xuất kích Boss F3 Corvette mỗi 8 waves hoặc mỗi 15,000m Sector Warp.\n" +
                  "2. Boss DDA Scaling: HP tăng +20% mỗi lần chạm trán, Khiên Hex-Shield tăng tương ứng.\n" +
                  "3. Cooldown vũ khí Flak/VLS/Laser thu ngắn theo Threat Level (giảm tối đa 40% trần hãm 0.60x).\n" +
                  "4. Kháng choáng: Thời gian choáng buồng lái giảm từ 4.0s về 2.0s; Lò phản ứng giảm từ 2.0s về 1.0s.\n" +
                  "5. Tiêu diệt Boss: Thưởng +50,000 pts, Sector Warp +10,000 pts, Threat Level +1, Breathing Room 5.0s.");
    }

    [MenuItem("Tools/Virtual Rail/56. Endless Boss Incursions: Disable Incursions (OFF - Legacy)")]
    public static void DisableEndlessBossIncursions()
    {
        var directors = Object.FindObjectsByType<VirtualRailEndlessDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < directors.Length; i++)
        {
            directors[i].EnableBossIncursions = false;
            EditorUtility.SetDirty(directors[i]);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ TẮT ENDLESS BOSS INCURSIONS (OFF - UNDO):</b></color> Chế độ Endless chỉ xuất kích các phi đội chiến đấu thông thường.");
    }

    [MenuItem("Tools/Virtual Rail/57. Procedural Spline Rail: Hermite Spline Chunker (ON)")]
    [MenuItem("Tools/Virtual Rail/Spline System/1. Enable Procedural Spline Rail (ON)")]
    public static void EnableProceduralSplineRail()
    {
        var anchors = Object.FindObjectsByType<VirtualRailAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < anchors.Length; i++)
        {
            anchors[i].UseProceduralSpline = true;
            var chunker = anchors[i].GetComponent<VirtualRailSplineChunker>();
            if (chunker == null)
            {
                chunker = anchors[i].gameObject.AddComponent<VirtualRailSplineChunker>();
            }
            chunker.Initialize(anchors[i].transform.position, anchors[i].transform.forward);
            EditorUtility.SetDirty(anchors[i]);
            EditorUtility.SetDirty(chunker);
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (rigPrefab != null)
        {
            var rigAnchor = rigPrefab.GetComponent<VirtualRailAnchor>();
            if (rigAnchor != null)
            {
                rigAnchor.UseProceduralSpline = true;
                if (rigPrefab.GetComponent<VirtualRailSplineChunker>() == null)
                {
                    rigPrefab.AddComponent<VirtualRailSplineChunker>();
                }
                EditorUtility.SetDirty(rigPrefab);
                PrefabUtility.SavePrefabAsset(rigPrefab);
            }
        }

        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enableProceduralSpline = true;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT PROCEDURAL SPLINE RAIL (HERMITE SPLINE CHUNKER - ON):</b></color>\n" +
                  "1. Sinh đường ray liên tục theo từng đoạn Chunk L = 500m.\n" +
                  "2. Duy trì Look-ahead Ring Buffer 3 Chunks đồng thời: [0] Behind, [1] Current, [2] Ahead.\n" +
                  "3. Đảm bảo tính trơn hình học C2 tuyệt đối tại giao diện tiếp nối (Position, Tangent, Curvature).\n" +
                  "4. Bán kính cong an toàn R_min >= 300m (kappa <= 0.0033 m^-1) triệt tiêu hoàn toàn giật cục.\n" +
                  "5. Tự động nghiêng thân ray khí động học (Aerodynamic Track Banking phi_bank trong [-35 deg, +35 deg]).");
    }

    [MenuItem("Tools/Virtual Rail/58. Procedural Spline Rail: Revert to Straight Rail (OFF - Legacy)")]
    [MenuItem("Tools/Virtual Rail/Spline System/2. Revert to Straight Rail (OFF - Legacy)")]
    public static void DisableProceduralSplineRail()
    {
        var anchors = Object.FindObjectsByType<VirtualRailAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < anchors.Length; i++)
        {
            anchors[i].UseProceduralSpline = false;
            EditorUtility.SetDirty(anchors[i]);
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (rigPrefab != null)
        {
            var rigAnchor = rigPrefab.GetComponent<VirtualRailAnchor>();
            if (rigAnchor != null)
            {
                rigAnchor.UseProceduralSpline = false;
                EditorUtility.SetDirty(rigPrefab);
                PrefabUtility.SavePrefabAsset(rigPrefab);
            }
        }

        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enableProceduralSpline = false;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỀ ĐƯỜNG RAY THẲNG CỔ ĐIỂN (OFF - LEGACY):</b></color> Phi thuyền chuyển động thẳng tịnh tiến theo trục Z như trước.");
    }

    [MenuItem("Tools/Virtual Rail/59. Golden Path & World Origin Shifting (ON)")]
    [MenuItem("Tools/Virtual Rail/Solvability & Shifting/1. Enable Golden Path & Origin Shifting (ON)")]
    public static void EnableGoldenPathAndOriginShifting()
    {
        var anchors = Object.FindObjectsByType<VirtualRailAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i].GetComponent<VirtualRailGoldenPathVerifier>() == null)
            {
                anchors[i].gameObject.AddComponent<VirtualRailGoldenPathVerifier>();
            }
            if (anchors[i].GetComponent<VirtualRailOriginShifter>() == null)
            {
                anchors[i].gameObject.AddComponent<VirtualRailOriginShifter>();
            }
            EditorUtility.SetDirty(anchors[i]);
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (rigPrefab != null)
        {
            if (rigPrefab.GetComponent<VirtualRailGoldenPathVerifier>() == null)
            {
                rigPrefab.AddComponent<VirtualRailGoldenPathVerifier>();
            }
            if (rigPrefab.GetComponent<VirtualRailOriginShifter>() == null)
            {
                rigPrefab.AddComponent<VirtualRailOriginShifter>();
            }
            EditorUtility.SetDirty(rigPrefab);
            PrefabUtility.SavePrefabAsset(rigPrefab);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ KÍCH HOẠT GOLDEN PATH VERIFIER & WORLD ORIGIN SHIFTER (ON):</b></color>\n" +
                  "1. Bảo chứng khả thi hình học: Quét trước hành lang an toàn 15m x 5.25m dọc theo Chunk mới sinh.\n" +
                  "2. Chống bức tường chết: Tự động phát hiện và giải phóng (Carve) luồng thoát hiểm góc 26.5 độ.\n" +
                  "3. Tái định vị thế giới: Dịch chuyển toàn bộ thực thể về (0,0,0) mỗi chu kỳ 4,000m triệt tiêu rung số thực 32-bit.\n" +
                  "4. Quãng đường tích lũy 64-bit double độc lập, bảo toàn 100% kỷ lục Telemetry HUD.");
    }

    [MenuItem("Tools/Virtual Rail/60. Revert Solvability & Origin Shifting (OFF - Legacy)")]
    [MenuItem("Tools/Virtual Rail/Solvability & Shifting/2. Revert Solvability & Origin Shifting (OFF - Legacy)")]
    public static void DisableGoldenPathAndOriginShifting()
    {
        var verifiers = Object.FindObjectsByType<VirtualRailGoldenPathVerifier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < verifiers.Length; i++)
        {
            Object.DestroyImmediate(verifiers[i]);
        }

        var shifters = Object.FindObjectsByType<VirtualRailOriginShifter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < shifters.Length; i++)
        {
            Object.DestroyImmediate(shifters[i]);
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIRTUAL_RAIL_PREFAB);
        if (rigPrefab != null)
        {
            var pVerifier = rigPrefab.GetComponent<VirtualRailGoldenPathVerifier>();
            if (pVerifier != null) Object.DestroyImmediate(pVerifier, true);
            var pShifter = rigPrefab.GetComponent<VirtualRailOriginShifter>();
            if (pShifter != null) Object.DestroyImmediate(pShifter, true);
            EditorUtility.SetDirty(rigPrefab);
            PrefabUtility.SavePrefabAsset(rigPrefab);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN GOLDEN PATH & WORLD ORIGIN SHIFTER (OFF - LEGACY):</b></color> Trở về cơ chế spawn và tọa độ thế giới nguyên thủy.");
    }
    #endregion

    #region Projectile Rail-Frame Trail Compensation (Direction A)
    [MenuItem("Tools/Virtual Rail/Projectiles/1. Enable Rail-Frame Trail Compensation (ON)")]
    public static void EnableRailFrameTrailCompensation()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enableRailFrameTrailCompensation = true;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ BẬT BÙ TRỪ ĐỘ TRÔI ĐƯỜNG RAY CHO VỆT ĐUÔI ĐẠN (ON):</b></color> TrailRenderer và ParticleSystem sẽ rải vết xuôi chiều theo góc nhìn buồng lái Arwing.");
    }

    [MenuItem("Tools/Virtual Rail/Projectiles/2. Disable Rail-Frame Trail Compensation (OFF - Legacy)")]
    public static void DisableRailFrameTrailCompensation()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enableRailFrameTrailCompensation = false;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỆT ĐUÔI ĐẠN VỀ WORLD SPACE NGUYÊN BẢN (OFF - LEGACY):</b></color> Tắt bù trừ dịch chuyển đỉnh trail.");
    }
    #endregion

    #region Player Health & Auto-Regen Toggles (Task: White Orb Only)
    [MenuItem("Tools/Virtual Rail/Player/1. Disable Auto Health Regen - White Orb Only (ON - Spec)")]
    public static void DisablePlayerAutoRegenWhiteOrbOnly()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enablePlayerAutoRegeneration = false;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ LOẠI BỎ CƠ CHẾ TỰ HỒI MÁU (ON - SPEC):</b></color> Người chơi chỉ có thể hồi máu khi thu thập Orb Trắng (White Orb).");
    }

    [MenuItem("Tools/Virtual Rail/Player/2. Enable Auto Health Regen (OFF - Legacy)")]
    public static void EnablePlayerAutoRegenLegacy()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enablePlayerAutoRegeneration = true;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ BẬT LẠI TỰ HỒI MÁU THEO THỜI GIAN (OFF - LEGACY):</b></color> Trở lại cơ chế tự hồi 1 HP mỗi 2 giây.");
    }
    #endregion

    #region Mobile Touch Controls (Minimalist White Rings)
    private static VirtualTouchButton CreateEditorTouchButton(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        Sprite ringSprite,
        Sprite iconSprite,
        Vector2 iconSize,
        bool flipIconX = false,
        bool hasChargeArc = false,
        Sprite arcSprite = null,
        bool hasBadge = false,
        Font badgeFont = null)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = anchoredPos;
        btnRt.sizeDelta = size;

        Image ringImg = btnObj.AddComponent<Image>();
        ringImg.sprite = ringSprite;
        ringImg.color = Color.white;
        ringImg.raycastTarget = true;

        VirtualTouchButton touchBtn = btnObj.AddComponent<VirtualTouchButton>();
        touchBtn.ringImage = ringImg;

        if (iconSprite != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = iconSize;
            if (flipIconX)
            {
                iconRt.localScale = new Vector3(-1f, 1f, 1f);
            }

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            touchBtn.iconImage = iconImg;
        }

        if (hasChargeArc && arcSprite != null)
        {
            GameObject arcObj = new GameObject("ChargeArc");
            arcObj.transform.SetParent(btnObj.transform, false);

            RectTransform arcRt = arcObj.AddComponent<RectTransform>();
            arcRt.anchorMin = new Vector2(0.5f, 0.5f);
            arcRt.anchorMax = new Vector2(0.5f, 0.5f);
            arcRt.anchoredPosition = Vector2.zero;
            arcRt.sizeDelta = size * 1.14f;

            Image arcImg = arcObj.AddComponent<Image>();
            arcImg.sprite = arcSprite;
            arcImg.type = Image.Type.Filled;
            arcImg.fillMethod = Image.FillMethod.Radial360;
            arcImg.fillOrigin = (int)Image.Origin360.Top;
            arcImg.fillClockwise = true;
            arcImg.fillAmount = 0f;
            arcImg.color = new Color(0.2f, 1f, 0.45f, 0.95f);
            arcImg.raycastTarget = false;

            touchBtn.chargeArcImage = arcImg;
            arcObj.SetActive(false);
        }

        if (hasBadge)
        {
            GameObject badgeObj = new GameObject("BadgeText");
            badgeObj.transform.SetParent(btnObj.transform, false);

            RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(0f, -size.y * 0.32f);
            badgeRt.sizeDelta = new Vector2(36f, 18f);

            Text badge = badgeObj.AddComponent<Text>();
            badge.font = badgeFont != null ? badgeFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            badge.fontSize = 12;
            badge.fontStyle = FontStyle.Bold;
            badge.alignment = TextAnchor.MiddleCenter;
            badge.color = new Color(1f, 0.88f, 0.2f, 1f);
            badge.raycastTarget = false;
            badge.text = "x3";

            Outline outline = badgeObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            touchBtn.badgeText = badge;
        }

        return touchBtn;
    }

    [MenuItem("Tools/Virtual Rail/Touch Controls/1. Enable Minimalist White Ring Controls (ON)")]
    public static void EnableMinimalistWhiteTouchControls()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enableMobileUI = true;
            cfg.forceShowMobileUI = true;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        var reader = Object.FindAnyObjectByType<VirtualRailInputReader>();
        if (reader != null)
        {
            reader.EnsureMobileUI();
        }

        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ KÍCH HOẠT HỆ THỐNG NÚT CẢM ỨNG VÒNG TRẮNG TỐI GIẢN (ON):</b></color> Đầy đủ cụm 5 nút hành động (Laser, Bom, Boost, Brake, Barrel Roll) & Cần gạt trái.");
    }

    [MenuItem("Tools/Virtual Rail/Touch Controls/2. Disable Mobile Touch Controls on PC (OFF - Legacy)")]
    public static void DisableMobileTouchControlsOnPC()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.forceShowMobileUI = false;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ TẮT HIỂN THỊ NÚT CẢM ỨNG TRÊN PC (OFF - LEGACY):</b></color> Mobile UI chỉ hiển thị khi chạy trên thiết bị di động.");
    }
    #endregion

    #region Charge Shot Audio Balancing Toggles (Task: Balance Charge Shot Audio)
    [MenuItem("Tools/Virtual Rail/Audio/1. Balance Charge Shot Audio (ON - Spec)")]
    public static void EnableBalancedChargeShotAudio()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.chargeHumVolumeMin = 0.28f;
            cfg.chargeHumVolumeMax = 0.45f;
            cfg.chargeFireVolume = 0.60f;
            cfg.chargeLockOnVolume = 0.55f;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ CÂN BẰNG ÂM LƯỢNG SẠC & PHÓNG ĐẠN (ON - SPEC):</b></color> Hum: 0.28-0.45, Fire: 0.60, Lock-on: 0.55.");
    }

    [MenuItem("Tools/Virtual Rail/Audio/2. Revert Charge Shot Audio to Loud (OFF - Legacy)")]
    public static void RevertChargeShotAudioToLegacyLoud()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.chargeHumVolumeMin = 0.75f;
            cfg.chargeHumVolumeMax = 0.95f;
            cfg.chargeFireVolume = 1.00f;
            cfg.chargeLockOnVolume = 0.95f;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN ÂM LƯỢNG SẠC ĐẠN CŨ (OFF - LEGACY):</b></color> Trở lại mức âm lượng lớn nguyên bản.");
    }
    #endregion

    #region APK Enemy Projectile Pool Toggles (Task: Fix Enemy Shooting on APK)
    private const string ENEMY_PROJECTILE_RESOURCES_PATH = "Prefabs/EnemyProjectile_PlasmaOrb";
    private const string ENEMY_IMPACT_RESOURCES_PATH = "Prefabs/CFX4_Hit_B_Orange";

    [MenuItem("Tools/Virtual Rail/Projectiles/3. Ensure APK Projectile Pool Resources (ON - Spec)")]
    public static void EnsureAPKProjectilePoolResources()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        GameObject projPrefab = Resources.Load<GameObject>(ENEMY_PROJECTILE_RESOURCES_PATH);
        if (projPrefab == null)
        {
            projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyProjectile_PlasmaOrb.prefab");
        }

        GameObject hitPrefab = Resources.Load<GameObject>(ENEMY_IMPACT_RESOURCES_PATH);
        if (hitPrefab == null)
        {
            hitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Hit B (Orange).prefab");
        }

        if (cfg != null)
        {
            cfg.useEnhancedPlasmaProjectiles = true;
            if (projPrefab != null) cfg.enemyProjectilePrefab = projPrefab;
            if (hitPrefab != null) cfg.enemyImpactVFXPrefab = hitPrefab;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        EnemyProjectilePool pool = Object.FindAnyObjectByType<EnemyProjectilePool>();
        if (pool != null)
        {
            if (projPrefab != null) pool.SetProjectilePrefab(projPrefab);
            if (hitPrefab != null) pool.SetImpactVFXPrefab(hitPrefab);
            EditorUtility.SetDirty(pool);
        }

        Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ BẢO ĐẢM TÀI NGUYÊN ĐẠN ĐỊCH CHO APK (ON - SPEC):</b></color> Prefab đạn và Impact FX đã sẵn sàng trong Resources/Prefabs và liên kết vào Config!");
    }

    [MenuItem("Tools/Virtual Rail/Projectiles/4. Revert APK Projectile Pool to Legacy (OFF - Legacy)")]
    public static void RevertAPKProjectilePoolToLegacy()
    {
        VirtualRailConfig cfg = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (cfg != null)
        {
            cfg.enemyProjectilePrefab = null;
            cfg.enemyImpactVFXPrefab = null;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        EnemyProjectilePool pool = Object.FindAnyObjectByType<EnemyProjectilePool>();
        if (pool != null)
        {
            pool.SetProjectilePrefab(null);
            pool.SetImpactVFXPrefab(null);
            EditorUtility.SetDirty(pool);
        }

        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN CẤU HÌNH ĐẠN ĐỊCH VỀ LEGACY (OFF - LEGACY):</b></color> Xóa liên kết Prefab đạn để quay về cơ chế sinh thủ tục.");
    }
    #endregion

    #region Mobile Frame Rate & Performance
    [MenuItem("Tools/Virtual Rail/Performance/1. Unlock Mobile 60-120 FPS Mode (ON - Spec)")]
    public static void UnlockMobile60FPSMode()
    {
        VirtualRail.SettingsManager mgr = Object.FindAnyObjectByType<VirtualRail.SettingsManager>();
        if (mgr != null)
        {
            mgr.SetUnlockMobileFramerate(true);
            EditorUtility.SetDirty(mgr);
        }
        PlayerPrefs.SetInt(VirtualRail.SettingsManager.PREF_UNLOCK_MOBILE_FPS, 1);
        PlayerPrefs.Save();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ MỞ KHÓA 60-120 FPS CHO MOBILE (ON - SPEC):</b></color> Đã tắt Unity vSyncCount (= 0) và thiết lập targetFrameRate >= 60 trên mobile.");
    }

    [MenuItem("Tools/Virtual Rail/Performance/2. Revert Mobile Frame Rate to Legacy 30 FPS Cap (OFF - Legacy)")]
    public static void RevertMobileFrameRateToLegacy()
    {
        VirtualRail.SettingsManager mgr = Object.FindAnyObjectByType<VirtualRail.SettingsManager>();
        if (mgr != null)
        {
            mgr.SetUnlockMobileFramerate(false);
            EditorUtility.SetDirty(mgr);
        }
        PlayerPrefs.SetInt(VirtualRail.SettingsManager.PREF_UNLOCK_MOBILE_FPS, 0);
        PlayerPrefs.Save();
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỀ MỨC KHÓA 30 FPS CỦA UNITY (OFF - LEGACY):</b></color> vSyncCount = 1, targetFrameRate = -1 (30 FPS throttle trên mobile).");
    }
    #endregion
}
