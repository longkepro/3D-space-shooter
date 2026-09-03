using UnityEngine;
using UnityEditor;

public class ReplaceAsteroid
{
    [MenuItem("Tools/Replace Asteroid (Lava Blue)")]
    public static void DoReplace()
    {
        string asteroidPrefabPath = "Assets/Prefabs/Asteroid (1).prefab";
        string newAssetPath = "Assets/FreeSciFi/Prefabs/PlanetAsteroids/Asteroid Lava Blue.prefab"; 

        GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(asteroidPrefabPath);
        GameObject newAsset = AssetDatabase.LoadAssetAtPath<GameObject>(newAssetPath);

        if (rootPrefab == null || newAsset == null)
        {
            Debug.LogError("Could not find prefabs.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(rootPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Remove all old graphics
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(contents.transform.GetChild(i).gameObject);
        }

        // Add new asset
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(newAsset);
        inst.transform.SetParent(contents.transform, false);
        inst.transform.localPosition = Vector3.zero;

        // Optimize Collider
        MeshCollider mc = contents.GetComponent<MeshCollider>();
        if (mc != null)
        {
            Object.DestroyImmediate(mc);
        }
        
        SphereCollider sc = contents.GetComponent<SphereCollider>();
        if (sc == null) sc = contents.AddComponent<SphereCollider>();
        sc.radius = 1.0f; // Safe radius for Lava Asteroid

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Replaced Asteroid with Lava Blue Asteroid from FreeSciFi successfully!");
    }
}
