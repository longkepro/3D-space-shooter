using System;
using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Custom Inspector chuyên nghiệp cho FormationPattern ScriptableObject.
    /// Cung cấp:
    /// 1. Giao diện trực quan xem sơ đồ vị trí tàu từ trên cao (Top-down Radar Preview).
    /// 2. Các nút Preset 1-click: Wedge V, Echelon Line, Spiral Stream, Diamond 4.
    /// 3. Công cụ đối xứng cánh [Auto-Mirror X] và dãn khoảng cách [Scale Spacing].
    /// 4. Danh sách quản lý slot trực quan không cần viết code.
    /// </summary>
    [CustomEditor(typeof(FormationPattern))]
    public class FormationPatternEditor : Editor
    {
        private SerializedProperty _patternNameProp;
        private SerializedProperty _slotsProp;
        private SerializedProperty _assemblyTimeProp;
        private SerializedProperty _swaySpeedProp;
        private SerializedProperty _swayAmplitudeXProp;
        private SerializedProperty _bankingAngleProp;
        private SerializedProperty _holdDistanceProp;
        private SerializedProperty _maxClosingSpeedProp;
        private SerializedProperty _maxLifetimeProp;
        private SerializedProperty _leaderDeathReactionProp;

        private bool _showSlotsFold = true;
        private bool _showDynamicsFold = true;
        private bool _showTacticalFold = true;

        private void OnEnable()
        {
            _patternNameProp = serializedObject.FindProperty("patternName");
            _slotsProp = serializedObject.FindProperty("slots");
            _assemblyTimeProp = serializedObject.FindProperty("assemblyTime");
            _swaySpeedProp = serializedObject.FindProperty("swaySpeed");
            _swayAmplitudeXProp = serializedObject.FindProperty("swayAmplitudeX");
            _bankingAngleProp = serializedObject.FindProperty("bankingAngle");
            _holdDistanceProp = serializedObject.FindProperty("holdDistance");
            _maxClosingSpeedProp = serializedObject.FindProperty("maxClosingSpeed");
            _maxLifetimeProp = serializedObject.FindProperty("maxLifetime");
            _leaderDeathReactionProp = serializedObject.FindProperty("leaderDeathReaction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FormationPattern pattern = (FormationPattern)target;

            // 1. Header Box
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField($"FORMATION PATTERN: {pattern.patternName}", titleStyle);
                EditorGUILayout.PropertyField(_patternNameProp, new GUIContent("Pattern Name"));

                int shipCount = _slotsProp != null ? _slotsProp.arraySize : 0;
                EditorGUILayout.HelpBox($"🛸 Tổng số tàu trong phi đội: {shipCount} chiếc | Phản ứng khi mất sếp: {pattern.leaderDeathReaction}", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2. Mini Top-down Radar Preview
            DrawTopDownRadarPreview(pattern);

            EditorGUILayout.Space(6);

            // 3. One-Click Quick Presets
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("⚡ Quick Presets (Nạp mẫu chuẩn 1-Click)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Wedge V (5)", GUILayout.Height(24)))
                {
                    ApplyPresetWedgeV();
                }
                if (GUILayout.Button("Echelon Line (3)", GUILayout.Height(24)))
                {
                    ApplyPresetEchelonLine();
                }
                if (GUILayout.Button("Diamond (4)", GUILayout.Height(24)))
                {
                    ApplyPresetDiamond();
                }
                if (GUILayout.Button("Spiral Stream (5)", GUILayout.Height(24)))
                {
                    ApplyPresetSpiralStream();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🪞 Auto-Mirror X (Đối xứng cánh)", GUILayout.Height(22)))
                {
                    AutoMirrorSlots();
                }
                if (GUILayout.Button("➕ Dãn cách x1.2", GUILayout.Height(22)))
                {
                    ScaleSpacing(1.2f);
                }
                if (GUILayout.Button("➖ Thu hẹp x0.8", GUILayout.Height(22)))
                {
                    ScaleSpacing(0.8f);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 4. Slot Matrix Table
            _showSlotsFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showSlotsFold, $"1. Slot Matrix (Danh sách {pattern.ShipCount} vị trí tàu)");
            if (_showSlotsFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    for (int i = 0; i < _slotsProp.arraySize; i++)
                    {
                        SerializedProperty slot = _slotsProp.GetArrayElementAtIndex(i);
                        SerializedProperty slotName = slot.FindPropertyRelative("slotName");
                        SerializedProperty offset = slot.FindPropertyRelative("localOffset");
                        SerializedProperty archetype = slot.FindPropertyRelative("preferredArchetype");
                        SerializedProperty isLeader = slot.FindPropertyRelative("isLeader");
                        SerializedProperty scale = slot.FindPropertyRelative("scaleMultiplier");

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        {
                            string badge = isLeader.boolValue ? "👑 [LEADER]" : $"[SLOT {i}]";
                            EditorGUILayout.LabelField($"{badge} {slotName.stringValue}", EditorStyles.boldLabel);
                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            {
                                _slotsProp.DeleteArrayElementAtIndex(i);
                                break;
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.PropertyField(slotName);
                        EditorGUILayout.PropertyField(offset, new GUIContent("Local Offset (X, Y, Z)"));
                        EditorGUILayout.PropertyField(archetype, new GUIContent("Preferred Archetype"));
                        EditorGUILayout.PropertyField(isLeader, new GUIContent("Is Leader"));
                        EditorGUILayout.PropertyField(scale, new GUIContent("Scale Multiplier"));

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }

                    if (GUILayout.Button("➕ Thêm Slot Tàu Mới", GUILayout.Height(26)))
                    {
                        _slotsProp.arraySize++;
                        SerializedProperty newSlot = _slotsProp.GetArrayElementAtIndex(_slotsProp.arraySize - 1);
                        newSlot.FindPropertyRelative("slotName").stringValue = $"Wingman {_slotsProp.arraySize - 1}";
                        newSlot.FindPropertyRelative("localOffset").vector3Value = new Vector3(0f, 0f, -(_slotsProp.arraySize - 1) * 6f);
                        newSlot.FindPropertyRelative("preferredArchetype").enumValueIndex = (int)EnemyArchetype.Flanker;
                        newSlot.FindPropertyRelative("isLeader").boolValue = false;
                        newSlot.FindPropertyRelative("scaleMultiplier").floatValue = 1.0f;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 5. Flight Dynamics
            _showDynamicsFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showDynamicsFold, "2. Flight Dynamics (Động Lực Học Bay Cả Đội)");
            if (_showDynamicsFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(_assemblyTimeProp, new GUIContent("Assembly Time (s)", "Thời gian ráp nối Bézier khi nhập đội"));
                    EditorGUILayout.PropertyField(_swaySpeedProp, new GUIContent("Sway Speed", "Tốc độ lượn sóng hình sin"));
                    EditorGUILayout.PropertyField(_swayAmplitudeXProp, new GUIContent("Sway Amplitude X (m)", "Biên độ lượn ngang"));
                    EditorGUILayout.PropertyField(_bankingAngleProp, new GUIContent("Banking Roll Angle (°)", "Góc nghiêng cánh khi lượn"));
                    EditorGUILayout.PropertyField(_holdDistanceProp, new GUIContent("Hold Distance (m)", "Cự ly ghìm trước tàu"));
                    EditorGUILayout.PropertyField(_maxClosingSpeedProp, new GUIContent("Max Closing Speed (m/s)"));
                    EditorGUILayout.PropertyField(_maxLifetimeProp, new GUIContent("Max Lifetime (s)"));
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 6. Tactical Behavior
            _showTacticalFold = EditorGUILayout.BeginFoldoutHeaderGroup(_showTacticalFold, "3. Tactical Reaction (Phản Ứng Khi Chỉ Huy Bị Hạ)");
            if (_showTacticalFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(_leaderDeathReactionProp, new GUIContent("Leader Death Reaction"));
                    LeaderDeathReaction react = (LeaderDeathReaction)_leaderDeathReactionProp.enumValueIndex;
                    string hint = react switch
                    {
                        LeaderDeathReaction.PanicScatter => "Tất cả tàu cánh mất khiên và hoảng loạn bẻ lái dạt biên thoát ly (Chuẩn Star Fox 64).",
                        LeaderDeathReaction.RetreatWarp => "Tất cả tàu cánh lập tức tăng tốc bứt phá vọt ra sau biến mất khỏi màn chơi.",
                        LeaderDeathReaction.KamikazeCharge => "Tất cả tàu cánh mất khiên và lao thẳng cảm tử vào tàu người chơi!",
                        _ => ""
                    };
                    EditorGUILayout.HelpBox(hint, MessageType.None);
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTopDownRadarPreview(FormationPattern pattern)
        {
            if (pattern.slots == null || pattern.slots.Length == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🗺️ Top-Down Radar View (Góc nhìn từ trên xuống)", EditorStyles.miniBoldLabel);

            float width = EditorGUIUtility.currentViewWidth - 40f;
            float height = 150f;
            Rect radarRect = GUILayoutUtility.GetRect(width, height);
            EditorGUI.DrawRect(radarRect, new Color(0.08f, 0.12f, 0.18f, 1f));

            Vector2 center = new Vector2(radarRect.x + radarRect.width * 0.5f, radarRect.y + 35f);

            // Vẽ trục tâm
            Handles.color = new Color(0.3f, 0.4f, 0.5f, 0.5f);
            Handles.DrawLine(new Vector3(radarRect.x, center.y, 0f), new Vector3(radarRect.x + radarRect.width, center.y, 0f));
            Handles.DrawLine(new Vector3(center.x, radarRect.y, 0f), new Vector3(center.x, radarRect.y + radarRect.height, 0f));

            // Tỉ lệ scale mét sang pixel
            float scale = 3.2f;

            for (int i = 0; i < pattern.slots.Length; i++)
            {
                var slot = pattern.slots[i];
                // LocalOffset: X là ngang, Z là chiều sâu (-Z là ở phía sau leader)
                Vector2 shipPos = center + new Vector2(slot.localOffset.x * scale, -slot.localOffset.z * scale);

                Color shipColor = slot.isLeader ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.8f, 1f);
                if (slot.preferredArchetype == EnemyArchetype.ShieldBearer) shipColor = new Color(0.3f, 1f, 0.4f);
                if (slot.preferredArchetype == EnemyArchetype.Interceptor) shipColor = new Color(1f, 0.3f, 0.3f);

                float size = slot.isLeader ? 10f : 7f;
                Rect shipRect = new Rect(shipPos.x - size * 0.5f, shipPos.y - size * 0.5f, size, size);
                EditorGUI.DrawRect(shipRect, shipColor);

                GUIStyle lblStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = shipColor },
                    fontSize = 9
                };
                GUI.Label(new Rect(shipPos.x + 6f, shipPos.y - 8f, 70f, 16f), slot.slotName, lblStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyPresetWedgeV()
        {
            Undo.RecordObject(target, "Apply Wedge V Preset");
            _slotsProp.arraySize = 5;

            SetSlot(0, "Leader", Vector3.zero, EnemyArchetype.SquadLeader, true, 1.35f);
            SetSlot(1, "Left Wing 1", new Vector3(-7f, 0f, -8f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(2, "Right Wing 1", new Vector3(7f, 0f, -8f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(3, "Left Wing 2", new Vector3(-14f, 0f, -16f), EnemyArchetype.Scout, false, 1.0f);
            SetSlot(4, "Right Wing 2", new Vector3(14f, 0f, -16f), EnemyArchetype.Scout, false, 1.0f);
        }

        private void ApplyPresetEchelonLine()
        {
            Undo.RecordObject(target, "Apply Echelon Line Preset");
            _slotsProp.arraySize = 3;

            SetSlot(0, "Leader", Vector3.zero, EnemyArchetype.SquadLeader, true, 1.35f);
            SetSlot(1, "Left Wing", new Vector3(-10f, 0f, -4f), EnemyArchetype.Flanker, false, 1.0f);
            SetSlot(2, "Right Wing", new Vector3(10f, 0f, -4f), EnemyArchetype.Flanker, false, 1.0f);
        }

        private void ApplyPresetDiamond()
        {
            Undo.RecordObject(target, "Apply Diamond Preset");
            _slotsProp.arraySize = 4;

            SetSlot(0, "Apex Leader", Vector3.zero, EnemyArchetype.SquadLeader, true, 1.35f);
            SetSlot(1, "Left Guard", new Vector3(-6f, 0f, -6f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(2, "Right Guard", new Vector3(6f, 0f, -6f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(3, "Rear Anchor", new Vector3(0f, 0f, -12f), EnemyArchetype.Interceptor, false, 1.0f);
        }

        private void ApplyPresetSpiralStream()
        {
            Undo.RecordObject(target, "Apply Spiral Stream Preset");
            _slotsProp.arraySize = 5;

            SetSlot(0, "Head", Vector3.zero, EnemyArchetype.SquadLeader, true, 1.25f);
            SetSlot(1, "Segment 1", new Vector3(6f, 3f, -10f), EnemyArchetype.Interceptor, false, 1.0f);
            SetSlot(2, "Segment 2", new Vector3(0f, 6f, -20f), EnemyArchetype.Interceptor, false, 1.0f);
            SetSlot(3, "Segment 3", new Vector3(-6f, 3f, -30f), EnemyArchetype.Interceptor, false, 1.0f);
            SetSlot(4, "Segment 4", new Vector3(-6f, -3f, -40f), EnemyArchetype.Interceptor, false, 1.0f);
        }

        private void AutoMirrorSlots()
        {
            Undo.RecordObject(target, "Auto-Mirror Slots");
            // Nhân bản các slot có X < 0 sang X > 0
            for (int i = 0; i < _slotsProp.arraySize; i++)
            {
                SerializedProperty slot = _slotsProp.GetArrayElementAtIndex(i);
                Vector3 off = slot.FindPropertyRelative("localOffset").vector3Value;
                if (off.x < -0.1f)
                {
                    // Tìm slot đối xứng
                    bool found = false;
                    for (int j = 0; j < _slotsProp.arraySize; j++)
                    {
                        if (i == j) continue;
                        SerializedProperty other = _slotsProp.GetArrayElementAtIndex(j);
                        Vector3 otherOff = other.FindPropertyRelative("localOffset").vector3Value;
                        if (Mathf.Abs(otherOff.x - (-off.x)) < 0.1f && Mathf.Abs(otherOff.z - off.z) < 0.1f)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        _slotsProp.arraySize++;
                        SerializedProperty newSlot = _slotsProp.GetArrayElementAtIndex(_slotsProp.arraySize - 1);
                        newSlot.FindPropertyRelative("slotName").stringValue = slot.FindPropertyRelative("slotName").stringValue.Replace("Left", "Right");
                        newSlot.FindPropertyRelative("localOffset").vector3Value = new Vector3(-off.x, off.y, off.z);
                        newSlot.FindPropertyRelative("preferredArchetype").enumValueIndex = slot.FindPropertyRelative("preferredArchetype").enumValueIndex;
                        newSlot.FindPropertyRelative("isLeader").boolValue = false;
                        newSlot.FindPropertyRelative("scaleMultiplier").floatValue = slot.FindPropertyRelative("scaleMultiplier").floatValue;
                    }
                }
            }
        }

        private void ScaleSpacing(float factor)
        {
            Undo.RecordObject(target, "Scale Slot Spacing");
            for (int i = 0; i < _slotsProp.arraySize; i++)
            {
                SerializedProperty slot = _slotsProp.GetArrayElementAtIndex(i);
                Vector3 off = slot.FindPropertyRelative("localOffset").vector3Value;
                slot.FindPropertyRelative("localOffset").vector3Value = off * factor;
            }
        }

        private void SetSlot(int index, string name, Vector3 offset, EnemyArchetype archetype, bool isLeader, float scale)
        {
            SerializedProperty slot = _slotsProp.GetArrayElementAtIndex(index);
            slot.FindPropertyRelative("slotName").stringValue = name;
            slot.FindPropertyRelative("localOffset").vector3Value = offset;
            slot.FindPropertyRelative("preferredArchetype").enumValueIndex = (int)archetype;
            slot.FindPropertyRelative("isLeader").boolValue = isLeader;
            slot.FindPropertyRelative("scaleMultiplier").floatValue = scale;
        }

        [MenuItem("Tools/Virtual Rail/Generate Formation Presets")]
        public static void GenerateDefaultPresets()
        {
            string folder = "Assets/Scripts/VirtualRail";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Scripts", "VirtualRail");
            }

            // 1. Wedge V
            CreateOrUpdateAsset($"{folder}/Formation_WedgeV.asset", "Wedge V Formation", pattern =>
            {
                pattern.patternName = "Wedge V (5 Ships)";
                pattern.assemblyTime = 1.2f;
                pattern.swaySpeed = 1.5f;
                pattern.swayAmplitudeX = 12f;
                pattern.bankingAngle = 25f;
                pattern.holdDistance = 65f;
                pattern.leaderDeathReaction = LeaderDeathReaction.PanicScatter;
                pattern.slots = new FormationSlotDefinition[]
                {
                    new FormationSlotDefinition { slotName = "Leader", localOffset = Vector3.zero, preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.35f },
                    new FormationSlotDefinition { slotName = "Left Wing 1", localOffset = new Vector3(-7f, 0f, -8f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Wing 1", localOffset = new Vector3(7f, 0f, -8f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Left Wing 2", localOffset = new Vector3(-14f, 0f, -16f), preferredArchetype = EnemyArchetype.Scout, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Wing 2", localOffset = new Vector3(14f, 0f, -16f), preferredArchetype = EnemyArchetype.Scout, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            // 2. Echelon Line
            CreateOrUpdateAsset($"{folder}/Formation_EchelonLine.asset", "Echelon Line Formation", pattern =>
            {
                pattern.patternName = "Echelon Line (3 Ships)";
                pattern.assemblyTime = 1.0f;
                pattern.swaySpeed = 1.8f;
                pattern.swayAmplitudeX = 14f;
                pattern.bankingAngle = 20f;
                pattern.holdDistance = 60f;
                pattern.leaderDeathReaction = LeaderDeathReaction.PanicScatter;
                pattern.slots = new FormationSlotDefinition[]
                {
                    new FormationSlotDefinition { slotName = "Leader", localOffset = Vector3.zero, preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.35f },
                    new FormationSlotDefinition { slotName = "Left Wing", localOffset = new Vector3(-10f, 0f, -4f), preferredArchetype = EnemyArchetype.Flanker, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Wing", localOffset = new Vector3(10f, 0f, -4f), preferredArchetype = EnemyArchetype.Flanker, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            // 3. Diamond 4
            CreateOrUpdateAsset($"{folder}/Formation_Diamond4.asset", "Diamond 4 Formation", pattern =>
            {
                pattern.patternName = "Diamond Guard (4 Ships)";
                pattern.assemblyTime = 1.2f;
                pattern.swaySpeed = 1.2f;
                pattern.swayAmplitudeX = 10f;
                pattern.bankingAngle = 25f;
                pattern.holdDistance = 65f;
                pattern.leaderDeathReaction = LeaderDeathReaction.PanicScatter;
                pattern.slots = new FormationSlotDefinition[]
                {
                    new FormationSlotDefinition { slotName = "Apex Leader", localOffset = Vector3.zero, preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.35f },
                    new FormationSlotDefinition { slotName = "Left Guard", localOffset = new Vector3(-6f, 0f, -6f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Guard", localOffset = new Vector3(6f, 0f, -6f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Rear Anchor", localOffset = new Vector3(0f, 0f, -12f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            // 4. Spiral Stream
            CreateOrUpdateAsset($"{folder}/Formation_SpiralStream.asset", "Spiral Stream Formation", pattern =>
            {
                pattern.patternName = "Spiral Stream (5 Ships)";
                pattern.assemblyTime = 1.5f;
                pattern.swaySpeed = 2.0f;
                pattern.swayAmplitudeX = 16f;
                pattern.bankingAngle = 35f;
                pattern.holdDistance = 70f;
                pattern.leaderDeathReaction = LeaderDeathReaction.RetreatWarp;
                pattern.slots = new FormationSlotDefinition[]
                {
                    new FormationSlotDefinition { slotName = "Head", localOffset = Vector3.zero, preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.25f },
                    new FormationSlotDefinition { slotName = "Segment 1", localOffset = new Vector3(6f, 3f, -10f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 2", localOffset = new Vector3(0f, 6f, -20f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 3", localOffset = new Vector3(-6f, 3f, -30f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 4", localOffset = new Vector3(-6f, -3f, -40f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green><b>[FormationPatternEditor] ĐÃ KHỞI TẠO THÀNH CÔNG 4 BỘ PRESET ĐỘI HÌNH CHUẨN!</b></color>");
        }

        private static void CreateOrUpdateAsset(string path, string defaultName, Action<FormationPattern> configure)
        {
            FormationPattern asset = AssetDatabase.LoadAssetAtPath<FormationPattern>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FormationPattern>();
                configure(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                configure(asset);
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
