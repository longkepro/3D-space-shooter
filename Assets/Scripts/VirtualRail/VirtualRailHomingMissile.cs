using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Đạn tự dẫn đuổi theo mục tiêu bị khóa (Homing Missile).
    /// </summary>
    public class VirtualRailHomingMissile : MonoBehaviour
    {
        public Transform target;
        public float speed = 90f;
        public float turnRate = 180f; // độ / giây
        public float lifeTime = 5f;
        public float detonationDistance = 2f;
        [Header("Splash AoE (TDD v1.0.0 Phần I.6)")]
        public float splashRadius = 10f;
        public GameObject explosionPrefab;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (target != null)
            {
                Vector3 targetDir = (target.position - transform.position).normalized;
                Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, turnRate * Mathf.Deg2Rad * Time.deltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(newDir);

                if (Vector3.Distance(transform.position, target.position) <= detonationDistance)
                {
                    Detonate();
                    return;
                }
            }

            transform.position += transform.forward * (speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy") || (target != null && other.transform == target))
            {
                Detonate();
            }
        }

        private void Detonate()
        {
            Vector3 impactPoint = transform.position;
            int killCount = 0;

            // 1. Quét nổ lan hình cầu R = 10m theo chuẩn TDD Phần I.6
            Collider[] colliders = Physics.OverlapSphere(impactPoint, splashRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;

                var enemy = colliders[i].GetComponentInParent<EnemyMovement>();
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    enemy.BlowUp();
                    killCount++;
                    continue;
                }

                var asteroid = colliders[i].GetComponentInParent<Asteroid>();
                if (asteroid != null && asteroid.gameObject.activeSelf)
                {
                    var exp = asteroid.GetComponent<Explosion>();
                    if (exp != null) exp.BlowUp();
                    else asteroid.Recycle();
                }
            }

            // 2. Gửi tín hiệu tính điểm Combo cấp số cộng: TotalHits = N + (N - 1)
            if (killCount > 0 && ComboScoringEngine.Instance != null)
            {
                ComboScoringEngine.Instance.RegisterSplashKill(killCount);
            }

            // 3. Tạo hiệu ứng nổ qua VFXPool (0 GC)
            var vfxPool = VirtualRailVFXPool.Instance;
            if (vfxPool != null && explosionPrefab != null)
            {
                vfxPool.SpawnVFX(explosionPrefab, impactPoint, Quaternion.identity, null, 3f);
            }
            else if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, impactPoint, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
