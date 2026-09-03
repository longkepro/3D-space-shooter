using UnityEngine;
using UnityEditor;

public class UndoRocketTrail
{
    [MenuItem("Tools/Undo Rocket Trail")]
    public static void DoUndo()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string thrusterPath = "Assets/Prefabs/Thruster.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(thrusterPath);

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Delete all added rockets/thrusters
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("rocket") || lowerName.Contains("thruster") || lowerName.Contains("fire"))
            {
                Object.DestroyImmediate(child);
            }
        }

        // Restore the single original Main Thruster
        if (vfxPrefab != null)
        {
            GameObject main = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
            main.name = "Main Thruster";
            main.transform.SetParent(contents.transform, false);
            main.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            main.transform.localScale = Vector3.one;
        }

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Rocket Trails removed and original Thruster restored!");
    }
}
