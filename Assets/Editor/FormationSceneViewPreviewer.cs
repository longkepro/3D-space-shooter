using System;
using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Custom Scene View Previewer chuyên nghiệp cho Đội hình bay (Formation Patterns).
    /// Chuẩn công nghiệp:
    /// 1. Tách biệt hoàn toàn (Decoupled Module) - 0 can thiệp Game Logic.
    /// 2. 0 GC Allocation trong vòng lặp OnSceneGUI (dùng Pre-allocated buffer).
    /// 3. Hỗ trợ Timeline Scrubber và Playback thời gian thực ngay trong Scene View mà không cần Play Game!
    /// 4. Tái sử dụng lõi toán học tất định FormationTrajectoryEvaluator.
    /// </summary>
    public class FormationSceneViewPreviewer : EditorWindow
    {
        private static FormationSceneViewPreviewer _instance;

        [Header("Target Pattern")]
        [SerializeField] private FormationPattern _targetPattern;

        [Header("Preview Settings")]
        [SerializeField] private bool _enablePreview = true;
        [SerializeField] private bool _isPlaying = false;
        [SerializeField] private float _playbackSpeed = 1.0f;
        [SerializeField] private float _previewTime = 0.0f;
        [SerializeField] private Vector3 _previewOrigin = new Vector3(0f, 0f, 75f);

        [Header("Visualization Toggles")]
        [SerializeField] private bool _showWireframeShips = true;
        [SerializeField] private bool _showTrajectories = true;
        [SerializeField] private bool _showTacticalLabels = true;
        [SerializeField] private bool _showAssemblyCurves = true;

        // Buffers tiền cấp phát (0 GC Alloc)
        private static readonly SlotPose[] _cachedPoses = new SlotPose[32];
        private static readonly Vector3[] _bezierSegmentBuffer = new Vector3[21];
        private static double _lastEditorTime = 0;

        // Mini floating toolbar rect
        private Rect _overlayRect = new Rect(16, 24, 280, 175);

        [MenuItem("Tools/Virtual Rail/60. Formation Scene View Previewer (Xem Trước Đội Hình 3D)")]
        public static void OpenWindow()
        {
            _instance = GetWindow<FormationSceneViewPreviewer>("Formation 3D Previewer");
            _instance.minSize = new Vector2(340, 480);
            _instance.Show();
        }

        public static void OpenWithPattern(FormationPattern pattern)
        {
            OpenWindow();
            if (_instance != null)
            {
                _instance._targetPattern = pattern;
                _instance._enablePreview = true;
                _instance._previewTime = 0f;
                _instance.Repaint();
                SceneView.RepaintAll();
            }
        }

        private void OnEnable()
        {
            _instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
            _lastEditorTime = EditorApplication.timeSinceStartup;

            // Tự động tìm pattern mặc định nếu chưa gán
            if (_targetPattern == null)
            {
                _targetPattern = AssetDatabase.LoadAssetAtPath<FormationPattern>("Assets/Scripts/VirtualRail/Formation_WedgeV.asset");
            }

            // Đồng bộ tọa độ gốc theo VirtualRailAnchor nếu có trong scene
            SyncOriginWithSceneAnchor();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            _isPlaying = false;
            SceneView.RepaintAll();
        }

        private void SyncOriginWithSceneAnchor()
        {
            VirtualRailAnchor anchor = UnityEngine.Object.FindAnyObjectByType<VirtualRailAnchor>();
            if (anchor != null)
            {
                _previewOrigin = anchor.transform.position + (anchor.ForwardTangent * 65f);
            }
            else
            {
                _previewOrigin = new Vector3(0f, 0f, 75f);
            }
        }

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            double dt = now - _lastEditorTime;
            _lastEditorTime = now;

            if (_isPlaying && _enablePreview && _targetPattern != null)
            {
                float maxLife = Mathf.Max(_targetPattern.maxLifetime, 8f);
                _previewTime += (float)dt * _playbackSpeed;
                if (_previewTime > maxLife)
                {
                    _previewTime = 0f; // Tự động lặp lại chu kỳ
                }

                Repaint();
                SceneView.RepaintAll();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField("🛸 FORMATION SCENE VIEW PREVIEWER", titleStyle);
                EditorGUILayout.HelpBox("Công cụ xem trước và tinh chỉnh đội hình bay trực tiếp trên Scene View 3D không cần chạy game!", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 1. Target Pattern
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("1. Nguồn Dữ Liệu Đội Hình", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                _targetPattern = (FormationPattern)EditorGUILayout.ObjectField("Formation Pattern", _targetPattern, typeof(FormationPattern), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _previewTime = 0f;
                    SceneView.RepaintAll();
                }

                _enablePreview = EditorGUILayout.Toggle("Kích Hoạt Preview 3D", _enablePreview);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2. Playback & Timeline Scrubber
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("2. Điều Khiển Thời Gian (Timeline Scrubber)", EditorStyles.boldLabel);

                float maxLifetime = _targetPattern != null ? Mathf.Max(_targetPattern.maxLifetime, 10f) : 14f;
                float assemblyTime = _targetPattern != null ? _targetPattern.assemblyTime : 1.2f;

                EditorGUI.BeginChangeCheck();
                _previewTime = EditorGUILayout.Slider($"Thời Gian (s)", _previewTime, 0f, maxLifetime);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }

                string phaseText = _previewTime < assemblyTime ? $"🟡 Pha 1: Ráp nối Bézier ({_previewTime:F2}s / {assemblyTime:F2}s)" : $"🟢 Pha 2: Khóa Slot & Lượn sóng ({_previewTime:F2}s)";
                EditorGUILayout.LabelField(phaseText, EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(_isPlaying ? "⏸ Tạm Dừng" : "▶ Chạy Mô Phỏng", GUILayout.Height(28)))
                {
                    _isPlaying = !_isPlaying;
                    _lastEditorTime = EditorApplication.timeSinceStartup;
                }
                if (GUILayout.Button("⏪ Về 0.0s", GUILayout.Height(28), GUILayout.Width(75)))
                {
                    _previewTime = 0f;
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();

                _playbackSpeed = EditorGUILayout.Slider("Tốc Độ Playback", _playbackSpeed, 0.2f, 2.0f);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 3. Toggles Hiển Thị
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("3. Bộ Lọc Hiển Thị (Display Layers)", EditorStyles.boldLabel);
                _showWireframeShips = EditorGUILayout.Toggle("Vẽ Khối Tàu 3D (Ghost Ships)", _showWireframeShips);
                _showAssemblyCurves = EditorGUILayout.Toggle("Vẽ Đường Bay Bézier (Assembly)", _showAssemblyCurves);
                _showTrajectories = EditorGUILayout.Toggle("Vẽ Đường Lượn Đội Hình (Sway)", _showTrajectories);
                _showTacticalLabels = EditorGUILayout.Toggle("Hiện Nhãn Chiến Thuật (Labels)", _showTacticalLabels);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 4. Định vị không gian
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("4. Tọa Độ Gốc (Preview Origin)", EditorStyles.boldLabel);
                _previewOrigin = EditorGUILayout.Vector3Field("Origin World Pos", _previewOrigin);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Đồng Bộ Anchor Scene"))
                {
                    SyncOriginWithSceneAnchor();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Focus Scene View"))
                {
                    FocusSceneView();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 5. Tinh Chỉnh Slot Tàu Đang Chọn (Interactive Slot Tuner)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("5. Tinh Chỉnh Slot Tàu (Interactive Slot Tuner)", EditorStyles.boldLabel);

                if (_targetPattern != null &&
                    FormationEditorSelection.ActivePattern == _targetPattern &&
                    FormationEditorSelection.SelectedSlotIndex >= 0 &&
                    FormationEditorSelection.SelectedSlotIndex < _targetPattern.slots.Length)
                {
                    int selIdx = FormationEditorSelection.SelectedSlotIndex;
                    var selSlot = _targetPattern.slots[selIdx];

                    EditorGUILayout.LabelField($"🛸 Đang chọn: Slot {selIdx} ({selSlot.slotName})", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.TextField("Tên Slot", selSlot.slotName);
                    Vector3 newOffset = EditorGUILayout.Vector3Field("Tọa độ Offset (m)", selSlot.localOffset);
                    newOffset.z = 0f; // Luôn giữ chuẩn phẳng XY
                    EnemyArchetype newArch = (EnemyArchetype)EditorGUILayout.EnumPopup("Loại Tàu", selSlot.preferredArchetype);
                    bool newLeader = EditorGUILayout.Toggle("Là Chỉ Huy (Leader)", selSlot.isLeader);
                    float newScale = EditorGUILayout.Slider("Hệ Số Kích Thước", selSlot.scaleMultiplier, 0.5f, 2.5f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_targetPattern, "Modify Slot Properties");
                        selSlot.slotName = newName;
                        selSlot.localOffset = newOffset;
                        selSlot.preferredArchetype = newArch;
                        selSlot.isLeader = newLeader;
                        selSlot.scaleMultiplier = newScale;
                        _targetPattern.slots[selIdx] = selSlot;
                        EditorUtility.SetDirty(_targetPattern);
                        SceneView.RepaintAll();
                    }

                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("🪞 Đảo X (Mirror)", GUILayout.Height(22)))
                    {
                        Undo.RecordObject(_targetPattern, "Mirror Slot X");
                        _targetPattern.slots[selIdx].localOffset.x = -_targetPattern.slots[selIdx].localOffset.x;
                        EditorUtility.SetDirty(_targetPattern);
                        SceneView.RepaintAll();
                    }
                    if (GUILayout.Button("🗑️ Xóa Slot", GUILayout.Height(22)))
                    {
                        Undo.RecordObject(_targetPattern, "Delete Slot");
                        var list = new System.Collections.Generic.List<FormationSlotDefinition>(_targetPattern.slots);
                        list.RemoveAt(selIdx);
                        _targetPattern.slots = list.ToArray();
                        FormationEditorSelection.Clear();
                        EditorUtility.SetDirty(_targetPattern);
                        SceneView.RepaintAll();
                    }
                    if (GUILayout.Button("Bỏ Chọn", GUILayout.Height(22)))
                    {
                        FormationEditorSelection.Clear();
                        SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("💡 Click vào bất kỳ con tàu nào trong Scene View hoặc trên 2D Inspector để chọn và hiện mũi tên điều hướng Handles!", MessageType.Info);
                }

                EditorGUILayout.Space(2);
                if (_targetPattern != null && GUILayout.Button("➕ Thêm Slot Tàu Mới Tại Tâm (0, 0, 0)", GUILayout.Height(26)))
                {
                    Undo.RecordObject(_targetPattern, "Add Slot in Scene View");
                    var list = new System.Collections.Generic.List<FormationSlotDefinition>(_targetPattern.slots);
                    int newIdx = list.Count;
                    list.Add(new FormationSlotDefinition
                    {
                        slotName = $"Wingman {newIdx}",
                        localOffset = Vector3.zero,
                        preferredArchetype = EnemyArchetype.Flanker,
                        isLeader = false,
                        scaleMultiplier = 1.0f
                    });
                    _targetPattern.slots = list.ToArray();
                    FormationEditorSelection.Select(_targetPattern, newIdx);
                    EditorUtility.SetDirty(_targetPattern);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void FocusSceneView()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(_previewOrigin, Quaternion.Euler(15f, 180f, 0f), 45f);
            }
        }

        /// <summary>
        /// Vòng lặp vẽ trực tiếp trên Scene View (0 GC Allocation).
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_enablePreview || _targetPattern == null) return;

            // 1. Tính toán Pose của tất cả các tàu tại thời điểm _previewTime
            Vector3 railForward = Vector3.forward;
            Vector3 railRight = Vector3.right;
            Quaternion baseRot = Quaternion.LookRotation(-railForward, Vector3.up);

            int slotCount = FormationTrajectoryEvaluator.EvaluateAllSlots(
                _targetPattern,
                _previewTime,
                _previewOrigin,
                baseRot,
                railForward,
                railRight,
                _cachedPoses);

            // 2. Vẽ tâm Đội hình (Formation Anchor)
            FormationTrajectoryEvaluator.EvaluateAnchor(
                _targetPattern,
                _previewTime,
                _previewOrigin,
                baseRot,
                railForward,
                railRight,
                out Vector3 anchorPos,
                out Quaternion anchorRot);

            DrawFormationAnchor(anchorPos, anchorRot);

            // 3. Vẽ đường lượn sóng tổng thể (Sway Trajectory)
            if (_showTrajectories)
            {
                DrawSwayTrajectoryCurve(baseRot, railForward, railRight);
            }

            // 4. Vẽ từng slot tàu và bắt sự kiện click chọn
            for (int i = 0; i < slotCount; i++)
            {
                SlotPose pose = _cachedPoses[i];

                // Cho phép click chọn tàu trực tiếp trong Scene View
                float handleSize = HandleUtility.GetHandleSize(pose.position);
                float pickRadius = handleSize * 0.22f;
                if (Handles.Button(pose.position, Quaternion.identity, pickRadius, pickRadius * 1.2f, Handles.SphereHandleCap))
                {
                    FormationEditorSelection.Select(_targetPattern, i);
                    Repaint();
                    EditorUtility.SetDirty(_targetPattern);
                }

                // Vẽ đường cong tiếp cận Bézier
                if (_showAssemblyCurves)
                {
                    DrawSlotAssemblyCurve(pose);
                }

                // Vẽ khối tàu 3D
                if (_showWireframeShips)
                {
                    DrawGhostShip(pose);
                }

                // Vẽ nhãn chiến thuật
                if (_showTacticalLabels)
                {
                    DrawTacticalLabel(pose);
                }
            }

            // 5. Handles kéo-thả vị trí 3D cho Slot đang chọn
            if (_targetPattern != null &&
                FormationEditorSelection.ActivePattern == _targetPattern &&
                FormationEditorSelection.SelectedSlotIndex >= 0 &&
                FormationEditorSelection.SelectedSlotIndex < _targetPattern.slots.Length)
            {
                int selIdx = FormationEditorSelection.SelectedSlotIndex;
                var selDef = _targetPattern.slots[selIdx];

                // Tọa độ thế giới mục tiêu của slot căn cứ vào Anchor hiện tại
                Vector3 slotWorldTarget = anchorPos + (anchorRot * selDef.localOffset);

                // Vẽ vòng hào quang vàng rực bao quanh tàu đang chọn
                Handles.color = new Color(1f, 0.9f, 0.1f, 0.9f);
                Handles.DrawWireDisc(slotWorldTarget, anchorRot * Vector3.forward, 3.5f);
                Handles.DrawWireCube(slotWorldTarget, new Vector3(5f, 2.5f, 5f));

                // Mũi tên điều hướng Handles.PositionHandle
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPos = Handles.PositionHandle(slotWorldTarget, anchorRot);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_targetPattern, "Move Formation Slot (Scene View 3D)");
                    Vector3 deltaWorld = newWorldPos - anchorPos;
                    Vector3 newLocalOffset = Quaternion.Inverse(anchorRot) * deltaWorld;

                    // Luôn bảo toàn mặt phẳng XY chính diện (Z = 0)
                    newLocalOffset.z = 0f;

                    // Bắt dính lưới nếu giữ Ctrl
                    if (Event.current.control)
                    {
                        newLocalOffset.x = Mathf.Round(newLocalOffset.x);
                        newLocalOffset.y = Mathf.Round(newLocalOffset.y);
                    }

                    _targetPattern.slots[selIdx].localOffset = newLocalOffset;
                    EditorUtility.SetDirty(_targetPattern);
                    Repaint();
                }

                // Hiển thị HUD nhãn tọa độ thời gian thực trên Scene View
                GUIStyle selStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.yellow },
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter
                };
                Handles.Label(slotWorldTarget + (Vector3.up * 2.8f),
                    $"🎯 [SLOT {selIdx}: {selDef.slotName}]\nX: {selDef.localOffset.x:F1}m | Y: {selDef.localOffset.y:F1}m (Snap: Giữ Ctrl)",
                    selStyle);
            }

            // 6. Xử lý phím tắt Delete trong Scene View
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace))
            {
                if (_targetPattern != null &&
                    FormationEditorSelection.ActivePattern == _targetPattern &&
                    FormationEditorSelection.SelectedSlotIndex >= 0 &&
                    FormationEditorSelection.SelectedSlotIndex < _targetPattern.slots.Length)
                {
                    Undo.RecordObject(_targetPattern, "Delete Formation Slot (Scene View)");
                    var list = new System.Collections.Generic.List<FormationSlotDefinition>(_targetPattern.slots);
                    list.RemoveAt(FormationEditorSelection.SelectedSlotIndex);
                    _targetPattern.slots = list.ToArray();
                    FormationEditorSelection.Clear();
                    EditorUtility.SetDirty(_targetPattern);
                    evt.Use();
                    Repaint();
                }
            }

            // 7. Vẽ mini toolbar nổi trong Scene View
            Handles.BeginGUI();
            _overlayRect = GUILayout.Window(987654, _overlayRect, DrawSceneViewOverlayWindow, "🛸 Formation Scrubber", GUI.skin.window);
            Handles.EndGUI();
        }

        private void DrawFormationAnchor(Vector3 pos, Quaternion rot)
        {
            Handles.color = new Color(0f, 1f, 1f, 0.7f);
            Handles.DrawWireDisc(pos, Vector3.up, 2.5f);
            Handles.color = new Color(1f, 0.8f, 0f, 0.9f);
            Handles.DrawLine(pos, pos + (rot * Vector3.forward * 4f));
        }

        private void DrawGhostShip(SlotPose pose)
        {
            Color shipColor;
            if (pose.isLeader)
            {
                shipColor = new Color(1f, 0.85f, 0.1f, 0.95f); // Vàng Hoàng Kim cho Chỉ Huy
            }
            else if (pose.preferredArchetype == EnemyArchetype.ShieldBearer)
            {
                shipColor = new Color(0.8f, 0.2f, 1f, 0.9f);  // Tím Neon cho Tàu Lá Chắn
            }
            else
            {
                shipColor = new Color(0.2f, 0.9f, 1f, 0.85f); // Xanh Cyan cho Tàu Cánh
            }

            Matrix4x4 shipMatrix = Matrix4x4.TRS(pose.position, pose.rotation, pose.scale);
            using (new Handles.DrawingScope(shipColor, shipMatrix))
            {
                // Thân tàu (Hình thoi mũi nhọn bay kiểu Star Fox Arwing)
                Vector3 nose = new Vector3(0f, 0f, 2.5f);
                Vector3 leftWing = new Vector3(-2.2f, 0f, -1.2f);
                Vector3 rightWing = new Vector3(2.2f, 0f, -1.2f);
                Vector3 dorsal = new Vector3(0f, 1.2f, -0.6f);
                Vector3 tail = new Vector3(0f, 0f, -1.8f);

                Handles.DrawLine(nose, leftWing);
                Handles.DrawLine(nose, rightWing);
                Handles.DrawLine(nose, dorsal);
                Handles.DrawLine(leftWing, tail);
                Handles.DrawLine(rightWing, tail);
                Handles.DrawLine(dorsal, tail);
                Handles.DrawLine(leftWing, dorsal);
                Handles.DrawLine(rightWing, dorsal);

                // Khung biên thể tích
                Handles.DrawWireCube(Vector3.zero, new Vector3(4.5f, 1.8f, 4f));
            }
        }

        private void DrawSlotAssemblyCurve(SlotPose pose)
        {
            const int segments = 20;
            for (int s = 0; s <= segments; s++)
            {
                float u = (float)s / segments;
                _bezierSegmentBuffer[s] = FormationTrajectoryEvaluator.EvaluateCubicBezier(
                    pose.spawnPos,
                    pose.ctrl1,
                    pose.ctrl2,
                    pose.targetSlotWorld,
                    u);
            }

            Handles.color = pose.isAssembled ? new Color(0.2f, 0.7f, 0.3f, 0.35f) : new Color(1f, 0.5f, 0f, 0.75f);
            Handles.DrawAAPolyLine(2.5f, _bezierSegmentBuffer);

            // Điểm xuất phát ngoài màn hình
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            Handles.DrawWireDisc(pose.spawnPos, Vector3.up, 1.2f);
        }

        private void DrawSwayTrajectoryCurve(Quaternion baseRot, Vector3 railFwd, Vector3 railRight)
        {
            const int steps = 30;
            float maxTime = Mathf.Max(_targetPattern.maxLifetime, 10f);
            Vector3 prevPt = Vector3.zero;

            Handles.color = new Color(0f, 0.8f, 1f, 0.3f);
            for (int i = 0; i <= steps; i++)
            {
                float t = (maxTime / steps) * i;
                FormationTrajectoryEvaluator.EvaluateAnchor(_targetPattern, t, _previewOrigin, baseRot, railFwd, railRight, out Vector3 p, out _);
                if (i > 0)
                {
                    Handles.DrawDottedLine(prevPt, p, 3.5f);
                }
                prevPt = p;
            }
        }

        private void DrawTacticalLabel(SlotPose pose)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = pose.isLeader ? Color.yellow : Color.white }
            };

            string status = pose.isAssembled ? "LOCKED" : "ASSEMBLING";
            string text = $"{pose.slotName}\n[{pose.preferredArchetype}] ({status})";
            Handles.Label(pose.position + (Vector3.up * 2.2f), text, labelStyle);
        }

        private void DrawSceneViewOverlayWindow(int windowID)
        {
            float maxLifetime = _targetPattern != null ? Mathf.Max(_targetPattern.maxLifetime, 10f) : 14f;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_isPlaying ? "⏸" : "▶", GUILayout.Width(30), GUILayout.Height(22)))
            {
                _isPlaying = !_isPlaying;
                _lastEditorTime = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("0s", GUILayout.Width(28), GUILayout.Height(22)))
            {
                _previewTime = 0f;
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            _previewTime = GUILayout.HorizontalSlider(_previewTime, 0f, maxLifetime);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
            EditorGUILayout.LabelField($"{_previewTime:F1}s", GUILayout.Width(34));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            _showWireframeShips = GUILayout.Toggle(_showWireframeShips, "Ships", EditorStyles.miniButtonLeft);
            _showAssemblyCurves = GUILayout.Toggle(_showAssemblyCurves, "Bézier", EditorStyles.miniButtonMid);
            _showTrajectories = GUILayout.Toggle(_showTrajectories, "Sway", EditorStyles.miniButtonMid);
            _showTacticalLabels = GUILayout.Toggle(_showTacticalLabels, "Tags", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus View (F)", EditorStyles.miniButton))
            {
                FocusSceneView();
            }

            if (_targetPattern != null &&
                FormationEditorSelection.ActivePattern == _targetPattern &&
                FormationEditorSelection.SelectedSlotIndex >= 0 &&
                FormationEditorSelection.SelectedSlotIndex < _targetPattern.slots.Length)
            {
                int selIdx = FormationEditorSelection.SelectedSlotIndex;
                var selSlot = _targetPattern.slots[selIdx];
                EditorGUILayout.LabelField($"★ S{selIdx} ({selSlot.localOffset.x:F0},{selSlot.localOffset.y:F0})", EditorStyles.miniBoldLabel, GUILayout.Width(75));

                if (GUILayout.Button("Del", EditorStyles.miniButton, GUILayout.Width(35)))
                {
                    Undo.RecordObject(_targetPattern, "Delete Slot");
                    var list = new System.Collections.Generic.List<FormationSlotDefinition>(_targetPattern.slots);
                    list.RemoveAt(selIdx);
                    _targetPattern.slots = list.ToArray();
                    FormationEditorSelection.Clear();
                    EditorUtility.SetDirty(_targetPattern);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
