using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Module Trợ ngắm & Khóa mục tiêu tụ lực (Charge Shot Lock-on).
    /// </summary>
    [DisallowMultipleComponent]
    public class AimAssistModule : MonoBehaviour
    {
        public static AimAssistModule Instance { get; private set; }

        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public GameObject missilePrefab;
        public RectTransform lockMarkerUI;
        private Camera mainCamera;

        [Header("Runtime State")]
        [SerializeField] private float currentCharge = 0f;
        [SerializeField] private bool isCharging = false;
        [SerializeField] private Transform lockedTarget;
        [SerializeField] private float _lockTimer = 0f;
        private bool _weaponDrivesCharge = false;

        [Header("Muzzle Charge Visual")]
        [SerializeField] private GameObject _muzzleVisualInstance;
        private Light _muzzleLight;

        public bool IsCharging => isCharging;
        public float CurrentCharge => currentCharge;
        public float ChargeProgress => (anchor != null && anchor.config != null && anchor.config.chargeTime > 0.001f)
            ? Mathf.Clamp01(currentCharge / anchor.config.chargeTime)
            : 0f;
        public bool IsFullyCharged => anchor != null && anchor.config != null && currentCharge >= anchor.config.chargeTime;
        public Transform LockedTarget => lockedTarget;
        public float LockProgress
        {
            get
            {
                if (lockedTarget == null) return 0f;
                float acqTime = (anchor != null && anchor.config != null && anchor.config.lockAcquisitionTime > 0.05f) ? anchor.config.lockAcquisitionTime : 0.35f;
                return Mathf.Clamp01(_lockTimer / acqTime);
            }
        }
        public bool IsHardLocked => lockedTarget != null && LockProgress >= 1.0f && IsFullyCharged;

        private VirtualRailShip _ship;
        private VirtualRailWeapon _weapon;
        private EnemyHealth _lockedTargetHealth;
        private AudioSource _chargeAudioSource;
        private float _nextAllowedLockSoundTime = 0f;
        private bool _hasPlayedChargeSpool = false;
        private static readonly Collider[] _scanColliders = new Collider[64];

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_muzzleVisualInstance != null)
            {
                Destroy(_muzzleVisualInstance);
            }
            if (_chargeAudioSource != null)
            {
                Destroy(_chargeAudioSource);
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (reticle == null) reticle = FindAnyObjectByType<VirtualRailReticle>();
            if (_ship == null && anchor != null) _ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (_ship == null) _ship = FindAnyObjectByType<VirtualRailShip>();
            if (_weapon == null && anchor != null) _weapon = anchor.GetComponentInChildren<VirtualRailWeapon>();
            if (_weapon == null) _weapon = FindAnyObjectByType<VirtualRailWeapon>();

            if (lockMarkerUI != null) lockMarkerUI.gameObject.SetActive(false);

            InitializeMuzzleChargeVisual();
        }

        private void InitializeMuzzleChargeVisual()
        {
            if (missilePrefab == null) return;
            if (_muzzleVisualInstance != null) return;

            // Gắn vào VisualMesh để quả cầu xoay đồng bộ 100% theo các thao tác nghiêng cánh, lượn, lộn vòng (Barrel Roll)
            Transform attachTransform = (_ship != null && _ship.VisualMesh != null) ? _ship.VisualMesh : ((_ship != null) ? _ship.transform : transform);

            // Kiểm tra xem đã có visual tồn tại sẵn chưa (tìm cả ở attachTransform lẫn ship gốc để tái sử dụng/reparent)
            Transform existing = attachTransform.Find("[ChargeMuzzleVisual]");
            if (existing == null && _ship != null && _ship.transform != null)
            {
                existing = _ship.transform.Find("[ChargeMuzzleVisual]");
                if (existing != null)
                {
                    existing.SetParent(attachTransform, false);
                }
            }

            if (existing != null)
            {
                _muzzleVisualInstance = existing.gameObject;
            }
            else
            {
                _muzzleVisualInstance = Instantiate(missilePrefab, attachTransform);
                _muzzleVisualInstance.name = "[ChargeMuzzleVisual]";
            }

            // Tọa độ Z = 5.5m đặt quả cầu ngay phía trước chóp mũi tàu StarSparrow1 (tránh kẹt trong buồng lái cockpit Z ~ 2.5m)
            _muzzleVisualInstance.transform.localPosition = new Vector3(0f, 0f, 5.5f);
            _muzzleVisualInstance.transform.localRotation = Quaternion.identity;
            _muzzleVisualInstance.transform.localScale = Vector3.zero;

            // Loại bỏ hoàn toàn các component vật lý và gameplay (Presentation only - 0 GC Alloc)
            var col = _muzzleVisualInstance.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rb = _muzzleVisualInstance.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            var missile = _muzzleVisualInstance.GetComponent<VirtualRailHomingMissile>();
            if (missile != null) Destroy(missile);
            var trail = _muzzleVisualInstance.GetComponent<TrailRenderer>();
            if (trail != null) Destroy(trail);

            _muzzleLight = _muzzleVisualInstance.GetComponentInChildren<Light>(true);

            _muzzleVisualInstance.SetActive(false);
        }

        /// <summary>
        /// Kích hoạt tụ lực từ hệ thống vũ khí VirtualRailWeapon (Giữ phím Fire1/Space >= 0.25s).
        /// </summary>
        public void BeginChargingFromWeapon()
        {
            _weaponDrivesCharge = true;
            isCharging = true;
            _hasPlayedChargeSpool = false;
        }

        /// <summary>
        /// Cập nhật thời gian tụ lực từ VirtualRailWeapon mỗi frame.
        /// </summary>
        public void UpdateChargeFromWeapon(float dt)
        {
            _weaponDrivesCharge = true;
            isCharging = true;
            currentCharge += dt;

            var cfg = (anchor != null) ? anchor.config : null;
            bool canScan = (cfg != null && cfg.enableEarlyLockScan) || (cfg != null && currentCharge >= cfg.chargeTime);
            if (canScan)
            {
                ScanForLockTarget(cfg, dt);
            }
        }

        /// <summary>
        /// Nhả nút bắn từ VirtualRailWeapon -> Phóng đạn tự dẫn (nếu có khóa) hoặc đạn Plasma tụ lực thẳng.
        /// </summary>
        public void ReleaseChargeFromWeapon()
        {
            _weaponDrivesCharge = false;
            if (IsFullyCharged)
            {
                if (lockedTarget != null)
                {
                    FireHomingMissile();
                }
                else
                {
                    FireDumbChargeShot();
                }
            }
            ResetChargeState();
        }

        /// <summary>
        /// Hủy trạng thái tụ lực.
        /// </summary>
        public void CancelChargeFromWeapon()
        {
            _weaponDrivesCharge = false;
            ResetChargeState();
        }

        private void ResetChargeState()
        {
            currentCharge = 0f;
            isCharging = false;
            lockedTarget = null;
            _lockTimer = 0f;
            _nextAllowedLockSoundTime = 0f;
            _hasPlayedChargeSpool = false;
            if (_chargeAudioSource != null && _chargeAudioSource.isPlaying)
            {
                _chargeAudioSource.Stop();
            }
            if (lockMarkerUI != null) lockMarkerUI.gameObject.SetActive(false);
            if (_muzzleVisualInstance != null && _muzzleVisualInstance.activeSelf)
            {
                _muzzleVisualInstance.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            UpdateMuzzleChargeVisual();
            UpdateChargeAudioLoop();
        }

        private void InitializeChargeAudio()
        {
            if (_chargeAudioSource == null)
            {
                _chargeAudioSource = gameObject.AddComponent<AudioSource>();
                _chargeAudioSource.loop = false; // Thuần One-Shot, không lặp lại song song với BGM
                _chargeAudioSource.playOnAwake = false;
                _chargeAudioSource.spatialBlend = 0f; // 2D Stereo
            }
        }

        private void UpdateChargeAudioLoop()
        {
            if (_chargeAudioSource == null) InitializeChargeAudio();

            var cfg = (anchor != null) ? anchor.config : null;
            float volMin = (cfg != null) ? cfg.chargeHumVolumeMin : 0.28f;
            float volMax = (cfg != null) ? cfg.chargeHumVolumeMax : 0.45f;
            float sfxMultiplier = (AudioManager.Instance != null)
                ? (AudioManager.Instance.SfxVolume * AudioManager.Instance.MasterVolume)
                : 1.0f;

            if (isCharging)
            {
                if (!_hasPlayedChargeSpool)
                {
                    AudioClip clip = AudioManager.Instance != null ? AudioManager.Instance.GetClipForType(AudioSFXType.ChargeHum) : null;
                    if (clip != null)
                    {
                        _chargeAudioSource.clip = clip;
                        _chargeAudioSource.volume = volMin * sfxMultiplier;
                        _chargeAudioSource.pitch = 1.0f;
                        _chargeAudioSource.loop = false;
                        _chargeAudioSource.Play();
                        _hasPlayedChargeSpool = true;
                    }
                }

                if (_chargeAudioSource.isPlaying)
                {
                    float progress = ChargeProgress;
                    _chargeAudioSource.pitch = Mathf.Lerp(0.98f, 1.04f, progress);
                    _chargeAudioSource.volume = Mathf.Lerp(volMin, volMax, progress) * sfxMultiplier;
                }
            }
            else
            {
                _hasPlayedChargeSpool = false;
                if (_chargeAudioSource != null && _chargeAudioSource.isPlaying)
                {
                    _chargeAudioSource.Stop();
                }
            }
        }

        /// <summary>
        /// Hiển thị và mở rộng quả cầu plasma năng lượng tại mũi tàu trong lúc giữ phím tụ lực (0 GC Alloc).
        /// </summary>
        private void UpdateMuzzleChargeVisual()
        {
            if (_muzzleVisualInstance == null) return;

            var cfg = (anchor != null) ? anchor.config : null;
            bool enableMuzzleVisual = (cfg == null || cfg.enableChargeMuzzleVisual);

            if (isCharging && enableMuzzleVisual)
            {
                if (!_muzzleVisualInstance.activeSelf)
                {
                    _muzzleVisualInstance.SetActive(true);
                }

                float maxDiameter = (cfg != null) ? cfg.chargeMuzzleMaxDiameter : 1.0f;
                float progress = ChargeProgress; // 0.0f đến 1.0f

                // Xung nhịp điện trường nhấp nháy tần số cao (chuẩn Star Fox 64)
                float freq = Mathf.Lerp(12f, 28f, progress);
                float wave = Mathf.Sin(Time.time * freq);
                float buzz = 1f + wave * 0.08f;

                // Quả cầu lớn dần từ 0.08m đến maxDiameter (1.0m)
                float currentDiameter = Mathf.Lerp(0.08f, maxDiameter, progress) * buzz;
                // Chuẩn Houidisoft gốc là 3.85m -> scale tương đối của prefab
                float rootScale = currentDiameter / 3.85f;

                _muzzleVisualInstance.transform.localScale = Vector3.one * rootScale;

                if (_muzzleLight != null)
                {
                    _muzzleLight.enabled = true;
                    _muzzleLight.intensity = Mathf.Lerp(0.8f, 4.5f, progress) * (1f + wave * 0.25f);
                    _muzzleLight.range = Mathf.Lerp(3f, 12f, progress);
                }
            }
            else
            {
                if (_muzzleVisualInstance.activeSelf)
                {
                    _muzzleVisualInstance.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (anchor == null || anchor.config == null || reticle == null) return;

            var cfg = anchor.config;

            // Nếu không bị điều khiển bởi VirtualRailWeapon, hỗ trợ Chuột phải (Fire2) độc lập (Chỉ dành cho PC, tránh đa chạm Mobile kích hoạt nhầm)
            if (!_weaponDrivesCharge && !Application.isMobilePlatform)
            {
                bool rightClickHeld = Input.GetButton("Fire2");

                if (rightClickHeld)
                {
                    if (!isCharging)
                    {
                        _hasPlayedChargeSpool = false;
                    }
                    isCharging = true;
                    currentCharge += Time.deltaTime;

                    bool canScan = (cfg.enableEarlyLockScan) || (currentCharge >= cfg.chargeTime);
                    if (canScan)
                    {
                        ScanForLockTarget(cfg, Time.deltaTime);
                    }
                }
                else if (isCharging)
                {
                    if (IsFullyCharged)
                    {
                        if (lockedTarget != null)
                        {
                            FireHomingMissile();
                        }
                        else
                        {
                            FireDumbChargeShot();
                        }
                    }

                    ResetChargeState();
                }
            }

            // Cập nhật vị trí UI Lock Marker
            UpdateLockMarkerUI();
        }

        private void ScanForLockTarget(VirtualRailConfig cfg, float dt)
        {
            if (cfg == null) return;
            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 shipPos = (_ship != null) ? _ship.transform.position : transform.position;
            Vector3 camPos = (mainCamera != null) ? mainCamera.transform.position : shipPos;
            Vector3 aimWorld = (reticle != null) ? reticle.WorldPosition : (shipPos + transform.forward * 150f);
            Vector3 aimRayDir = (aimWorld - camPos).normalized;

            LayerMask mask = (cfg.enemyLayer.value != 0) ? cfg.enemyLayer : ~0;
            int count = Physics.OverlapSphereNonAlloc(shipPos, cfg.lockMaxDistance, _scanColliders, mask, QueryTriggerInteraction.Ignore);

            float cosLimit = Mathf.Cos(cfg.lockConeAngle * Mathf.Deg2Rad);
            float bestCos = cosLimit;
            Transform bestTarget = null;

            Vector3 railFwd = (anchor != null) ? anchor.ForwardTangent : transform.forward;

            for (int i = 0; i < count; i++)
            {
                Collider c = _scanColliders[i];
                if (c == null) continue;

                // Loại trừ thân tàu người chơi
                if (c.transform == transform || c.transform.root == transform.root || c.CompareTag("Player")) continue;
                if (_ship != null && (c.transform == _ship.transform || c.transform.IsChildOf(_ship.transform))) continue;

                // Lấy entity chính (EnemyHealth hoặc EnemyMovement hoặc Asteroid)
                var eh = c.GetComponentInParent<EnemyHealth>();
                if (eh != null && eh.IsDead)
                {
                    _scanColliders[i] = null;
                    continue;
                }

                Transform enemyT;
                if (eh != null)
                {
                    enemyT = eh.transform;
                }
                else
                {
                    var em = c.GetComponentInParent<EnemyMovement>();
                    enemyT = (em != null) ? em.transform : c.transform.root;
                }

                if (enemyT == null || !enemyT.gameObject.activeInHierarchy)
                {
                    _scanColliders[i] = null;
                    continue;
                }

                Vector3 enemyPos = enemyT.position;
                Vector3 toEnemy = enemyPos - shipPos;
                float dist = toEnemy.magnitude;

                // Đảm bảo mục tiêu ở phía trước tàu theo trục tiến và trong tầm tối đa
                if (dist < 5f || dist > cfg.lockMaxDistance || Vector3.Dot(toEnemy, railFwd) <= 1.0f)
                {
                    _scanColliders[i] = null;
                    continue;
                }

                // Tính góc giữa tia ngắm của Camera/Reticle và vị trí kẻ địch (Chuẩn Star Fox 64 View-Frustum Lock)
                Vector3 toEnemyFromCam = (enemyPos - camPos).normalized;
                float cosTheta = Vector3.Dot(toEnemyFromCam, aimRayDir);

                if (cosTheta > bestCos)
                {
                    bestCos = cosTheta;
                    bestTarget = enemyT;
                }
                _scanColliders[i] = null;
            }

            if (bestTarget != null)
            {
                if (bestTarget == lockedTarget)
                {
                    _lockTimer += dt;
                }
                else
                {
                    lockedTarget = bestTarget;
                    _lockedTargetHealth = bestTarget != null ? bestTarget.GetComponentInParent<EnemyHealth>() : null;
                    _lockTimer = dt;
                    if (Time.time >= _nextAllowedLockSoundTime)
                    {
                        float lockVol = (cfg != null) ? cfg.chargeLockOnVolume : 0.55f;
                        AudioManager.Instance?.PlaySFX2D(AudioSFXType.ChargeLockOn, lockVol);
                        _nextAllowedLockSoundTime = Time.time + 0.35f;
                    }
                }
            }
            else
            {
                lockedTarget = null;
                _lockedTargetHealth = null;
                _lockTimer = 0f;
            }
        }

        private void FireHomingMissile()
        {
            if (missilePrefab == null)
            {
                // Nếu chưa có prefab riêng, dùng nổ lan trực tiếp
                if (lockedTarget != null)
                {
                    TriggerDirectSplash(lockedTarget.position, lockedTarget);
                }
                return;
            }

            Vector3 nosePos = (_ship != null && _ship.VisualMesh != null)
                ? (_ship.VisualMesh.position + _ship.VisualMesh.forward * 5.5f)
                : ((_ship != null)
                    ? (_ship.transform.position + _ship.ShipForward * 5.5f)
                    : (transform.position + transform.forward * 5.5f));

            var cfg = (anchor != null) ? anchor.config : null;
            Vector3 launchDir;
            if (_weapon != null && cfg != null && cfg.enableGimbalWeapon)
            {
                launchDir = _weapon.CurrentGimbalDir;
            }
            else if (reticle != null)
            {
                launchDir = (reticle.WorldPosition - nosePos).normalized;
            }
            else
            {
                launchDir = (_ship != null) ? _ship.ShipForward : transform.forward;
            }

            Quaternion spawnRot = Quaternion.LookRotation(launchDir);
            VirtualRailHomingMissile missile = (VirtualRailMissilePool.Instance != null)
                ? VirtualRailMissilePool.Instance.SpawnMissile(missilePrefab, nosePos, spawnRot)
                : null;
            if (missile == null) return;

            float shipSpeed = (anchor != null) ? anchor.CurrentSpeed : 70f;
            float baseSpeed = (cfg != null && cfg.chargeShotBaseSpeed > 10f) ? cfg.chargeShotBaseSpeed : 240f;
            float turnSpeed = (cfg != null && cfg.chargeShotTurnRate > 10f) ? cfg.chargeShotTurnRate : 420f;
            float inheritedSpeed = (cfg != null && cfg.enableChargeVelocityInheritance) ? shipSpeed : 0f;
            missile.InitializeKinematics(baseSpeed, inheritedSpeed, turnSpeed, cfg, lockedTarget);
            float splashR = (cfg != null && cfg.chargeShotSplashRadius > 0.1f) ? cfg.chargeShotSplashRadius : 10f;
            missile.splashRadius = splashR;

            float fireVol = (cfg != null) ? cfg.chargeFireVolume : 0.60f;
            AudioManager.Instance?.PlaySFX2D(AudioSFXType.ChargeFire, fireVol);
            VirtualRailCameraRig.TriggerShake(0.18f);
        }

        public void FireDumbChargeShot()
        {
            if (missilePrefab == null)
            {
                Vector3 center = transform.position + transform.forward * 40f;
                TriggerDirectSplash(center, null);
                return;
            }

            Vector3 nosePos = (_ship != null && _ship.VisualMesh != null)
                ? (_ship.VisualMesh.position + _ship.VisualMesh.forward * 5.5f)
                : ((_ship != null)
                    ? (_ship.transform.position + _ship.ShipForward * 5.5f)
                    : (transform.position + transform.forward * 5.5f));

            // Giai đoạn 1: Dynamic Depth Convergence cho Đạn tụ lực Dumb-fire
            Vector3 convergenceTarget = (_weapon != null)
                ? _weapon.GetCurrentConvergencePoint(out _, out _)
                : ((reticle != null) ? reticle.WorldPosition : (nosePos + transform.forward * 150f));

            Vector3 launchDir = (convergenceTarget - nosePos).normalized;
            if (launchDir.sqrMagnitude < 0.001f)
            {
                launchDir = (_ship != null) ? _ship.ShipForward : transform.forward;
            }

            Quaternion spawnRot = Quaternion.LookRotation(launchDir);
            VirtualRailHomingMissile missile = (VirtualRailMissilePool.Instance != null)
                ? VirtualRailMissilePool.Instance.SpawnMissile(missilePrefab, nosePos, spawnRot)
                : null;
            if (missile == null) return;

            var cfg = (anchor != null) ? anchor.config : null;
            float shipSpeed = (anchor != null) ? anchor.CurrentSpeed : 70f;
            float baseSpeed = (cfg != null && cfg.chargeShotBaseSpeed > 10f) ? cfg.chargeShotBaseSpeed : 240f;
            float turnSpeed = (cfg != null && cfg.chargeShotTurnRate > 10f) ? cfg.chargeShotTurnRate : 420f;
            float inheritedSpeed = (cfg != null && cfg.enableChargeVelocityInheritance) ? shipSpeed : 0f;
            missile.InitializeKinematics(baseSpeed, inheritedSpeed, turnSpeed, cfg, null);
            float splashRDumb = (cfg != null && cfg.chargeShotSplashRadius > 0.1f) ? cfg.chargeShotSplashRadius : 10f;
            missile.splashRadius = splashRDumb;

            float fireVol = (cfg != null) ? cfg.chargeFireVolume : 0.60f;
            AudioManager.Instance?.PlaySFX2D(AudioSFXType.ChargeFire, fireVol);
            VirtualRailCameraRig.TriggerShake(0.18f);
        }

        private void TriggerDirectSplash(Vector3 center, Transform directTarget)
        {
            var cfg = (anchor != null) ? anchor.config : null;
            float splashR = (cfg != null && cfg.chargeShotSplashRadius > 0.1f) ? cfg.chargeShotSplashRadius : 10f;
            int killCount = 0;
            LayerMask mask = (cfg != null && cfg.enemyLayer.value != 0) ? cfg.enemyLayer : ~0;
            int hitCount = Physics.OverlapSphereNonAlloc(center, splashR, _scanColliders, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var col = _scanColliders[i];
                if (col == null) continue;
                if (col.CompareTag("Player") || col.transform.root == transform.root) continue;

                var eh = col.GetComponentInParent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    bool isDirect = (directTarget != null && (col.transform == directTarget || col.transform.IsChildOf(directTarget)));
                    if (eh.IsShieldActive)
                    {
                        Vector3 toExplosion = (center - eh.transform.position).normalized;
                        bool hitShieldFront = Vector3.Dot(toExplosion, eh.transform.forward) > 0.1f;
                        if (hitShieldFront)
                        {
                            eh.TakeDamage(0, eh.transform.position, transform.forward);
                            continue;
                        }
                    }

                    int dmg = isDirect ? 100 : 50;
                    if (eh.TakeDamage(dmg, eh.transform.position, (eh.transform.position - transform.position).normalized))
                    {
                        killCount++;
                    }
                    continue;
                }

                var em = col.GetComponentInParent<EnemyMovement>();
                if (em != null && em.gameObject.activeSelf)
                {
                    em.BlowUp();
                    killCount++;
                    continue;
                }

                var ast = col.GetComponentInParent<Asteroid>();
                if (ast != null && ast.gameObject.activeSelf)
                {
                    ast.TakeDamage(100, ast.transform.position);
                }
                _scanColliders[i] = null;
            }
            if (killCount > 0 && ComboScoringEngine.Instance != null)
            {
                ComboScoringEngine.Instance.RegisterSplashKill(killCount);
            }
        }

        private void UpdateLockMarkerUI()
        {
            var cfg = (anchor != null) ? anchor.config : null;
            if (cfg != null && cfg.enableArcadeLockVisual)
            {
                // Nhường quyền hiển thị khung khóa chuyên nghiệp cho DualReticleHUD
                if (lockMarkerUI != null && lockMarkerUI.gameObject.activeSelf)
                {
                    lockMarkerUI.gameObject.SetActive(false);
                }
                return;
            }

            if (lockMarkerUI == null) return;
            if (mainCamera == null) mainCamera = Camera.main;

            if (lockedTarget != null && mainCamera != null)
            {
                // Nếu mục tiêu đã bị tiêu diệt hoặc inactive, lập tức hủy khóa
                if (!lockedTarget.gameObject.activeInHierarchy)
                {
                    lockedTarget = null;
                    _lockedTargetHealth = null;
                    _lockTimer = 0f;
                    lockMarkerUI.gameObject.SetActive(false);
                    return;
                }

                if (_lockedTargetHealth != null && _lockedTargetHealth.IsDead)
                {
                    lockedTarget = null;
                    _lockedTargetHealth = null;
                    _lockTimer = 0f;
                    lockMarkerUI.gameObject.SetActive(false);
                    return;
                }

                Vector3 screenPos = mainCamera.WorldToScreenPoint(lockedTarget.position);
                if (screenPos.z > 0)
                {
                    lockMarkerUI.gameObject.SetActive(true);
                    lockMarkerUI.position = screenPos;
                    return;
                }
            }

            lockMarkerUI.gameObject.SetActive(false);
        }
    }
}
