using UnityEngine;
using UnityEditor;

public class UndoThrusters
{
    [MenuItem("Tools/Undo Thruster Changes")]
    public static void DoUndo()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string thrusterPath = "Assets/Prefabs/Thruster.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(thrusterPath);

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Delete all my added thrusters (Center, Left, Right, Magic fire)
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            if (child.name.Contains("Thruster") || child.name.Contains("fire"))
            {
                Object.DestroyImmediate(child);
            }
        }

        // Restore the original Main Thruster (Just one, default color)
        if (vfxPrefab != null)
        {
            GameObject main = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
            main.name = "Main Thruster";
            main.transform.SetParent(contents.transform, false);
            // Default position for StarSparrow center
            main.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            main.transform.localScale = Vector3.one;
        }

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Thruster changes undone! Restored to original Main Thruster.");
    }
}
