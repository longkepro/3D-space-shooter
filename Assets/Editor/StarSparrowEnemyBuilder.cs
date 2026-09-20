using UnityEditor;
using UnityEngine;
using VirtualRail;

/// <summary>
/// Công cụ tự động tạo 3 Prefab độc lập StarSparrow cho hệ thống kẻ địch:
/// - VirtualRail_Enemy_StarSparrow1.prefab (Scout / Flanker)
/// - VirtualRail_Enemy_StarSparrow4.prefab (SquadLeader / Heavy Flanker)
/// - VirtualRail_Enemy_StarSparrow6.prefab (Interceptor)
/// Tuân thủ quy tắc 3 tầng an toàn: Giữ nguyên vẹn 100% prefab gốc của dự án.
/// </summary>
[InitializeOnLoad]
public static class StarSparrowEnemyBuilder
{
    public const string PREFAB_SS1_PATH = "Assets/Prefabs/VirtualRail_Enemy_StarSparrow1.prefab";
    public const string PREFAB_SS4_PATH = "Assets/Prefabs/VirtualRail_Enemy_StarSparrow4.prefab";
    public const string PREFAB_SS6_PATH = "Assets/Prefabs/VirtualRail_Enemy_StarSparrow6.prefab";

    public const string SOURCE_SS1_PATH = "Assets/StarSparrow/Prefabs/Examples/StarSparrow1.prefab";
    public const string SOURCE_SS4_PATH = "Assets/StarSparrow/Prefabs/Examples/StarSparrow4.prefab";
    public const string SOURCE_SS6_PATH = "Assets/StarSparrow/Prefabs/Examples/StarSparrow6.prefab";

    static StarSparrowEnemyBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(PREFAB_SS1_PATH) ||
                !System.IO.File.Exists(PREFAB_SS4_PATH) ||
                !System.IO.File.Exists(PREFAB_SS6_PATH))
            {
                BuildAllStarSparrowPrefabs();
            }
        };
    }

    [MenuItem("Tools/Virtual Rail/Build StarSparrow Enemy Prefabs")]
    public static void BuildAllStarSparrowPrefabs()
        {
            BuildSinglePrefab(SOURCE_SS1_PATH, PREFAB_SS1_PATH, "VirtualRail_Enemy_StarSparrow1", EnemyArchetype.Scout, new Vector3(-1.6f, 0f, 0.5f), new Vector3(1.6f, 0f, 0.5f));
            BuildSinglePrefab(SOURCE_SS4_PATH, PREFAB_SS4_PATH, "VirtualRail_Enemy_StarSparrow4", EnemyArchetype.SquadLeader, new Vector3(-2.0f, 0f, 1.0f), new Vector3(2.0f, 0f, 1.0f));
            BuildSinglePrefab(SOURCE_SS6_PATH, PREFAB_SS6_PATH, "VirtualRail_Enemy_StarSparrow6", EnemyArchetype.Interceptor, new Vector3(-1.2f, 0f, 0.8f), new Vector3(1.2f, 0f, 0.8f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green><b>[VirtualRail] ĐÃ TẠO THÀNH CÔNG 3 PREFAB CHIẾN HẠM STARSPARROW MỚI ĐỘC LẬP!</b></color>");
        }

        public static GameObject BuildSinglePrefab(string sourceModelPath, string targetPrefabPath, string rootName, EnemyArchetype archetype, Vector3 muzzleLeftPos, Vector3 muzzleRightPos)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"[StarSparrowEnemyBuilder] Không tìm thấy source prefab: {sourceModelPath}");
                return null;
            }

            GameObject enemyRoot = new GameObject(rootName);
            enemyRoot.tag = "Enemy";

            // 1. Nhúng Model 3D StarSparrow làm đối tượng con
            GameObject visual = Object.Instantiate(sourcePrefab, enemyRoot.transform);
            visual.name = "[Visual_Model]";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Đảm bảo tất cả Collider con đều được gắn Tag "Enemy" để Laser và Bullet Magnetism bắt trúng
            var colliders = enemyRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].gameObject.tag = "Enemy";
            }

            // Nếu chưa có Collider nào thì tự thêm 1 BoxCollider bao quanh
            if (colliders.Length == 0)
            {
                var box = enemyRoot.AddComponent<BoxCollider>();
                box.size = new Vector3(4f, 1.5f, 6f);
            }

            // 2. Tạo 2 nòng súng Laser
            GameObject muzzleL = new GameObject("[Muzzle_L]");
            muzzleL.transform.SetParent(enemyRoot.transform, false);
            muzzleL.transform.localPosition = muzzleLeftPos;

            GameObject muzzleR = new GameObject("[Muzzle_R]");
            muzzleR.transform.SetParent(enemyRoot.transform, false);
            muzzleR.transform.localPosition = muzzleRightPos;

            // 3. Gắn các Component Logic cốt lõi
            EnemyMovement movement = enemyRoot.AddComponent<EnemyMovement>();
            EnemyHealth health = enemyRoot.AddComponent<EnemyHealth>();
            EnemyAttack attack = enemyRoot.AddComponent<EnemyAttack>();
            Explosion explosion = enemyRoot.AddComponent<Explosion>();
            enemyRoot.AddComponent<EnemyLifecycleFSM>();
            enemyRoot.AddComponent<FormationWingman>();

            // Gán dữ liệu Archetype
            var preset = EnemyData.CreateDefaultPreset(archetype);
            movement.ApplyArchetype(preset);

            // Nối Muzzles vào EnemyAttack qua SerializedObject
            SerializedObject attackSo = new SerializedObject(attack);
            SerializedProperty lasersProp = attackSo.FindProperty("_lasers");
            if (lasersProp != null)
            {
                lasersProp.arraySize = 2;
                lasersProp.GetArrayElementAtIndex(0).objectReferenceValue = muzzleL.transform;
                lasersProp.GetArrayElementAtIndex(1).objectReferenceValue = muzzleR.transform;
                attackSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Nối VFX Nổ vào Explosion qua SerializedObject
            GameObject blowUpVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Magic Hit.prefab");
            if (blowUpVfx == null)
            {
                blowUpVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX_ElectricityBall.prefab");
            }
            if (blowUpVfx != null)
            {
                SerializedObject expSo = new SerializedObject(explosion);
                SerializedProperty blowUpProp = expSo.FindProperty("_blowUp");
                if (blowUpProp != null)
                {
                    blowUpProp.objectReferenceValue = blowUpVfx;
                    expSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 4. Lưu thành Prefab độc lập (Không ghi đè prefab cũ)
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(enemyRoot, targetPrefabPath);
            Object.DestroyImmediate(enemyRoot);

            return savedPrefab;
        }
    }
