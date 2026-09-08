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

            if (isManualFire || isInputReaderFiring || isReticleAutoFire)
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
            if (reticle == null || anchor == null || anchor.config == null) return;

            Vector3 aimWorld = reticle.WorldPosition;
            var cfg = anchor.config;

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
            Vector3 fireDirection = (aimWorld - muzzlePos).normalized;
            RaycastHit hit;
            Transform hitTarget = null;
            Vector3 targetPoint = aimWorld;

            if (cfg.useSegmentedLaser)
            {
                // Hướng 1: Bắn đoạn đạn laser độc lập (Segmented Laser Bolt)
                if (Physics.SphereCast(muzzlePos, cfg.magnetismRadius, fireDirection, out hit, cfg.convergenceDistance * 1.5f, cfg.enemyLayer))
                {
                    bool isSelf = hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.root == transform.root || hit.transform.CompareTag("Player");
                    if (!isSelf)
                    {
                        // Nắn nhẹ hướng bay về trọng tâm mục tiêu (Magnetism)
                        fireDirection = (hit.point - muzzlePos).normalized;
                    }
                }

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
                if (Physics.SphereCast(muzzlePos, cfg.magnetismRadius, fireDirection, out hit, cfg.convergenceDistance * 1.5f, cfg.enemyLayer))
                {
                    bool isSelf = hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.root == transform.root || hit.transform.CompareTag("Player");
                    if (!isSelf)
                    {
                        fireDirection = (hit.point - muzzlePos).normalized;
                        targetPoint = hit.point;
                        hitTarget = hit.transform;

                        if (hit.transform.CompareTag("Enemy"))
                        {
                            var enemy = hit.transform.GetComponent<EnemyMovement>();
                            if (enemy != null) enemy.BlowUp();
                        }
                        else if (hit.transform.CompareTag("Pickup"))
                        {
                            var pickup = hit.transform.GetComponent<Pickup>();
                            if (pickup != null) pickup.Collect();
                        }
                    }
                }
                else if (Physics.Raycast(muzzlePos, fireDirection, out hit, cfg.convergenceDistance * 2f))
                {
                    targetPoint = hit.point;
                    hitTarget = hit.transform;
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
