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
        public VirtualRailInputReader inputReader;
        public Laser[] muzzles;

        [Header("Weapon Timing")]
        public float fireRate = 0.15f;
        private float nextFireTime = 0f;

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
            if (inputReader == null && anchor != null) inputReader = anchor.GetComponentInChildren<VirtualRailInputReader>();
        }

        private void Update()
        {
            var cfg = anchor != null ? anchor.config : null;
            bool isManualFire = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);
            bool isInputReaderFiring = inputReader != null && inputReader.IsFiring();
            bool isReticleAutoFire = cfg != null && cfg.autoFireOnAimMove && reticle != null && reticle.IsAimingMoving;
            bool isHoverFire = cfg != null && cfg.enableMouseHoverFire && reticle != null && reticle.IsMouseTouchingReticle;

            if (isManualFire || isInputReaderFiring || isReticleAutoFire || isHoverFire)
            {
                if (Time.time >= nextFireTime)
                {
                    FireAllMuzzles();
                    nextFireTime = Time.time + fireRate;
                }
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

            // Giai đoạn 1: Dynamic Depth Convergence (Hội tụ độ sâu động theo mục tiêu)
            if (cfg.enableDynamicDepthConvergence)
            {
                Camera cam = Camera.main;
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

                float closestDist = float.MaxValue;
                Vector3 dynamicTarget = aimWorld;
                bool foundTarget = false;

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
                FireSingleMuzzle(leftWing, null, aimWorld, cfg);
                FireSingleMuzzle(rightWing, null, aimWorld, cfg);
                return;
            }

            foreach (var muzzle in muzzles)
            {
                if (muzzle == null) continue;
                FireSingleMuzzle(muzzle.transform.position, muzzle, aimWorld, cfg);
            }
        }

        private void FireSingleMuzzle(Vector3 muzzlePos, Laser muzzle, Vector3 aimWorld, VirtualRailConfig cfg)
        {
            // Vector ngắm chuẩn xác 100% từ nòng súng tới tọa độ hồng tâm 3D (Pure 3D Convergence)
            Vector3 diff = aimWorld - muzzlePos;
            Vector3 baseFireDirection = diff.sqrMagnitude > 0.001f ? diff.normalized : transform.forward;
            Vector3 fireDirection = baseFireDirection;
            RaycastHit hit;
            Transform hitTarget = null;
            Vector3 targetPoint = aimWorld;

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
                // Hướng 1: Bắn đoạn đạn laser độc lập (Segmented Laser Bolt)
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
                    cfg.enemyLayer
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
