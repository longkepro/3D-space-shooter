using UnityEngine;
using UnityEditor;

public class FixThrusters
{
    [MenuItem("Tools/Fix and Recolor Thrusters (Blue)")]
    public static void DoFix()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string thrusterPath = "Assets/Prefabs/Thruster.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(thrusterPath);

        if (shipPrefab == null || vfxPrefab == null)
        {
            Debug.LogError("Could not find prefabs.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Delete campfire thrusters
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            if (child.name.Contains("Thruster") || child.name.Contains("fire"))
            {
                Object.DestroyImmediate(child);
            }
        }

        // Color for new thrusters
        Color blueColor = new Color(0f, 0.5f, 1f, 1f);

        // Helper function
        void CreateThruster(string name, Vector3 pos, Vector3 scale)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
            inst.name = name;
            inst.transform.SetParent(contents.transform, false);
            inst.transform.localPosition = pos;
            inst.transform.localScale = scale;
            
            // It's a jet pointing down usually? Or pointing back?
            // Usually thruster z goes backwards. We can leave original rotation of Thruster.prefab
            
            ParticleSystem ps = inst.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = blueColor;
            }
        }

        // Apply 3 Thrusters
        // Coordinates for StarSparrow1 rear nozzles
        CreateThruster("Center Thruster", new Vector3(0f, 0f, -2.5f), new Vector3(1.5f, 1.5f, 1.5f));
        CreateThruster("Left Thruster", new Vector3(-1.2f, -0.4f, -2.0f), new Vector3(0.8f, 0.8f, 0.8f));
        CreateThruster("Right Thruster", new Vector3(1.2f, -0.4f, -2.0f), new Vector3(0.8f, 0.8f, 0.8f));

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Sci-fi Blue Jet Thrusters applied successfully!");
    }
}
