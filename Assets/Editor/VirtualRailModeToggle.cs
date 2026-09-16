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

    [MenuItem("Tools/Virtual Rail/9. Mobile: Show Touch Joysticks (ON)")]
    public static void EnableForceShowMobileUI()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableMobileUI = true;
            config.forceShowMobileUI = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT GIAO DIỆN MOBILE UI:</b></color> 2 cần gạt ảo (Trái & Phải) đang hiển thị. Trong lúc chơi có thể bấm F1 để ẩn/hiện nhanh!");
        }
    }

    [MenuItem("Tools/Virtual Rail/10. Mobile: Hide Touch Joysticks (OFF)")]
    public static void DisableForceShowMobileUI()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.forceShowMobileUI = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ ẨN GIAO DIỆN MOBILE UI:</b></color> Cần gạt ảo đã được ẩn tạm thời. Muốn bật lại chỉ cần chọn Menu 9 hoặc bấm F1 khi đang chơi!");
        }
    }

    [MenuItem("Tools/Virtual Rail/11. Weapons: Direction 1 (Segmented Laser Bolts ON)")]
    public static void EnableSegmentedLaserBolts()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useSegmentedLaser = true;
            config.laserBoltLength = 8.5f;
            config.laserBoltWidth = 0.85f;
            config.laserBoltSpeed = 220f;
            config.laserColor = new Color(0.1f, 1f, 0.85f, 1f);

            var neonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
            if (neonMat != null)
            {
                config.laserBoltMaterial = neonMat;
            }
            else
            {
                config.laserBoltMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Stretched/CFX_RayRounded.mat");
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT HƯỚNG 1 (SEGMENTED LASER BOLTS):</b></color> Đạn laser to rõ (0.85m), dài (8.5m), phát sáng Neon rực rỡ và có Point Light chiếu sáng!");
        }
    }

    [MenuItem("Tools/Virtual Rail/12. Weapons: Undo to Instant Raycast Beam (OFF)")]
    public static void DisableSegmentedLaserBolts()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useSegmentedLaser = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ TIA LASER TỨC THỜI:</b></color> Tia laser kéo dài từ nòng súng tới mục tiêu.");
        }
    }

    [MenuItem("Tools/Virtual Rail/13. Spawner: Rail Wave Spawner (ON - Ahead of Player)")]
    public static void EnableRailWaveSpawner()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useRailWaveSpawner = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT RAIL WAVE SPAWNER:</b></color> Quái vật sinh ra phía trước đón đầu người chơi (220m), tái sử dụng qua Object Pool 0 GC!");
        }
    }

    [MenuItem("Tools/Virtual Rail/14. Spawner: Undo to Legacy Static Spawner (OFF)")]
    public static void DisableRailWaveSpawner()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useRailWaveSpawner = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ ENEMY SPAWNER CŨ:</b></color> Tắt bộ điều phối đợt sóng đường ray.");
        }
    }

    [MenuItem("Tools/Virtual Rail/15. Asteroids: Streaming Rail Asteroids (ON - Ahead of Player)")]
    public static void EnableStreamingAsteroids()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useRailAsteroidSpawner = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT STREAMING ASTEROIDS:</b></color> Thiên thạch liên tục sinh ra đón đầu phía trước mũi tàu (250m), thu hồi sau lưng (0 GC Alloc)!");
        }
    }

    [MenuItem("Tools/Virtual Rail/16. Asteroids: Undo to Legacy Static Asteroids (OFF)")]
    public static void DisableStreamingAsteroids()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useRailAsteroidSpawner = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN VỀ THIÊN THẠCH TĨNH:</b></color> Tắt bộ điều phối thiên thạch đón đầu đường ray.");
        }
    }

    [MenuItem("Tools/Virtual Rail/17. Kinematics: Golden Ratio Motion (ON - 50% X, 35% Y, Adaptive Lag)")]
    public static void EnableGoldenRatioMotion()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useGoldenRatioMotion = true;
            config.forwardSpeed = 40f;
            config.boostMultiplier = 1.5f;
            config.goldenRatioX = 0.50f;
            config.goldenRatioY = 0.35f;
            config.maxSafeDriftDistance = 2.5f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT ĐỘNG HỌC TỶ LỆ VÀNG:</b></color> Vx:Vy:Vz = 0.50:0.35:1.00, Transit Time ~0.7s, Tự động siết trôi dạt S_drift <= 2.5m!");
        }
    }

    [MenuItem("Tools/Virtual Rail/18. Kinematics: Undo to Legacy Fixed Speeds (OFF)")]
    public static void DisableGoldenRatioMotion()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useGoldenRatioMotion = false;
            config.forwardSpeed = 28f;
            config.boostMultiplier = 1.6f;
            config.shipSpeedX = 140f;
            config.shipSpeedY = 85f;
            config.smoothDampLag = 0.1f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ VẬN TỐC CỐ ĐỊNH CŨ:</b></color> Dùng shipSpeedX = 140, shipSpeedY = 85, lag = 0.1s.");
        }
    }

    [MenuItem("Tools/Virtual Rail/19. Combat: Active Counter-Play & Plasma Projectiles (ON)")]
    public static void EnableCombatCounterPlay()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableBarrelRollDeflection = true;
            config.enableSmartBomb = true;
            config.usePhysicalEnemyProjectiles = true;
            config.chargeShotSplashRadius = 10f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT COMBAT & ACTIVE COUNTER-PLAY (TDD PHẦN I):</b></color> Barrel Roll hất đạn 90°, Smart Bomb quét sạch màn hình, Đạn Plasma vật lý V_closing, Charge Shot nổ lan Combo!");
        }
    }

    [MenuItem("Tools/Virtual Rail/20. Combat: Undo to Legacy Hitscan Attack (OFF)")]
    public static void DisableCombatCounterPlay()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableBarrelRollDeflection = false;
            config.enableSmartBomb = false;
            config.usePhysicalEnemyProjectiles = false;
            config.chargeShotSplashRadius = 0f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ COMBAT CŨ:</b></color> Tắt Barrel Roll hất đạn, Tắt Smart Bomb, Quay lại Laser Hitscan truyền thống.");
        }
    }

    [MenuItem("Tools/Virtual Rail/21. Spatial Vectors: 10 Spawn Trajectories & 4-Phase FSM (ON)")]
    public static void EnableSpatialVectors()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useSpatialVectors = true;
            config.enable4PhaseLifecycle = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT 10 VECTOR KHÔNG GIAN & FSM 4 PHA (TDD PHẦN II):</b></color> Trực diện, Đột kích sau lưng, Cắt ngang sườn, Bổ nhào trần mây, Trồi đáy sâu, Warp-in, FSM Telegraph -> Approach -> Action -> Exit!");
        }
    }

    [MenuItem("Tools/Virtual Rail/22. Spatial Vectors: Undo to Legacy Static Spawn (OFF)")]
    public static void DisableSpatialVectors()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.useSpatialVectors = false;
            config.enable4PhaseLifecycle = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ SINH QUÁI TĨNH CŨ:</b></color> Tắt 10 Vector không gian, tắt FSM 4 pha, quay lại vị trí tĩnh phía trước.");
        }
    }

    [MenuItem("Tools/Virtual Rail/23. Formations: Bézier Assembly & Panic Scatter (ON)")]
    public static void EnableFormationChoreography()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableFormations = true;
            config.enablePanicScatter = true;
            config.formationAssemblyTime = 1.2f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT BIÊN ĐẠO ĐỘI HÌNH & PANIC SCATTER (TDD PHẦN III):</b></color> Đội hình chữ V ráp nối Bézier 3D 1.2s, Đánh đổi Rủi ro/Phần thưởng Combo, Bắn hạ chỉ huy kích hoạt Panic Scatter!");
        }
    }

    [MenuItem("Tools/Virtual Rail/24. Formations: Undo to Solitary Spawning (OFF)")]
    public static void DisableFormationChoreography()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableFormations = false;
            config.enablePanicScatter = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ SINH QUÁI ĐƠN LẺ:</b></color> Tắt bay đội hình chữ V, tắt Panic Scatter.");
        }
    }

    [MenuItem("Tools/Virtual Rail/25. Mouse Aim: Hover-to-Fire on Reticle (ON)")]
    public static void EnableMouseHoverFire()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableMouseHoverFire = true;
            config.reticleMouseTouchRadius = 45f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT HOVER-TO-FIRE (HƯỚNG 1):</b></color> Chuột chạm/nằm trong vùng tâm ngắm sẽ luôn tự động xả đạn liên tục!");
        }
    }

    [MenuItem("Tools/Virtual Rail/26. Mouse Aim: Undo to Manual Click Fire (OFF)")]
    public static void DisableMouseHoverFire()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableMouseHoverFire = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ BẮN THỦ CÔNG:</b></color> Tắt Hover-to-Fire, quay lại bấm chuột trái/Space để bắn.");
        }
    }

    [MenuItem("Tools/Virtual Rail/27. Asteroids: Guaranteed Safe Corridor Trench (ON)")]
    public static void EnableSafeCorridorPath()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableSafeCorridorPath = true;
            config.safeCorridorRadius = 5.5f;
            config.safePathWiggleFrequency = 0.012f;
            config.safePathWiggleAmplitudeX = 12f;
            config.safePathWiggleAmplitudeY = 4.5f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT LÀN BAY AN TOÀN ĐẢM BẢO (PERLIN SAFE SPINE):</b></color> Thuật toán Perlin Noise tạo luồng hẻm núi an toàn R=5.5m xuyên qua biển thiên thạch, 100% không bị tường chắn bất khả thi!");
        }
    }

    [MenuItem("Tools/Virtual Rail/28. Asteroids: Undo to Pure Random Spawn (OFF)")]
    public static void DisableSafeCorridorPath()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableSafeCorridorPath = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) VỀ NGẪU NHIÊN THUẦN TÚY:</b></color> Tắt luồng an toàn, quay lại rải thiên thạch Random.Range cũ.");
        }
    }

    [MenuItem("Tools/Virtual Rail/29. Aiming: Pure 3D Convergence to Reticle (ON - Star Fox 64 Arcade)")]
    public static void EnablePureConvergenceAiming()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableBulletMagnetism = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT BẮN HỘI TỤ THUẦN TÚY (PURE 3D CONVERGENCE - STAR FOX 64 ARCADE):</b></color> Đạn từ mọi nòng luôn bay thẳng xuyên tâm ngắm reticle ở cự ly Z_conv, 100% không bị thiên thạch hay vật cản ven đường bẻ lệch!");
        }
    }

    [MenuItem("Tools/Virtual Rail/30. Aiming: Bullet Magnetism Assist (ON - Filtered Enemy Only)")]
    public static void EnableFilteredBulletMagnetism()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableBulletMagnetism = true;
            config.magnetismRadius = 1.2f;
            config.magnetismMaxAngle = 6.0f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=cyan><b>[VirtualRail] ĐÃ BẬT PHỄU TỪ TÍNH CÓ KIỂM SOÁT (BULLET MAGNETISM ASSIST):</b></color> Chỉ nắn hướng đạn tối đa 6 độ về trọng tâm kẻ địch (Enemy), hoàn toàn bỏ qua thiên thạch.");
        }
    }

    [MenuItem("Tools/Virtual Rail/31. Aiming: Dynamic Depth Convergence (ON - Star Fox 64)")]
    public static void EnableDynamicDepthConvergence()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableDynamicDepthConvergence = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT HỘI TỤ ĐỘ SÂU ĐỘNG (DYNAMIC DEPTH CONVERGENCE - STAR FOX 64):</b></color> Tia ngắm tự động đo độ sâu mục tiêu, chụm 2 nòng súng chính xác vào kẻ địch ở mọi cự ly (20m - 250m)!");
        }
    }

    [MenuItem("Tools/Virtual Rail/32. Aiming: Fixed Depth Convergence 150m (OFF - Legacy)")]
    public static void DisableDynamicDepthConvergence()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableDynamicDepthConvergence = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ QUAY VỀ HỘI TỤ CỐ ĐỊNH 150M (FIXED DEPTH CONVERGENCE - LEGACY):</b></color> Đạn luôn chụm ở cự ly cố định 150m.");
        }
    }

    [MenuItem("Tools/Virtual Rail/33. Aiming: Depth-Responsive Sci-Fi Reticle HUD (ON - Independent Aim)")]
    public static void EnableDepthResponsiveReticleHUD()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableDepthResponsiveReticle = true;
            config.enableDistanceTelemetry = true;
            config.enableDualReticle = false;
            config.enableMicroAimAlignment = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ BẬT TÂM NGẮM ĐƠN SCI-FI PHẢN HỒI ĐỘ SÂU (DEPTH-RESPONSIVE HUD):</b></color> Tâm ngắm tự động co giãn theo khoảng cách mục tiêu và hiển thị đo đạc cự ly số hóa (0 GC Telemetry).");
        }
    }

    [MenuItem("Tools/Virtual Rail/34. Aiming: Classic Dual Reticle (OFF - Star Fox 64 Coupled)")]
    public static void EnableClassicDualReticle()
    {
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>(CONFIG_PATH);
        if (config != null)
        {
            config.enableDepthResponsiveReticle = false;
            config.enableDistanceTelemetry = false;
            config.enableDualReticle = true;
            config.enableMicroAimAlignment = true;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ CHUYỂN VỀ TÂM NGẮM KÉP CỔ ĐIỂN (STAR FOX 64 DUAL RETICLE):</b></color> Bật tâm gần và tâm xa 2 tầng.");
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
        crossImg.color = new Color(0.1f, 0.95f, 1f, 0.20f);
        RectTransform crossRt = crossObj.GetComponent<RectTransform>();
        float farSize = config.farReticleSize > 10f ? config.farReticleSize : 56f;
        crossRt.sizeDelta = new Vector2(farSize, farSize);
        reticle.crosshairUI = crossRt;
        CreateHollowBoxBorders(crossObj.transform, crossRt.sizeDelta, new Color(0.1f, 0.95f, 1f, 0.90f));

        // 2. Near Reticle (Tâm ngắm gần - Inner Crosshair Emerald Green 28x28)
        GameObject nearCrossObj = new GameObject("ReticleNearCrosshair");
        nearCrossObj.transform.SetParent(canvasObj.transform, false);
        Image nearCrossImg = nearCrossObj.AddComponent<Image>();
        nearCrossImg.color = new Color(0.2f, 1f, 0.4f, 0.25f);
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
}
