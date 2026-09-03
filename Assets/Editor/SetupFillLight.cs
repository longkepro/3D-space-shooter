using UnityEngine;
using UnityEditor;

public class SetupFillLight
{
    [MenuItem("Tools/Setup Fill Light (Solution 2)")]
    public static void DoSetup()
    {
        // Find existing Sun Light
        Light[] allLights = Object.FindObjectsOfType<Light>();
        Light mainSun = null;
        foreach(Light l in allLights)
        {
            if(l.type == LightType.Directional && (l.name == "Sun Light" || l.name == "Directional Light"))
            {
                mainSun = l;
                break;
            }
        }

        // Clean up if already exists
        GameObject existingFill = GameObject.Find("Fill Space Light");
        if(existingFill != null)
        {
            Object.DestroyImmediate(existingFill);
        }

        // Create new Fill Light
        GameObject fillLightObj = new GameObject("Fill Space Light");
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.25f; // Soft intensity
        fillLight.color = new Color(0.6f, 0.7f, 0.9f); // Subtle blue tint for space
        fillLight.shadows = LightShadows.None; // Important: Fill lights shouldn't cast extra shadows

        // Set rotation opposite to main sun
        if (mainSun != null)
        {
            Vector3 sunRot = mainSun.transform.rotation.eulerAngles;
            // Opposite direction
            fillLightObj.transform.rotation = Quaternion.Euler(-sunRot.x, sunRot.y + 180f, 0);
            
            // Put it right under Sun Light in hierarchy
            fillLightObj.transform.SetSiblingIndex(mainSun.transform.GetSiblingIndex() + 1);
        }
        else
        {
            fillLightObj.transform.rotation = Quaternion.Euler(-45f, 180f, 0f);
        }

        // Mark scene as modified
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("Fill Light (Solution 2) successfully added!");
    }
}
