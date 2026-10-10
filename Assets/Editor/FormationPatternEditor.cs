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
    public class FormationPatternEditor : UnityEditor.Editor
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

        private static float _testLeadDistance = 120f;

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

            EditorGUILayout.Space(3);
            if (GUILayout.Button("🛸 MỞ SCENE VIEW 3D PREVIEWER (XEM TRƯỚC THỜI GIAN THỰC)", GUILayout.Height(30)))
            {
                FormationSceneViewPreviewer.OpenWithPattern(pattern);
            }

            EditorGUILayout.Space(4);

            // 2.5 Live Play Mode Testing Panel (1-Click Test Triệu Hồi Đội Hình Tức Thì)
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("🎮 LIVE PLAYMODE CONTROLLER", EditorStyles.boldLabel);

                    _testLeadDistance = EditorGUILayout.Slider("Cự ly xuất phát (m)", _testLeadDistance, 50f, 160f);

                    GUI.backgroundColor = new Color(0.2f, 0.75f, 1.0f, 1f); // Cyan chuẩn điện ảnh
                    if (GUILayout.Button($"🎬 TRIỆU HỒI CHUẨN ĐIỆN ẢNH (BÉZIER - {_testLeadDistance:F0}M)", GUILayout.Height(30)))
                    {
                        if (VirtualRailWaveSpawner.Instance != null)
                        {
                            bool ok = VirtualRailWaveSpawner.Instance.SpawnScriptedFormation(pattern, _testLeadDistance, -1f);
                            if (ok)
                            {
                                Debug.Log($"<color=cyan><b>[TEST SPAWN BÉZIER]</b></color> Đã triệu hồi phi đội [{pattern.patternName}] từ cự ly xa {_testLeadDistance:F0}m (Thời gian ghép đội: {pattern.assemblyTime:F1}s)!");
                            }
                            else
                            {
                                Debug.LogWarning($"[TEST SPAWN] Không thể triệu hồi đội hình [{pattern.patternName}]! Kiểm tra xem object pool có đủ quái không.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[TEST SPAWN] Không tìm thấy VirtualRailWaveSpawner trong scene!");
                        }
                    }

                    GUI.backgroundColor = new Color(0.25f, 0.85f, 0.45f, 1f); // Xanh lá ngắm bắn ngay
                    if (GUILayout.Button("⚡ TRIỆU HỒI BẮN NGAY (KHÓA SLOT 0S - 65M)", GUILayout.Height(30)))
                    {
                        if (VirtualRailWaveSpawner.Instance != null)
                        {
                            bool ok = VirtualRailWaveSpawner.Instance.SpawnScriptedFormation(pattern, 65f, 0f);
                            if (ok)
                            {
                                Debug.Log($"<color=green><b>[TEST SPAWN INSTANT]</b></color> Đã triệu hồi tức thì phi đội [{pattern.patternName}] vào vị trí khóa Slot tại cự ly 65m để tập bắn!");
                            }
                            else
                            {
                                Debug.LogWarning($"[TEST SPAWN] Không thể triệu hồi đội hình [{pattern.patternName}]! Kiểm tra xem object pool có đủ quái không.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[TEST SPAWN] Không tìm thấy VirtualRailWaveSpawner trong scene!");
                        }
                    }

                    GUI.backgroundColor = new Color(1f, 0.35f, 0.35f, 1f); // Màu đỏ cảnh báo
                    if (GUILayout.Button("💥 KÍCH NỔ CHỈ HUY (TEST LEADER DEATH REACTION)", GUILayout.Height(26)))
                    {
                        VirtualFormationAnchor activeAnchor = UnityEngine.Object.FindAnyObjectByType<VirtualFormationAnchor>();
                        if (activeAnchor != null && activeAnchor.HasActiveLeader)
                        {
                            var health = activeAnchor.Leader.GetComponent<EnemyHealth>();
                            if (health != null)
                            {
                                health.TakeDamage(9999, activeAnchor.Leader.transform.position, Vector3.forward);
                                Debug.Log($"<color=red><b>[TEST LEADER DEATH]</b></color> Đã kích nổ Chỉ Huy của [{pattern.patternName}]! Hãy quan sát phản xạ của tàu cánh.");
                            }
                            else
                            {
                                activeAnchor.Leader.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[TEST LEADER DEATH] Không tìm thấy Tàu Chỉ Huy đang hoạt động trong scene!");
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("🚀 BẤM PLAY ĐỂ KÍCH HOẠT NÚT TRIỆU HỒI TRONG GAME", GUILayout.Height(26));
                GUI.enabled = true;
            }

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
            _showSlotsFold = DrawSectionHeader(_showSlotsFold, $"1. Slot Matrix (Danh sách {pattern.ShipCount} vị trí tàu)");
            if (_showSlotsFold)
            {
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

                        bool isSelected = FormationEditorSelection.IsSelected(pattern, i);
                        if (isSelected) GUI.backgroundColor = new Color(1f, 0.95f, 0.5f, 1f);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        if (isSelected) GUI.backgroundColor = Color.white;

                        EditorGUILayout.BeginHorizontal();
                        {
                            string badge = isLeader.boolValue ? "👑 [LEADER]" : $"[SLOT {i}]";
                            EditorGUILayout.LabelField($"{badge} {slotName.stringValue}", EditorStyles.boldLabel);

                            if (GUILayout.Button(isSelected ? "🎯 ĐANG CHỌN" : "Chọn", EditorStyles.miniButton, GUILayout.Width(80)))
                            {
                                FormationEditorSelection.Select(pattern, i);
                                SceneView.RepaintAll();
                            }

                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            {
                                if (isSelected) FormationEditorSelection.Clear();
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
                        newSlot.FindPropertyRelative("localOffset").vector3Value = Vector3.zero;
                        newSlot.FindPropertyRelative("preferredArchetype").enumValueIndex = (int)EnemyArchetype.Flanker;
                        newSlot.FindPropertyRelative("isLeader").boolValue = false;
                        newSlot.FindPropertyRelative("scaleMultiplier").floatValue = 1.0f;
                        FormationEditorSelection.Select(pattern, _slotsProp.arraySize - 1);
                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // 5. Flight Dynamics
            _showDynamicsFold = DrawSectionHeader(_showDynamicsFold, "2. Flight Dynamics (Động Lực Học Bay Cả Đội)");
            if (_showDynamicsFold)
            {
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
            }

            EditorGUILayout.Space(6);

            // 6. Tactical Behavior
            _showTacticalFold = DrawSectionHeader(_showTacticalFold, "3. Tactical Reaction (Phản Ứng Khi Chỉ Huy Bị Hạ)");
            if (_showTacticalFold)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(_leaderDeathReactionProp, new GUIContent("Leader Death Reaction"));
                    LeaderDeathReaction react = (LeaderDeathReaction)_leaderDeathReactionProp.enumValueIndex;
                    string hint = react switch
                    {
                        LeaderDeathReaction.MaintainFormation => "Đội hình KHÔNG tan rã. Tàu cánh mất khiên nhưng giữ nguyên 100% cự ly và hình khối, tiếp tục chiến đấu!",
                        LeaderDeathReaction.BreakShieldAndEngage => "Mất khiên bảo vệ, thoát khỏi đội hình và tiếp tục chiến đấu tự do.",
                        LeaderDeathReaction.PanicScatter => "Tất cả tàu cánh mất khiên và hoảng loạn bẻ lái dạt biên thoát ly (Chuẩn Star Fox 64).",
                        LeaderDeathReaction.RetreatWarp => "Tất cả tàu cánh lập tức tăng tốc bứt phá vọt ra sau biến mất khỏi màn chơi.",
                        LeaderDeathReaction.KamikazeCharge => "Tất cả tàu cánh mất khiên và lao thẳng cảm tử vào tàu người chơi!",
                        _ => ""
                    };
                    EditorGUILayout.HelpBox(hint, MessageType.None);
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool DrawSectionHeader(bool foldout, string title)
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 24f);

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

        private enum RadarPlaneMode
        {
            FrontalXY = 0, // Kính ngắm Chính diện (Mặc định)
            TopDownXZ = 1  // Nhìn từ trên xuống
        }
        private static RadarPlaneMode _radarPlaneMode = RadarPlaneMode.FrontalXY;
        private static bool _isDragging2D = false;
        private static int _draggedSlotIndex = -1;
        private static float _radarScale = 4.5f;

        private void DrawTopDownRadarPreview(FormationPattern pattern)
        {
            if (pattern.slots == null) pattern.slots = Array.Empty<FormationSlotDefinition>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            {
                string title = _radarPlaneMode == RadarPlaneMode.FrontalXY
                    ? "🎯 Frontal HUD View (Kính ngắm Chính diện XY: X Ngang, Y Đứng)"
                    : "🗺️ Top-Down Radar View (Góc nhìn từ trên xuống XZ)";
                EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);

                EditorGUILayout.LabelField("Zoom:", GUILayout.Width(40));
                _radarScale = EditorGUILayout.Slider(_radarScale, 2.0f, 8.0f, GUILayout.Width(110));

                if (GUILayout.Button(_radarPlaneMode == RadarPlaneMode.FrontalXY ? "Đổi sang XZ (Mặt đất)" : "Đổi sang XY (Kính ngắm)", EditorStyles.miniButton, GUILayout.Width(130)))
                {
                    _radarPlaneMode = _radarPlaneMode == RadarPlaneMode.FrontalXY ? RadarPlaneMode.TopDownXZ : RadarPlaneMode.FrontalXY;
                }
            }
            EditorGUILayout.EndHorizontal();

            float width = EditorGUIUtility.currentViewWidth - 40f;
            float height = 180f;
            Rect radarRect = GUILayoutUtility.GetRect(width, height);
            EditorGUI.DrawRect(radarRect, new Color(0.05f, 0.08f, 0.12f, 1f));

            Vector2 center = new Vector2(radarRect.x + radarRect.width * 0.5f, radarRect.y + radarRect.height * 0.5f);
            Vector2 topCenter = new Vector2(radarRect.x + radarRect.width * 0.5f, radarRect.y + 35f);

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            // Xử lý tương tác Chuột và Phím tắt
            if (evt.type == EventType.MouseDown && radarRect.Contains(evt.mousePosition))
            {
                if (evt.button == 0) // Click chuột trái
                {
                    int hitSlot = -1;
                    for (int i = pattern.slots.Length - 1; i >= 0; i--)
                    {
                        var slot = pattern.slots[i];
                        Vector2 sPos = (_radarPlaneMode == RadarPlaneMode.FrontalXY)
                            ? center + new Vector2(slot.localOffset.x * _radarScale, -slot.localOffset.y * _radarScale)
                            : topCenter + new Vector2(slot.localOffset.x * _radarScale, -slot.localOffset.z * _radarScale);

                        if (Vector2.Distance(evt.mousePosition, sPos) <= 14f)
                        {
                            hitSlot = i;
                            break;
                        }
                    }

                    if (hitSlot >= 0)
                    {
                        FormationEditorSelection.Select(pattern, hitSlot);
                        _isDragging2D = true;
                        _draggedSlotIndex = hitSlot;
                        Undo.RecordObject(pattern, "Drag Formation Slot");
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                    else
                    {
                        if (evt.clickCount == 2)
                        {
                            // Double-click vào khoảng trống để thêm tàu mới ngay tại vị trí con trỏ
                            Undo.RecordObject(pattern, "Add Formation Slot at Cursor");
                            Vector3 newOffset;
                            if (_radarPlaneMode == RadarPlaneMode.FrontalXY)
                            {
                                float lx = (evt.mousePosition.x - center.x) / _radarScale;
                                float ly = -(evt.mousePosition.y - center.y) / _radarScale;
                                if (evt.control)
                                {
                                    lx = Mathf.Round(lx);
                                    ly = Mathf.Round(ly);
                                }
                                newOffset = new Vector3(lx, ly, 0f);
                            }
                            else
                            {
                                float lx = (evt.mousePosition.x - topCenter.x) / _radarScale;
                                float lz = -(evt.mousePosition.y - topCenter.y) / _radarScale;
                                if (evt.control)
                                {
                                    lx = Mathf.Round(lx);
                                    lz = Mathf.Round(lz);
                                }
                                newOffset = new Vector3(lx, 0f, lz);
                            }

                            var list = new System.Collections.Generic.List<FormationSlotDefinition>(pattern.slots);
                            int newIdx = list.Count;
                            list.Add(new FormationSlotDefinition
                            {
                                slotName = $"Wingman {newIdx}",
                                localOffset = newOffset,
                                preferredArchetype = EnemyArchetype.Flanker,
                                isLeader = false,
                                scaleMultiplier = 1.0f
                            });
                            pattern.slots = list.ToArray();
                            FormationEditorSelection.Select(pattern, newIdx);
                            EditorUtility.SetDirty(pattern);
                            serializedObject.Update();
                            evt.Use();
                            SceneView.RepaintAll();
                        }
                        else
                        {
                            FormationEditorSelection.Clear();
                            evt.Use();
                            SceneView.RepaintAll();
                        }
                    }
                }
            }
            else if (evt.type == EventType.MouseDrag && _isDragging2D && GUIUtility.hotControl == controlID)
            {
                if (_draggedSlotIndex >= 0 && _draggedSlotIndex < pattern.slots.Length)
                {
                    if (_radarPlaneMode == RadarPlaneMode.FrontalXY)
                    {
                        float lx = (evt.mousePosition.x - center.x) / _radarScale;
                        float ly = -(evt.mousePosition.y - center.y) / _radarScale;
                        if (evt.control)
                        {
                            lx = Mathf.Round(lx);
                            ly = Mathf.Round(ly);
                        }
                        pattern.slots[_draggedSlotIndex].localOffset = new Vector3(lx, ly, 0f);
                    }
                    else
                    {
                        float lx = (evt.mousePosition.x - topCenter.x) / _radarScale;
                        float lz = -(evt.mousePosition.y - topCenter.y) / _radarScale;
                        if (evt.control)
                        {
                            lx = Mathf.Round(lx);
                            lz = Mathf.Round(lz);
                        }
                        pattern.slots[_draggedSlotIndex].localOffset = new Vector3(lx, 0f, lz);
                    }

                    EditorUtility.SetDirty(pattern);
                    serializedObject.Update();
                    evt.Use();
                    SceneView.RepaintAll();
                }
            }
            else if (evt.type == EventType.MouseUp && _isDragging2D)
            {
                _isDragging2D = false;
                _draggedSlotIndex = -1;
                GUIUtility.hotControl = 0;
                evt.Use();
            }
            else if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                {
                    if (FormationEditorSelection.ActivePattern == pattern &&
                        FormationEditorSelection.SelectedSlotIndex >= 0 &&
                        FormationEditorSelection.SelectedSlotIndex < pattern.slots.Length)
                    {
                        Undo.RecordObject(pattern, "Delete Formation Slot");
                        var list = new System.Collections.Generic.List<FormationSlotDefinition>(pattern.slots);
                        list.RemoveAt(FormationEditorSelection.SelectedSlotIndex);
                        pattern.slots = list.ToArray();
                        FormationEditorSelection.Clear();
                        EditorUtility.SetDirty(pattern);
                        serializedObject.Update();
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                }
            }

            // VẼ NỀN TÂM NGẮM & LƯỚI TỌA ĐỘ
            if (_radarPlaneMode == RadarPlaneMode.FrontalXY)
            {
                // Vòng cự ly ngắm (Reticle Rings)
                Handles.color = new Color(0.15f, 0.45f, 0.65f, 0.25f);
                Handles.DrawWireDisc(new Vector3(center.x, center.y, 0f), Vector3.forward, 5f * _radarScale);
                Handles.DrawWireDisc(new Vector3(center.x, center.y, 0f), Vector3.forward, 10f * _radarScale);
                Handles.DrawWireDisc(new Vector3(center.x, center.y, 0f), Vector3.forward, 15f * _radarScale);
                Handles.DrawWireDisc(new Vector3(center.x, center.y, 0f), Vector3.forward, 20f * _radarScale);

                // Trục tâm chữ thập chính
                Handles.color = new Color(0.25f, 0.65f, 0.85f, 0.55f);
                Handles.DrawLine(new Vector3(radarRect.x, center.y, 0f), new Vector3(radarRect.x + radarRect.width, center.y, 0f));
                Handles.DrawLine(new Vector3(center.x, radarRect.y, 0f), new Vector3(center.x, radarRect.y + radarRect.height, 0f));

                // Lưới phụ đứt đoạn (Grid ticks mỗi 5m)
                Handles.color = new Color(0.2f, 0.5f, 0.7f, 0.2f);
                for (float m = -30f; m <= 30f; m += 5f)
                {
                    if (Mathf.Abs(m) < 0.1f) continue;
                    float gx = center.x + (m * _radarScale);
                    if (gx >= radarRect.x && gx <= radarRect.x + radarRect.width)
                    {
                        Handles.DrawLine(new Vector3(gx, center.y - 4f, 0f), new Vector3(gx, center.y + 4f, 0f));
                    }
                    float gy = center.y - (m * _radarScale);
                    if (gy >= radarRect.y && gy <= radarRect.y + radarRect.height)
                    {
                        Handles.DrawLine(new Vector3(center.x - 4f, gy, 0f), new Vector3(center.x + 4f, gy, 0f));
                    }
                }
            }
            else
            {
                // VẼ GÓC NHÌN TỪ TRÊN XUỐNG XZ (TOP-DOWN)
                Handles.color = new Color(0.3f, 0.5f, 0.7f, 0.5f);
                Handles.DrawLine(new Vector3(radarRect.x, topCenter.y, 0f), new Vector3(radarRect.x + radarRect.width, topCenter.y, 0f));
                Handles.DrawLine(new Vector3(topCenter.x, radarRect.y, 0f), new Vector3(topCenter.x, radarRect.y + radarRect.height, 0f));
            }

            // VẼ TỪNG CON TÀU
            for (int i = 0; i < pattern.slots.Length; i++)
            {
                var slot = pattern.slots[i];
                Vector2 shipPos = (_radarPlaneMode == RadarPlaneMode.FrontalXY)
                    ? center + new Vector2(slot.localOffset.x * _radarScale, -slot.localOffset.y * _radarScale)
                    : topCenter + new Vector2(slot.localOffset.x * _radarScale, -slot.localOffset.z * _radarScale);

                bool isSelected = FormationEditorSelection.IsSelected(pattern, i);

                Color shipColor = slot.isLeader ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.85f, 1f);
                if (slot.preferredArchetype == EnemyArchetype.ShieldBearer) shipColor = new Color(0.85f, 0.35f, 1f);
                if (slot.preferredArchetype == EnemyArchetype.Interceptor) shipColor = new Color(1f, 0.35f, 0.35f);

                float size = slot.isLeader ? 12f : 9f;

                // Nếu slot đang được chọn: Vẽ vòng hào quang / viền nổi bật
                if (isSelected)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireDisc(new Vector3(shipPos.x, shipPos.y, 0f), Vector3.forward, size + 5f);
                    Handles.DrawWireDisc(new Vector3(shipPos.x, shipPos.y, 0f), Vector3.forward, size + 2f);
                }

                Rect shipRect = new Rect(shipPos.x - size * 0.5f, shipPos.y - size * 0.5f, size, size);
                EditorGUI.DrawRect(shipRect, isSelected ? Color.white : shipColor);

                // Nhãn thông tin tàu
                GUIStyle lblStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = isSelected ? Color.yellow : shipColor },
                    fontSize = isSelected ? 10 : 9,
                    fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
                };

                string coords = _radarPlaneMode == RadarPlaneMode.FrontalXY
                    ? $"({slot.localOffset.x:F1}, {slot.localOffset.y:F1})"
                    : $"({slot.localOffset.x:F1}, {slot.localOffset.z:F1})";
                string slotLabel = isSelected ? $"★ {slot.slotName} {coords}" : $"{slot.slotName}";
                GUI.Label(new Rect(shipPos.x + 8f, shipPos.y - 8f, 140f, 16f), slotLabel, lblStyle);
            }

            // Thanh Action Bar & Hướng dẫn bên dưới HUD
            EditorGUILayout.Space(2);
            if (FormationEditorSelection.ActivePattern == pattern &&
                FormationEditorSelection.SelectedSlotIndex >= 0 &&
                FormationEditorSelection.SelectedSlotIndex < pattern.slots.Length)
            {
                int selIdx = FormationEditorSelection.SelectedSlotIndex;
                var selSlot = pattern.slots[selIdx];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    string info = $"🎯 ĐANG CHỌN: [{selIdx}] {selSlot.slotName} | X: {selSlot.localOffset.x:F1}m, Y: {selSlot.localOffset.y:F1}m, Z: {selSlot.localOffset.z:F1}m";
                    EditorGUILayout.LabelField(info, EditorStyles.boldLabel);

                    if (GUILayout.Button("👑 Đặt Leader", EditorStyles.miniButton, GUILayout.Width(90)))
                    {
                        Undo.RecordObject(pattern, "Set As Leader");
                        for (int k = 0; k < pattern.slots.Length; k++)
                        {
                            pattern.slots[k].isLeader = (k == selIdx);
                        }
                        EditorUtility.SetDirty(pattern);
                        serializedObject.Update();
                    }

                    if (GUILayout.Button("🪞 Đảo X", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        Undo.RecordObject(pattern, "Mirror Slot X");
                        pattern.slots[selIdx].localOffset.x = -pattern.slots[selIdx].localOffset.x;
                        EditorUtility.SetDirty(pattern);
                        serializedObject.Update();
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("🗑️ Xóa", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        Undo.RecordObject(pattern, "Delete Slot");
                        var list = new System.Collections.Generic.List<FormationSlotDefinition>(pattern.slots);
                        list.RemoveAt(selIdx);
                        pattern.slots = list.ToArray();
                        FormationEditorSelection.Clear();
                        EditorUtility.SetDirty(pattern);
                        serializedObject.Update();
                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("💡 Kéo chuột để di chuyển tàu | Double Click vùng trống để thêm tàu mới | Phím Delete để xóa | Giữ Ctrl để bắt dính lưới (Snap 1m)", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyPresetWedgeV()
        {
            Undo.RecordObject(target, "Apply Wedge V Preset (Coplanar XY)");
            _slotsProp.arraySize = 5;

            SetSlot(0, "Leader", new Vector3(0f, -2.0f, 0f), EnemyArchetype.SquadLeader, true, 1.0f);
            SetSlot(1, "Left Wing 1", new Vector3(-8f, 2.0f, 0f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(2, "Right Wing 1", new Vector3(8f, 2.0f, 0f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(3, "Left Wing 2", new Vector3(-16f, 6.0f, 0f), EnemyArchetype.Scout, false, 1.0f);
            SetSlot(4, "Right Wing 2", new Vector3(16f, 6.0f, 0f), EnemyArchetype.Scout, false, 1.0f);
        }

        private void ApplyPresetEchelonLine()
        {
            Undo.RecordObject(target, "Apply Echelon Line Preset (Coplanar XY)");
            _slotsProp.arraySize = 3;

            SetSlot(0, "Leader", new Vector3(0f, 0f, 0f), EnemyArchetype.SquadLeader, true, 1.0f);
            SetSlot(1, "Left Wing", new Vector3(-12f, 2.5f, 0f), EnemyArchetype.Flanker, false, 1.0f);
            SetSlot(2, "Right Wing", new Vector3(12f, -2.5f, 0f), EnemyArchetype.Flanker, false, 1.0f);
        }

        private void ApplyPresetDiamond()
        {
            Undo.RecordObject(target, "Apply Diamond Preset (Coplanar XY)");
            _slotsProp.arraySize = 4;

            SetSlot(0, "Apex Leader", new Vector3(0f, 5f, 0f), EnemyArchetype.SquadLeader, true, 1.0f);
            SetSlot(1, "Left Guard", new Vector3(-8f, 0f, 0f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(2, "Right Guard", new Vector3(8f, 0f, 0f), EnemyArchetype.ShieldBearer, false, 1.0f);
            SetSlot(3, "Rear Anchor", new Vector3(0f, -5f, 0f), EnemyArchetype.Interceptor, false, 1.0f);
        }

        private void ApplyPresetSpiralStream()
        {
            Undo.RecordObject(target, "Apply Wave Stream Preset (Coplanar XY)");
            _slotsProp.arraySize = 5;

            SetSlot(0, "Head", new Vector3(0f, 0f, 0f), EnemyArchetype.SquadLeader, true, 1.0f);
            SetSlot(1, "Segment 1", new Vector3(7f, 4f, 0f), EnemyArchetype.Interceptor, false, 1.0f);
            SetSlot(2, "Segment 2", new Vector3(12f, 0f, 0f), EnemyArchetype.Interceptor, false, 1.0f);
            SetSlot(3, "Segment 3", new Vector3(7f, -4f, 0f), EnemyArchetype.Interceptor, false, 1.0f);
            SetSlot(4, "Segment 4", new Vector3(0f, -8f, 0f), EnemyArchetype.Interceptor, false, 1.0f);
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

            // 1. Wedge V (Coplanar XY)
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
                    new FormationSlotDefinition { slotName = "Leader", localOffset = new Vector3(0f, -2.0f, 0f), preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Left Wing 1", localOffset = new Vector3(-8f, 2.0f, 0f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Wing 1", localOffset = new Vector3(8f, 2.0f, 0f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Left Wing 2", localOffset = new Vector3(-16f, 6.0f, 0f), preferredArchetype = EnemyArchetype.Scout, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Wing 2", localOffset = new Vector3(16f, 6.0f, 0f), preferredArchetype = EnemyArchetype.Scout, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            // 2. Echelon Line (Coplanar XY)
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
                    new FormationSlotDefinition { slotName = "Leader", localOffset = new Vector3(0f, 0f, 0f), preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Left Wing", localOffset = new Vector3(-12f, 2.5f, 0f), preferredArchetype = EnemyArchetype.Flanker, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Wing", localOffset = new Vector3(12f, -2.5f, 0f), preferredArchetype = EnemyArchetype.Flanker, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            // 3. Diamond 4 (Coplanar XY)
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
                    new FormationSlotDefinition { slotName = "Apex Leader", localOffset = new Vector3(0f, 5f, 0f), preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Left Guard", localOffset = new Vector3(-8f, 0f, 0f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Right Guard", localOffset = new Vector3(8f, 0f, 0f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Rear Anchor", localOffset = new Vector3(0f, -5f, 0f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f }
                };
            });

            // 4. Spiral Stream (Coplanar XY)
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
                    new FormationSlotDefinition { slotName = "Head", localOffset = new Vector3(0f, 0f, 0f), preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 1", localOffset = new Vector3(7f, 4f, 0f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 2", localOffset = new Vector3(12f, 0f, 0f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 3", localOffset = new Vector3(7f, -4f, 0f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f },
                    new FormationSlotDefinition { slotName = "Segment 4", localOffset = new Vector3(0f, -8f, 0f), preferredArchetype = EnemyArchetype.Interceptor, isLeader = false, scaleMultiplier = 1.0f }
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
