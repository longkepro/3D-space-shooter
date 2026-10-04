using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Custom Inspector chuyên nghiệp cho StageData ScriptableObject.
    /// Cung cấp:
    /// 1. Tùy biến Chế độ hoàn thành (Thời gian phút, Cự ly Z, Diệt quái, Diệt Boss).
    /// 2. Cấu hình bảng quái vật và vector độc lập 100% không dùng fallback mặc định.
    /// 3. Soạn thảo kịch bản Timeline theo mốc thời gian/cự ly trực quan.
    /// 4. Kéo thả trực tiếp Texture2D ảnh màn chơi 4K mà không cần tạo file .mat thủ công.
    /// 5. Nút xem trước 1-click (Preview In Scene/Game) và Nút nhảy màn tức thì trong Play Mode.
    /// </summary>
    [CustomEditor(typeof(StageData))]
    public class StageDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _stageIdProp;
        private SerializedProperty _stageTitleProp;
        private SerializedProperty _stageSubtitleProp;
        private SerializedProperty _stageIndexProp;

        private SerializedProperty _progressionSettingsProp;
        private SerializedProperty _railSettingsProp;
        private SerializedProperty _envSettingsProp;
        private SerializedProperty _enemySettingsProp;
        private SerializedProperty _hazardSettingsProp;
        private SerializedProperty _climaxSettingsProp;
        private SerializedProperty _timelineEventsProp;

        private SerializedProperty _skyboxMatProp;
        private SerializedProperty _backdropMatProp;
        private SerializedProperty _backdropTexProp;
        private SerializedProperty _backdropExposureProp;
        private SerializedProperty _backdropTintProp;

        private bool _showProgressionFold = true;
        private bool _showEnvFold = true;
        private bool _showEnemyFold = true;
        private bool _showTimelineFold = true;
        private bool _showHazardFold = false;
        private bool _showClimaxFold = false;

        private void OnEnable()
        {
            _stageIdProp = serializedObject.FindProperty("stageId");
            _stageTitleProp = serializedObject.FindProperty("stageTitle");
            _stageSubtitleProp = serializedObject.FindProperty("stageSubtitle");
            _stageIndexProp = serializedObject.FindProperty("stageIndex");

            _progressionSettingsProp = serializedObject.FindProperty("progressionSettings");
            _railSettingsProp = serializedObject.FindProperty("railSettings");
            _envSettingsProp = serializedObject.FindProperty("environmentSettings");
            _enemySettingsProp = serializedObject.FindProperty("enemySettings");
            _hazardSettingsProp = serializedObject.FindProperty("hazardSettings");
            _climaxSettingsProp = serializedObject.FindProperty("climaxSettings");
            _timelineEventsProp = serializedObject.FindProperty("timelineEvents");

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

            // 3. Progression & Completion Mode
            _showProgressionFold = DrawSectionHeader(_showProgressionFold, "1. Progression & Completion Mode (Thời Gian & Cự Ly)");
            if (_showProgressionFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    SerializedProperty compMode = _progressionSettingsProp.FindPropertyRelative("completionMode");
                    SerializedProperty durMin = _progressionSettingsProp.FindPropertyRelative("durationMinutes");
                    SerializedProperty targetZ = _progressionSettingsProp.FindPropertyRelative("targetDistanceZ");
                    SerializedProperty baseSpeed = _progressionSettingsProp.FindPropertyRelative("baseForwardSpeed");
                    SerializedProperty warpDur = _progressionSettingsProp.FindPropertyRelative("warpDuration");
                    SerializedProperty warpMult = _progressionSettingsProp.FindPropertyRelative("warpSpeedMultiplier");
                    SerializedProperty targetElim = _progressionSettingsProp.FindPropertyRelative("targetEliminationCount");

                    EditorGUILayout.PropertyField(compMode, new GUIContent("Completion Mode (Kiểu Hoàn Thành)"));

                    StageCompletionMode mode = (StageCompletionMode)compMode.enumValueIndex;
                    if (mode == StageCompletionMode.TimeDuration || mode == StageCompletionMode.SurvivalUntilBoss)
                    {
                        EditorGUILayout.PropertyField(durMin, new GUIContent("Target Duration (Phút)", "Thời lượng màn chơi tính bằng phút"));
                        float totalSec = durMin.floatValue * 60f;
                        EditorGUILayout.HelpBox($"⏱️ Màn chơi sẽ kéo dài chính xác: {durMin.floatValue:F1} phút ({totalSec:F0} giây)", MessageType.Info);
                    }
                    if (mode == StageCompletionMode.DistanceTraveled || mode == StageCompletionMode.SurvivalUntilBoss)
                    {
                        EditorGUILayout.PropertyField(targetZ, new GUIContent("Target Distance Z (m)", "Quãng đường ray mục tiêu"));
                    }
                    if (mode == StageCompletionMode.EliminationQuota)
                    {
                        EditorGUILayout.PropertyField(targetElim, new GUIContent("Target Elimination Quota", "Số lượng quái cần tiêu diệt"));
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.PropertyField(baseSpeed, new GUIContent("Base Forward Speed (m/s)"));
                    EditorGUILayout.PropertyField(warpDur, new GUIContent("Warp Transition Duration (s)"));
                    EditorGUILayout.PropertyField(warpMult, new GUIContent("Warp Speed Multiplier"));
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // 4. Enemy & Wave Palette
            _showEnemyFold = DrawSectionHeader(_showEnemyFold, "2. Enemy & Wave Palette (Quái Vật & Hỏa Lực Riêng)");
            if (_showEnemyFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.HelpBox("⚠️ NGUYÊN TẮC: Không dùng fallback mặc định ngầm. Màn này chỉ xuất hiện những quái và vector được chỉ định dưới đây.", MessageType.None);
                    EditorGUILayout.PropertyField(_enemySettingsProp, true);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // 5. Scripted Timeline Events
            _showTimelineFold = DrawSectionHeader(_showTimelineFold, "3. Scripted Timeline (Kịch Bản Màn Chơi Theo Mốc)");
            if (_showTimelineFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.HelpBox("Các sự kiện kích hoạt theo thời gian (giây) hoặc theo cự ly (mét Z) để tạo nhịp điệu phong phú cho màn chơi.", MessageType.Info);
                    EditorGUILayout.PropertyField(_timelineEventsProp, new GUIContent("Danh Sách Sự Kiện Kịch Bản"), true);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // 6. Environment & Atmosphere (Smart Backdrop 4K)
            _showEnvFold = DrawSectionHeader(_showEnvFold, "4. Environment & Atmosphere (Phông Nền 4K & Khí Quyển)");
            if (_showEnvFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
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
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // 7. Hazards Settings
            _showHazardFold = DrawSectionHeader(_showHazardFold, "5. Hazards & Collectibles (Thiên Thạch & Vòng Bay)");
            if (_showHazardFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(_hazardSettingsProp, true);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // 8. Climax Settings
            _showClimaxFold = DrawSectionHeader(_showClimaxFold, "6. Climax & Boss Encounter (Cao Trào & Trùm)");
            if (_showClimaxFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(_climaxSettingsProp, true);
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Vẽ thanh Foldout Section dạng banner độc lập an toàn 100%, không xung đột stack với ReorderableList nội bộ của Unity.
        /// </summary>
        private static bool DrawSectionHeader(bool foldout, string title)
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 24f);

            // Nền banner bo nhẹ màu than chuẩn Unity Pro Inspector
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.22f, 0.28f, 1f));

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) },
                alignment = TextAnchor.MiddleLeft
            };

            Rect foldoutRect = new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, rect.height - 4f);
            return EditorGUI.Foldout(foldoutRect, foldout, title, true, style);
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
