using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class VirtualRailEnemyLightingEditor
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string FrontFillLightName = "Enemy Front Fill Light";
    private const string LegacyFillLightName = "Fill Space Light";

    [MenuItem("Tools/Virtual Rail/43. Setup Enemy Front Fill Light (Option 1 - ON)")]
    public static void SetupFrontFillLightOnly()
    {
        EnsureGameSceneOpen();

        // 1. Cấu hình Front Fill Light (Chiếu thẳng vào mặt trước kẻ địch)
        ConfigureFrontFillLight();

        // 2. Tinh chỉnh Ambient Light trong Scene
        ConfigureAmbientLighting(true);

        SaveActiveScene();
        Debug.Log("<color=green>[Virtual Rail]</color> Đã bật: Enemy Front Fill Light (Intensity 1.25, góc chiếu đối diện kẻ địch) và nâng sáng Ambient!");
    }

    [MenuItem("Tools/Virtual Rail/44. Undo Enemy Front Fill Light (Option 1 - OFF)")]
    public static void UndoFrontFillLightOnly()
    {
        EnsureGameSceneOpen();

        RevertFrontFillLight();
        ConfigureAmbientLighting(false);

        SaveActiveScene();
        Debug.Log("<color=yellow>[Virtual Rail]</color> Đã hoàn nguyên Front Fill Light và Ambient Light về trạng thái ban đầu.");
    }

    [MenuItem("Tools/Virtual Rail/45. Setup Enemy Material Emission (Option 3 - ON)")]
    public static void SetupEnemyMaterialsEmissionOnly()
    {
        ConfigureEnemyMaterialsEmission(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[Virtual Rail]</color> Đã bật hiệu ứng phát quang Emission cho các vật liệu kẻ địch.");
    }

    [MenuItem("Tools/Virtual Rail/46. Undo Enemy Material Emission (Option 3 - OFF)")]
    public static void UndoEnemyMaterialsEmissionOnly()
    {
        ConfigureEnemyMaterialsEmission(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=yellow>[Virtual Rail]</color> Đã tắt hiệu ứng phát quang Emission trên các vật liệu kẻ địch.");
    }

    private static void EnsureGameSceneOpen()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != GameScenePath)
        {
            if (File.Exists(GameScenePath))
            {
                EditorSceneManager.OpenScene(GameScenePath);
            }
        }
    }

    private static void SaveActiveScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
    }

    private static void ConfigureFrontFillLight()
    {
        GameObject existingFill = GameObject.Find(LegacyFillLightName);
        if (existingFill != null)
        {
            existingFill.SetActive(false);
        }

        GameObject frontFillObj = GameObject.Find(FrontFillLightName);
        if (frontFillObj == null)
        {
            frontFillObj = new GameObject(FrontFillLightName);
        }

        Light frontLight = frontFillObj.GetComponent<Light>();
        if (frontLight == null)
        {
            frontLight = frontFillObj.AddComponent<Light>();
        }

        frontLight.type = LightType.Directional;
        frontLight.color = new Color(0.82f, 0.90f, 1.0f);
        frontLight.intensity = 0.55f; // Mức 1: Soft Fill điện ảnh vừa vặn
        frontLight.shadows = LightShadows.None;

        // Chiếu lệch góc chéo trên-trái (X=25, Y=-35) để tạo bóng khối 3D sắc nét
        frontFillObj.transform.rotation = Quaternion.Euler(25f, -35f, 0f);
        frontFillObj.transform.position = new Vector3(0, 10f, 0);
    }

    private static void RevertFrontFillLight()
    {
        GameObject frontFillObj = GameObject.Find(FrontFillLightName);
        if (frontFillObj != null)
        {
            Object.DestroyImmediate(frontFillObj);
        }

        GameObject legacyFill = GameObject.Find(LegacyFillLightName);
        if (legacyFill != null)
        {
            legacyFill.SetActive(true);
            Light l = legacyFill.GetComponent<Light>();
            if (l != null)
            {
                l.intensity = 0.25f;
            }
        }
    }

    private static void ConfigureAmbientLighting(bool enhanced)
    {
        if (enhanced)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.28f, 0.33f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.10f);
            RenderSettings.ambientIntensity = 1.0f;
        }
        else
        {
            RenderSettings.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
            RenderSettings.ambientIntensity = 1.0f;
        }
    }

    private static void ConfigureEnemyMaterialsEmission(bool enableEmission)
    {
        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Red/MainBody2.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/MainBodies/MainBody2/Masks/MainBody2_Mask1.png",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Grey/MainBody2.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/MainBodies/MainBody2/Masks/MainBody2_Mask1.png",
            new Color(1.8f, 0.9f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Red/MainBody13.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/MainBodies/MainBody13/Masks/MainBody13_Mask1.png",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Grey/MainBody13.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/MainBodies/MainBody13/Masks/MainBody13_Mask1.png",
            new Color(1.8f, 0.9f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Red/Wing2.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Wings/Wing2/Masks/Wing2_Mask1.png",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Grey/Wing2.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Wings/Wing2/Masks/Wing2_Mask1.png",
            new Color(1.8f, 0.9f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Red/Wing6.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Wings/Wing6/Masks/Wing6_Mask1.png",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Grey/Wing6.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Wings/Wing6/Masks/Wing6_Mask1.png",
            new Color(1.8f, 0.9f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Weapons/BigLauncher.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Weapons/BigLauncher/Masks/BigLauncher_Mask1.png",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Weapons/Blaster.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Weapons/Blaster/Masks/Blaster_Mask1.png",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Red/Cockpit3Red.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Cockpits/Cockpit3/Cockpit3_Emission.png",
            new Color(2.0f, 0.2f, 0.2f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Grey/Cockpit3Grey.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Cockpits/Cockpit3/Cockpit3_Emission.png",
            new Color(0.2f, 1.5f, 2.0f), enableEmission);

        SetupHiRezMaterial("Assets/HiRezSpaceshipsCreatorFree/Materials/Engines/Thrusters/Thrusters1.mat",
            "Assets/HiRezSpaceshipsCreatorFree/Textures/Engines/Thrusters/Thrusters_Emission1.png",
            new Color(2.5f, 1.0f, 0.2f), enableEmission);

        SetupStarSparrowMaterial("Assets/StarSparrow/Materials/StarSparrow_Red.mat",
            new Color(2.0f, 0.3f, 0.15f), enableEmission);

        SetupStarSparrowMaterial("Assets/StarSparrow/Materials/StarSparrow_Black.mat",
            new Color(2.0f, 0.2f, 0.1f), enableEmission);
    }

    private static void SetupHiRezMaterial(string matPath, string maskTexPath, Color glowColor, bool enable)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) return;

        if (enable)
        {
            mat.EnableKeyword("_EMISSION");
            Texture2D maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(maskTexPath);
            if (maskTex != null)
            {
                mat.SetTexture("_EmissionMap", maskTex);
            }
            mat.SetColor("_EmissionColor", glowColor);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", null);
            mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
    }

    private static void SetupStarSparrowMaterial(string matPath, Color glowColor, bool enable)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) return;

        if (enable)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor);
        }
        else
        {
            mat.SetColor("_EmissionColor", Color.white);
        }
        EditorUtility.SetDirty(mat);
    }
}
