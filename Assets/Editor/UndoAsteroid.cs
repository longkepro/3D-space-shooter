using UnityEngine;
using UnityEditor;

public class UndoAsteroid
{
    [MenuItem("Tools/Undo Asteroid")]
    public static void DoUndo()
    {
        string asteroidPrefabPath = "Assets/Prefabs/Asteroid (1).prefab";
        string originalAssetPath = "Assets/Asset Store/BrokenVector/LowPolyRockPack/Prefabs/Rock Type2 04.prefab";

        GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(asteroidPrefabPath);
        GameObject originalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(originalAssetPath);

        string assetPath = AssetDatabase.GetAssetPath(rootPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Remove all children
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(contents.transform.GetChild(i).gameObject);
        }

        // Add original asset back
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(originalAsset);
        inst.transform.SetParent(contents.transform, false);
        inst.transform.localPosition = Vector3.zero;

        // Restore Collider
        SphereCollider sc = contents.GetComponent<SphereCollider>();
        if (sc != null)
        {
            Object.DestroyImmediate(sc);
        }
        
        MeshCollider mc = contents.GetComponent<MeshCollider>();
        if (mc == null) 
        {
            mc = contents.AddComponent<MeshCollider>();
            mc.convex = true;
            // The original mesh GUID for the collider was 19497d68b37d2594fb3e269601d333be
            // We can grab it from the child's MeshFilter
            MeshFilter childMf = inst.GetComponentInChildren<MeshFilter>();
            if (childMf != null) {
                mc.sharedMesh = childMf.sharedMesh;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}
