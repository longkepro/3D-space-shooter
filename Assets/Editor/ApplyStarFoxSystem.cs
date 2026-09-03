using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class ApplyStarFoxSystem
{
    [MenuItem("Tools/Star Fox/1. Apply Modular System (Safe)")]
    public static void DoApply()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        if (shipPrefab == null) return;
        
        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        Player oldPlayer = contents.GetComponent<Player>();
        if (oldPlayer != null) oldPlayer.enabled = false;

        StarFoxInput input = contents.GetComponent<StarFoxInput>();
        if (input == null) input = contents.AddComponent<StarFoxInput>();

        StarFoxCrosshair crosshair = contents.GetComponent<StarFoxCrosshair>();
        if (crosshair == null) crosshair = contents.AddComponent<StarFoxCrosshair>();

        StarFoxMovement movement = contents.GetComponent<StarFoxMovement>();
        if (movement == null) movement = contents.AddComponent<StarFoxMovement>();

        StarFoxWeapons weapons = contents.GetComponent<StarFoxWeapons>();
        if (weapons == null) weapons = contents.AddComponent<StarFoxWeapons>();
        
        StarFoxCameraRig camRig = contents.GetComponent<StarFoxCameraRig>();
        if (camRig == null) camRig = contents.AddComponent<StarFoxCameraRig>();

        Transform existingCanvas = contents.transform.Find("StarFoxCanvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas.gameObject);

        GameObject canvasObj = new GameObject("StarFoxCanvas");
        canvasObj.transform.SetParent(contents.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject crossObj = new GameObject("SquareCrosshair");
        crossObj.transform.SetParent(canvasObj.transform, false);
        Image img = crossObj.AddComponent<Image>();
        img.color = new Color(0, 1, 0, 0.5f); 
        
        RectTransform rt = crossObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 40);

        // LINK REFERENCES
        movement.input = input;
        crosshair.movement = movement;
        crosshair.crosshairUI = rt;
        weapons.input = input;
        
        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Star Fox Classic Modular System Applied Successfully!");
    }

    [MenuItem("Tools/Star Fox/2. Restore Original System")]
    public static void DoRestore()
    {
        string shipPrefabPath = "Assets/Prefabs/Player Ship (1).prefab";
        GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shipPrefabPath);
        if (shipPrefab == null) return;
        
        string assetPath = AssetDatabase.GetAssetPath(shipPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);

        Object.DestroyImmediate(contents.GetComponent<StarFoxInput>(), true);
        Object.DestroyImmediate(contents.GetComponent<StarFoxCrosshair>(), true);
        Object.DestroyImmediate(contents.GetComponent<StarFoxMovement>(), true);
        Object.DestroyImmediate(contents.GetComponent<StarFoxWeapons>(), true);
        Object.DestroyImmediate(contents.GetComponent<StarFoxCameraRig>(), true);

        Transform existingCanvas = contents.transform.Find("StarFoxCanvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas.gameObject, true);

        Player oldPlayer = contents.GetComponent<Player>();
        if (oldPlayer != null) oldPlayer.enabled = true;

        PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("Original System Restored!");
    }
}
