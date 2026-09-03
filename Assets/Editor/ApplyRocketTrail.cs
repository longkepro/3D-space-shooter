using UnityEngine;
using UnityEditor;

public class ApplyRocketTrail
{
    [MenuItem("Tools/Apply Blue Rocket Trails")]
    public static void DoApply()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        string vfxPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/RocketTrail.prefab";

        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);

        if (shipPrefab == null || vfxPrefab == null)
        {
            Debug.LogError("Could not find prefabs.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        // Delete all old thrusters/fires
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = contents.transform.GetChild(i).gameObject;
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("thruster") || lowerName.Contains("fire") || lowerName.Contains("rocket"))
            {
                Object.DestroyImmediate(child);
            }
        }

        Color blueColor = new Color(0f, 0.6f, 1f, 1f); // Bright blue
        Color lightBlue = new Color(0.5f, 0.8f, 1f, 1f);

        void CreateThruster(string name, Vector3 pos, Vector3 scale)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
            inst.name = name;
            inst.transform.SetParent(contents.transform, false);
            inst.transform.localPosition = pos;
            inst.transform.localScale = scale;
            inst.transform.localRotation = Quaternion.identity; // default

            // Tint all fire-related particle systems blue
            ParticleSystem[] systems = inst.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in systems)
            {
                string psName = ps.gameObject.name.ToLower();
                var main = ps.main;

                // Color fire, embers, and twinkle to blue
                if (psName.Contains("fire") || psName.Contains("twinkle") || psName.Contains("ember") || psName.Contains("rocket"))
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(blueColor, lightBlue);
                }
                else if (psName.Contains("smoke"))
                {
                    // Slightly tint smoke blue-grey, or just leave it
                    // var c = main.startColor.color;
                    // main.startColor = new Color(0.2f, 0.3f, 0.4f, c.a);
                }
            }
        }

        // Apply 3 Thrusters using StarSparrow coordinates
        CreateThruster("Center Rocket", new Vector3(0f, 0f, -2.5f), new Vector3(1.0f, 1.0f, 1.0f));
        CreateThruster("Left Rocket", new Vector3(-1.2f, -0.4f, -2.0f), new Vector3(0.6f, 0.6f, 0.6f));
        CreateThruster("Right Rocket", new Vector3(1.2f, -0.4f, -2.0f), new Vector3(0.6f, 0.6f, 0.6f));

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Blue Rocket Trails applied successfully!");
    }
}
