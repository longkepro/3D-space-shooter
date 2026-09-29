using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Custom Inspector chuyên nghiệp cho StageData ScriptableObject.
    /// Cung cấp:
    /// 1. Kéo thả trực tiếp Texture2D ảnh màn chơi 4K mà không cần tạo file .mat thủ công.
    /// 2. Hộp xem trước trực quan (Thumbnail Preview 16:9) ngay trên Inspector.
    /// 3. Nút xem trước 1-click (Preview In Scene/Game) trong Editor mà không cần ấn Play.
    /// 4. Nút nhảy màn tức thì (Warp To Stage) khi đang test trong Play Mode.
    /// </summary>
    [CustomEditor(typeof(StageData))]
    public class StageDataEditor : Editor
    {
        private SerializedProperty _stageIdProp;
        private SerializedProperty _stageTitleProp;
        private SerializedProperty _stageSubtitleProp;
        private SerializedProperty _stageIndexProp;

        private SerializedProperty _railSettingsProp;
        private SerializedProperty _envSettingsProp;
        private SerializedProperty _enemySettingsProp;
        private SerializedProperty _hazardSettingsProp;
        private SerializedProperty _climaxSettingsProp;

        private SerializedProperty _skyboxMatProp;
        private SerializedProperty _backdropMatProp;
        private SerializedProperty _backdropTexProp;
        private SerializedProperty _backdropExposureProp;
        private SerializedProperty _backdropTintProp;

        private bool _showEnvFold = true;
        private bool _showRailFold = true;
        private bool _showEnemyFold = false;
        private bool _showHazardFold = false;
        private bool _showClimaxFold = false;

        private void OnEnable()
        {
            _stageIdProp = serializedObject.FindProperty("stageId");
            _stageTitleProp = serializedObject.FindProperty("stageTitle");
            _stageSubtitleProp = serializedObject.FindProperty("stageSubtitle");
            _stageIndexProp = serializedObject.FindProperty("stageIndex");

            _railSettingsProp = serializedObject.FindProperty("railSettings");
            _envSettingsProp = serializedObject.FindProperty("environmentSettings");
            _enemySettingsProp = serializedObject.FindProperty("enemySettings");
            _hazardSettingsProp = serializedObject.FindProperty("hazardSettings");
            _climaxSettingsProp = serializedObject.FindProperty("climaxSettings");

            if (_envSettingsProp != null)
            {
                _skyboxMatProp = _envSettingsProp.FindPropertyRelative("skyboxMaterial");
                _backdropMatProp = _envSettingsProp.FindPropertyRelative("backdropMaterial");
                _backdropTexProp = _envSettingsProp.FindPropertyRelative("backdropTexture");
                _backdropExposureProp = _envSettingsProp.FindPropertyRelative("backdropExposure");
                _backdropTintProp = _envSettingsProp.FindPropertyRelative("backdropTint");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            StageData stage = (StageData)target;

            // 1. Header & Stage Identity
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField($"STAGE CONFIG: {stage.stageTitle}", titleStyle);

                EditorGUILayout.PropertyField(_stageIdProp);
                EditorGUILayout.PropertyField(_stageTitleProp);
                EditorGUILayout.PropertyField(_stageSubtitleProp);
                EditorGUILayout.PropertyField(_stageIndexProp);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2. Play Mode Quick Actions
            if (Application.isPlaying && VirtualRailStageDirector.Instance != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f);
                if (GUILayout.Button($"🚀 WARP TO THIS STAGE (INDEX {stage.stageIndex}) IN GAME", GUILayout.Height(32)))
                {
                    VirtualRailStageDirector.Instance.StartStage(stage.stageIndex - 1);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }

            // 3. Rail Settings
            _showRailFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showRailFold, "1. Rail & Kinematics (Vận Tốc & Cự Ly)");
            if (_showRailFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_railSettingsProp, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 4. Environment & Atmosphere (Smart Backdrop 4K)
            _showEnvFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showEnvFold, "2. Environment & Atmosphere (Phông Nền 4K & Khí Quyển)");
            if (_showEnvFold)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("4K Horizon Backdrop Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_backdropTexProp, new GUIContent("Backdrop Image (4K Texture2D)", "Kéo thả thẳng ảnh 4K PNG/JPG màn chơi"));
                EditorGUILayout.PropertyField(_backdropExposureProp, new GUIContent("Backdrop Exposure Boost"));
                EditorGUILayout.PropertyField(_backdropTintProp, new GUIContent("Backdrop Color Tint"));
                EditorGUILayout.PropertyField(_backdropMatProp, new GUIContent("Custom Material Override", "Tùy chọn: chỉ gán khi muốn dùng shader/material riêng"));

                // Thumbnail Visual Preview
                Texture2D tex = _backdropTexProp.objectReferenceValue as Texture2D;
                if (tex != null)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.LabelField($"Preview: {tex.name} ({tex.width} x {tex.height})", EditorStyles.miniLabel);
                        float previewWidth = EditorGUIUtility.currentViewWidth - 60f;
                        float previewHeight = previewWidth * (9f / 16f);
                        Rect r = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                        GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
                    }
                    EditorGUILayout.EndVertical();
                }

                // 1-Click Preview Action Button
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("👁️ Preview Backdrop In Scene/Game", GUILayout.Height(26)))
                {
                    PreviewStageBackdrop(stage);
                }
                if (GUILayout.Button("🌌 Reset Skybox To Stars", GUILayout.Height(26)))
                {
                    ResetSceneSkybox();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Atmosphere & Lighting", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_skyboxMatProp, new GUIContent("Skybox Material (Mặc định: Sao đêm)"));

                // Ambient, Fog, Light, Dust
                SerializedProperty ambientLight = _envSettingsProp.FindPropertyRelative("ambientLightColor");
                SerializedProperty enableFog = _envSettingsProp.FindPropertyRelative("enableFog");
                SerializedProperty fogColor = _envSettingsProp.FindPropertyRelative("fogColor");
                SerializedProperty fogDensity = _envSettingsProp.FindPropertyRelative("fogDensity");
                SerializedProperty lightColor = _envSettingsProp.FindPropertyRelative("directionalLightColor");
                SerializedProperty lightInt = _envSettingsProp.FindPropertyRelative("lightIntensity");
                SerializedProperty dustColor = _envSettingsProp.FindPropertyRelative("spaceDustColor");
                SerializedProperty dustCount = _envSettingsProp.FindPropertyRelative("spaceDustCount");

                EditorGUILayout.PropertyField(ambientLight);
                EditorGUILayout.PropertyField(enableFog);
                if (enableFog.boolValue)
                {
                    EditorGUILayout.PropertyField(fogColor);
                    EditorGUILayout.PropertyField(fogDensity);
                }
                EditorGUILayout.PropertyField(lightColor);
                EditorGUILayout.PropertyField(lightInt);
                EditorGUILayout.PropertyField(dustColor);
                EditorGUILayout.PropertyField(dustCount);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 5. Enemy Settings
            _showEnemyFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showEnemyFold, "3. Enemy & Wave Palette (Quái Vật & Đội Hình)");
            if (_showEnemyFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_enemySettingsProp, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 6. Hazards Settings
            _showHazardFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showHazardFold, "4. Hazards & Collectibles (Thiên Thạch & Vòng Bay)");
            if (_showHazardFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_hazardSettingsProp, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 7. Climax Settings
            _showClimaxFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showClimaxFold, "5. Climax & Completion (Cao Trào & Chuyển Màn)");
            if (_showClimaxFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_climaxSettingsProp, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private static void PreviewStageBackdrop(StageData stage)
        {
            if (stage == null) return;

            var backdrops = Object.FindObjectsByType<VirtualRailHorizonBackdrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bd in backdrops)
            {
                bd.ApplyStageSettings(stage.environmentSettings);
                bd.Initialize();
                bd.SetBackdropActive(true);
                EditorUtility.SetDirty(bd);
            }

            Material sky = stage.environmentSettings.skyboxMaterial;
            if (sky == null)
            {
                sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat");
            }
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                DynamicGI.UpdateEnvironment();
            }

            SceneView.RepaintAll();
            Debug.Log($"<color=cyan><b>[StageDataEditor] ĐÃ XEM TRƯỚC PHÔNG NỀN MÀN CHƠI:</b> {stage.stageTitle}</color>");
        }

        private static void ResetSceneSkybox()
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat");
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                DynamicGI.UpdateEnvironment();
                SceneView.RepaintAll();
                Debug.Log("<color=green><b>[StageDataEditor] ĐÃ RESET SKYBOX VỀ SAO ĐÊM:</b> skyboxv1_starsonly.mat</color>");
            }
        }
    }
}
