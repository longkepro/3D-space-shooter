using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualRail;

/// <summary>
/// Công cụ tự động tạo 2 Prefab riêng biệt cho Squad Leader và Shield Bearer:
/// - Enemy (3) - Squad Leader.prefab (Sử dụng model Wrecker hạng nặng, chỉ huy phi đội, 80 HP, đạn chùm 3 viên)
/// - Enemy (4) - Shield Bearer.prefab (Sử dụng model Fighter hộ tống, trang bị lồng khiên AuraBubble, 40 HP)
/// Tuân thủ quy tắc 3 tầng an toàn: Giữ nguyên 100% 3 Prefab cũ và hỗ trợ Undo 1-click.
/// </summary>
[InitializeOnLoad]
public static class VirtualRailEnemyPrefabBuilder
{
    public const string PREFAB_SQUAD_LEADER_PATH = "Assets/Prefabs/Enemy (3) - Squad Leader.prefab";
    public const string PREFAB_SHIELD_BEARER_PATH = "Assets/Prefabs/Enemy (4) - Shield Bearer.prefab";

    public const string SOURCE_WRECKER_PATH = "Assets/Asset Store/SpaceshipCollectionPack/Prefabs/Spaceship - Wrecker/spaceship_wrecker_a.prefab";
    public const string SOURCE_FIGHTER_PATH = "Assets/Asset Store/SpaceshipCollectionPack/Prefabs/Spaceship - Fighter/spaceship_fighter_a.prefab";
    public const string PURPLE_THRUSTER_PATH = "Assets/Prefabs/Enemy_Thruster_Purple.prefab";
    public const string SHIELD_MAT_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Light/CFX4_AuraBubble ADD.mat";
    public const string LASER_PREFAB_PATH = "Assets/Prefabs/Laser.prefab";
    public const string BLOWUP_VFX_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Magic Hit.prefab";
    public const string EXPLOSION_VFX_PATH = "Assets/Asset Store/SpaceshipCollectionPack/Prefabs/Spaceship - Drone/spaceship_drone_a.prefab"; // Fallback / reference

    static VirtualRailEnemyPrefabBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PREFAB_SQUAD_LEADER_PATH) || !File.Exists(PREFAB_SHIELD_BEARER_PATH))
            {
                BuildAllDedicatedPrefabs();
            }
        };
    }

    [MenuItem("Tools/Virtual Rail/49. Build Dedicated Squad Leader and Shield Bearer Prefabs")]
    public static void BuildAllDedicatedPrefabs()
    {
        BuildSquadLeaderPrefab();
        BuildShieldBearerPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ TẠO THÀNH CÔNG 2 PREFAB RIÊNG BIỆT CHO SQUAD LEADER VÀ SHIELD BEARER!</b></color>");
    }

    [MenuItem("Tools/Virtual Rail/50. Revert to 3 Enemy Prefabs Only (Undo 49)")]
    public static void RevertDedicatedPrefabs()
    {
        bool deletedAny = false;
        if (File.Exists(PREFAB_SQUAD_LEADER_PATH))
        {
            AssetDatabase.DeleteAsset(PREFAB_SQUAD_LEADER_PATH);
            deletedAny = true;
        }
        if (File.Exists(PREFAB_SHIELD_BEARER_PATH))
        {
            AssetDatabase.DeleteAsset(PREFAB_SHIELD_BEARER_PATH);
            deletedAny = true;
        }

        if (deletedAny)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=yellow>[VirtualRail] Đã hoàn nguyên: Xóa 2 Prefab Squad Leader và Shield Bearer, quay về 3 Prefab cũ.</color>");
        }
    }

    private static GameObject BuildSquadLeaderPrefab()
    {
        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_WRECKER_PATH);
        if (sourceModel == null)
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy source model: {SOURCE_WRECKER_PATH}");
            return null;
        }

        GameObject root = new GameObject("Enemy (3) - Squad Leader");
        root.tag = "Enemy";

        // 1. Model 3D Wrecker (Scale 1.35x thể hiện chiến hạm hạng nặng)
        GameObject visual = Object.Instantiate(sourceModel, root.transform);
        visual.name = "[Visual_Model]";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 1.35f;

        // Gắn Tag Enemy cho mọi collider con
        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.tag = "Enemy";
        }

        // Box Collider thân tàu
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(7.5f, 2.5f, 7.0f);
        box.center = new Vector3(0f, 0f, 0f);

        // 2. Nòng súng Laser
        GameObject muzzleL = CreateLaserMuzzle(root.transform, "Left Laser", new Vector3(-2.4f, 0f, 1.8f));
        GameObject muzzleR = CreateLaserMuzzle(root.transform, "Right Laser", new Vector3(2.4f, 0f, 1.8f));

        // 3. Động cơ phản lực tím Neon
        AttachPurpleThrusters(root.transform, new Vector3(-1.0f, 0.2f, -3.2f), new Vector3(1.0f, 0.2f, -3.2f));

        // 4. Các Component Logic Gameplay
        EnemyMovement movement = root.AddComponent<EnemyMovement>();
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        EnemyAttack attack = root.AddComponent<EnemyAttack>();
        Explosion explosion = root.AddComponent<Explosion>();
        root.AddComponent<EnemyLifecycleFSM>();
        root.AddComponent<FormationWingman>();

        // Thiết lập chỉ số Archetype Squad Leader
        var preset = EnemyData.CreateDefaultPreset(EnemyArchetype.SquadLeader);
        movement.ApplyArchetype(preset);
        health.ApplyData(preset);

        // Nối muzzles vào EnemyAttack
        SerializedObject attackSo = new SerializedObject(attack);
        SerializedProperty lasersProp = attackSo.FindProperty("_lasers");
        if (lasersProp != null)
        {
            lasersProp.arraySize = 2;
            lasersProp.GetArrayElementAtIndex(0).objectReferenceValue = muzzleL.GetComponent<Laser>();
            lasersProp.GetArrayElementAtIndex(1).objectReferenceValue = muzzleR.GetComponent<Laser>();
            attackSo.ApplyModifiedPropertiesWithoutUndo();
        }

        ConfigureExplosion(explosion);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_SQUAD_LEADER_PATH);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject BuildShieldBearerPrefab()
    {
        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_FIGHTER_PATH);
        if (sourceModel == null)
        {
            Debug.LogError($"[VirtualRail] Không tìm thấy source model: {SOURCE_FIGHTER_PATH}");
            return null;
        }

        GameObject root = new GameObject("Enemy (4) - Shield Bearer");
        root.tag = "Enemy";

        // 1. Model 3D Fighter (Scale 1.0x)
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

        // Box Collider thân tàu
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(4.5f, 1.8f, 5.0f);
        box.center = new Vector3(0f, 0f, 0f);

        // 2. Nòng súng Laser
        GameObject muzzleL = CreateLaserMuzzle(root.transform, "Left Laser", new Vector3(-1.4f, 0f, 1.2f));
        GameObject muzzleR = CreateLaserMuzzle(root.transform, "Right Laser", new Vector3(1.4f, 0f, 1.2f));

        // 3. Động cơ phản lực tím Neon
        AttachPurpleThrusters(root.transform, new Vector3(0f, 0.1f, -2.4f));

        // 4. Lồng khiên năng lượng AuraBubble ([Shield_Barrier])
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

        // 5. Các Component Logic Gameplay
        EnemyMovement movement = root.AddComponent<EnemyMovement>();
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        EnemyAttack attack = root.AddComponent<EnemyAttack>();
        Explosion explosion = root.AddComponent<Explosion>();
        root.AddComponent<EnemyLifecycleFSM>();
        root.AddComponent<FormationWingman>();

        // Thiết lập chỉ số Archetype Shield Bearer
        var preset = EnemyData.CreateDefaultPreset(EnemyArchetype.ShieldBearer);
        movement.ApplyArchetype(preset);
        health.ApplyData(preset);

        // Nối muzzles vào EnemyAttack
        SerializedObject attackSo = new SerializedObject(attack);
        SerializedProperty lasersProp = attackSo.FindProperty("_lasers");
        if (lasersProp != null)
        {
            lasersProp.arraySize = 2;
            lasersProp.GetArrayElementAtIndex(0).objectReferenceValue = muzzleL.GetComponent<Laser>();
            lasersProp.GetArrayElementAtIndex(1).objectReferenceValue = muzzleR.GetComponent<Laser>();
            attackSo.ApplyModifiedPropertiesWithoutUndo();
        }

        ConfigureExplosion(explosion);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_SHIELD_BEARER_PATH);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject CreateLaserMuzzle(Transform parent, string name, Vector3 localPos)
    {
        GameObject laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LASER_PREFAB_PATH);
        if (laserPrefab != null)
        {
            GameObject muzzle = (GameObject)PrefabUtility.InstantiatePrefab(laserPrefab, parent);
            muzzle.name = name;
            muzzle.tag = "Laser";
            muzzle.transform.localPosition = localPos;
            muzzle.transform.localRotation = Quaternion.identity;
            return muzzle;
        }
        else
        {
            GameObject muzzle = new GameObject(name);
            muzzle.tag = "Laser";
            muzzle.transform.SetParent(parent, false);
            muzzle.transform.localPosition = localPos;
            muzzle.AddComponent<Laser>();
            return muzzle;
        }
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
