using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using VirtualRail;

public class VirtualRailModeToggle
{
    private const string ORIGINAL_SHIP_PREFAB = "Assets/Prefabs/Player Ship (1).prefab";
    private const string VIRTUAL_RAIL_PREFAB = "Assets/Prefabs/VirtualRail_PlayerRig.prefab";
    private const string CONFIG_PATH = "Assets/Scripts/VirtualRail/DefaultVirtualRailConfig.asset";
    private const string MISSILE_PREFAB = "Assets/FreeSciFi/Prefabs/MinesBombs/Missile.prefab";

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

    [MenuItem("Tools/Virtual Rail/3. Controls: Direction A (Free-Roam Velocity - No Auto-Centering)")]
    public static void EnableDirectionA()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useDirectAnalogMapping = false;
            config.reticleSpeedX = 140f;
            config.reticleSpeedY = 85f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT HƯỚNG A (FREE-ROAM VELOCITY):</b></color> Nhả phím tàu và tâm ngắm đứng yên tại chỗ, không bao giờ bị kéo về trung tâm!");
        }
    }

    [MenuItem("Tools/Virtual Rail/4. Controls: Undo to Direct Analog (Auto-Centering)")]
    public static void DisableDirectionA()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useDirectAnalogMapping = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỀ DIRECT ANALOG:</b></color> Nhả phím tự động kéo về giữa.");
        }
    }

    [MenuItem("Tools/Virtual Rail/5. Camera: Direction 3 (Telephoto Lens ON: FOV 40, Dist 35)")]
    public static void EnableTelephotoDirection3()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.cameraFOV = 40f;
            config.cameraDistance = 35f;
            config.cameraHeight = 8.0f;
            config.fixedCameraPitch = 5.0f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.fieldOfView = 40f;
                EditorUtility.SetDirty(cam);
            }

            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT HƯỚNG 3 (TELEPHOTO LENS):</b></color> FOV = 40, Distance = 35m, Height = 8m. Giảm mạnh thị sai mép màn hình, giữ camera hoàn toàn bất động!");
        }
    }

    [MenuItem("Tools/Virtual Rail/6. Camera: Undo Direction 3 (Classic Wide Lens: FOV 60, Dist 22)")]
    public static void DisableTelephotoDirection3()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.cameraFOV = 60f;
            config.cameraDistance = 22f;
            config.cameraHeight = 5.5f;
            config.fixedCameraPitch = 5.0f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.fieldOfView = 60f;
                EditorUtility.SetDirty(cam);
            }

            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) HƯỚNG 3:</b></color> Trở về ống kính góc rộng ban đầu: FOV = 60, Distance = 22m, Height = 5.5m.");
        }
    }

    [MenuItem("Tools/Virtual Rail/7. Controls: Independent (Ship: Keys, Reticle: Mouse + Auto-Fire)")]
    public static void EnableIndependentControls()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableIndependentControls = true;
            config.enableMouseAim = true;
            config.autoFireOnAimMove = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT ĐIỀU KHIỂN ĐỘC LẬP & TỰ ĐỘNG BẮN:</b></color> Tàu bay bằng phím (WASD/Arrows), Tâm ngắm trỏ chuột (Mouse), và tự động xả đạn khi rê chuột!");
        }
    }

    [MenuItem("Tools/Virtual Rail/8. Controls: Undo to Coupled (Ship follows Reticle, Manual Fire)")]
    public static void DisableIndependentControls()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableIndependentControls = false;
            config.enableMouseAim = false;
            config.autoFireOnAimMove = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ ĐIỀU KHIỂN LIÊN KẾT CŨ:</b></color> Tàu bám theo tâm ngắm bằng phím bấm, chỉ bắn khi bấm Space/Click.");
        }
    }

    private static GameObject BuildVirtualRailRig(VirtualRailConfig config)
    {
        GameObject root = new GameObject("VirtualRail_PlayerRig");
        root.tag = "Player";

        // Gốc neo đường ray
        VirtualRailAnchor anchor = root.AddComponent<VirtualRailAnchor>();
        anchor.config = config;

        // 1. Camera Rig
        GameObject camRigObj = new GameObject("[Camera Rig]");
        camRigObj.transform.SetParent(root.transform, false);
        camRigObj.transform.localPosition = new Vector3(0, config.cameraHeight, -config.cameraDistance);
        VirtualRailCameraRig camRig = camRigObj.AddComponent<VirtualRailCameraRig>();
        camRig.anchor = anchor;

        // 2. Reticle Anchor
        GameObject reticleObj = new GameObject("[Reticle Anchor]");
        reticleObj.transform.SetParent(root.transform, false);
        reticleObj.transform.localPosition = new Vector3(0, 0, config.convergenceDistance);
        VirtualRailReticle reticle = reticleObj.AddComponent<VirtualRailReticle>();
        reticle.anchor = anchor;

        // UI Canvas cho Crosshair & Lock Marker
        GameObject canvasObj = new GameObject("VirtualRailCanvas");
        canvasObj.transform.SetParent(reticleObj.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Crosshair
        GameObject crossObj = new GameObject("ReticleCrosshair");
        crossObj.transform.SetParent(canvasObj.transform, false);
        Image crossImg = crossObj.AddComponent<Image>();
        crossImg.color = new Color(0.2f, 1f, 0.4f, 0.85f);
        RectTransform crossRt = crossObj.GetComponent<RectTransform>();
        crossRt.sizeDelta = new Vector2(48, 48);
        reticle.crosshairUI = crossRt;

        // Lock Marker
        GameObject lockObj = new GameObject("LockMarker");
        lockObj.transform.SetParent(canvasObj.transform, false);
        Image lockImg = lockObj.AddComponent<Image>();
        lockImg.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        RectTransform lockRt = lockObj.GetComponent<RectTransform>();
        lockRt.sizeDelta = new Vector2(60, 60);
        lockObj.SetActive(false);

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
        ship.shipVisualMesh = shipVisual != null ? shipVisual.transform : shipContainer.transform;

        VirtualRailWeapon weapon = shipContainer.AddComponent<VirtualRailWeapon>();
        weapon.anchor = anchor;
        weapon.reticle = reticle;
        weapon.muzzles = shipContainer.GetComponentsInChildren<Laser>();

        AimAssistModule aimAssist = shipContainer.AddComponent<AimAssistModule>();
        aimAssist.anchor = anchor;
        aimAssist.reticle = reticle;
        aimAssist.lockMarkerUI = lockRt;
        aimAssist.missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MISSILE_PREFAB);

        camRig.ship = ship;

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
}
