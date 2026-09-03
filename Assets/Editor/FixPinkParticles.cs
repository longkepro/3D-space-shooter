using UnityEngine;
using UnityEditor;

public class FixPinkParticles
{
    [MenuItem("Tools/Fix Pink Particles (Rocket Trail)")]
    public static void DoFix()
    {
        string vfxPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/RocketTrail.prefab";
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);

        if (vfxPrefab == null)
        {
            Debug.LogError("Could not find RocketTrail prefab.");
            return;
        }

        // We load the prefab contents to modify its materials if needed, but actually we should just modify the material assets directly.
        ParticleSystemRenderer[] renderers = vfxPrefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
        
        int fixedCount = 0;
        foreach (ParticleSystemRenderer psr in renderers)
        {
            Material mat = psr.sharedMaterial;
            if (mat != null)
            {
                string matName = mat.name.ToLower();
                Shader newShader = null;

                // Determine additive (fire, sparks, glow) vs blended (smoke)
                if (matName.Contains("smoke") || matName.Contains("dust"))
                {
                    newShader = Shader.Find("Mobile/Particles/Alpha Blended");
                }
                else
                {
                    newShader = Shader.Find("Mobile/Particles/Additive");
                }

                if (newShader != null && mat.shader != newShader)
                {
                    mat.shader = newShader;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Fixed {fixedCount} materials to remove pink errors!");
    }
}
