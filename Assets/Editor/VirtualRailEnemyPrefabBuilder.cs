using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualRail;

/// <summary>
/// Công cụ tự động tạo và quản lý 5 Prefab riêng biệt cho toàn bộ hệ thống Kẻ địch:
/// - Enemy (0).prefab: Model StarSparrow1 (Scout trinh sát cơ động, 30 HP)
/// - Enemy (1).prefab: Model StarSparrow4 (Flanker bọc sườn giáp dày, 45 HP)
/// - Enemy (2).prefab: Model StarSparrow6 (Interceptor tiêm kích siêu thanh, 40 HP)
/// - Enemy (3) - Squad Leader.prefab: Model Wrecker hạng nặng (Chỉ huy phi đội Wedge V, 80 HP)
/// - Enemy (4) - Shield Bearer.prefab: Model Fighter hộ tống (Trang bị lồng khiên năng lượng AuraBubble, 40 HP)
/// 
/// Tuân thủ nghiêm ngặt quy tắc an toàn 3 tầng bảo lưu:
/// - Tầng 1: Git checkpoint branch.
/// - Tầng 2: Sao lưu an toàn Enemy (0)-(2) cũ vào Assets/Prefabs/Backup_Legacy_Enemies/.
/// - Tầng 3: Menu Item 1-click [41] Apply (ON) và [42] Undo Restore (OFF - Legacy).
/// </summary>
public static class VirtualRailEnemyPrefabBuilder
{
    public const string PREFAB_ENEMY_0_PATH = "Assets/Prefabs/Enemy (0) - Scout.prefab";
    public const string PREFAB_ENEMY_1_PATH = "Assets/Prefabs/Enemy (1) - Flanker.prefab";
    public const string PREFAB_ENEMY_2_PATH = "Assets/Prefabs/Enemy (2) - Interceptor.prefab";
    public const string PREFAB_SQUAD_LEADER_PATH = "Assets/Prefabs/Enemy (3) - Squad Leader.prefab";
    public const string PREFAB_SHIELD_BEARER_PATH = "Assets/Prefabs/Enemy (4) - Shield Bearer.prefab";

    public const string BACKUP_DIR_PATH = "Assets/Prefabs/Backup_Legacy_Enemies";

    public const string SOURCE_SS1_PATH = "Assets/StarSparrow/Prefabs/Examples/StarSparrow1.prefab";
    public const string SOURCE_SS4_PATH = "Assets/StarSparrow/Prefabs/Examples/StarSparrow4.prefab";
    public const string SOURCE_SS6_PATH = "Assets/StarSparrow/Prefabs/Examples/StarSparrow6.prefab";
    public const string SOURCE_WRECKER_PATH = "Assets/Asset Store/SpaceshipCollectionPack/Prefabs/Spaceship - Wrecker/spaceship_wrecker_a.prefab";
    public const string SOURCE_FIGHTER_PATH = "Assets/Asset Store/SpaceshipCollectionPack/Prefabs/Spaceship - Fighter/spaceship_fighter_a.prefab";

    public const string VIRTUAL_SS1_PATH = "Assets/Prefabs/VirtualRail_Enemy_StarSparrow1.prefab";
    public const string VIRTUAL_SS4_PATH = "Assets/Prefabs/VirtualRail_Enemy_StarSparrow4.prefab";
    public const string VIRTUAL_SS6_PATH = "Assets/Prefabs/VirtualRail_Enemy_StarSparrow6.prefab";

    public const string PURPLE_THRUSTER_PATH = "Assets/Prefabs/Enemy_Thruster_Purple.prefab";
    public const string SHIELD_MAT_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Light/CFX4_AuraBubble ADD.mat";
    public const string LASER_PREFAB_PATH = "Assets/Prefabs/Laser.prefab";
    public const string BLOWUP_VFX_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Magic Hit.prefab";

    public static void MigrateStarSparrowToEnemies()
    {
        // 1. Sao lưu 3 prefab cũ nếu chưa sao lưu
        BackupLegacyEnemies();

        // 2. Build 3 prefab Enemy (0), (1), (2) với model StarSparrow
        BuildEnemy0Prefab();
        BuildEnemy1Prefab();
        BuildEnemy2Prefab();

        // 3. Đảm bảo Squad Leader và Shield Bearer cũng tồn tại
        if (!File.Exists(PREFAB_SQUAD_LEADER_PATH)) BuildSquadLeaderPrefab();
        if (!File.Exists(PREFAB_SHIELD_BEARER_PATH)) BuildShieldBearerPrefab();

        // 4. Dọn dẹp 3 prefab Virtual thừa nếu tồn tại
        CleanupVirtualStarSparrowPrefabs();

        // 5. Cập nhật VirtualRailConfig
        VirtualRailConfig config = AssetDatabase.LoadAssetAtPath<VirtualRailConfig>("Assets/Scripts/VirtualRail/DefaultVirtualRailConfig.asset");
        if (config != null)
        {
            config.useStarSparrowFleet = false;
            EditorUtility.SetDirty(config);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ NÂNG CẤP THÀNH CÔNG ENEMY (0)-(2) SANG MODEL STARSPARROW!</b></color>\n- Enemy (0): StarSparrow1 (Scout)\n- Enemy (1): StarSparrow4 (Flanker)\n- Enemy (2): StarSparrow6 (Interceptor)");
    }

    public static void RestoreLegacyEnemies()
    {
        if (!Directory.Exists(BACKUP_DIR_PATH))
        {
            Debug.LogWarning("[VirtualRail] Không tìm thấy thư mục sao lưu: " + BACKUP_DIR_PATH);
            return;
        }

        string[] files = { "Enemy (0).prefab", "Enemy (1).prefab", "Enemy (2).prefab" };
        foreach (var file in files)
        {
            string backupFile = Path.Combine(BACKUP_DIR_PATH, file);
            string targetFile = Path.Combine("Assets/Prefabs", file);
            if (File.Exists(backupFile))
            {
                File.Copy(backupFile, targetFile, true);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (RESTORE) ENEMY (0)-(2) VỀ MODEL NGUYÊN MẪU CŨ!</b></color>");
    }

    public static void BackupLegacyEnemies()
    {
        if (!Directory.Exists(BACKUP_DIR_PATH))
        {
            Directory.CreateDirectory(BACKUP_DIR_PATH);
        }

        string[] files = { "Enemy (0).prefab", "Enemy (0).prefab.meta", "Enemy (1).prefab", "Enemy (1).prefab.meta", "Enemy (2).prefab", "Enemy (2).prefab.meta" };
        foreach (var file in files)
        {
            string src = Path.Combine("Assets/Prefabs", file);
            string dst = Path.Combine(BACKUP_DIR_PATH, file);
            if (File.Exists(src) && !File.Exists(dst))
            {
                File.Copy(src, dst, false);
            }
        }
    }

    public static void CleanupVirtualStarSparrowPrefabs()
    {
        bool deletedAny = false;
        if (File.Exists(VIRTUAL_SS1_PATH))
        {
            AssetDatabase.DeleteAsset(VIRTUAL_SS1_PATH);
            deletedAny = true;
        }
        if (File.Exists(VIRTUAL_SS4_PATH))
        {
            AssetDatabase.DeleteAsset(VIRTUAL_SS4_PATH);
            deletedAny = true;
        }
        if (File.Exists(VIRTUAL_SS6_PATH))
        {
            AssetDatabase.DeleteAsset(VIRTUAL_SS6_PATH);
            deletedAny = true;
        }

        if (deletedAny)
        {
            Debug.Log("<color=cyan>[VirtualRail] Đã dọn dẹp sạch 3 prefab Virtual StarSparrow thừa.</color>");
        }
    }

    public static GameObject BuildEnemy0Prefab()
    {
        return BuildStarSparrowEnemy(
            SOURCE_SS1_PATH,
            PREFAB_ENEMY_0_PATH,
            "Enemy (0) - Scout",
            EnemyArchetype.Scout,
            new Vector3(-1.6f, 0f, 0.5f),
            new Vector3(1.6f, 0f, 0.5f),
            new Vector3(3.5f, 1.5f, 5.0f),
            new Vector3(0f, 0.1f, -2.4f)
        );
    }

    public static GameObject BuildEnemy1Prefab()
    {
        return BuildStarSparrowEnemy(
            SOURCE_SS4_PATH,
            PREFAB_ENEMY_1_PATH,
            "Enemy (1) - Flanker",
            EnemyArchetype.Flanker,
            new Vector3(-2.0f, 0f, 1.0f),
            new Vector3(2.0f, 0f, 1.0f),
            new Vector3(5.0f, 1.8f, 5.5f),
            new Vector3(-1.0f, 0.1f, -2.8f),
            new Vector3(1.0f, 0.1f, -2.8f)
        );
    }

    public static GameObject BuildEnemy2Prefab()
    {
        return BuildStarSparrowEnemy(
            SOURCE_SS6_PATH,
            PREFAB_ENEMY_2_PATH,
            "Enemy (2) - Interceptor",
            EnemyArchetype.Interceptor,
            new Vector3(-1.2f, 0f, 0.8f),
            new Vector3(1.2f, 0f, 0.8f),
            new Vector3(3.2f, 1.5f, 5.5f),
            new Vector3(0f, 0.1f, -2.6f)
        );
    }

    public static GameObject BuildSquadLeaderPrefab()
    {
        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_WRECKER_PATH);
        if (sourceModel == null)
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy source model: {SOURCE_WRECKER_PATH}");
            return null;
        }

        GameObject root = new GameObject("Enemy (3) - Squad Leader");
        root.tag = "Enemy";

        GameObject visual = Object.Instantiate(sourceModel, root.transform);
        visual.name = "[Visual_Model]";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 1.35f;

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.tag = "Enemy";
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(7.5f, 2.5f, 7.0f);
        box.center = new Vector3(0f, 0f, 0f);

        GameObject muzzleL = CreateLaserMuzzle(root.transform, "Left Laser", new Vector3(-2.4f, 0f, 1.8f));
        GameObject muzzleR = CreateLaserMuzzle(root.transform, "Right Laser", new Vector3(2.4f, 0f, 1.8f));

        AttachPurpleThrusters(root.transform, new Vector3(-1.0f, 0.2f, -3.2f), new Vector3(1.0f, 0.2f, -3.2f));

        EnemyMovement movement = root.AddComponent<EnemyMovement>();
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        EnemyAttack attack = root.AddComponent<EnemyAttack>();
        Explosion explosion = root.AddComponent<Explosion>();
        root.AddComponent<EnemyLifecycleFSM>();
        root.AddComponent<FormationWingman>();
        root.AddComponent<VirtualRail.EnemyBoosterTrail>();

        var preset = EnemyData.CreateDefaultPreset(EnemyArchetype.SquadLeader);
        movement.ApplyArchetype(preset);
        health.ApplyData(preset);

        WireUpLaserAttack(attack, muzzleL, muzzleR);
        ConfigureExplosion(explosion);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_SQUAD_LEADER_PATH);
        Object.DestroyImmediate(root);
        return saved;
    }

    public static GameObject BuildShieldBearerPrefab()
    {
        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_FIGHTER_PATH);
        if (sourceModel == null)
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy source model: {SOURCE_FIGHTER_PATH}");
            return null;
        }

        GameObject root = new GameObject("Enemy (4) - Shield Bearer");
        root.tag = "Enemy";

        GameObject visual = Object.Instantiate(sourceModel, root.transform);
        visual.name = "[Visual_Model]";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.tag = "Enemy";
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(4.5f, 1.8f, 5.0f);
        box.center = new Vector3(0f, 0f, 0f);

        GameObject muzzleL = CreateLaserMuzzle(root.transform, "Left Laser", new Vector3(-1.4f, 0f, 1.2f));
        GameObject muzzleR = CreateLaserMuzzle(root.transform, "Right Laser", new Vector3(1.4f, 0f, 1.2f));

        AttachPurpleThrusters(root.transform, new Vector3(0f, 0.1f, -2.4f));

        // Lồng khiên năng lượng AuraBubble ([Shield_Barrier])
        GameObject shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldObj.name = "[Shield_Barrier]";
        shieldObj.transform.SetParent(root.transform, false);
        shieldObj.transform.localPosition = Vector3.zero;
        shieldObj.transform.localRotation = Quaternion.identity;
        shieldObj.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
        Collider sphereCol = shieldObj.GetComponent<Collider>();
        if (sphereCol != null) Object.DestroyImmediate(sphereCol);

        Renderer shieldRend = shieldObj.GetComponent<Renderer>();
        if (shieldRend != null)
        {
            Material shieldMat = AssetDatabase.LoadAssetAtPath<Material>(SHIELD_MAT_PATH);
            if (shieldMat != null) shieldRend.sharedMaterial = shieldMat;
            shieldRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shieldRend.receiveShadows = false;
        }

        EnemyMovement movement = root.AddComponent<EnemyMovement>();
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        EnemyAttack attack = root.AddComponent<EnemyAttack>();
        Explosion explosion = root.AddComponent<Explosion>();
        root.AddComponent<EnemyLifecycleFSM>();
        root.AddComponent<FormationWingman>();
        root.AddComponent<VirtualRail.EnemyBoosterTrail>();

        var preset = EnemyData.CreateDefaultPreset(EnemyArchetype.ShieldBearer);
        movement.ApplyArchetype(preset);
        health.ApplyData(preset);

        WireUpLaserAttack(attack, muzzleL, muzzleR);
        ConfigureExplosion(explosion);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_SHIELD_BEARER_PATH);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject BuildStarSparrowEnemy(
        string sourceModelPath,
        string targetPrefabPath,
        string rootName,
        EnemyArchetype archetype,
        Vector3 muzzleLeftPos,
        Vector3 muzzleRightPos,
        Vector3 boxColliderSize,
        params Vector3[] thrusterPositions)
    {
        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
        if (sourceModel == null)
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy source model: {sourceModelPath}");
            return null;
        }

        GameObject root = new GameObject(rootName);
        root.tag = "Enemy";

        GameObject visual = Object.Instantiate(sourceModel, root.transform);
        visual.name = "[Visual_Model]";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.tag = "Enemy";
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = boxColliderSize;
        box.center = Vector3.zero;

        GameObject muzzleL = CreateLaserMuzzle(root.transform, "Left Laser", muzzleLeftPos);
        GameObject muzzleR = CreateLaserMuzzle(root.transform, "Right Laser", muzzleRightPos);

        if (thrusterPositions != null && thrusterPositions.Length > 0)
        {
            AttachPurpleThrusters(root.transform, thrusterPositions);
        }

        EnemyMovement movement = root.AddComponent<EnemyMovement>();
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        EnemyAttack attack = root.AddComponent<EnemyAttack>();
        Explosion explosion = root.AddComponent<Explosion>();
        root.AddComponent<EnemyLifecycleFSM>();
        root.AddComponent<FormationWingman>();
        root.AddComponent<VirtualRail.EnemyBoosterTrail>();

        var preset = EnemyData.CreateDefaultPreset(archetype);
        movement.ApplyArchetype(preset);
        health.ApplyData(preset);

        WireUpLaserAttack(attack, muzzleL, muzzleR);
        ConfigureExplosion(explosion);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject CreateLaserMuzzle(Transform parent, string name, Vector3 localPos)
    {
        GameObject muzzle = new GameObject(name);
        muzzle.tag = "Laser";
        muzzle.transform.SetParent(parent, false);
        muzzle.transform.localPosition = localPos;
        muzzle.transform.localRotation = Quaternion.identity;
        muzzle.AddComponent<VirtualRail.VirtualRailMuzzle>();
        return muzzle;
    }

    private static void AttachPurpleThrusters(Transform parent, params Vector3[] localPositions)
    {
        GameObject thrusterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PURPLE_THRUSTER_PATH);
        if (thrusterPrefab == null) return;

        for (int i = 0; i < localPositions.Length; i++)
        {
            GameObject thruster = (GameObject)PrefabUtility.InstantiatePrefab(thrusterPrefab, parent);
            thruster.name = localPositions.Length == 1 ? "Enemy_Thruster_Purple" : $"Enemy_Thruster_Purple_{i}";
            thruster.transform.localPosition = localPositions[i];
            thruster.transform.localRotation = Quaternion.identity;
        }
    }

    private static void WireUpLaserAttack(EnemyAttack attack, GameObject muzzleL, GameObject muzzleR)
    {
        SerializedObject attackSo = new SerializedObject(attack);

        SerializedProperty muzzlesProp = attackSo.FindProperty("_muzzles");
        if (muzzlesProp != null)
        {
            muzzlesProp.arraySize = 2;
            var muzzL = muzzleL != null ? muzzleL.GetComponent<VirtualRail.VirtualRailMuzzle>() : null;
            var muzzR = muzzleR != null ? muzzleR.GetComponent<VirtualRail.VirtualRailMuzzle>() : null;
            if (muzzL != null) muzzlesProp.GetArrayElementAtIndex(0).objectReferenceValue = muzzL;
            if (muzzR != null) muzzlesProp.GetArrayElementAtIndex(1).objectReferenceValue = muzzR;
        }

        SerializedProperty lasersProp = attackSo.FindProperty("_lasers");
        if (lasersProp != null)
        {
            lasersProp.arraySize = 2;
            Laser laserL = muzzleL != null ? muzzleL.GetComponent<Laser>() : null;
            Laser laserR = muzzleR != null ? muzzleR.GetComponent<Laser>() : null;
            if (laserL != null) lasersProp.GetArrayElementAtIndex(0).objectReferenceValue = laserL;
            if (laserR != null) lasersProp.GetArrayElementAtIndex(1).objectReferenceValue = laserR;
        }

        attackSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureExplosion(Explosion explosion)
    {
        GameObject blowUpVfx = AssetDatabase.LoadAssetAtPath<GameObject>(BLOWUP_VFX_PATH);
        if (blowUpVfx == null)
        {
            blowUpVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX_ElectricityBall.prefab");
        }

        SerializedObject expSo = new SerializedObject(explosion);
        if (blowUpVfx != null)
        {
            SerializedProperty blowUpProp = expSo.FindProperty("_blowUp");
            if (blowUpProp != null) blowUpProp.objectReferenceValue = blowUpVfx;
        }
        SerializedProperty forceProp = expSo.FindProperty("_laserHitForce");
        if (forceProp != null) forceProp.floatValue = 12f;

        SerializedProperty durProp = expSo.FindProperty("_explosionDuration");
        if (durProp != null) durProp.floatValue = 6f;

        expSo.ApplyModifiedPropertiesWithoutUndo();
    }
}
