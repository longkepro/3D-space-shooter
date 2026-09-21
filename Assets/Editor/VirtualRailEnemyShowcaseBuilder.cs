using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VirtualRail;

/// <summary>
/// Công cụ thiết lập Phòng Triển Lãm & Kiểm Tra Kẻ Địch 360 độ trong Scenes/Test.unity.
/// Tuân thủ quy tắc 3 tầng bảo lưu:
/// - Tầng 1: Git checkpoint branch backup/enemy-showcase-gallery.
/// - Tầng 2: Sao lưu an toàn Scenes/Test.unity sang Assets/Scenes/Backup_Scenes/.
/// - Tầng 3: Menu Item 1-click đối xứng [55] Setup Gallery, [56] Undo Restore và [57] Return to Game.
/// </summary>
public static class VirtualRailEnemyShowcaseBuilder
{
    private const string SCENE_TEST_PATH = "Assets/Scenes/Test.unity";
    private const string SCENE_GAME_PATH = "Assets/Scenes/Game.unity";
    private const string SCENE_BACKUP_PATH = "Assets/Scenes/Backup_Scenes/Test.unity.backup";
    private const string SKYBOX_MAT_PATH = "Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat";

    [MenuItem("Tools/Virtual Rail/55. Open Enemy Showcase Gallery (ON)")]
    public static void OpenAndSetupShowcaseGallery()
    {
        // 1. Sao lưu Test.unity nếu chưa có
        if (!Directory.Exists("Assets/Scenes/Backup_Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes/Backup_Scenes");
        }

        if (File.Exists(SCENE_TEST_PATH) && !File.Exists(SCENE_BACKUP_PATH))
        {
            File.Copy(SCENE_TEST_PATH, SCENE_BACKUP_PATH, false);
            AssetDatabase.Refresh();
        }

        // 2. Mở Scene Test.unity và thiết lập
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            var scene = EditorSceneManager.OpenScene(SCENE_TEST_PATH, OpenSceneMode.Single);
            BuildShowcaseInActiveScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ THIẾT LẬP VÀ MỞ PHÒNG TRIỂN LÃM KẺ ĐỊCH TRONG SCENES/TEST.UNITY!</b></color>");
        }
    }

    [MenuItem("Tools/Virtual Rail/56. Undo Enemy Showcase Gallery (OFF - Legacy)")]
    public static void UndoShowcaseGallery()
    {
        if (!File.Exists(SCENE_BACKUP_PATH))
        {
            Debug.LogWarning("[VirtualRail] Không tìm thấy file sao lưu: " + SCENE_BACKUP_PATH);
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            File.Copy(SCENE_BACKUP_PATH, SCENE_TEST_PATH, true);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SCENE_TEST_PATH, OpenSceneMode.Single);
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) SCENES/TEST.UNITY VỀ NGUYÊN BẢN TRƯỚC TRIỂN LÃM!</b></color>");
        }
    }

    [MenuItem("Tools/Virtual Rail/57. Return to Main Game Scene (Game.unity)")]
    public static void ReturnToGameScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_GAME_PATH, OpenSceneMode.Single);
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ QUAY LẠI SCENE GAME CHÍNH: ASSETS/SCENES/GAME.UNITY!</b></color>");
        }
    }

    public static void BuildShowcaseInActiveScene()
    {
        // 1. Dọn dẹp triệt để các GameObject cũ để chống trùng lặp và chồng đè model
        var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in allRoots)
        {
            if (root.name.StartsWith("[Showcase_") || root.name.StartsWith("[Enemy_Showcase_Gallery]") || root.name.StartsWith("[Target_Hologram"))
            {
                Object.DestroyImmediate(root);
            }
        }

        // 2. Tạo đối tượng quản lý Gallery duy nhất
        GameObject galleryObj = new GameObject("[Enemy_Showcase_Gallery]");
        galleryObj.AddComponent<EnemyShowcaseGallery>();

        // 3. Cấu hình Camera chuẩn
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCam.transform.position = new Vector3(0f, 1.5f, -7.5f);
        mainCam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.015f, 0.018f, 0.035f, 1f);

        // Cấu hình Skybox vũ trụ
        Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(SKYBOX_MAT_PATH);
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
        }

        // 4. Cấu hình Ánh sáng Studio (Key Light + Fill Light)
        Light mainLight = Object.FindAnyObjectByType<Light>();
        if (mainLight == null || mainLight.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            mainLight = lightObj.AddComponent<Light>();
            mainLight.type = LightType.Directional;
        }
        mainLight.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
        mainLight.color = new Color(0.98f, 0.98f, 1f);
        mainLight.intensity = 1.35f;
        mainLight.shadows = LightShadows.Soft;

        // Tạo Fill Light xanh tím chiếu từ dưới lên tạo không khí sci-fi
        GameObject fillLightObj = GameObject.Find("SciFi_Fill_Light");
        if (fillLightObj == null)
        {
            fillLightObj = new GameObject("SciFi_Fill_Light");
        }
        Light fillLight = fillLightObj.GetComponent<Light>();
        if (fillLight == null) fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.transform.rotation = Quaternion.Euler(-30f, 150f, 0f);
        fillLight.color = new Color(0.35f, 0.2f, 0.6f);
        fillLight.intensity = 0.5f;
        fillLight.shadows = LightShadows.None;

        EditorUtility.SetDirty(galleryObj);
    }
}
