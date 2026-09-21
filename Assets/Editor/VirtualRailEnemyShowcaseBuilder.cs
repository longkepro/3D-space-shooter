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
/// - Tầng 3: Menu Item 1-click đối xứng [55] Setup & Open Gallery và [56] Undo Restore.
/// </summary>
public static class VirtualRailEnemyShowcaseBuilder
{
    private const string SCENE_TEST_PATH = "Assets/Scenes/Test.unity";
    private const string SCENE_BACKUP_PATH = "Assets/Scenes/Backup_Scenes/Test.unity.backup";
    private const string SKYBOX_MAT_PATH = "Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat";

    private static readonly string[] PREFAB_PATHS = new string[]
    {
        "Assets/Prefabs/Enemy (0) - Scout.prefab",
        "Assets/Prefabs/Enemy (1) - Flanker.prefab",
        "Assets/Prefabs/Enemy (2) - Interceptor.prefab",
        "Assets/Prefabs/Enemy (3) - Squad Leader.prefab",
        "Assets/Prefabs/Enemy (4) - Shield Bearer.prefab"
    };

    private static readonly Vector3[] DISPLAY_POSITIONS = new Vector3[]
    {
        new Vector3(-14f, 0f, 0f),
        new Vector3(-7f, 0f, 0f),
        new Vector3(0f, 0f, 0f),
        new Vector3(7f, 0f, 0f),
        new Vector3(14f, 0f, 0f)
    };

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

        // 2. Mở Scene Test.unity
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

    public static void BuildShowcaseInActiveScene()
    {
        // 1. Dọn dẹp các đối tượng showcase cũ nếu có
        GameObject oldGallery = GameObject.Find("[Enemy_Showcase_Gallery]");
        if (oldGallery != null)
        {
            Object.DestroyImmediate(oldGallery);
        }

        // Xóa các tàu hiển thị cũ trong scene
        for (int i = 0; i < 5; i++)
        {
            GameObject oldShip = GameObject.Find($"[Showcase_Ship_{i}]");
            if (oldShip != null) Object.DestroyImmediate(oldShip);
        }

        // 2. Tạo đối tượng quản lý Gallery
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
        mainCam.transform.position = new Vector3(0f, 2f, -8f);
        mainCam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        mainCam.clearFlags = CameraClearFlags.Skybox;

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
        mainLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        mainLight.color = new Color(0.95f, 0.95f, 1f);
        mainLight.intensity = 1.3f;
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

        // 5. Đặt sẵn 5 tàu ngay trong Edit Mode để xem được luôn trong Scene View mà không cần Play
        for (int i = 0; i < 5; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATHS[i]);
            if (prefab == null) continue;

            GameObject ship = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ship.name = $"[Showcase_Ship_{i}]";
            ship.transform.position = DISPLAY_POSITIONS[i];
            ship.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Hướng mũi về phía camera

            // Vô hiệu hóa logic di chuyển để tàu đứng yên trưng bày
            var movement = ship.GetComponent<EnemyMovement>();
            if (movement != null) movement.enabled = false;

            var attack = ship.GetComponent<EnemyAttack>();
            if (attack != null) attack.enabled = false;

            var rb = ship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Kích hoạt vệt đuôi động cơ tím
            var trails = ship.GetComponentsInChildren<TrailRenderer>(true);
            foreach (var t in trails)
            {
                t.emitting = true;
            }

            // Kích hoạt lồng khiên cho Shield Bearer
            if (i == 4)
            {
                Transform shield = ship.transform.Find("Shield") ?? ship.transform.Find("[Shield]");
                if (shield != null) shield.gameObject.SetActive(true);
            }
        }

        EditorUtility.SetDirty(galleryObj);
    }
}
