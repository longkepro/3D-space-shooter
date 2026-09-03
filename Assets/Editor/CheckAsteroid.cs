using UnityEngine;
using UnityEditor;

public class CheckAsteroid
{
    [MenuItem("Tools/Check Asteroid")]
    public static void DoCheck()
    {
        string path = "Assets/Prefabs/Asteroid (1).prefab";
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null)
        {
            string output = "Root: " + go.name + "\n";
            foreach (Transform child in go.transform)
            {
                output += "- Child: " + child.name;
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) output += " (Mesh: " + mf.sharedMesh.name + ")";
                output += "\n";
            }
            Debug.Log(output);
        }
    }
}
