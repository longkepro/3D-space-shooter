using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Cơ chế Hội tụ Đạn đạo 3D (3D Convergence) & Phễu từ tính (Bullet Magnetism).
    /// Đạn bắn từ mọi nòng súng luôn hội tụ xuyên qua đúng tâm của khung ngắm 3D ở cự ly Z_conv.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailWeapon : MonoBehaviour
    {
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public VirtualRailShip ship;
        public VirtualRailInputReader inputReader;
        public Laser[] muzzles;

        [Header("Weapon Timing")]
        public float fireRate = 0.15f;
        private float nextFireTime = 0f;

        // Ụ súng xoay cơ học (Gimbaled Turrets - 0 GC Alloc)
        private Vector3 _currentGimbalDir = Vector3.forward;
        private bool _gimbalInitialized = false;
        private bool _isGimbalClamped = false;
        private float _gimbalDeviationAngle = 0f;
        private Camera _cachedCam;

        public Vector3 CurrentGimbalDir => _currentGimbalDir;
        public bool IsGimbalClamped => _isGimbalClamped;
        public float GimbalDeviationAngle => _gimbalDeviationAngle;

        // Bộ đệm RaycastHit dùng chung 0 GC Alloc cho Dynamic Depth Convergence
        private static readonly RaycastHit[] _depthScanHits = new RaycastHit[16];

        private void Awake()
        {
            if (muzzles == null || muzzles.Length == 0)
            {
                muzzles = GetComponentsInChildren<Laser>();
            }
        }

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (ship == null) ship = FindAnyObjectByType<VirtualRailShip>();
            if (inputReader == null && anchor != null) inputReader = anchor.GetComponentInChildren<VirtualRailInputReader>();
            if (_cachedCam == null) _cachedCam = Camera.main;
        }

        private void Update()
        {
            var cfg = anchor != null ? anchor.config : null;
            if (cfg != null)
            {
                UpdateGimbalOrientation(cfg);
            }

            bool isManualFire = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);
            bool isInputReaderFiring = inputReader != null && inputReader.IsFiring();
            bool isReticleAutoFire = cfg != null && cfg.autoFireOnAimMove && reticle != null && reticle.IsAimingMoving;
            bool isHoverFire = cfg != null && cfg.enableMouseHoverFire && !cfg.enableMouseAim && reticle != null && reticle.IsMouseTouchingReticle;

            if (isManualFire || isInputReaderFiring || isReticleAutoFire || isHoverFire)
            {
                if (Time.time >= nextFireTime)
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

        public void FireAllMuzzles()
        {
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (reticle == null) reticle = FindAnyObjectByType<VirtualRailReticle>();
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (anchor == null) anchor = FindAnyObjectByType<VirtualRailAnchor>();
            if (reticle == null || anchor == null || anchor.config == null) return;

            Vector3 aimWorld = reticle.WorldPosition;
            var cfg = anchor.config;

            float closestDist = float.MaxValue;
            Vector3 dynamicTarget = aimWorld;
            bool foundTarget = false;

            // Giai đoạn 1: Dynamic Depth Convergence (Hội tụ độ sâu động theo mục tiêu)
            if (cfg.enableDynamicDepthConvergence)
            {
                Camera cam = _cachedCam != null ? _cachedCam : Camera.main;
                Vector3 camPos = cam != null ? cam.transform.position : transform.position;
                Vector3 rayDir = (aimWorld - camPos).normalized;

                // Chiếu tia từ phía trước mũi tàu dọc theo trục ngắm của Camera để không quét trúng thân tàu
                float camToShipProj = Vector3.Dot(transform.position - camPos, rayDir);
                Vector3 rayStart = camPos + rayDir * Mathf.Max(0f, camToShipProj + 1.5f);
                float maxScanDist = cfg.maxConvergenceScanDistance > 0f ? cfg.maxConvergenceScanDistance : 250f;
                float scanDist = Mathf.Max(50f, maxScanDist - camToShipProj);
                float scanRadius = cfg.dynamicConvergenceRadius > 0.05f ? cfg.dynamicConvergenceRadius : 0.8f;

                int hitCount = Physics.SphereCastNonAlloc(
                    rayStart,
                    scanRadius,
                    rayDir,
                    _depthScanHits,
                    scanDist,
                    cfg.enemyLayer,
                    QueryTriggerInteraction.Ignore
                );

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit h = _depthScanHits[i];
                    if (h.collider == null) continue;

                    bool isSelf = h.transform == transform || 
                                  h.transform.IsChildOf(transform) || 
                                  h.transform.root == transform.root || 
                                  h.transform.CompareTag("Player");
                    if (isSelf) continue;

                    // Đảm bảo mục tiêu nằm ở phía trước tàu theo trục tiến của ray
                    Vector3 toHit = h.point - transform.position;
                    if (Vector3.Dot(toHit, anchor.transform.forward) <= 1.0f) continue;

                    if (h.distance < closestDist)
                    {
                        closestDist = h.distance;
                        // Nếu là kẻ địch có EnemyMovement, chụm đạn trực diện vào tâm kẻ địch; nếu là thiên thạch, chụm vào điểm chạm bề mặt
                        var enemy = h.transform.GetComponentInParent<EnemyMovement>();
                        dynamicTarget = enemy != null ? enemy.transform.position : h.point;
                        foundTarget = true;
                    }
                }

                if (foundTarget)
                {
                    aimWorld = dynamicTarget;
                }
            }

            if (muzzles == null || muzzles.Length == 0)
            {
                muzzles = GetComponentsInChildren<Laser>();
            }

            if (muzzles == null || muzzles.Length == 0)
            {
                // Fallback nếu prefab chưa có Laser component: Bắn từ 2 cánh tàu
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

            // Kiểm tra phễu từ tính (Bullet Magnetism) - Chỉ can thiệp nếu được chủ động bật trong Config
            if (cfg.enableBulletMagnetism && cfg.magnetismRadius > 0.01f)
            {
                if (Physics.SphereCast(muzzlePos, cfg.magnetismRadius, baseFireDirection, out hit, cfg.convergenceDistance * 1.5f, cfg.enemyLayer))
                {
                    bool isSelf = hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.root == transform.root || hit.transform.CompareTag("Player");
                    if (!isSelf)
                    {
                        // Lọc nghiêm ngặt: Chỉ áp dụng từ tính khi đối tượng quét trúng THẬT SỰ là Enemy
                        var enemy = hit.transform.GetComponentInParent<EnemyMovement>();
                        if (enemy != null)
                        {
                            // Hướng về tâm hình học của kẻ địch thay vì mép chạm vỏ ngoài hit.point
                            Vector3 enemyCenter = enemy.transform.position;
                            Vector3 candidateDir = (enemyCenter - muzzlePos).normalized;
                            float angle = Vector3.Angle(baseFireDirection, candidateDir);
                            if (angle <= cfg.magnetismMaxAngle)
                            {
                                fireDirection = candidateDir;
                                hitTarget = enemy.transform;
                                targetPoint = enemyCenter;
                            }
                        }
                    }
                }
            }

            if (cfg.useSegmentedLaser)
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

                // Hướng 1: Bắn đoạn đạn laser độc lập (Segmented Laser Bolt với Hành lang hỏa lực 3D thẳng tắp)
                VirtualRailLaserPool.Instance.FireBolt(
                    muzzlePos,
                    fireDirection,
                    cfg.laserBoltSpeed,
                    cfg.laserBoltLength,
                    cfg.laserBoltLifetime,
                    cfg.laserColor,
                    cfg.laserBoltWidth,
                    cfg.laserBoltMaterial,
                    transform,
                    cfg.enemyLayer,
                    collisionRadius,
                    inheritedVel
                );
            }
            else
            {
                // Fallback (Layer 2 Undo): Cơ chế tia tức thời cũ (Instant Hitscan Beam)
                if (hitTarget != null)
                {
                    var enemy = hitTarget.GetComponentInParent<EnemyMovement>();
                    if (enemy != null) enemy.BlowUp();
                }
                else if (Physics.Raycast(muzzlePos, fireDirection, out hit, cfg.convergenceDistance * 2f))
                {
                    targetPoint = hit.point;
                    hitTarget = hit.transform;

                    var enemy = hit.transform.GetComponentInParent<EnemyMovement>();
                    if (enemy != null)
                    {
                        enemy.BlowUp();
                    }
                    else if (hit.transform.CompareTag("Pickup"))
                    {
                        var pickup = hit.transform.GetComponentInParent<Pickup>();
                        if (pickup != null) pickup.Collect();
                    }
                }
                else
                {
                    targetPoint = muzzlePos + fireDirection * (cfg.convergenceDistance * 1.5f);
                }

                if (muzzle != null)
                {
                    muzzle.FireLaser(targetPoint, hitTarget);
                }
            }
        }
    }
}
