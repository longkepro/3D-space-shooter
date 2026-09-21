using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Phòng Triển Lãm và Kiểm Tra Toàn Diện Kẻ Địch (Enemy Showcase Gallery).
    /// Cho phép người dùng quan sát 360 độ từng model, màu sơn, cụm ống xả tím,
    /// nòng súng Laser và hiệu ứng lồng khiên năng lượng AuraBubble.
    /// Tuân thủ quy tắc 0 GC Alloc trong Update().
    /// </summary>
    public class EnemyShowcaseGallery : MonoBehaviour
    {
        public static EnemyShowcaseGallery Instance { get; private set; }

        public struct ShipSpec
        {
            public string name;
            public string archetype;
            public string modelName;
            public string materialName;
            public int health;
            public string weaponInfo;
            public string thrusterInfo;
            public string hitboxInfo;
            public string specialAbility;
        }

        [Header("Display Positions")]
        private static readonly Vector3[] SLOT_POSITIONS = new Vector3[]
        {
            new Vector3(-14f, 0f, 0f),
            new Vector3(-7f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(7f, 0f, 0f),
            new Vector3(14f, 0f, 0f)
        };

        private static readonly ShipSpec[] SHIP_SPECS = new ShipSpec[]
        {
            new ShipSpec
            {
                name = "Enemy (0) - Scout",
                archetype = "Trinh Sát Cơ Động (Fast Scout)",
                modelName = "Hi-Rez Spaceships Example3",
                materialName = "Xám Quân Sự Chống Nhận Diện (Custom Grey Coat)",
                health = 20,
                weaponInfo = "2 Khẩu Blaster mạn cánh (Laser chuẩn hóa)",
                thrusterInfo = "1 Luồng phản lực tím đặt tại Z = -2.10 (khớp ống xả)",
                hitboxInfo = "BoxCollider (4.4 x 1.6 x 4.8) - Vừa khít thân",
                specialAbility = "Lao bổ nhào cự ly gần, tốc độ cao nhất đội hình trinh sát"
            },
            new ShipSpec
            {
                name = "Enemy (1) - Flanker",
                archetype = "Tạt Sườn Bọc Hậu (Armored Flanker)",
                modelName = "Hi-Rez Spaceships Example5",
                materialName = "Xám Thép Mạ Crom (HiRez Factory Grey)",
                health = 40,
                weaponInfo = "2 Pháo Blaster mạn sườn (Laser chuẩn hóa)",
                thrusterInfo = "1 Ống xả trung tâm tại Z = -2.40 (khớp ống xả)",
                hitboxInfo = "BoxCollider (4.0 x 1.6 x 5.0) - Vừa khít thân",
                specialAbility = "Bay vòng tạt cánh trái/phải bắn chặn đón đầu đường bay"
            },
            new ShipSpec
            {
                name = "Enemy (2) - Interceptor",
                archetype = "Tiêm Kích Siêu Thanh (Interceptor)",
                modelName = "Hi-Rez Spaceships Example4",
                materialName = "Xám Titan Nhám Chống Nhiệt (HiRez Grey)",
                health = 35,
                weaponInfo = "2 Khẩu Laser cánh siêu thanh (Laser chuẩn hóa)",
                thrusterInfo = "1 Động cơ phản lực tăng áp tại Z = -2.50",
                hitboxInfo = "BoxCollider (4.2 x 1.6 x 4.8) - Vừa khít thân",
                specialAbility = "Điểm yếu đuôi tàu x200% DMG, bay tốc độ cao né tránh"
            },
            new ShipSpec
            {
                name = "Enemy (3) - Squad Leader",
                archetype = "Chiến Hạm Chỉ Huy Phi Đội (Flagship Leader)",
                modelName = "Hi-Rez Spaceships Example2 + Big_Launcher",
                materialName = "Giáp Hợp Kim Gia Cường (Tàu Chỉ Huy)",
                health = 80,
                weaponInfo = "Pháo hạng nặng Big_Launcher (Bắn loạt Burst 3 viên)",
                thrusterInfo = "2 Động cơ tím song song tại X = ±0.95, Z = -2.25",
                hitboxInfo = "BoxCollider (4.8 x 1.8 x 5.0) - Đã xóa Hitbox ma cũ (7.5x7.0)",
                specialAbility = "Dáng vóc 1.35x uy nghiêm, dẫn đầu đội hình Wedge V. Bị diệt gây Panic Scatter"
            },
            new ShipSpec
            {
                name = "Enemy (4) - Shield Bearer",
                archetype = "Chiến Đấu Cơ Hộ Vệ Bọc Khiên (Shield Bearer)",
                modelName = "Hi-Rez Spaceships Example1 + Kitbash Wing2/Wing6",
                materialName = "Xám Hợp Kim Cường Lực (HiRez Grey)",
                health = 40,
                weaponInfo = "2 Súng xung điện tần số cao (Laser chuẩn hóa)",
                thrusterInfo = "1 Động cơ phản lực tím tại Z = -2.40",
                hitboxInfo = "BoxCollider (4.8 x 1.8 x 5.2) - Bao bọc trọn vẹn sải cánh kitbash",
                specialAbility = "Lồng khiên năng lượng AuraBubble xoay tròn miễn nhiễm đạn thường"
            }
        };

        [Header("Spawned Displays")]
        private readonly GameObject[] _spawnedShips = new GameObject[5];
        private Camera _galleryCamera;

        [Header("Camera Control")]
        [SerializeField] private int _selectedIndex = 2; // Mặc định ở giữa (Interceptor)
        private bool _isOverview = false;
        private bool _autoRotate = true;
        private float _rotationSpeed = 25f;
        private float _cameraDistance = 7f;
        private float _orbitYaw = 180f; // Nhìn từ phía trước đầu tàu
        private float _orbitPitch = 15f;
        private Vector3 _currentCameraVelocity;
        private Vector3 _targetCameraPos;

        [Header("GUI Styling Cache")]
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _buttonActiveStyle;
        private GUIStyle _buttonInactiveStyle;
        private GUIStyle _labelBoldStyle;
        private GUIStyle _labelValueStyle;
        private GUIStyle _shortcutStyle;
        private Texture2D _bgCardTex;
        private Texture2D _btnActiveTex;
        private Texture2D _btnInactiveTex;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            SetupCamera();
            SpawnAllShips();
        }

        private void Start()
        {
            SelectShip(_selectedIndex);
        }

        private void SetupCamera()
        {
            _galleryCamera = Camera.main;
            if (_galleryCamera == null)
            {
                GameObject camObj = new GameObject("Showcase_Camera");
                _galleryCamera = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            _galleryCamera.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
            _galleryCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        public void SpawnAllShips()
        {
            string[] prefabPaths = new string[]
            {
                "Assets/Prefabs/Enemy (0) - Scout.prefab",
                "Assets/Prefabs/Enemy (1) - Flanker.prefab",
                "Assets/Prefabs/Enemy (2) - Interceptor.prefab",
                "Assets/Prefabs/Enemy (3) - Squad Leader.prefab",
                "Assets/Prefabs/Enemy (4) - Shield Bearer.prefab"
            };

            for (int i = 0; i < 5; i++)
            {
                if (_spawnedShips[i] != null) continue;

                GameObject prefab = null;
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
#endif
                if (prefab == null) continue;

                GameObject ship = Instantiate(prefab, SLOT_POSITIONS[i], Quaternion.Euler(0f, 180f, 0f), transform);
                ship.name = $"[Showcase_{i}] {prefab.name}";

                // Vô hiệu hóa AI và Physics để tàu đứng yên trên bệ triển lãm
                var movement = ship.GetComponent<EnemyMovement>();
                if (movement != null) movement.enabled = false;

                var attack = ship.GetComponent<EnemyAttack>();
                if (attack != null) attack.enabled = false;

                var rb = ship.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // Đảm bảo hiển thị đầy đủ hiệu ứng động cơ
                var trails = ship.GetComponentsInChildren<TrailRenderer>(true);
                foreach (var t in trails)
                {
                    t.emitting = true;
                }

                // Đảm bảo lồng khiên của Shield Bearer luôn sáng xoay tròn
                if (i == 4)
                {
                    Transform shield = ship.transform.Find("Shield") ?? ship.transform.Find("[Shield]");
                    if (shield != null) shield.gameObject.SetActive(true);
                }

                _spawnedShips[i] = ship;
            }
        }

        private void Update()
        {
            HandleHotkeys();
            UpdateShipRotation();
            UpdateCameraPosition();
        }

        private void HandleHotkeys()
        {
            // Phím 1 đến 5: Chọn trực tiếp từng tàu
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectShip(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectShip(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectShip(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectShip(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectShip(4);

            // Phím A/D hoặc Mũi tên: Chuyển tàu tới / lui
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SelectShip((_selectedIndex - 1 + 5) % 5);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SelectShip((_selectedIndex + 1) % 5);
            }

            // Phím V: Bật/Tắt chế độ Toàn cảnh (Overview)
            if (Input.GetKeyDown(KeyCode.V))
            {
                _isOverview = !_isOverview;
            }

            // Phím Space hoặc R: Bật/Tắt xoay 360 độ
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
            {
                _autoRotate = !_autoRotate;
            }

            // Chuột phải kéo để xoay góc nhìn tự do
            if (Input.GetMouseButton(1))
            {
                _orbitYaw += Input.GetAxis("Mouse X") * 4f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - Input.GetAxis("Mouse Y") * 3f, -40f, 75f);
            }

            // Cuộn chuột hoặc phím W/S để Zoom cự ly
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _cameraDistance = Mathf.Clamp(_cameraDistance - scroll * 5f, 3.5f, 15f);
            }
        }

        public void SelectShip(int index)
        {
            _selectedIndex = Mathf.Clamp(index, 0, 4);
            _isOverview = false;
            _orbitYaw = 180f; // Nhìn từ hướng đối diện mũi tàu
            _orbitPitch = 15f;
        }

        private void UpdateShipRotation()
        {
            if (!_autoRotate) return;

            float delta = _rotationSpeed * Time.deltaTime;
            for (int i = 0; i < 5; i++)
            {
                if (_spawnedShips[i] != null)
                {
                    _spawnedShips[i].transform.Rotate(0f, delta, 0f, Space.Self);
                }
            }
        }

        private void UpdateCameraPosition()
        {
            if (_galleryCamera == null) return;

            if (_isOverview)
            {
                // Góc nhìn toàn cảnh 5 tàu xếp hàng
                _targetCameraPos = new Vector3(0f, 6f, -22f);
                _galleryCamera.transform.position = Vector3.SmoothDamp(_galleryCamera.transform.position, _targetCameraPos, ref _currentCameraVelocity, 0.25f);
                _galleryCamera.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            }
            else
            {
                // Góc nhìn bám sát tàu đang chọn với khả năng xoay Orbit
                Vector3 shipPos = SLOT_POSITIONS[_selectedIndex];
                Quaternion orbitRot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
                Vector3 offset = orbitRot * new Vector3(0f, 0f, -_cameraDistance);
                _targetCameraPos = shipPos + Vector3.up * 0.8f + offset;

                _galleryCamera.transform.position = Vector3.SmoothDamp(_galleryCamera.transform.position, _targetCameraPos, ref _currentCameraVelocity, 0.15f);
                _galleryCamera.transform.LookAt(shipPos + Vector3.up * 0.5f);
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Tiêu đề Hangar Top Banner
            GUI.Box(new Rect(10, 10, Screen.width - 20, 50), GUIContent.none, _cardStyle);
            GUI.Label(new Rect(25, 15, 600, 30), "STAR FOX 64 — PHÒNG TRIỂN LÃM & KIỂM TRA MÔ HÌNH KẺ ĐỊCH", _headerStyle);

            // 2. Thanh nút chọn nhanh 5 tàu
            float btnX = 25;
            float btnY = 70;
            float btnW = 140;
            float btnH = 34;

            for (int i = 0; i < 5; i++)
            {
                bool isSelected = (!_isOverview && _selectedIndex == i);
                GUIStyle style = isSelected ? _buttonActiveStyle : _buttonInactiveStyle;
                if (GUI.Button(new Rect(btnX + i * (btnW + 8), btnY, btnW, btnH), $"[{i + 1}] {SHIP_SPECS[i].name.Replace("Enemy (" + i + ") - ", "")}", style))
                {
                    SelectShip(i);
                }
            }

            // Nút Toàn cảnh
            GUIStyle ovStyle = _isOverview ? _buttonActiveStyle : _buttonInactiveStyle;
            if (GUI.Button(new Rect(btnX + 5 * (btnW + 8), btnY, btnW, btnH), "[V] Toàn Cảnh (5 Tàu)", ovStyle))
            {
                _isOverview = !_isOverview;
            }

            // 3. Bảng thông số kỹ thuật bên góc trái
            if (!_isOverview && _selectedIndex >= 0 && _selectedIndex < 5)
            {
                ShipSpec spec = SHIP_SPECS[_selectedIndex];
                float cardW = 480;
                float cardH = 330;
                float cardY = 115;
                GUI.Box(new Rect(25, cardY, cardW, cardH), GUIContent.none, _cardStyle);

                float lineY = cardY + 15;
                GUI.Label(new Rect(40, lineY, cardW - 30, 25), $"THÔNG SỐ KỸ THUẬT: {spec.name.ToUpper()}", _headerStyle);
                lineY += 30;

                DrawSpecRow(ref lineY, "Vai trò / Phân loại:", spec.archetype);
                DrawSpecRow(ref lineY, "Mô hình gốc (HiRez):", spec.modelName);
                DrawSpecRow(ref lineY, "Lớp sơn & Vật liệu:", spec.materialName);
                DrawSpecRow(ref lineY, "Máu tối đa (HP):", $"{spec.health} HP");
                DrawSpecRow(ref lineY, "Hệ thống vũ khí:", spec.weaponInfo);
                DrawSpecRow(ref lineY, "Động cơ ion tím:", spec.thrusterInfo);
                DrawSpecRow(ref lineY, "Hitbox va chạm:", spec.hitboxInfo);
                DrawSpecRow(ref lineY, "Đặc tính chiến đấu:", spec.specialAbility);
            }

            // 4. Hướng dẫn phím tắt ở đáy màn hình
            float bottomY = Screen.height - 40;
            GUI.Box(new Rect(10, bottomY, Screen.width - 20, 32), GUIContent.none, _cardStyle);
            GUI.Label(new Rect(20, bottomY + 5, Screen.width - 40, 25),
                "Phím tắt: [1 - 5] Chọn tàu  |  [A / D] Chuyển tàu  |  [Space / R] Bật/Tắt xoay 360°  |  [Giữ Chuột Phải] Quay góc nhìn  |  [Cuộn chuột] Zoom  |  [V] Xem toàn cảnh",
                _shortcutStyle);
        }

        private void DrawSpecRow(ref float y, string label, string val)
        {
            GUI.Label(new Rect(40, y, 160, 22), label, _labelBoldStyle);
            GUI.Label(new Rect(200, y, 290, 38), val, _labelValueStyle);
            y += 34;
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _bgCardTex = MakeTex(2, 2, new Color(0.04f, 0.06f, 0.12f, 0.88f));
            _btnActiveTex = MakeTex(2, 2, new Color(0.18f, 0.45f, 0.95f, 0.9f));
            _btnInactiveTex = MakeTex(2, 2, new Color(0.12f, 0.16f, 0.25f, 0.8f));

            _headerStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.85f, 1f) }
            };

            _cardStyle = new GUIStyle
            {
                normal = { background = _bgCardTex },
                border = new RectOffset(4, 4, 4, 4)
            };

            _buttonActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnActiveTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _buttonInactiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = _btnInactiveTex, textColor = new Color(0.75f, 0.8f, 0.9f) },
                alignment = TextAnchor.MiddleCenter
            };

            _labelBoldStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.4f, 0.7f, 1f) }
            };

            _labelValueStyle = new GUIStyle
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            _shortcutStyle = new GUIStyle
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.8f, 1f) }
            };
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnDestroy()
        {
            if (_bgCardTex != null) Destroy(_bgCardTex);
            if (_btnActiveTex != null) Destroy(_btnActiveTex);
            if (_btnInactiveTex != null) Destroy(_btnInactiveTex);
        }
    }
}
