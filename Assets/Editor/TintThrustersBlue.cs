using UnityEngine;
using UnityEditor;

public class TintThrustersBlue
{
    [MenuItem("Tools/Tint 3 Thrusters Blue")]
    public static void DoTint()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        
        if (shipPrefab == null)
        {
            Debug.LogError("Could not find Player Ship prefab.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        Color cyanColor = new Color(0f, 0.9f, 1f, 1f);   
        Color blueColor = new Color(0f, 0.4f, 1f, 1f);   
        Color darkBlue = new Color(0f, 0.1f, 0.8f, 1f);

        int count = 0;

        for (int i = 0; i < contents.transform.childCount; i++)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            if (child.name.Contains("Thruster"))
            {
                count++;
                
                // 1. Update Particle System
                ParticleSystem ps = child.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(cyanColor, blueColor);
                    
                    var col = ps.colorOverLifetime;
                    if (col.enabled)
                    {
                        Gradient grad = new Gradient();
                        grad.SetKeys(
                            new GradientColorKey[] { new GradientColorKey(cyanColor, 0.0f), new GradientColorKey(darkBlue, 1.0f) },
                            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                        );
                        col.color = new ParticleSystem.MinMaxGradient(grad);
                    }
                }

                // 2. Update Trail Renderer (V?t sáng duôi)
                TrailRenderer tr = child.GetComponent<TrailRenderer>();
                if (tr != null)
                {
                    tr.startColor = cyanColor;
                    tr.endColor = new Color(0f, 0f, 1f, 0f);
                    
                    Gradient grad = new Gradient();
                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(cyanColor, 0.0f), new GradientColorKey(darkBlue, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                    );
                    tr.colorGradient = grad;
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log($"Successfully tinted {count} thrusters to Blue!");
    }
}
