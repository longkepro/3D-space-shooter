using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Cơ chế Hội tụ Đạn đạo 3D (3D Convergence) & Phễu từ tính (Bullet Magnetism).
    /// Đạn bắn từ mọi nòng súng luôn hội tụ xuyên qua đúng tâm của khung ngắm 3D ở cự ly Z_conv.
    /// </summary>
    public enum LaserUpgradeLevel
    {
        Single = 1, // Laser đơn tiêu chuẩn (20 DMG, Cyan/Lam)
        Twin = 2,   // Laser kép nâng cấp (30 DMG/bolt, Vàng kim)
        Hyper = 3   // Hyper Laser tối thượng (45 DMG/bolt, Ngọc lục bảo)
    }

    [DisallowMultipleComponent]
    public class VirtualRailWeapon : MonoBehaviour
    {
        public static VirtualRailWeapon Instance { get; private set; }

        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public VirtualRailShip ship;
        public VirtualRailInputReader inputReader;
        public Laser[] muzzles;
        public VirtualRailMuzzle[] cleanMuzzles;

        [Header("Weapon Upgrade System")]
        [SerializeField] private LaserUpgradeLevel _laserLevel = LaserUpgradeLevel.Single;
        [SerializeField] private int _goldRingsCollected = 0;

        public LaserUpgradeLevel CurrentLaserLevel => _laserLevel;
        public int GoldRingsCollected => _goldRingsCollected;

        [Header("Weapon Timing")]
        public float fireRate = 0.15f;
        private float nextFireTime = 0f;

        [Header("Charge Shot Integration (Star Fox 64 1B)")]
        [SerializeField] private float holdToChargeThreshold = 0.25f;
        private float _fireButtonDownTime = -1f;
        private float _weaponCooldownUntil = 0f;
        private AimAssistModule _cachedAimAssist;

        private enum ChargeState : byte
        {
            Idle = 0,
            ChargingDedicated = 1,
            ChargingLegacy = 2
        }
        private ChargeState _chargeState = ChargeState.Idle;
        public bool IsChargeActive => _chargeState != ChargeState.Idle;

        // Ụ súng xoay cơ học (Gimbaled Turrets - 0 GC Alloc)
        private Vector3 _currentGimbalDir = Vector3.forward;
        private bool _gimbalInitialized = false;
        private bool _isGimbalClamped = false;
        private float _gimbalDeviationAngle = 0f;
        private Camera _cachedCam;

        public Vector3 CurrentGimbalDir => _currentGimbalDir;
        public bool IsGimbalClamped => _isGimbalClamped;
        public float GimbalDeviationAngle => _gimbalDeviationAngle;
        public float WeaponCooldownRemaining => Mathf.Max(0f, _weaponCooldownUntil - Time.time);
        public bool IsWeaponInCooldown => Time.time < _weaponCooldownUntil;

        // Bộ đệm RaycastHit dùng chung 0 GC Alloc cho Dynamic Depth Convergence
        private static readonly RaycastHit[] _depthScanHits = new RaycastHit[16];

        private void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureMuzzles();
        }

        private void EnsureMuzzles()
        {
            if (cleanMuzzles == null || cleanMuzzles.Length == 0)
            {
                cleanMuzzles = GetComponentsInChildren<VirtualRailMuzzle>(true);
            }
            if ((cleanMuzzles == null || cleanMuzzles.Length == 0) && (muzzles == null || muzzles.Length == 0))
            {
                muzzles = GetComponentsInChildren<Laser>(true);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void CollectGoldRing()
        {
            _goldRingsCollected++;
            if (_goldRingsCollected >= 3)
            {
                _goldRingsCollected = 0;
                UpgradeLaser();
            }
        }

        public void UpgradeLaser()
        {
            if (_laserLevel == LaserUpgradeLevel.Single)
            {
                _laserLevel = LaserUpgradeLevel.Twin;
                Debug.Log("<color=yellow><b>[WEAPON UPGRADE]</b></color> Nâng cấp thành TWIN LASER! Hỏa lực gia tăng!");
                AudioManager.Instance?.PlaySFX2D(AudioSFXType.RingCollect, 1.0f, 1.2f);
            }
            else if (_laserLevel == LaserUpgradeLevel.Twin)
            {
                _laserLevel = LaserUpgradeLevel.Hyper;
                Debug.Log("<color=green><b>[WEAPON UPGRADE]</b></color> Nâng cấp tối thượng HYPER LASER! Hỏa lực cực đại!");
                AudioManager.Instance?.PlaySFX2D(AudioSFXType.RingCollect, 1.0f, 1.4f);
            }
        }

        public void DowngradeLaser()
        {
            if (_laserLevel == LaserUpgradeLevel.Hyper) _laserLevel = LaserUpgradeLevel.Twin;
            else if (_laserLevel == LaserUpgradeLevel.Twin) _laserLevel = LaserUpgradeLevel.Single;
        }

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (anchor == null) anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (reticle == null) reticle = FindAnyObjectByType<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (ship == null) ship = FindAnyObjectByType<VirtualRailShip>();
            if (inputReader == null && anchor != null) inputReader = anchor.GetComponentInChildren<VirtualRailInputReader>();
            if (_cachedCam == null)
            {
                var camRig = anchor != null ? anchor.GetComponentInChildren<VirtualRailCameraRig>() : null;
                _cachedCam = (camRig != null && camRig.targetCamera != null) ? camRig.targetCamera : Camera.main;
            }
        }

        private void EnsureAimAssist()
        {
            if (_cachedAimAssist == null)
            {
                _cachedAimAssist = GetComponent<AimAssistModule>();
                if (_cachedAimAssist == null && anchor != null)
                {
                    _cachedAimAssist = anchor.GetComponentInChildren<AimAssistModule>();
                }
                if (_cachedAimAssist == null)
                {
                    _cachedAimAssist = FindAnyObjectByType<AimAssistModule>();
                }
            }
        }

        private void Update()
        {
            var cfg = anchor != null ? anchor.config : null;
            if (cfg != null)
            {
                UpdateGimbalOrientation(cfg);
            }

            if (ship != null && ship.IsDead) return;
            if (UIManager.Instance != null && UIManager.Instance.CurrentState != UIViewState.Gameplay) return;

            EnsureAimAssist();

            bool isChargeHeld = (inputReader != null && inputReader.IsChargeHeld());
            bool isRightStickActive = (inputReader != null && inputReader.IsRightStickActive);
            bool isBtnFireDown = (inputReader != null && inputReader.btnFire != null && inputReader.btnFire.WasPressedThisFrame);
            bool isBtnFireHeld = (inputReader != null && inputReader.btnFire != null && inputReader.btnFire.IsHeld);
            bool isBtnFireUp = (inputReader != null && inputReader.btnFire != null && inputReader.btnFire.WasReleasedThisFrame);

            // Trên Mobile, chạm màn hình (kể cả cần gạt ngắm phải) bị Unity ánh xạ nhầm thành mouse 0 (Fire1).
            // Do đó chỉ đọc phím chuột/bàn phím PC khi không phải nền tảng di động.
            bool isPcFireDown = false;
            bool isPcFireHeld = false;
            bool isPcFireUp = false;

            if (!Application.isMobilePlatform)
            {
                isPcFireDown = Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space);
                isPcFireHeld = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);
                isPcFireUp = Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.Space);
            }

            bool isLegacyFireDown = isPcFireDown || isBtnFireDown;
            bool isLegacyFireHeld = isPcFireHeld || isBtnFireHeld;
            bool isLegacyFireUp = isPcFireUp || isBtnFireUp;

            // Kiểm tra trạng thái nạp đạn tụ lực & khóa hồi nòng (0 GC Alloc)
            bool isAimAssistCharging = (_cachedAimAssist != null && _cachedAimAssist.IsCharging);
            bool isCurrentlyCharging = (_chargeState != ChargeState.Idle) || isAimAssistCharging;
            bool canFireRegular = (Time.time >= _weaponCooldownUntil) && !isCurrentlyCharging;

            // 1. Máy trạng thái Nút Sạc Hỏa Lực Riêng Biệt (Dedicated Charge Shot - Mobile 4 Ngón)
            if (_cachedAimAssist != null)
            {
                if (_chargeState == ChargeState.Idle && isChargeHeld)
                {
                    // Chuyển sang nạp tụ lực Mobile chuyên biệt (không bị chặn bởi cooldown của đạn thường)
                    _chargeState = ChargeState.ChargingDedicated;
                    _cachedAimAssist.BeginChargingFromWeapon();
                    _cachedAimAssist.UpdateChargeFromWeapon(Time.deltaTime);
                }
                else if (_chargeState == ChargeState.ChargingDedicated)
                {
                    if (isChargeHeld)
                    {
                        _cachedAimAssist.UpdateChargeFromWeapon(Time.deltaTime);
                    }
                    else
                    {
                        // Nhả nút sạc -> Phóng đạn tụ lực (nếu nạp đủ 100%) hoặc hủy nạp (nếu nhả sớm)
                        _cachedAimAssist.ReleaseChargeFromWeapon();
                        _chargeState = ChargeState.Idle;

                        // Khóa hồi nòng sau khi phóng tụ lực (chuẩn Star Fox 64, chặn 100% rò rỉ đạn thường)
                        float cooldown = (cfg != null && cfg.postChargeCooldown > 0f) ? cfg.postChargeCooldown : 0.35f;
                        _weaponCooldownUntil = Time.time + cooldown;
                        nextFireTime = _weaponCooldownUntil;
                        _fireButtonDownTime = -1f;
                    }
                }
            }

            // 2. Máy trạng thái Tap-to-Fire vs Hold-to-Charge cho Phím PC (Space/Fire1) và Nút Cảm Ứng Cũ (btnFire)
            // Chỉ hoạt động khi không bị chiếm quyền bởi nút sạc chuyên biệt Mobile
            if (_chargeState != ChargeState.ChargingDedicated)
            {
                if (_cachedAimAssist != null)
                {
                    if (_chargeState == ChargeState.Idle)
                    {
                        if (isLegacyFireDown)
                        {
                            _fireButtonDownTime = Time.time;

                            // Phản xạ tức thời phát bắn đầu tiên (0ms input lag), chỉ bắn khi không trong cooldown
                            if (canFireRegular && Time.time >= nextFireTime)
                            {
                                FireAllMuzzles();
                                nextFireTime = Time.time + fireRate;
                            }
                        }
                        else if (isLegacyFireHeld && _fireButtonDownTime > 0f)
                        {
                            float holdDuration = Time.time - _fireButtonDownTime;
                            if (holdDuration >= holdToChargeThreshold)
                            {
                                // Giữ phím >= 0.25s -> Chuyển sang nạp tụ lực PC Legacy
                                _chargeState = ChargeState.ChargingLegacy;
                                _cachedAimAssist.BeginChargingFromWeapon();
                                _cachedAimAssist.UpdateChargeFromWeapon(Time.deltaTime);
                            }
                            else
                            {
                                // Trong khoảng 0 < holdDuration < 0.25s: Bắn đạn liên thanh bình thường
                                if (canFireRegular && Time.time >= nextFireTime)
                                {
                                    FireAllMuzzles();
                                    nextFireTime = Time.time + fireRate;
                                }
                            }
                        }

                        if (isLegacyFireUp)
                        {
                            _fireButtonDownTime = -1f;
                        }
                    }
                    else if (_chargeState == ChargeState.ChargingLegacy)
                    {
                        if (isLegacyFireHeld)
                        {
                            // Tiếp tục tích lũy tụ lực mỗi frame (Khắc phục lỗi 1 Deadlock trên PC)
                            _cachedAimAssist.UpdateChargeFromWeapon(Time.deltaTime);
                        }

                        if (isLegacyFireUp)
                        {
                            // Nhả phím PC -> Phóng đạn tụ lực và giải phóng trạng thái súng
                            _cachedAimAssist.ReleaseChargeFromWeapon();
                            _chargeState = ChargeState.Idle;
                            _fireButtonDownTime = -1f;

                            float cooldown = (cfg != null && cfg.postChargeCooldown > 0f) ? cfg.postChargeCooldown : 0.35f;
                            _weaponCooldownUntil = Time.time + cooldown;
                            nextFireTime = _weaponCooldownUntil;
                        }
                    }
                }
                else
                {
                    // Fallback nếu không có AimAssistModule
                    if (isLegacyFireHeld && canFireRegular && Time.time >= nextFireTime)
                    {
                        FireAllMuzzles();
                        nextFireTime = Time.time + fireRate;
                    }
                }
            }

            // 3. Tự động xả đạn thường (Auto-fire): Cần gạt ngắm phải (Right Aim Stick) hoặc Mouse Auto-Fire
            // Bị chặn hoàn toàn khi đang nạp tụ lực HOẶC trong thời gian hồi nòng
            if (canFireRegular && !isLegacyFireHeld && _chargeState == ChargeState.Idle)
            {
                bool isStickAutoFire = isRightStickActive;
                bool isReticleAutoFire = cfg != null && cfg.autoFireOnAimMove && reticle != null && reticle.IsAimingMoving;
                bool isHoverFire = cfg != null && cfg.enableMouseHoverFire && !cfg.enableMouseAim && reticle != null && reticle.IsMouseTouchingReticle;

                if ((isStickAutoFire || isReticleAutoFire || isHoverFire) && Time.time >= nextFireTime)
                {
                    FireAllMuzzles();
                    nextFireTime = Time.time + fireRate;
                }
            }
        }

        /// <summary>
        /// Cập nhật hướng xoay của Ụ súng cơ học (Gimbaled Turrets - 0 GC Alloc).
        /// Nòng súng tự động xoay đuổi theo con trỏ chuột trong nón hỏa lực maxGimbalAngle.
        /// </summary>
        public void UpdateGimbalOrientation(VirtualRailConfig cfg)
        {
            if (cfg == null) return;

            Vector3 origin = (ship != null) ? ship.transform.position : transform.position;
            Vector3 shipFwd = (ship != null) ? ship.ShipForward : transform.forward;
            if (shipFwd.sqrMagnitude < 0.001f) shipFwd = Vector3.forward;

            if (!cfg.enableGimbalWeapon)
            {
                _currentGimbalDir = shipFwd;
                return;
            }

            float convDist = (cfg.convergenceDistance > 10f) ? cfg.convergenceDistance : 150f;
            Vector3 targetAimWorld = (reticle != null) 
                ? reticle.WorldPosition 
                : (anchor != null ? anchor.ToWorldPoint(new Vector3(0, 0, convDist)) : origin + shipFwd * convDist);

            Vector3 toTarget = targetAimWorld - origin;
            Vector3 desiredDir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : shipFwd;

            // Khống chế trong hình nón hỏa lực tối đa (maxGimbalAngle)
            float maxAngle = Mathf.Clamp(cfg.maxGimbalAngle, 5f, 85f);
            float angle = Vector3.Angle(shipFwd, desiredDir);
            _gimbalDeviationAngle = angle;
            _isGimbalClamped = (angle > maxAngle);

            Vector3 clampedTargetDir = desiredDir;
            if (angle > maxAngle)
            {
                clampedTargetDir = Vector3.RotateTowards(shipFwd, desiredDir, maxAngle * Mathf.Deg2Rad, 0f);
            }

            if (!_gimbalInitialized)
            {
                _currentGimbalDir = clampedTargetDir;
                _gimbalInitialized = true;
            }
            else
            {
                float speedRad = Mathf.Max(10f, cfg.gimbalTrackingSpeed) * Mathf.Deg2Rad;
                _currentGimbalDir = Vector3.RotateTowards(_currentGimbalDir, clampedTargetDir, speedRad * Time.deltaTime, 0f);
            }
        }

        /// <summary>
        /// Tính toán điểm hội tụ độ sâu động theo trục ngắm của Camera (Dynamic Depth Convergence - 0 GC Alloc).
        /// Dùng chung cho cả Súng Laser thường và Đạn Tụ Lực Dumb-fire.
        /// </summary>
        public Vector3 GetCurrentConvergencePoint(out bool foundTarget, out float closestDist)
        {
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();

            Vector3 aimWorld = (reticle != null) ? reticle.WorldPosition : (transform.position + transform.forward * 150f);
            var cfg = (anchor != null) ? anchor.config : null;

            closestDist = float.MaxValue;
            Vector3 dynamicTarget = aimWorld;
            foundTarget = false;

            if (cfg != null && cfg.enableDynamicDepthConvergence)
            {
                Camera cam = _cachedCam != null ? _cachedCam : Camera.main;
                Vector3 camPos = cam != null ? cam.transform.position : transform.position;
                Vector3 rayDir = (aimWorld - camPos).normalized;

                float camToShipProj = Vector3.Dot(transform.position - camPos, rayDir);
                Vector3 rayStart = camPos + rayDir * Mathf.Max(0f, camToShipProj + 1.5f);
                float maxScanDist = cfg.maxConvergenceScanDistance > 0f ? cfg.maxConvergenceScanDistance : 250f;
                float scanDist = Mathf.Max(50f, maxScanDist - camToShipProj);
                float scanRadius = cfg.dynamicConvergenceRadius > 0.05f ? cfg.dynamicConvergenceRadius : 0.8f;
                LayerMask mask = (cfg.enemyLayer.value != 0) ? cfg.enemyLayer : ~0;

                int hitCount = Physics.SphereCastNonAlloc(
                    rayStart,
                    scanRadius,
                    rayDir,
                    _depthScanHits,
                    scanDist,
                    mask,
                    QueryTriggerInteraction.Ignore
                );

                Vector3 fwd = (anchor != null) ? anchor.transform.forward : transform.forward;
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit h = _depthScanHits[i];
                    if (h.collider == null) continue;

                    bool isSelf = h.transform == transform || 
                                  h.transform.IsChildOf(transform) || 
                                  h.transform.root == transform.root || 
                                  h.transform.CompareTag("Player");
                    if (isSelf) continue;

                    Vector3 toHit = h.point - transform.position;
                    if (Vector3.Dot(toHit, fwd) <= 1.0f) continue;

                    if (h.distance < closestDist)
                    {
                        closestDist = h.distance;
                        if (h.transform.CompareTag("Enemy"))
                        {
                            dynamicTarget = h.transform.position;
                        }
                        else
                        {
                            var enemy = h.transform.GetComponentInParent<EnemyMovement>();
                            dynamicTarget = enemy != null ? enemy.transform.position : h.point;
                        }
                        foundTarget = true;
                    }
                }
            }

            return foundTarget ? dynamicTarget : aimWorld;
        }

        public void FireAllMuzzles()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            var cfg = (anchor != null) ? anchor.config : null;

            Vector3 aimWorld = GetCurrentConvergencePoint(out bool foundTarget, out float closestDist);

            if (AudioManager.Instance != null)
            {
                AudioSFXType laserSfx = AudioSFXType.LaserSingle;
                if (_laserLevel == LaserUpgradeLevel.Twin) laserSfx = AudioSFXType.LaserTwin;
                else if (_laserLevel == LaserUpgradeLevel.Hyper) laserSfx = AudioSFXType.LaserHyper;
                AudioManager.Instance.PlaySFX2D(laserSfx, 0.75f, UnityEngine.Random.Range(0.97f, 1.03f));
            }
            VirtualRailCameraRig.TriggerShake(0.035f);

            // 1. Ưu tiên bắn từ nòng súng sạch VirtualRailMuzzle
            if (cleanMuzzles != null && cleanMuzzles.Length > 0)
            {
                for (int i = 0; i < cleanMuzzles.Length; i++)
                {
                    if (cleanMuzzles[i] == null) continue;
                    FireSingleMuzzle(cleanMuzzles[i].Position, null, aimWorld, cfg, foundTarget, closestDist);
                    cleanMuzzles[i].PlayFireEffects();
                }
                return;
            }

            // 2. Fallback duyệt nòng súng Laser (chỉ lấy vị trí, không bắn hitscan)
            if (muzzles == null || muzzles.Length == 0)
            {
                muzzles = GetComponentsInChildren<Laser>();
            }

            if (muzzles == null || muzzles.Length == 0)
            {
                // Fallback nếu prefab chưa có nòng súng: Bắn từ 2 cánh tàu
                Vector3 leftWing = transform.position - transform.right * 1.5f;
                Vector3 rightWing = transform.position + transform.right * 1.5f;
                FireSingleMuzzle(leftWing, null, aimWorld, cfg, foundTarget, closestDist);
                FireSingleMuzzle(rightWing, null, aimWorld, cfg, foundTarget, closestDist);
                return;
            }

            foreach (var muzzle in muzzles)
            {
                if (muzzle == null) continue;
                FireSingleMuzzle(muzzle.transform.position, muzzle, aimWorld, cfg, foundTarget, closestDist);
            }
        }

        private void FireSingleMuzzle(Vector3 muzzlePos, Laser muzzle, Vector3 aimWorld, VirtualRailConfig cfg, bool foundTarget = false, float dynamicDist = 150f)
        {
            Vector3 targetAim = aimWorld;
            if (cfg != null && cfg.enableGimbalWeapon)
            {
                float convDist = (foundTarget && dynamicDist < cfg.convergenceDistance && dynamicDist > 5f)
                    ? dynamicDist
                    : ((cfg.convergenceDistance > 10f) ? cfg.convergenceDistance : 150f);
                Vector3 origin = (ship != null) ? ship.transform.position : transform.position;
                targetAim = origin + _currentGimbalDir * convDist;
            }

            // Vector ngắm chuẩn xác từ nòng súng tới tọa độ mục tiêu súng đang chĩa
            Vector3 diff = targetAim - muzzlePos;
            Vector3 baseFireDirection = diff.sqrMagnitude > 0.001f ? diff.normalized : (cfg != null && cfg.enableGimbalWeapon ? _currentGimbalDir : transform.forward);
            Vector3 fireDirection = baseFireDirection;
            RaycastHit hit;
            Transform hitTarget = null;
            Vector3 targetPoint = targetAim;

            // Kiểm tra phễu từ tính (Bullet Magnetism) - Tự động kích hoạt trên Mobile hoặc khi bật trong Config
            bool isMagnetismEnabled = (cfg != null && cfg.enableBulletMagnetism) || Application.isMobilePlatform;
            if (isMagnetismEnabled)
            {
                float magRadius = (cfg != null && cfg.magnetismRadius > 0.01f) ? cfg.magnetismRadius : 3.5f;
                float maxAngle = (cfg != null && cfg.magnetismMaxAngle > 0.01f) ? cfg.magnetismMaxAngle : 7.0f;
                float scanDist = (cfg != null && cfg.convergenceDistance > 10f) ? cfg.convergenceDistance * 1.5f : 225f;
                LayerMask enemyMask = (cfg != null && cfg.enemyLayer.value != 0) ? cfg.enemyLayer : ~0;

                if (Physics.SphereCast(muzzlePos, magRadius, baseFireDirection, out hit, scanDist, enemyMask))
                {
                    bool isSelf = hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.root == transform.root || hit.transform.CompareTag("Player");
                    if (!isSelf)
                    {
                        var eh = hit.transform.GetComponentInParent<EnemyHealth>();
                        var em = (eh == null) ? hit.transform.GetComponentInParent<EnemyMovement>() : null;
                        Transform targetTrans = (eh != null) ? eh.transform : ((em != null) ? em.transform : (hit.transform.CompareTag("Enemy") ? hit.transform : null));

                        if (targetTrans != null)
                        {
                            Vector3 enemyCenter = targetTrans.position;
                            Vector3 candidateDir = (enemyCenter - muzzlePos).normalized;
                            float angle = Vector3.Angle(baseFireDirection, candidateDir);
                            if (angle <= maxAngle)
                            {
                                fireDirection = candidateDir;
                                hitTarget = targetTrans;
                                targetPoint = enemyCenter;
                            }
                        }
                    }
                }
            }

            int boltDamage = 20;
            Color boltColor = cfg.laserColor;
            float boltWidth = cfg.laserBoltWidth;

            if (_laserLevel == LaserUpgradeLevel.Twin)
            {
                boltDamage = 30;
                boltColor = new Color(1.0f, 0.82f, 0.15f); // Vàng rực Twin Laser
                boltWidth = cfg.laserBoltWidth * 1.25f;
            }
            else if (_laserLevel == LaserUpgradeLevel.Hyper)
            {
                boltDamage = 45;
                boltColor = new Color(0.2f, 1.0f, 0.45f); // Xanh lục ngọc Hyper Laser
                boltWidth = cfg.laserBoltWidth * 1.5f;
            }

            if (VirtualRailLaserPool.Instance != null)
            {
                float collisionRadius = cfg.laserBoltCollisionRadius > 0.05f ? cfg.laserBoltCollisionRadius : 1.0f;
                Vector3 inheritedVel = Vector3.zero;
                if (anchor != null)
                {
                    inheritedVel = anchor.transform.forward * anchor.CurrentSpeed;
                }
                if (cfg.enableLateralVelocityInheritance && ship != null)
                {
                    inheritedVel += ship.CurrentLateralVelocityWorld * cfg.lateralVelocityWeight;
                }

                // Bắn đoạn đạn laser độc lập (Segmented Laser Bolt - Chuẩn Star Fox 64, 0 GC Alloc)
                VirtualRailLaserPool.Instance.FireBolt(
                    muzzlePos,
                    fireDirection,
                    cfg.laserBoltSpeed,
                    cfg.laserBoltLength,
                    cfg.laserBoltLifetime,
                    boltColor,
                    boltWidth,
                    cfg.laserBoltMaterial,
                    transform,
                    cfg.enemyLayer,
                    collisionRadius,
                    inheritedVel,
                    boltDamage
                );
            }
        }

        /// <summary>
        /// Khôi phục hệ thống vũ khí về trạng thái ban đầu khi hồi sinh.
        /// </summary>
        public void ResetWeapon()
        {
            _laserLevel = LaserUpgradeLevel.Single;
            _goldRingsCollected = 0;
            nextFireTime = 0f;
            _weaponCooldownUntil = 0f;
            _chargeState = ChargeState.Idle;
            _fireButtonDownTime = -1f;
        }
    }
}
