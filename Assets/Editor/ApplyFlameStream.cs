using UnityEngine;
using UnityEditor;

public class ApplyFlameStream
{
    private const string PrefX = "ThrusterBackup_X";
    private const string PrefY = "ThrusterBackup_Y";
    private const string PrefZ = "ThrusterBackup_Z";

    [MenuItem("Tools/1. Apply FlameStream (Safe)")]
    public static void DoApply()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string vfxPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/FlameStream.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);

        if (shipPrefab == null || vfxPrefab == null)
        {
            Debug.LogError("Could not find prefabs.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // 1. Save Coordinates and Delete Old
        Vector3 savedPos = new Vector3(0, 0, -2.5f);
        bool found = false;
        
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            if (child.name.Contains("Thruster") || child.name.Contains("Flame") || child.name.Contains("Rocket"))
            {
                if (!found)
                {
                    savedPos = child.transform.localPosition;
                    EditorPrefs.SetFloat(PrefX, savedPos.x);
                    EditorPrefs.SetFloat(PrefY, savedPos.y);
                    EditorPrefs.SetFloat(PrefZ, savedPos.z);
                    found = true;
                }
                Object.DestroyImmediate(child);
            }
        }

        if (!found && EditorPrefs.HasKey(PrefX)) 
        {
            savedPos = new Vector3(EditorPrefs.GetFloat(PrefX), EditorPrefs.GetFloat(PrefY), EditorPrefs.GetFloat(PrefZ));
        }

        // 2. Add FlameStream
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
        inst.name = "Main FlameStream";
        inst.transform.SetParent(contents.transform, false);
        inst.transform.localPosition = savedPos;
        
        // Scale down and rotate backwards
        inst.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        inst.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // 3. Tint Blue Sci-fi
        ParticleSystem[] pss = inst.GetComponentsInChildren<ParticleSystem>(true);
        Color blueColor = new Color(0f, 0.5f, 1f, 1f);
        Color cyanColor = new Color(0f, 1f, 1f, 1f);
        foreach (ParticleSystem ps in pss)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(blueColor, cyanColor);
        }

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("FlameStream applied successfully! Old coordinates saved to memory.");
    }

    [MenuItem("Tools/2. Restore Old Thruster")]
    public static void DoRestore()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string thrusterPath = "Assets/Prefabs/Thruster.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(thrusterPath);

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Delete FlameStream
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            if (child.name.Contains("Flame") || child.name.Contains("Thruster") || child.name.Contains("Rocket"))
            {
                Object.DestroyImmediate(child);
            }
        }

        // Retrieve Coordinates
        Vector3 pos = new Vector3(0, 0, -2.5f);
        if (EditorPrefs.HasKey(PrefX))
        {
            pos = new Vector3(EditorPrefs.GetFloat(PrefX), EditorPrefs.GetFloat(PrefY), EditorPrefs.GetFloat(PrefZ));
        }

        // Restore Original
        GameObject main = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
        main.name = "Main Thruster";
        main.transform.SetParent(contents.transform, false);
        main.transform.localPosition = pos;
        main.transform.localScale = Vector3.one;
        main.transform.localRotation = Quaternion.identity;

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Restored Old Thruster with exact saved coordinates!");
    }
}
