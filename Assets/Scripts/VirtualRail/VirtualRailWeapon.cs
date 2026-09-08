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
        }

        private void Update()
        {
            if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space))
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

            foreach (var muzzle in muzzles)
            {
                if (muzzle == null) continue;

                Vector3 muzzlePos = muzzle.transform.position;
                Vector3 fireDirection = (aimWorld - muzzlePos).normalized;

                // 1. Phễu từ tính (Bullet Magnetism) qua SphereCast
                RaycastHit hit;
                Transform hitTarget = null;
                Vector3 targetPoint = aimWorld;

                if (Physics.SphereCast(muzzlePos, cfg.magnetismRadius, fireDirection, out hit, cfg.convergenceDistance * 1.5f, cfg.enemyLayer))
                {
                    // Nắn nhẹ hướng đạn về trọng tâm mục tiêu
                    fireDirection = (hit.point - muzzlePos).normalized;
                    targetPoint = hit.point;
                    hitTarget = hit.transform;

                    // Kích nổ / xử lý sát thương
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
                else if (Physics.Raycast(muzzlePos, fireDirection, out hit, cfg.convergenceDistance * 2f))
                {
                    targetPoint = hit.point;
                    hitTarget = hit.transform;
                }
                else
                {
                    // Không trúng gì -> Bắn xuyên qua điểm hội tụ ra xa
                    targetPoint = muzzlePos + fireDirection * (cfg.convergenceDistance * 1.5f);
                }

                // Kích hoạt hiệu ứng tia laser từ nòng súng tới điểm đích
                muzzle.FireLaser(targetPoint, hitTarget);
            }
        }
    }
}
