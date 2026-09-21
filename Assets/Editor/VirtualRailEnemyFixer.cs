using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualRail;

/// <summary>
/// Công cụ tự động sửa lỗi và cân chỉnh toàn diện cho 5 Prefab Kẻ địch:
/// 1. Sửa đứt kết nối Laser (EnemyAttack._lasers) trên Enemy (0), (1) và (2).
/// 2. Thêm BoxCollider chuẩn xác cho Enemy (0), (1), (2) (vốn đang hoàn toàn thiếu Collider).
/// 3. Gắn và định vị chính xác vệt đuôi động cơ tím Enemy_Thruster_Purple khớp từng đuôi ống xả.
/// 4. Khắc phục lỗi lửa tím trôi nổi cách đuôi 1.5m trên Squad Leader (kéo từ Z=-3.2 về Z=-2.25).
/// 5. Thu nhỏ Hitbox ma khổng lồ trên Squad Leader (từ 7.5x7.0 xuống 4.8x5.0 vừa khít thân tàu).
/// 6. Phóng to thân tàu Squad Leader 1.35x thể hiện dáng vóc Tàu Chỉ Huy.
/// 7. Mở rộng BoxCollider của Shield Bearer lên 4.8x5.2 để bao bọc các cánh Wing2/Wing6 ghép thêm.
/// 8. Đổi màu sơn Enemy (0) từ Đỏ sang Xám quân sự HiRez để không trùng màu tàu Người chơi.
/// 9. Gắn Tag "Enemy" cho 100% Collider con để cơ chế đạn đạo bắt trúng mục tiêu.
/// 10. Tuân thủ Điều 3: Cung cấp cặp MenuItem 1-click [53] Apply (ON) và [54] Undo (OFF - Legacy).
/// </summary>
[InitializeOnLoad]
public static class VirtualRailEnemyFixer
{
    private const string PREFAB_0 = "Assets/Prefabs/Enemy (0) - Scout.prefab";
    private const string PREFAB_1 = "Assets/Prefabs/Enemy (1) - Flanker.prefab";
    private const string PREFAB_2 = "Assets/Prefabs/Enemy (2) - Interceptor.prefab";
    private const string PREFAB_3 = "Assets/Prefabs/Enemy (3) - Squad Leader.prefab";
    private const string PREFAB_4 = "Assets/Prefabs/Enemy (4) - Shield Bearer.prefab";

    private const string BACKUP_DIR = "Assets/Prefabs/Backup_Enemy_Before_Fix";

    private const string LASER_PREFAB_PATH = "Assets/Prefabs/Laser.prefab";
    private const string PURPLE_THRUSTER_PATH = "Assets/Prefabs/Enemy_Thruster_Purple.prefab";

    // HiRez Grey Materials
    private const string HIREZ_COCKPIT_INT = "Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Grey/Cockpit3Grey_interior.mat";
    private const string HIREZ_COCKPIT_GLASS = "Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Grey/Cockpit3Grey_Glass.mat";
    private const string HIREZ_COCKPIT = "Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Grey/Cockpit3Grey.mat";
    private const string HIREZ_ENGINE = "Assets/HiRezSpaceshipsCreatorFree/Materials/Engines/Grey/Engine1.mat";
    private const string HIREZ_BODY13 = "Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Grey/MainBody13.mat";
    private const string HIREZ_BODY2 = "Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Grey/MainBody2.mat";
    private const string HIREZ_WING2 = "Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Grey/Wing2.mat";
    private const string HIREZ_WING6 = "Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Grey/Wing6.mat";

    static VirtualRailEnemyFixer()
    {
        // Đã tắt tự động chạy ngầm khi compile để bảo vệ 100% tọa độ do người dùng đặt tay trên Prefab!
        // Chỉ chạy khi người dùng chủ động click menu: Tools > Virtual Rail > 53. Auto-Calibrate...
    }

    [MenuItem("Tools/Virtual Rail/53. Auto-Calibrate and Fix All Enemy Prefabs (ACTIVE)")]
    public static void FixAllEnemyPrefabs()
    {
        // 1. Sao lưu 5 prefab trước khi sửa đổi (Tuân thủ Điều 3 Tầng 2)
        BackupPrefabs();

        // 2. Cân chỉnh từng loại kẻ địch
        FixEnemy0Scout();
        FixEnemy1Flanker();
        FixEnemy2Interceptor();
        FixEnemy3SquadLeader();
        FixEnemy4ShieldBearer();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[VirtualRail] ĐÃ TỰ ĐỘNG CÂN CHỈNH VÀ SỬA XONG TOÀN BỘ 5 KẺ ĐỊCH!</b></color>");
    }

    [MenuItem("Tools/Virtual Rail/54. Undo Enemy Prefab Calibration (OFF - Legacy)")]
    public static void UndoEnemyCalibration()
    {
        if (!Directory.Exists(BACKUP_DIR))
        {
            Debug.LogWarning("[VirtualRail] Không tìm thấy thư mục sao lưu: " + BACKUP_DIR);
            return;
        }

        string[] prefabs = { PREFAB_0, PREFAB_1, PREFAB_2, PREFAB_3, PREFAB_4 };
        foreach (string path in prefabs)
        {
            string fileName = Path.GetFileName(path);
            string backupFile = Path.Combine(BACKUP_DIR, fileName);
            if (File.Exists(backupFile))
            {
                File.Copy(backupFile, path, true);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=yellow><b>[VirtualRail] ĐÃ HOÀN NGUYÊN (UNDO) 5 PREFAB KẺ ĐỊCH VỀ NGUYÊN BẢN TRƯỚC CÂN CHỈNH!</b></color>");
    }

    private static void BackupPrefabs()
    {
        if (!Directory.Exists(BACKUP_DIR))
        {
            Directory.CreateDirectory(BACKUP_DIR);
        }

        string[] prefabs = { PREFAB_0, PREFAB_1, PREFAB_2, PREFAB_3, PREFAB_4 };
        foreach (string path in prefabs)
        {
            if (File.Exists(path))
            {
                string fileName = Path.GetFileName(path);
                string backupFile = Path.Combine(BACKUP_DIR, fileName);
                if (!File.Exists(backupFile))
                {
                    File.Copy(path, backupFile, false);
                }
            }
        }
    }

    private static void FixEnemy0Scout()
    {
        if (!File.Exists(PREFAB_0)) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_0);
        if (root == null) return;

        try
        {
            // 1. Đổi toàn bộ vật liệu đỏ của Example3_Red sang xám quân sự HiRez
            ConvertRedMaterialsToGrey(root);

            // 2. Thêm và cân chỉnh BoxCollider vừa khít thân tàu
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(4.4f, 1.6f, 4.8f);
            box.center = Vector3.zero;

            // 3. Sửa nòng súng Laser và nối vào EnemyAttack
            Laser leftLaser = EnsureLaserMuzzle(root.transform, "Left Laser", new Vector3(-2.1f, 0f, 0.3f), "[Muzzle_L]");
            Laser rightLaser = EnsureLaserMuzzle(root.transform, "Right Laser", new Vector3(2.1f, 0f, 0.3f), "[Muzzle_R]");

            EnemyAttack attack = root.GetComponent<EnemyAttack>();
            if (attack != null && leftLaser != null && rightLaser != null)
            {
                WireLasers(attack, leftLaser, rightLaser);
            }

            // 4. Gắn vệt đuôi động cơ tím khớp ống xả
            EnsurePurpleThruster(root.transform, "Enemy_Thruster_Purple", new Vector3(0f, 0.05f, -2.1f));

            // 5. Gắn Tag Enemy cho mọi collider con
            TagAllCollidersAsEnemy(root);

            // 6. Gắn bộ điều phối dải booster tím
            EnsureBoosterTrail(root);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_0);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FixEnemy1Flanker()
    {
        if (!File.Exists(PREFAB_1)) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_1);
        if (root == null) return;

        try
        {
            // 1. Thêm và cân chỉnh BoxCollider vừa khít StarSparrow5 / Example5
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(4.0f, 1.6f, 5.0f);
            box.center = Vector3.zero;

            // 2. Sửa nòng súng Laser và nối vào EnemyAttack
            Laser leftLaser = EnsureLaserMuzzle(root.transform, "Left Laser", new Vector3(-1.8f, 0f, 0.3f), "[Muzzle_L]");
            Laser rightLaser = EnsureLaserMuzzle(root.transform, "Right Laser", new Vector3(1.8f, 0f, 0.3f), "[Muzzle_R]");

            EnemyAttack attack = root.GetComponent<EnemyAttack>();
            if (attack != null && leftLaser != null && rightLaser != null)
            {
                WireLasers(attack, leftLaser, rightLaser);
            }

            // 3. Gắn vệt đuôi động cơ tím khớp ống xả
            EnsurePurpleThruster(root.transform, "Enemy_Thruster_Purple", new Vector3(0f, 0.1f, -2.4f));

            // 4. Gắn Tag Enemy cho mọi collider con
            TagAllCollidersAsEnemy(root);

            // 5. Gắn bộ điều phối dải booster tím
            EnsureBoosterTrail(root);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_1);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FixEnemy2Interceptor()
    {
        if (!File.Exists(PREFAB_2)) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_2);
        if (root == null) return;

        try
        {
            // 1. Thêm và cân chỉnh BoxCollider vừa khít Example4
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(4.2f, 1.6f, 4.8f);
            box.center = Vector3.zero;

            // 2. Sửa nòng súng Laser và nối vào EnemyAttack
            Laser leftLaser = EnsureLaserMuzzle(root.transform, "Left Laser", new Vector3(-2.0f, 0f, 0.4f), "[Muzzle_L]");
            Laser rightLaser = EnsureLaserMuzzle(root.transform, "Right Laser", new Vector3(2.0f, 0f, 0.4f), "[Muzzle_R]");

            EnemyAttack attack = root.GetComponent<EnemyAttack>();
            if (attack != null && leftLaser != null && rightLaser != null)
            {
                WireLasers(attack, leftLaser, rightLaser);
            }

            // 3. Gắn vệt đuôi động cơ tím khớp ống xả
            EnsurePurpleThruster(root.transform, "Enemy_Thruster_Purple", new Vector3(0f, 0.1f, -2.5f));

            // 4. Gắn Tag Enemy cho mọi collider con
            TagAllCollidersAsEnemy(root);

            // 5. Gắn bộ điều phối dải booster tím
            EnsureBoosterTrail(root);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_2);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FixEnemy3SquadLeader()
    {
        if (!File.Exists(PREFAB_3)) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_3);
        if (root == null) return;

        try
        {
            // 1. Phóng to thân tàu 1.35x thể hiện dáng vóc Tàu Chỉ Huy
            Transform visual = root.transform.Find("Example2_Grey");
            if (visual == null) visual = root.transform.Find("[Visual_Model]");
            if (visual != null)
            {
                visual.localScale = Vector3.one * 1.35f;
            }

            // 2. Thu nhỏ BoxCollider từ 7.5x7.0 (của Wrecker cũ) xuống 4.8x5.0 (vừa khít thân tàu)
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(4.8f, 1.8f, 5.0f);
            box.center = Vector3.zero;

            // 3. Đảm bảo 2 động cơ tím (Bảo vệ tọa độ đặt tay của người dùng nếu đã có)
            EnsurePurpleThruster(root.transform, "Enemy_Thruster_Purple_0", new Vector3(-0.95f, 0.2f, -2.25f));
            EnsurePurpleThruster(root.transform, "Enemy_Thruster_Purple_1", new Vector3(0.95f, 0.2f, -2.25f));

            // 4. Gắn Tag Enemy cho mọi collider con
            TagAllCollidersAsEnemy(root);

            // 5. Gắn bộ điều phối dải booster tím
            EnsureBoosterTrail(root);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_3);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FixEnemy4ShieldBearer()
    {
        if (!File.Exists(PREFAB_4)) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_4);
        if (root == null) return;

        try
        {
            // 1. Cân chỉnh BoxCollider bao trọn các cánh Wing2/Wing6 mới ghép
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(4.8f, 1.8f, 5.2f);
            box.center = Vector3.zero;

            // 2. Đảm bảo ống xả tím (Bảo vệ tọa độ đặt tay nếu đã có)
            EnsurePurpleThruster(root.transform, "Enemy_Thruster_Purple", new Vector3(0f, 0.1f, -2.4f));

            // 3. Gắn Tag Enemy cho mọi collider con
            TagAllCollidersAsEnemy(root);

            // 4. Gắn bộ điều phối dải booster tím
            EnsureBoosterTrail(root);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_4);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConvertRedMaterialsToGrey(GameObject root)
    {
        Material matCockpitInt = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_COCKPIT_INT);
        Material matCockpitGlass = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_COCKPIT_GLASS);
        Material matCockpit = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_COCKPIT);
        Material matEngine = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_ENGINE);
        Material matBody13 = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_BODY13);
        Material matBody2 = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_BODY2);
        Material matWing2 = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_WING2);
        Material matWing6 = AssetDatabase.LoadAssetAtPath<Material>(HIREZ_WING6);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            var mats = rend.sharedMaterials;
            bool modified = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string matName = mats[i].name;

                if (matName.Contains("Cockpit3Red_interior") && matCockpitInt != null)
                {
                    mats[i] = matCockpitInt; modified = true;
                }
                else if (matName.Contains("Cockpit3Red_Glass") && matCockpitGlass != null)
                {
                    mats[i] = matCockpitGlass; modified = true;
                }
                else if (matName.Contains("Cockpit3Red") && matCockpit != null)
                {
                    mats[i] = matCockpit; modified = true;
                }
                else if (matName.Contains("Engine1") && matName.Contains("Red") && matEngine != null)
                {
                    mats[i] = matEngine; modified = true;
                }
                else if (matName.Contains("MainBody13") && matBody13 != null)
                {
                    mats[i] = matBody13; modified = true;
                }
                else if (matName.Contains("MainBody2") && matBody2 != null)
                {
                    mats[i] = matBody2; modified = true;
                }
                else if (matName.Contains("Wing2") && matName.Contains("Red") && matWing2 != null)
                {
                    mats[i] = matWing2; modified = true;
                }
                else if (matName.Contains("Wing6") && matName.Contains("Red") && matWing6 != null)
                {
                    mats[i] = matWing6; modified = true;
                }
                else if (matName.Contains("Red") && matBody13 != null)
                {
                    mats[i] = matBody13; modified = true;
                }
            }

            if (modified)
            {
                rend.sharedMaterials = mats;
                EditorUtility.SetDirty(rend);
            }
        }
    }

    private static Laser EnsureLaserMuzzle(Transform parent, string name, Vector3 localPos, string legacyName = null)
    {
        Transform existing = parent.Find(name);
        if (existing == null && !string.IsNullOrEmpty(legacyName))
        {
            existing = parent.Find(legacyName);
        }

        if (existing != null)
        {
            // Đảm bảo có component VirtualRailMuzzle sạch
            var muzz = existing.GetComponent<VirtualRail.VirtualRailMuzzle>();
            if (muzz == null) muzz = existing.gameObject.AddComponent<VirtualRail.VirtualRailMuzzle>();

            // Vô hiệu hóa LineRenderer và Light cũ nếu có
            LineRenderer lr = existing.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = false;
            Light lt = existing.GetComponent<Light>();
            if (lt != null) lt.enabled = false;

            Laser laser = existing.GetComponent<Laser>();
            // BẢO VỆ TỌA ĐỘ ĐẶT TAY CỦA NGƯỜI DÙNG: Tuyệt đối không ghi đè localPosition/localRotation
            existing.name = name;
            existing.tag = "Laser";
            return laser;
        }
        else
        {
            GameObject muzzle = new GameObject(name);
            muzzle.tag = "Laser";
            muzzle.transform.SetParent(parent, false);
            muzzle.transform.localPosition = localPos;
            muzzle.transform.localRotation = Quaternion.identity;
            muzzle.AddComponent<VirtualRail.VirtualRailMuzzle>();
            Laser laser = muzzle.AddComponent<Laser>();
            return laser;
        }
    }

    private static void EnsurePurpleThruster(Transform parent, string name, Vector3 localPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            // BẢO VỆ TỌA ĐỘ ĐẶT TAY CỦA NGƯỜI DÙNG: Tuyệt đối không ghi đè localPosition/localRotation
            var th = existing.GetComponent<Thruster>();
            if (th == null) existing.gameObject.AddComponent<Thruster>();
            return;
        }

        GameObject thrusterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PURPLE_THRUSTER_PATH);
        if (thrusterPrefab != null)
        {
            GameObject thruster = (GameObject)PrefabUtility.InstantiatePrefab(thrusterPrefab, parent);
            thruster.name = name;
            thruster.transform.localPosition = localPos;
            thruster.transform.localRotation = Quaternion.identity;
        }
    }

    private static void WireLasers(EnemyAttack attack, Laser left, Laser right)
    {
        SerializedObject so = new SerializedObject(attack);

        SerializedProperty muzzlesProp = so.FindProperty("_muzzles");
        if (muzzlesProp != null)
        {
            muzzlesProp.arraySize = 2;
            var ml = left != null ? left.GetComponent<VirtualRail.VirtualRailMuzzle>() : null;
            var mr = right != null ? right.GetComponent<VirtualRail.VirtualRailMuzzle>() : null;
            if (ml != null) muzzlesProp.GetArrayElementAtIndex(0).objectReferenceValue = ml;
            if (mr != null) muzzlesProp.GetArrayElementAtIndex(1).objectReferenceValue = mr;
        }

        SerializedProperty lasersProp = so.FindProperty("_lasers");
        if (lasersProp != null)
        {
            lasersProp.arraySize = 2;
            lasersProp.GetArrayElementAtIndex(0).objectReferenceValue = left;
            lasersProp.GetArrayElementAtIndex(1).objectReferenceValue = right;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void TagAllCollidersAsEnemy(GameObject root)
    {
        root.tag = "Enemy";
        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.tag = "Enemy";
        }
    }

    private static void EnsureBoosterTrail(GameObject root)
    {
        var booster = root.GetComponent<VirtualRail.EnemyBoosterTrail>();
        if (booster == null) booster = root.AddComponent<VirtualRail.EnemyBoosterTrail>();
        booster.CacheComponents();
    }
}
