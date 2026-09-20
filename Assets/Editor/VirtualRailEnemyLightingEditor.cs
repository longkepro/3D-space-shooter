using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class VirtualRailEnemyLightingEditor
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string FrontFillLightName = "Enemy Front Fill Light";
    private const string LegacyFillLightName = "Fill Space Light";
    private const string ArcadeOutlineShaderName = "VirtualRail/ArcadeHullOutlineStandard";
    private const string StandardShaderName = "Standard";
    private const string OriginalThrusterGuid = "090a69de2d30791479ce0e552e8b8fa7";
    private const string PurpleThrusterGuid = "8dcbc56eeb8b40c28293ffc3795eacef";
    private const string ThrustersMaterialPath = "Assets/HiRezSpaceshipsCreatorFree/Materials/Engines/Thrusters/Thrusters1.mat";

    private static readonly string[] EnemyPrefabsWithThrusters = new string[]
    {
        "Assets/Prefabs/Enemy (0).prefab",
        "Assets/Prefabs/Enemy (1).prefab"
    };

    private static readonly string[] HiRezMaterials = new string[]
    {
        "Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Grey/MainBody2.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Red/MainBody2.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Grey/MainBody13.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/MainBodies/Red/MainBody13.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Grey/Wing2.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Red/Wing2.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Grey/Wing6.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Wings/Red/Wing6.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Weapons/BigLauncher.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Weapons/Blaster.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Grey/Cockpit3Grey.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Cockpits/Red/Cockpit3Red.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Engines/Grey/Engine1.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Engines/Red/Engine1.mat",
        "Assets/HiRezSpaceshipsCreatorFree/Materials/Engines/Thrusters/Thrusters1.mat"
    };

    [MenuItem("Tools/Virtual Rail/43. Setup Enemy Front Fill Light (Option 1 - ON)")]
    public static void SetupFrontFillLightOnly()
    {
        EnsureGameSceneOpen();

        // 1. Cấu hình Front Fill Light (Chiếu thẳng vào mặt trước kẻ địch)
        ConfigureFrontFillLight();

        // 2. Tinh chỉnh Ambient Light trong Scene
        ConfigureAmbientLighting(true);

        SaveActiveScene();
        Debug.Log("<color=green>[Virtual Rail]</color> Đã bật: Enemy Front Fill Light (Intensity 0.55, góc chiếu đối diện kẻ địch) và nâng sáng Ambient!");
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

    [MenuItem("Tools/Virtual Rail/47. Setup Enemy Arcade Outline (Direction 2 - ON)")]
    public static void SetupEnemyArcadeOutline()
    {
        Shader outlineShader = Shader.Find(ArcadeOutlineShaderName);
        if (outlineShader == null)
        {
            Debug.LogError($"[Virtual Rail] Không tìm thấy Shader: {ArcadeOutlineShaderName}!");
            return;
        }

        Color neonPurple = new Color(0.85f, 0.20f, 1.20f, 1.0f);
        float outlineWidth = 0.0035f; // Viền sắc nét 2px cố định màn hình (Screen-Space), không nở khối model

        ApplyShaderToMaterials(HiRezMaterials, outlineShader, neonPurple, outlineWidth, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[Virtual Rail]</color> ĐÃ BẬT THÀNH CÔNG: Viền Arcade Silhouette Outline màu tím Neon 2px cho toàn bộ module tàu kẻ địch!");
    }

    [MenuItem("Tools/Virtual Rail/48. Undo Enemy Arcade Outline (Direction 2 - OFF)")]
    public static void UndoEnemyArcadeOutline()
    {
        Shader standardShader = Shader.Find(StandardShaderName);
        if (standardShader == null)
        {
            Debug.LogError($"[Virtual Rail] Không tìm thấy Standard Shader!");
            return;
        }

        ApplyShaderToMaterials(HiRezMaterials, standardShader, Color.clear, 0f, false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=yellow>[Virtual Rail]</color> ĐÃ HOÀN NGUYÊN 100%: Tắt viền Arcade Outline, trả toàn bộ vật liệu tàu địch về Standard Shader gốc.");
    }

    [MenuItem("Tools/Virtual Rail/49. Apply Purple Thrusters to Enemies (ON)")]
    public static void ApplyPurpleThrustersToEnemies()
    {
        // 1. Kích hoạt phát quang tím cho miệng xả Thrusters1.mat
        Material thrusterMat = AssetDatabase.LoadAssetAtPath<Material>(ThrustersMaterialPath);
        if (thrusterMat != null)
        {
            thrusterMat.EnableKeyword("_EMISSION");
            thrusterMat.SetColor("_EmissionColor", new Color(0.85f, 0.20f, 1.20f, 1.0f));
            EditorUtility.SetDirty(thrusterMat);
        }

        // 2. Chuyển đổi GUID của Thruster trong Prefab kẻ địch sang Enemy_Thruster_Purple
        SwapThrusterGuidInEnemyPrefabs(OriginalThrusterGuid, PurpleThrusterGuid);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[Virtual Rail]</color> ĐÃ BẬT THÀNH CÔNG: Động cơ và vệt lửa tím Neon cho tàu kẻ địch!");
    }

    [MenuItem("Tools/Virtual Rail/50. Undo Purple Thrusters (OFF - Restore Orange)")]
    public static void UndoPurpleThrusters()
    {
        // 1. Tắt phát quang miệng xả Thrusters1.mat
        Material thrusterMat = AssetDatabase.LoadAssetAtPath<Material>(ThrustersMaterialPath);
        if (thrusterMat != null)
        {
            thrusterMat.DisableKeyword("_EMISSION");
            thrusterMat.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(thrusterMat);
        }

        // 2. Hoàn nguyên GUID của Thruster trong Prefab kẻ địch về Thruster gốc (màu Cam)
        SwapThrusterGuidInEnemyPrefabs(PurpleThrusterGuid, OriginalThrusterGuid);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=yellow>[Virtual Rail]</color> ĐÃ HOÀN NGUYÊN 100%: Trả động cơ tàu kẻ địch về màu Cam gốc.");
    }

    private static void SwapThrusterGuidInEnemyPrefabs(string oldGuid, string newGuid)
    {
        foreach (string prefabPath in EnemyPrefabsWithThrusters)
        {
            if (!File.Exists(prefabPath)) continue;
            string text = File.ReadAllText(prefabPath);
            if (text.Contains(oldGuid))
            {
                text = text.Replace(oldGuid, newGuid);
                File.WriteAllText(prefabPath, text);
            }
        }
    }

    private static void ApplyShaderToMaterials(string[] paths, Shader targetShader, Color outlineColor, float outlineWidth, bool enableOutline)
    {
        foreach (string path in paths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Xóa emission cho vỏ tàu (trừ vật liệu ống xả Thrusters1)
            if (!path.Contains("Thrusters1"))
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", null);
                mat.SetColor("_EmissionColor", Color.black);
            }

            // Đổi Shader
            mat.shader = targetShader;

            if (enableOutline)
            {
                mat.SetColor("_OutlineColor", outlineColor);
                mat.SetFloat("_OutlineWidth", outlineWidth);
                mat.SetFloat("_OutlineZBias", 0.0f);
            }

            EditorUtility.SetDirty(mat);
        }
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
}
