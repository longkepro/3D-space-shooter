using UnityEngine;
using UnityEditor;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Custom Inspector chuyên dụng cho VirtualRailChargeShotVisual.
    /// Cho phép điều chỉnh kích thước quả cầu, độ rộng đuôi, tầm đèn, bán kính nổ trực quan với 0 GC Alloc.
    /// </summary>
    [CustomEditor(typeof(VirtualRailChargeShotVisual))]
    [CanEditMultipleObjects]
    public class VirtualRailChargeShotVisualEditor : UnityEditor.Editor
    {
        private SerializedProperty outerDiameterProp;
        private SerializedProperty innerCoreRatioProp;
        private SerializedProperty enableTrailProp;
        private SerializedProperty trailWidthProp;
        private SerializedProperty lightRangeProp;
        private SerializedProperty autoSyncHitboxProp;
        private SerializedProperty collisionRadiusProp;
        private SerializedProperty splashRadiusProp;
        private SerializedProperty spinSpeedZProp;
        private SerializedProperty tumbleSpeedProp;
        private SerializedProperty pulseFrequencyProp;
        private SerializedProperty pulseAmplitudeProp;

        private void OnEnable()
        {
            outerDiameterProp = serializedObject.FindProperty("outerDiameter");
            innerCoreRatioProp = serializedObject.FindProperty("innerCoreRatio");
            enableTrailProp = serializedObject.FindProperty("enableTrail");
            trailWidthProp = serializedObject.FindProperty("trailWidth");
            lightRangeProp = serializedObject.FindProperty("lightRange");
            autoSyncHitboxProp = serializedObject.FindProperty("autoSyncHitbox");
            collisionRadiusProp = serializedObject.FindProperty("collisionRadius");
            splashRadiusProp = serializedObject.FindProperty("splashRadius");
            spinSpeedZProp = serializedObject.FindProperty("spinSpeedZ");
            tumbleSpeedProp = serializedObject.FindProperty("tumbleSpeed");
            pulseFrequencyProp = serializedObject.FindProperty("pulseFrequency");
            pulseAmplitudeProp = serializedObject.FindProperty("pulseAmplitude");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Title Banner
            EditorGUILayout.Space(4);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⚡ VIRTUAL RAIL: PLASMA ORB ADJUSTER", titleStyle);
            EditorGUILayout.LabelField("Bộ điều chỉnh kích thước & hiệu ứng quả cầu đạn tụ lực", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2. Preset Quick Buttons
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("QUICK PRESETS (CHỌN NHANH KÍCH THƯỚC):", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Gọn gàng\n(0.35m)", GUILayout.Height(36)))
            {
                ApplyPreset(0.35f, 0.54f, 1.2f, 16f, 1.2f, 10f);
            }
            if (GUILayout.Button("Cân đối\n(0.80m)", GUILayout.Height(36)))
            {
                ApplyPreset(0.80f, 0.54f, 1.8f, 22f, 1.5f, 14f);
            }
            if (GUILayout.Button("Star Fox 64\n(1.50m)", GUILayout.Height(36)))
            {
                ApplyPreset(1.50f, 0.54f, 2.4f, 28f, 2.2f, 18f);
            }
            if (GUILayout.Button("Gốc Houidi\n(3.85m)", GUILayout.Height(36)))
            {
                ApplyPreset(3.85f, 0.54f, 1.2f, 32f, 1.5f, 10f);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 3. Visual Dimensions Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("KÍCH THƯỚC HIỂN THỊ (VISUAL MESH & LIGHT):", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(outerDiameterProp, new GUIContent("Đường kính vỏ ngoài (m)", "Đường kính hiển thị của vỏ cầu Plasma bên ngoài"));
            EditorGUILayout.PropertyField(innerCoreRatioProp, new GUIContent("Tỉ lệ lõi trong (Ratio)", "Kích thước lõi năng lượng bên trong so với vỏ ngoài"));
            EditorGUILayout.PropertyField(enableTrailProp, new GUIContent("Bật vệt đuôi (Trail)", "Bật hoặc tắt dải ruy-băng vệt đuôi phát sáng phía sau"));
            if (enableTrailProp.boolValue)
            {
                EditorGUILayout.PropertyField(trailWidthProp, new GUIContent("Độ rộng vệt đuôi (m)", "Bề rộng đầu vệt đuôi TrailRenderer"));
            }
            EditorGUILayout.PropertyField(lightRangeProp, new GUIContent("Bán kính phát sáng (m)", "Phạm vi chiếu sáng PointLight của quả cầu"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 4. Physics & Combat Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("VA CHẠM & SÁT THƯƠNG NỔ (PHYSICS & AOE):", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoSyncHitboxProp, new GUIContent("Tự động đồng bộ Hitbox", "Tự động tính bán kính va chạm và bán kính nổ theo kích thước vỏ ngoài"));

            if (autoSyncHitboxProp.boolValue)
            {
                GUI.enabled = false;
                float calcCol = Mathf.Clamp(outerDiameterProp.floatValue * 0.38f, 0.5f, 1.5f);
                float calcSplash = 10.0f;
                EditorGUILayout.FloatField(new GUIContent("Bán kính va chạm (Auto)", "Được tự động tính theo tỉ lệ ôm lõi"), calcCol);
                EditorGUILayout.FloatField(new GUIContent("Bán kính nổ AoE (Auto)", "Chuẩn Star Fox 64: 10m"), calcSplash);
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.PropertyField(collisionRadiusProp, new GUIContent("Bán kính va chạm (m)", "Bán kính SphereCollider"));
                EditorGUILayout.PropertyField(splashRadiusProp, new GUIContent("Bán kính nổ AoE (m)", "Bán kính nổ diện rộng của VirtualRailHomingMissile"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 5. Animation Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("HIỆU ỨNG QUAY & NHỊP ĐẬP (ANIMATION):", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spinSpeedZProp, new GUIContent("Tốc độ xoay trục Z (độ/s)"));
            EditorGUILayout.PropertyField(tumbleSpeedProp, new GUIContent("Tốc độ lộn nhào"));
            EditorGUILayout.PropertyField(pulseFrequencyProp, new GUIContent("Tần số nhịp đập"));
            EditorGUILayout.PropertyField(pulseAmplitudeProp, new GUIContent("Biên độ nhịp đập"));
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                {
                    var visual = (VirtualRailChargeShotVisual)t;
                    if (visual != null)
                    {
                        visual.ApplyDimensions();
                        EditorUtility.SetDirty(visual);
                    }
                }
            }

            EditorGUILayout.Space(8);
        }

        private void ApplyPreset(float outer, float core, float trail, float light, float col, float splash)
        {
            outerDiameterProp.floatValue = outer;
            innerCoreRatioProp.floatValue = core;
            trailWidthProp.floatValue = trail;
            lightRangeProp.floatValue = light;
            collisionRadiusProp.floatValue = col;
            splashRadiusProp.floatValue = splash;

            serializedObject.ApplyModifiedProperties();
            foreach (var t in targets)
            {
                var visual = (VirtualRailChargeShotVisual)t;
                if (visual != null)
                {
                    visual.ApplyDimensions();
                    EditorUtility.SetDirty(visual);
                }
            }
        }
    }
}
