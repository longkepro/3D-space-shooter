using UnityEngine;
using UnityEditor;

public class FixAllPinkParticles
{
    [MenuItem("Tools/Fix ALL Pink Particles (Whole Pack)")]
    public static void DoFixAll()
    {
        string folderPath = "Assets/UnityTechnologies";
        
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        int fixedCount = 0;

        Shader additiveShader = Shader.Find("Mobile/Particles/Additive");
        Shader blendedShader = Shader.Find("Mobile/Particles/Alpha Blended");

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null)
            {
                string matName = mat.name.ToLower();
                Shader newShader = additiveShader; 

                if (matName.Contains("smoke") || matName.Contains("dust") || matName.Contains("debris") || matName.Contains("rock") || matName.Contains("ground"))
                {
                    newShader = blendedShader;
                }

                if (mat.shader != newShader)
                {
                    mat.shader = newShader;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Scanned {matGuids.Length} materials. Fixed {fixedCount} materials in Unity Particle Pack!");
    }
}
