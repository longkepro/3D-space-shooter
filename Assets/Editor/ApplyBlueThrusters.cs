using UnityEngine;
using UnityEditor;

public class ApplyBlueThrusters
{
    [MenuItem("Tools/Apply Blue Thrusters")]
    public static void DoApply()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string vfxPath = "Assets/Hovl Studio/Procedural fire/Prefabs/Magic fire pro blue.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);

        if (shipPrefab == null || vfxPrefab == null)
        {
            Debug.LogError("Could not find prefabs.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Delete old thrusters
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            if (child.name.Contains("Thruster") || child.name.Contains("fire"))
            {
                Object.DestroyImmediate(child);
            }
        }

        // Center Thruster
        GameObject center = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
        center.name = "Center Thruster";
        center.transform.SetParent(contents.transform, false);
        center.transform.localPosition = new Vector3(0f, 0f, -2.5f);
        center.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        // Left Thruster
        GameObject left = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
        left.name = "Left Thruster";
        left.transform.SetParent(contents.transform, false);
        left.transform.localPosition = new Vector3(-1.2f, -0.4f, -2.0f);
        left.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        // Right Thruster
        GameObject right = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
        right.name = "Right Thruster";
        right.transform.SetParent(contents.transform, false);
        right.transform.localPosition = new Vector3(1.2f, -0.4f, -2.0f);
        right.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Blue Thrusters applied successfully!");
    }
}
