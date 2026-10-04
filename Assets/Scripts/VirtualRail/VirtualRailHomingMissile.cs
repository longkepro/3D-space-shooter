using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Đạn tự dẫn đuổi theo mục tiêu bị khóa (Homing Missile).
    /// </summary>
    public class VirtualRailHomingMissile : MonoBehaviour
    {
        public Transform target;
        public float speed = 240f;
        public float turnRate = 420f; // độ / giây
        public float inheritedSpeed = 0f;
        public float lifeTime = 5f;
        public float detonationDistance = 2f;
        [Header("Splash AoE (TDD v1.0.0 Phần I.6)")]
        public float splashRadius = 10f;
        public GameObject explosionPrefab;

        /// <summary>
        /// Khởi tạo thông số động học chuẩn xác từ hệ thống vũ khí (0 GC Alloc).
        /// </summary>
        public void InitializeKinematics(float baseSpeed, float shipSpeed, float turnSpeed)
        {
            if (baseSpeed > 10f) speed = baseSpeed;
            if (turnSpeed > 10f) turnRate = turnSpeed;
            inheritedSpeed = Mathf.Max(0f, shipSpeed);
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);

            // Đảm bảo Collider và Rigidbody được thiết lập tin cậy 100%
            var col = GetComponentInChildren<Collider>();
            if (col != null) col.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (target != null)
            {
                Vector3 targetDir = (target.position - transform.position).normalized;
                Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, turnRate * Mathf.Deg2Rad * dt, 0.0f);
                if (newDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(newDir);
                }

                if (Vector3.Distance(transform.position, target.position) <= detonationDistance)
                {
                    Detonate();
                    return;
                }
            }

            float currentVelocity = speed + inheritedSpeed;
            transform.position += transform.forward * (currentVelocity * dt);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Bỏ qua thân tàu người chơi
            if (other.CompareTag("Player") || other.transform.root == transform.root) return;

            if (other.CompareTag("Enemy") || other.GetComponentInParent<EnemyHealth>() != null ||
                other.GetComponentInParent<EnemyMovement>() != null || other.GetComponentInParent<Asteroid>() != null ||
                (target != null && other.transform == target))
            {
                Detonate();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player")) return;
            Detonate();
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

                var eh = colliders[i].GetComponentInParent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    bool isDirect = (target != null && (colliders[i].transform == target || colliders[i].transform.IsChildOf(target)));
                    if (!isDirect && eh.IsShieldActive)
                    {
                        eh.TakeDamage(0, eh.transform.position, transform.forward);
                        continue;
                    }

                    int dmg = isDirect ? 100 : 50;
                    if (eh.TakeDamage(dmg, eh.transform.position, transform.forward))
                    {
                        killCount++;
                    }
                    continue;
                }

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
                    asteroid.TakeDamage(100, impactPoint);
                }
            }

            // 2. Gửi tín hiệu tính điểm Combo cấp số cộng: TotalHits = N + (N - 1)
            if (killCount > 0 && ComboScoringEngine.Instance != null)
            {
                ComboScoringEngine.Instance.RegisterSplashKill(killCount);
            }

            // 3. Tạo hiệu ứng nổ qua VFXPool (0 GC)
            var vfxPool = VirtualRailVFXPool.Instance;
            if (explosionPrefab == null)
            {
#if UNITY_EDITOR
                explosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/BigExplosion.prefab");
#endif
            }

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
