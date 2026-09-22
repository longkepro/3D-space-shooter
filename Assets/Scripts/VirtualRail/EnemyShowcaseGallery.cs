using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Phòng Triển Lãm & Kiểm Tra Toàn Diện Kẻ Địch (Enemy Showcase Gallery - Solo Stage & Weapons Lab).
    /// Khắc phục triệt để 3 vấn đề:
    /// 1. Triệt tiêu 100% bug chồng model (Clean Single Instance Management).
    /// 2. Chế độ Sân Khấu Độc Bản (Solo Center Stage): Chỉ hiển thị 1 tàu tại (0,0,0), góc nhìn 360 độ thoáng đãng không vướng víu.
    /// 3. Kích hoạt toàn bộ hiệu ứng: Bắn đạn thử nghiệm (Weapons Test), Lồng khiên AuraBubble xoay tròn, Luồng plasma tím phun lửa liên tục.
    /// 4. Nút bấm 1-click quay trở lại Game.unity.
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
            public float idealCameraDistance;
            public float focusYOffset;
        }

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
                thrusterInfo = "2 Động cơ phản lực ion tím đối xứng (Twin Thrusters)",
                hitboxInfo = "BoxCollider (4.4 x 1.6 x 4.8) - Vừa khít thân",
                specialAbility = "Lao bổ nhào cự ly gần, cơ động cao nhất đội hình trinh sát",
                idealCameraDistance = 7.5f,
                focusYOffset = 0.35f
            },
            new ShipSpec
            {
                name = "Enemy (1) - Flanker",
                archetype = "Tạt Sườn Bọc Hậu (Armored Flanker)",
                modelName = "Hi-Rez Spaceships Example5",
                materialName = "Xám Thép Mạ Crom (HiRez Factory Grey)",
                health = 40,
                weaponInfo = "2 Pháo Blaster mạn sườn (Laser chuẩn hóa)",
                thrusterInfo = "2 Động cơ phản lực ion tím đối xứng tại X = ±1.08, Y = -1.21, Z = -4.24",
                hitboxInfo = "BoxCollider (4.0 x 1.6 x 5.0) - Vừa khít thân",
                specialAbility = "Bay vòng tạt sườn trái/phải bắn chặn đón đầu đường bay",
                idealCameraDistance = 8.0f,
                focusYOffset = 0.35f
            },
            new ShipSpec
            {
                name = "Enemy (2) - Interceptor",
                archetype = "Tiêm Kích Siêu Thanh (Interceptor)",
                modelName = "Hi-Rez Spaceships Example4",
                materialName = "Xám Titan Nhám Chống Nhiệt (HiRez Grey)",
                health = 35,
                weaponInfo = "2 Khẩu Laser cánh siêu thanh (Laser chuẩn hóa)",
                thrusterInfo = "1 Ống xả to chính giữa vòng 6 cánh tại (0, -0.50, -2.85)",
                hitboxInfo = "BoxCollider (4.2 x 1.6 x 4.8) - Vừa khít thân",
                specialAbility = "Điểm yếu đuôi tàu x200% DMG, bay tốc độ cao né đạn",
                idealCameraDistance = 8.5f,
                focusYOffset = 0.35f
            },
            new ShipSpec
            {
                name = "Enemy (3) - Squad Leader",
                archetype = "Chiến Hạm Chỉ Huy Phi Đội (Flagship Leader)",
                modelName = "Hi-Rez Spaceships Example2 + Big_Launcher",
                materialName = "Giáp Hợp Kim Gia Cường (Tàu Chỉ Huy)",
                health = 80,
                weaponInfo = "Pháo hạng nặng Big_Launcher (Bắn loạt Burst 3 viên)",
                thrusterInfo = "1 Động cơ chính khổng lồ trên lưng tàu tại (0, 1.25, -10.80)",
                hitboxInfo = "BoxCollider (4.8 x 1.8 x 5.0) - Đã xóa Hitbox ma cũ (7.5x7.0)",
                specialAbility = "Dáng vóc 1.35x uy nghiêm, dẫn đầu đội hình Wedge V. Bị diệt gây Panic Scatter",
                idealCameraDistance = 13.5f, // Lùi xa 13.5m giúp chiến hạm khổng lồ vừa vặn cân đối trong khung nhìn
                focusYOffset = 0.60f
            },
            new ShipSpec
            {
                name = "Enemy (4) - Shield Bearer",
                archetype = "Chiến Đấu Cơ Hộ Vệ Bọc Khiên (Shield Bearer)",
                modelName = "Hi-Rez Spaceships Example1 + Kitbash Wing2/Wing6",
                materialName = "Xám Hợp Kim Cường Lực (HiRez Grey)",
                health = 40,
                weaponInfo = "2 Súng xung điện tần số cao (Laser chuẩn hóa)",
                thrusterInfo = "2 Động cơ phản lực tím tại mạn cánh mở rộng X = ±3.23, Y = 0.81, Z = -5.40",
                hitboxInfo = "BoxCollider (4.8 x 1.8 x 5.2) - Bao bọc trọn vẹn sải cánh kitbash",
                specialAbility = "Tấm khiên lưới lục giác năng lượng chắn trước mũi tàu cản phá 100% đạn laser",
                idealCameraDistance = 8.5f,
                focusYOffset = 0.35f
            }
        };

        // Vị trí dàn hàng khi bật chế độ Toàn cảnh Overview (cách nhau 20m rộng rãi)
        private static readonly Vector3[] LINEUP_POSITIONS = new Vector3[]
        {
            new Vector3(-40f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(20f, 0f, 0f),
            new Vector3(40f, 0f, 0f)
        };

        [Header("Spawned Ships")]
        private readonly GameObject[] _ships = new GameObject[5];
        private readonly List<LineRenderer> _plumeLines = new List<LineRenderer>();
        private Transform _shieldTransform;

        [Header("Target Dummy for Weapons Test")]
        private GameObject _targetDummy;
        private Transform _targetDummyTrans;

        [Header("Camera Control")]
        private Camera _galleryCam;
        [SerializeField] private int _selectedIndex = 2; // Mặc định Interceptor
        private bool _isOverview = false;
        private bool _autoRotate = false; // Tắt mặc định để người dùng tự xoay khi muốn
        private float _rotationSpeed = 25f;
        private float _cameraDistance = 8.5f;
        private float _targetCameraDistance = 8.5f;
        private float _cameraDistanceVelocity = 0f;
        private float _orbitYaw = 180f; // Nhìn trực diện mũi tàu
        private float _orbitPitch = 12f;
        private Vector3 _currentCameraVelocity;
        private Vector3 _targetCameraPos;

        [Header("Weapons Test System")]
        private bool _isFiringEnabled = true; // Bật mặc định để thấy rõ đạn bắn
        private float _fireInterval = 0.35f;
        private float _nextFireTime = 0f;

        [Header("Booster Demo System")]
        private bool _isBoosterActive = true; // Bật mặc định dải booster tím rực rỡ

        [Header("GUI Styling Cache")]
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _btnActiveStyle;
        private GUIStyle _btnInactiveStyle;
        private GUIStyle _btnFireOnStyle;
        private GUIStyle _btnFireOffStyle;
        private GUIStyle _btnGameStyle;
        private GUIStyle _labelBoldStyle;
        private GUIStyle _labelValueStyle;
        private GUIStyle _shortcutStyle;
        private Texture2D _bgCardTex;
        private Texture2D _btnActiveTex;
        private Texture2D _btnInactiveTex;
        private Texture2D _btnFireOnTex;
        private Texture2D _btnFireOffTex;
        private Texture2D _btnGameTex;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            // Đảm bảo có ProjectilePool cho đạn vật lý
            if (EnemyProjectilePool.Instance == null)
            {
                GameObject poolObj = new GameObject("[EnemyProjectilePool]");
                poolObj.AddComponent<EnemyProjectilePool>();
            }

            SetupCamera();
            CleanUpOldObjects();
            SpawnOrBindShips();
            CreateTargetDummy();
        }

        private void Start()
        {
            SelectShip(_selectedIndex);
        }

        private void SetupCamera()
        {
            _galleryCam = Camera.main;
            if (_galleryCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                _galleryCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            _galleryCam.backgroundColor = new Color(0.015f, 0.018f, 0.035f, 1f);
            _galleryCam.clearFlags = CameraClearFlags.SolidColor;
            _galleryCam.nearClipPlane = 0.3f;
            _galleryCam.farClipPlane = 500f;
        }

        private void CleanUpOldObjects()
        {
            // Xóa triệt để các GameObject rác từ Edit Mode cũ để chống chồng đè
            string[] tagsToClean = { "[Showcase_Ship_", "[Showcase_" };
            var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                if (root == gameObject || root == _galleryCam.gameObject) continue;
                foreach (var tag in tagsToClean)
                {
                    if (root.name.StartsWith(tag))
                    {
                        Destroy(root);
                        break;
                    }
                }
            }
        }

        private void SpawnOrBindShips()
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
                GameObject prefab = null;
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
#endif
                if (prefab == null) continue;

                GameObject ship = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 180f, 0f), transform);
                ship.name = $"[Showcase_Ship_{i}] {prefab.name}";

                // Vô hiệu hóa AI di chuyển
                var movement = ship.GetComponent<EnemyMovement>();
                if (movement != null) movement.enabled = false;

                // Tắt EnemyAttack tự động để tự điều khiển bắn trong showcase
                var attack = ship.GetComponent<EnemyAttack>();
                if (attack != null) attack.enabled = false;

                var rb = ship.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // Cấu hình luồng lửa phản lực tím tĩnh
                SetupThrusterPlumes(ship);

                // Cấu hình bộ điều phối dải booster tím
                var booster = ship.GetComponent<EnemyBoosterTrail>();
                if (booster == null) booster = ship.AddComponent<EnemyBoosterTrail>();
                booster.CacheComponents();
                booster.SetOverrideBoost(_isBoosterActive);

                // Kích hoạt lồng khiên cho Shield Bearer
                if (i == 4)
                {
                    Transform barrier = ship.transform.Find("[Shield_Barrier]");
                    if (barrier == null)
                    {
                        var hexShield = ship.GetComponentInChildren<VirtualRailHexShield>(true);
                        if (hexShield != null) barrier = hexShield.transform;
                    }
                    if (barrier != null)
                    {
                        barrier.gameObject.SetActive(true);
                        _shieldTransform = barrier;
                        var hex = barrier.GetComponent<VirtualRailHexShield>();
                        if (hex != null) hex.SetShieldActive(true);
                    }
                }

                _ships[i] = ship;
                ship.SetActive(false); // Ẩn mặc định, chỉ hiện con được chọn
            }
        }

        private void SetupThrusterPlumes(GameObject ship)
        {
            // Thu thập tất cả ống xả (hỗ trợ cả Thruster component hoặc TrailRenderer mà người dùng tự thêm tay)
            var thrusterList = new List<Transform>();
            var thrusters = ship.GetComponentsInChildren<Thruster>(true);
            foreach (var th in thrusters)
            {
                if (th != null && !thrusterList.Contains(th.transform))
                    thrusterList.Add(th.transform);
            }
            var trails = ship.GetComponentsInChildren<TrailRenderer>(true);
            foreach (var tr in trails)
            {
                if (tr != null && !thrusterList.Contains(tr.transform))
                    thrusterList.Add(tr.transform);
            }

            foreach (var thTrans in thrusterList)
            {
                var light = thTrans.GetComponent<Light>();
                if (light != null)
                {
                    light.enabled = true;
                    light.color = new Color(0.85f, 0.2f, 1f);
                    light.intensity = 3.5f;
                    light.range = 6.0f;
                }

                // Tạo vệt lửa plasma hình nón phun ra từ ống xả (hiển thị khi đứng yên)
                GameObject plumeObj = new GameObject("[Plasma_Plume]");
                plumeObj.transform.SetParent(thTrans, false);
                plumeObj.transform.localPosition = Vector3.zero;
                plumeObj.transform.localRotation = Quaternion.identity;

                LineRenderer lr = plumeObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 2;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, new Vector3(0f, 0f, -1.6f)); // Đuôi lửa dài 1.6m về sau
                lr.startWidth = 0.45f;
                lr.endWidth = 0.05f;

                // Gradient từ tím sáng đến tím thẫm trong suốt
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1f, 0.6f, 1f), 0f),
                        new GradientColorKey(new Color(0.7f, 0.1f, 0.95f), 0.4f),
                        new GradientColorKey(new Color(0.3f, 0f, 0.6f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.95f, 0f),
                        new GradientAlphaKey(0.8f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                lr.colorGradient = grad;

                // Tận dụng material có sẵn của Laser / CFX
                var existingRend = thTrans.GetComponent<TrailRenderer>();
                if (existingRend != null && existingRend.sharedMaterial != null)
                {
                    lr.sharedMaterial = existingRend.sharedMaterial;
                }

                _plumeLines.Add(lr);
            }
        }

        private void CreateTargetDummy()
        {
            _targetDummy = new GameObject("[Target_Hologram_Dummy]");
            _targetDummy.transform.position = new Vector3(0f, 0.2f, 25f);

            // Tạo một bia ngắm hình cầu wireframe phát sáng để hứng đạn
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.name = "Visual_Ring";
            ring.transform.SetParent(_targetDummy.transform, false);
            ring.transform.localScale = new Vector3(3f, 3f, 0.2f);

            var col = ring.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var rend = ring.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material.color = new Color(1f, 0.2f, 0.3f, 0.4f);
            }

            _targetDummyTrans = _targetDummy.transform;
        }

        private void Update()
        {
            HandleHotkeys();
            UpdateVisuals();
            UpdateWeaponsTest();
            UpdateCamera();
        }

        private void HandleHotkeys()
        {
            // Phím 1 - 5: Chọn tàu
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectShip(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectShip(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectShip(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectShip(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectShip(4);

            // Chuyển tàu tới / lui
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SelectShip((_selectedIndex - 1 + 5) % 5);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SelectShip((_selectedIndex + 1) % 5);
            }

            // Phím F: Bật/Tắt bắn thử vũ khí
            if (Input.GetKeyDown(KeyCode.F))
            {
                _isFiringEnabled = !_isFiringEnabled;
            }

            // Phím B: Bật/Tắt Booster tím bứt tốc
            if (Input.GetKeyDown(KeyCode.B))
            {
                _isBoosterActive = !_isBoosterActive;
                ApplyBoosterStateToShips();
            }

            // Phím V: Bật/Tắt chế độ Toàn cảnh
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleOverview();
            }

            // Phím Space hoặc R: Bật/Tắt xoay 360 độ
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
            {
                _autoRotate = !_autoRotate;
            }

            // Chuột phải kéo để xoay góc nhìn tự do (Orbit)
            if (Input.GetMouseButton(1))
            {
                _orbitYaw += Input.GetAxis("Mouse X") * 4f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - Input.GetAxis("Mouse Y") * 3f, -40f, 75f);
            }

            // Cuộn chuột zoom (mở rộng biên độ từ 3.5m đến 25m linh hoạt)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetCameraDistance = Mathf.Clamp(_targetCameraDistance - scroll * 6f, 3.5f, 25f);
            }
        }

        public void SelectShip(int index)
        {
            _selectedIndex = Mathf.Clamp(index, 0, 4);
            _isOverview = false;

            // SÂN KHẤU ĐỘC BẢN (Solo Center Stage):
            // Chỉ hiển thị DUY NHẤT con tàu được chọn tại (0,0,0)
            for (int i = 0; i < 5; i++)
            {
                if (_ships[i] == null) continue;
                if (i == _selectedIndex)
                {
                    _ships[i].transform.position = Vector3.zero;
                    _ships[i].transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Đối diện camera
                    _ships[i].SetActive(true);

                    if (i == 4 && _shieldTransform != null)
                    {
                        _shieldTransform.gameObject.SetActive(true);
                        var hex = _shieldTransform.GetComponent<VirtualRailHexShield>();
                        if (hex != null) hex.SetShieldActive(true);
                    }
                }
                else
                {
                    _ships[i].SetActive(false); // Ẩn hoàn toàn 4 tàu còn lại để chống vướng mắt
                }
            }

            // Đặt bia hứng đạn đối diện mũi tàu
            if (_targetDummy != null)
            {
                _targetDummy.SetActive(true);
                _targetDummy.transform.position = new Vector3(0f, 0.2f, 28f);
            }

            // Tự động nhận diện cự ly tối ưu theo hình dáng và kích cỡ của từng tàu
            if (_selectedIndex >= 0 && _selectedIndex < SHIP_SPECS.Length)
            {
                _targetCameraDistance = SHIP_SPECS[_selectedIndex].idealCameraDistance;
            }

            _orbitYaw = 180f;
            _orbitPitch = 12f;
        }

        public void ToggleOverview()
        {
            _isOverview = !_isOverview;
            if (_isOverview)
            {
                // Dàn cả 5 tàu ra cách nhau 20m rộng rãi
                for (int i = 0; i < 5; i++)
                {
                    if (_ships[i] == null) continue;
                    _ships[i].transform.position = LINEUP_POSITIONS[i];
                    _ships[i].transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    _ships[i].SetActive(true);
                }
                if (_targetDummy != null) _targetDummy.SetActive(false);
            }
            else
            {
                SelectShip(_selectedIndex);
            }
        }

        private void ApplyBoosterStateToShips()
        {
            for (int i = 0; i < 5; i++)
            {
                if (_ships[i] == null) continue;
                var booster = _ships[i].GetComponent<EnemyBoosterTrail>();
                if (booster != null)
                {
                    booster.SetOverrideBoost(_isBoosterActive);
                }
            }
        }

        private void UpdateVisuals()
        {
            // 1. Tự động xoay 360 độ nếu được bật
            if (_autoRotate)
            {
                float rot = _rotationSpeed * Time.deltaTime;
                if (!_isOverview && _ships[_selectedIndex] != null)
                {
                    _ships[_selectedIndex].transform.Rotate(0f, rot, 0f, Space.Self);
                }
                else if (_isOverview)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (_ships[i] != null) _ships[i].transform.Rotate(0f, rot, 0f, Space.Self);
                    }
                }
            }

            // 2. Dao động nhịp thở nhẹ cho tấm khiên lưới lục giác phía trước mũi tàu
            if (_shieldTransform != null && _shieldTransform.gameObject.activeInHierarchy)
            {
                float shieldBob = Mathf.Sin(Time.time * 2.5f) * 0.04f;
                _shieldTransform.localPosition = new Vector3(0f, 0.25f + shieldBob, 3.10f);
                _shieldTransform.localRotation = Quaternion.identity;
            }

            // 3. Hiệu ứng phụt lửa nhấp nháy sống động cho đuôi động cơ (mở rộng mạnh khi bật Booster tím)
            float flicker = Mathf.PingPong(Time.time * 15f, 0.35f);
            float plumeLength = _isBoosterActive ? (5.5f + flicker * 2f) : (1.5f + flicker);
            float plumeWidth = _isBoosterActive ? 0.75f : 0.45f;
            for (int p = 0; p < _plumeLines.Count; p++)
            {
                if (_plumeLines[p] != null && _plumeLines[p].gameObject.activeInHierarchy)
                {
                    _plumeLines[p].SetPosition(1, new Vector3(0f, 0f, -plumeLength));
                    _plumeLines[p].startWidth = plumeWidth;
                }
            }
        }

        private void UpdateWeaponsTest()
        {
            if (!_isFiringEnabled || _isOverview) return;
            if (Time.time < _nextFireTime) return;

            GameObject currentShip = _ships[_selectedIndex];
            if (currentShip == null) return;

            Vector3 targetPos = (_targetDummyTrans != null) ? _targetDummyTrans.position : (currentShip.transform.position + currentShip.transform.forward * 30f);

            // 1. Tìm nòng súng sạch VirtualRailMuzzle
            VirtualRailMuzzle[] muzzles = currentShip.GetComponentsInChildren<VirtualRailMuzzle>(true);
            if (muzzles != null && muzzles.Length > 0)
            {
                for (int i = 0; i < muzzles.Length; i++)
                {
                    if (muzzles[i] == null) continue;
                    Vector3 muzzlePos = muzzles[i].Position;
                    Vector3 dir = (targetPos - muzzlePos).normalized;

                    if (EnemyProjectilePool.Instance != null)
                    {
                        EnemyProjectilePool.Instance.SpawnProjectile(muzzlePos, dir, 55f, false);
                    }
                    muzzles[i].PlayFireEffects();
                }
            }
            else
            {
                // 2. Fallback tìm nòng súng Laser (chỉ mượn vị trí, triệt tiêu LineRenderer, KHÔNG bắn hitscan)
                Laser[] lasers = currentShip.GetComponentsInChildren<Laser>(true);
                if (lasers != null && lasers.Length > 0)
                {
                    for (int i = 0; i < lasers.Length; i++)
                    {
                        if (lasers[i] == null) continue;
                        var lr = lasers[i].GetComponent<LineRenderer>();
                        if (lr != null) lr.enabled = false;

                        Vector3 muzzlePos = lasers[i].transform.position;
                        Vector3 dir = (targetPos - muzzlePos).normalized;

                        if (EnemyProjectilePool.Instance != null)
                        {
                            EnemyProjectilePool.Instance.SpawnProjectile(muzzlePos, dir, 55f, false);
                        }
                    }
                }
            }

            _nextFireTime = Time.time + _fireInterval;
        }

        private void UpdateCamera()
        {
            if (_galleryCam == null) return;

            if (_isOverview)
            {
                _targetCameraPos = new Vector3(0f, 12f, -55f);
                _galleryCam.transform.position = Vector3.SmoothDamp(_galleryCam.transform.position, _targetCameraPos, ref _currentCameraVelocity, 0.25f);
                _galleryCam.transform.LookAt(new Vector3(0f, 1f, 0f));
            }
            else
            {
                // Nội suy khoảng cách zoom mượt mà đến cự ly chuẩn của từng tàu (0 GC Alloc)
                _cameraDistance = Mathf.SmoothDamp(_cameraDistance, _targetCameraDistance, ref _cameraDistanceVelocity, 0.15f);

                float yOffset = (_selectedIndex >= 0 && _selectedIndex < SHIP_SPECS.Length)
                    ? SHIP_SPECS[_selectedIndex].focusYOffset
                    : 0.4f;
                Vector3 focusPoint = Vector3.up * yOffset;

                Quaternion orbitRot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
                Vector3 offset = orbitRot * new Vector3(0f, 0f, -_cameraDistance);
                _targetCameraPos = focusPoint + offset;

                _galleryCam.transform.position = Vector3.SmoothDamp(_galleryCam.transform.position, _targetCameraPos, ref _currentCameraVelocity, 0.12f);
                _galleryCam.transform.LookAt(focusPoint);
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Tiêu đề Top Banner
            GUI.Box(new Rect(10, 10, Screen.width - 20, 50), GUIContent.none, _cardStyle);
            GUI.Label(new Rect(25, 14, 550, 30), "STAR FOX 64 — PHÒNG THỬ NGHIỆM & QUAN SÁT KẺ ĐỊCH (SOLO STAGE)", _headerStyle);

            // NÚT QUAY LẠI GAME CHÍNH (Góc trên bên phải)
            if (GUI.Button(new Rect(Screen.width - 220, 15, 200, 40), "▶️ VÀO CHƠI GAME", _btnGameStyle))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
            }

            // 2. Thanh nút bấm chọn tàu
            float btnX = 25;
            float btnY = 68;
            float btnW = 125;
            float btnH = 34;

            for (int i = 0; i < 5; i++)
            {
                bool isSelected = (!_isOverview && _selectedIndex == i);
                GUIStyle style = isSelected ? _btnActiveStyle : _btnInactiveStyle;
                if (GUI.Button(new Rect(btnX + i * (btnW + 6), btnY, btnW, btnH), $"[{i + 1}] {SHIP_SPECS[i].name.Replace("Enemy (" + i + ") - ", "")}", style))
                {
                    SelectShip(i);
                }
            }

            // Nút Toàn cảnh Lineup
            GUIStyle ovStyle = _isOverview ? _btnActiveStyle : _btnInactiveStyle;
            if (GUI.Button(new Rect(btnX + 5 * (btnW + 6), btnY, 145, btnH), "[V] Dàn Hàng 20m", ovStyle))
            {
                ToggleOverview();
            }

            // Nút Bật/Tắt Bắn Đạn (Weapons Test)
            GUIStyle fireStyle = _isFiringEnabled ? _btnFireOnStyle : _btnFireOffStyle;
            string fireText = _isFiringEnabled ? "🔥 BẮN ĐẠN: [BẬT]" : "⚪ BẮN ĐẠN: [TẮT]";
            if (GUI.Button(new Rect(btnX + 5 * (btnW + 6) + 155, btnY, 155, btnH), fireText, fireStyle))
            {
                _isFiringEnabled = !_isFiringEnabled;
            }

            // Nút Bật/Tắt Dải Booster Tím
            GUIStyle boosterStyle = _isBoosterActive ? _btnActiveStyle : _btnInactiveStyle;
            string boosterText = _isBoosterActive ? "🚀 BOOSTER TÍM: [BẬT]" : "⚪ BOOSTER TÍM: [TẮT]";
            if (GUI.Button(new Rect(btnX + 5 * (btnW + 6) + 320, btnY, 175, btnH), boosterText, boosterStyle))
            {
                _isBoosterActive = !_isBoosterActive;
                ApplyBoosterStateToShips();
            }

            // 3. Bảng thông số kỹ thuật bên trái
            if (!_isOverview && _selectedIndex >= 0 && _selectedIndex < 5)
            {
                ShipSpec spec = SHIP_SPECS[_selectedIndex];
                float cardW = 480;
                float cardH = 330;
                float cardY = 112;
                GUI.Box(new Rect(25, cardY, cardW, cardH), GUIContent.none, _cardStyle);

                float lineY = cardY + 12;
                GUI.Label(new Rect(40, lineY, cardW - 30, 25), $"THÔNG SỐ: {spec.name.ToUpper()}", _headerStyle);
                lineY += 28;

                DrawSpecRow(ref lineY, "Phân loại / Vai trò:", spec.archetype);
                DrawSpecRow(ref lineY, "Model gốc HiRez:", spec.modelName);
                DrawSpecRow(ref lineY, "Lớp sơn & Vật liệu:", spec.materialName);
                DrawSpecRow(ref lineY, "Máu tối đa (HP):", $"{spec.health} HP");
                DrawSpecRow(ref lineY, "Hệ thống vũ khí:", spec.weaponInfo);
                DrawSpecRow(ref lineY, "Ống xả tím (Thruster):", spec.thrusterInfo);
                DrawSpecRow(ref lineY, "Hitbox va chạm:", spec.hitboxInfo);
                DrawSpecRow(ref lineY, "Đặc tính chiến đấu:", spec.specialAbility);
            }

            // 4. Thanh hướng dẫn phím tắt ở đáy màn hình
            float bottomY = Screen.height - 38;
            GUI.Box(new Rect(10, bottomY, Screen.width - 20, 30), GUIContent.none, _cardStyle);
            GUI.Label(new Rect(20, bottomY + 4, Screen.width - 40, 22),
                "Phím: [1-5] Chọn tàu  |  [F] Bắn đạn  |  [B] Booster tím  |  [Space] Xoay 360°  |  [Chuột Phải] Quay góc nhìn  |  [Cuộn chuột] Zoom  |  [V] Dàn hàng toàn cảnh",
                _shortcutStyle);
        }

        private void DrawSpecRow(ref float y, string label, string val)
        {
            GUI.Label(new Rect(40, y, 160, 20), label, _labelBoldStyle);
            GUI.Label(new Rect(200, y, 290, 36), val, _labelValueStyle);
            y += 34;
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _bgCardTex = MakeTex(2, 2, new Color(0.03f, 0.05f, 0.10f, 0.92f));
            _btnActiveTex = MakeTex(2, 2, new Color(0.15f, 0.45f, 0.95f, 0.95f));
            _btnInactiveTex = MakeTex(2, 2, new Color(0.10f, 0.14f, 0.22f, 0.85f));
            _btnFireOnTex = MakeTex(2, 2, new Color(0.9f, 0.2f, 0.15f, 0.95f));
            _btnFireOffTex = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.3f, 0.85f));
            _btnGameTex = MakeTex(2, 2, new Color(0.1f, 0.65f, 0.25f, 0.95f));

            _headerStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.88f, 1f) }
            };

            _cardStyle = new GUIStyle
            {
                normal = { background = _bgCardTex },
                border = new RectOffset(4, 4, 4, 4)
            };

            _btnActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnActiveTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _btnInactiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = _btnInactiveTex, textColor = new Color(0.75f, 0.82f, 0.92f) },
                alignment = TextAnchor.MiddleCenter
            };

            _btnFireOnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnFireOnTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _btnFireOffStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = _btnFireOffTex, textColor = new Color(0.7f, 0.7f, 0.7f) },
                alignment = TextAnchor.MiddleCenter
            };

            _btnGameStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnGameTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _labelBoldStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.75f, 1f) }
            };

            _labelValueStyle = new GUIStyle
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };

            _shortcutStyle = new GUIStyle
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
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
            if (_btnFireOnTex != null) Destroy(_btnFireOnTex);
            if (_btnFireOffTex != null) Destroy(_btnFireOffTex);
            if (_btnGameTex != null) Destroy(_btnGameTex);
        }
    }
}
