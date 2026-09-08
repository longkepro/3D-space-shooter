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
            if (target != null && target.CompareTag("Enemy"))
            {
                var enemy = target.GetComponent<EnemyMovement>();
                if (enemy != null) enemy.BlowUp();
            }

            // Tạo hiệu ứng nổ nếu có
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
