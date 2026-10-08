using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Đạn tự dẫn đuổi theo mục tiêu bị khóa (Homing Missile / Charge Shot Projectile).
    /// Chuẩn Star Fox 64 Arcade: Gia tốc phóng vọt (Ejection), quỹ đạo xoắn ốc uốn lượn (Corkscrew),
    /// dẫn đường đón đầu (Lead Pursuit), chống xuyên thấu (Anti-Tunneling), 0 GC Alloc.
    /// </summary>
    public class VirtualRailHomingMissile : MonoBehaviour
    {
        public Transform target;
        public float speed = 240f;
        public float turnRate = 420f; // độ / giây
        public float inheritedSpeed = 0f;
        public float lifeTime = 5f;
        public float detonationDistance = 2.5f;

        [Header("Splash AoE (TDD v1.0.0 Phần I.6)")]
        public float splashRadius = 10f;
        public GameObject explosionPrefab;

        [Header("Smart Bomb Mode")]
        public bool isSmartBomb = false;

        private static readonly Collider[] _cachedOverlapColliders = new Collider[64];
        private static readonly RaycastHit[] _cachedSweepHits = new RaycastHit[16];

        private VirtualRailConfig _cachedConfig;
        private TrailRenderer _trail;
        private float _age = 0f;
        private Vector3 _lastKnownPosition;
        private bool _hasTarget = false;
        private bool _isDetonated = false;
        private Vector3 _prevTargetPos;
        private bool _hasPrevTargetPos = false;
        private float _corkscrewSeed = 0f;

        /// <summary>
        /// Khởi tạo thông số động học chuẩn xác từ hệ thống vũ khí (0 GC Alloc).
        /// </summary>
        public void InitializeKinematics(float baseSpeed, float shipSpeed, float turnSpeed, VirtualRailConfig cfg = null, Transform lockedTarget = null)
        {
            if (baseSpeed > 10f) speed = baseSpeed;
            if (turnSpeed > 10f) turnRate = turnSpeed;
            inheritedSpeed = Mathf.Max(0f, shipSpeed);
            _cachedConfig = cfg;
            target = lockedTarget;
            _hasTarget = (target != null);

            if (_hasTarget)
            {
                _lastKnownPosition = target.position;
                _prevTargetPos = target.position;
                _hasPrevTargetPos = true;
            }
            else
            {
                _hasPrevTargetPos = false;
                _lastKnownPosition = transform.position + transform.forward * 150f;
            }

            _age = 0f;
            _isDetonated = false;
            _corkscrewSeed = Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// Tái kích hoạt đạn khi lấy ra từ Object Pool.
        /// </summary>
        public void OnSpawnFromPool()
        {
            _isDetonated = false;
            _age = 0f;
            _hasPrevTargetPos = false;

            if (_trail == null) _trail = GetComponentInChildren<TrailRenderer>(true);
            if (_trail != null)
            {
                _trail.Clear();
                _trail.enabled = true;
            }

            var col = GetComponentInChildren<Collider>();
            if (col != null) col.enabled = true;
        }

        /// <summary>
        /// Kích nổ chủ động tức thì giữa không trung (Manual Air Detonation khi người chơi ấn phím B lần 2).
        /// </summary>
        public void ManualDetonate()
        {
            if (_isDetonated || !gameObject.activeSelf) return;
            Detonate();
        }

        /// <summary>
        /// Thu hồi đạn về Object Pool (0 GC Alloc).
        /// </summary>
        public void Recycle()
        {
            _isDetonated = true;
            if (_trail != null)
            {
                _trail.Clear();
            }
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (_trail == null) _trail = GetComponentInChildren<TrailRenderer>(true);

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
            if (_isDetonated) return;

            float dt = Time.deltaTime;
            _age += dt;

            // Tự kích nổ khi hết thời gian sống
            if (_age >= lifeTime)
            {
                Detonate();
                return;
            }

            // 1. Gia tốc phóng vọt thoát nòng (Ejection Acceleration Curve)
            float targetVelocity = speed + inheritedSpeed;
            float currentVelocity;
            if (_cachedConfig != null && _cachedConfig.enableMissileAcceleration && _age < 0.15f)
            {
                float t = _age / 0.15f;
                currentVelocity = Mathf.Lerp(targetVelocity * 0.45f, targetVelocity, t * t);
            }
            else
            {
                currentVelocity = targetVelocity;
            }

            // 2. Dẫn đường đuổi mục tiêu & Đón đầu (Lead Pursuit)
            Vector3 aimTargetPos;
            bool targetValid = false;

            if (target != null && target.gameObject.activeInHierarchy)
            {
                var eh = target.GetComponentInParent<EnemyHealth>();
                if (eh == null || !eh.IsDead)
                {
                    targetValid = true;
                    _lastKnownPosition = target.position;

                    // Đo đạc vận tốc mục tiêu không phụ thuộc Rigidbody (Universal Velocity Tracking)
                    Vector3 targetVel = Vector3.zero;
                    if (_hasPrevTargetPos && dt > 0.0001f)
                    {
                        targetVel = Vector3.ClampMagnitude((target.position - _prevTargetPos) / dt, 100f);
                    }
                    _prevTargetPos = target.position;
                    _hasPrevTargetPos = true;

                    if (_cachedConfig != null && _cachedConfig.enableLeadPursuit)
                    {
                        float dist = Vector3.Distance(transform.position, target.position);
                        float timeToIntercept = (currentVelocity > 1f) ? (dist / currentVelocity) : 0f;
                        aimTargetPos = target.position + targetVel * Mathf.Min(timeToIntercept, 0.45f);
                    }
                    else
                    {
                        aimTargetPos = target.position;
                    }
                }
                else
                {
                    // Mục tiêu bị bắn chết giữa chừng -> Lao tới vị trí cuối cùng để nổ quét quái xung quanh
                    aimTargetPos = _lastKnownPosition;
                }
            }
            else if (_hasTarget)
            {
                // Mục tiêu bị xóa sổ khỏi Scene -> Tiếp tục bay tới tọa độ cuối cùng
                aimTargetPos = _lastKnownPosition;
            }
            else
            {
                // Chế độ Dumb-fire: bay thẳng theo hướng hội tụ
                aimTargetPos = transform.position + transform.forward * 80f;
            }

            // 3. Xoay hướng bẻ lái tự dẫn (Steering Kinematics)
            if (targetValid || _hasTarget)
            {
                Vector3 toTarget = (aimTargetPos - transform.position);
                float distToTarget = toTarget.magnitude;

                if (distToTarget > 0.1f)
                {
                    Vector3 targetDir = toTarget / distToTarget;
                    Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, turnRate * Mathf.Deg2Rad * dt, 0.0f);
                    if (newDir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(newDir);
                    }
                }

                // Kích nổ sớm khi đã áp sát cự ly sát thủ
                if (distToTarget <= detonationDistance)
                {
                    Detonate();
                    return;
                }
            }

            // 4. Quỹ đạo xoắn ốc uốn lượn (Corkscrew Wobble) chuẩn Star Fox 64 Arcade
            Vector3 forwardMove = transform.forward * (currentVelocity * dt);
            Vector3 corkscrewOffset = Vector3.zero;

            if (_cachedConfig != null && _cachedConfig.enableCorkscrewTrajectory && _cachedConfig.corkscrewAmplitude > 0.01f)
            {
                float distToAim = Vector3.Distance(transform.position, aimTargetPos);
                // Triệt tiêu biên độ xoắn về 0 khi cự ly < 12m để đâm chính xác vào tâm điểm
                float distDamp = Mathf.Clamp01((distToAim - 3f) / 9f);
                float amp = _cachedConfig.corkscrewAmplitude * distDamp;
                float freq = _cachedConfig.corkscrewFrequency > 1f ? _cachedConfig.corkscrewFrequency : 14f;
                float angle = _corkscrewSeed + _age * freq * Mathf.PI * 2f;

                corkscrewOffset = (transform.right * Mathf.Cos(angle) + transform.up * Mathf.Sin(angle)) * (amp * dt * freq);
            }

            Vector3 step = forwardMove + corkscrewOffset;
            float stepDist = step.magnitude;

            // 5. Chống xuyên thấu ở tốc độ cao (Anti-Tunneling Continuous Sweep - 0 GC Alloc)
            if (stepDist > 0.01f)
            {
                Vector3 stepDir = step / stepDist;
                int hitCount = Physics.SphereCastNonAlloc(transform.position, 0.65f, stepDir, _cachedSweepHits, stepDist, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hitCount; i++)
                {
                    var hit = _cachedSweepHits[i];
                    var col = hit.collider;
                    if (col == null) continue;

                    // Bỏ qua thân tàu người chơi
                    if (col.CompareTag("Player") || col.transform.root == transform.root) continue;

                    if (col.CompareTag("Enemy") || col.GetComponentInParent<EnemyHealth>() != null ||
                        col.GetComponentInParent<EnemyMovement>() != null || col.GetComponentInParent<Asteroid>() != null ||
                        (target != null && (col.transform == target || col.transform.IsChildOf(target))))
                    {
                        transform.position = hit.point;
                        Detonate();
                        return;
                    }
                }
            }

            transform.position += step;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDetonated) return;
            if (other.CompareTag("Player") || other.transform.root == transform.root) return;

            if (other.CompareTag("Enemy") || other.GetComponentInParent<EnemyHealth>() != null ||
                other.GetComponentInParent<EnemyMovement>() != null || other.GetComponentInParent<Asteroid>() != null ||
                (target != null && (other.transform == target || other.transform.IsChildOf(target))))
            {
                Detonate();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDetonated) return;
            if (collision.gameObject.CompareTag("Player")) return;
            Detonate();
        }

        private void Detonate()
        {
            if (_isDetonated) return;
            _isDetonated = true;

            Vector3 impactPoint = transform.position;
            int killCount = 0;

            // 0. Nếu là Smart Bomb: Xóa sạch 100% đạn địch trong màn chơi và áp dụng bán kính sóng xung kích cực đại
            if (isSmartBomb)
            {
                if (EnemyProjectilePool.Instance != null)
                {
                    EnemyProjectilePool.Instance.ClearAllActiveProjectiles();
                }

                if (_cachedConfig != null && _cachedConfig.smartBombShockwaveRadius > 10f)
                {
                    splashRadius = _cachedConfig.smartBombShockwaveRadius;
                }
                else if (splashRadius < 50f)
                {
                    splashRadius = 150f;
                }

                AudioManager.Instance?.PlaySFX2D(AudioSFXType.SmartBomb, 1.0f);
                VirtualRailCameraRig.TriggerShake(0.85f);
            }
            else
            {
                AudioManager.Instance?.PlaySFX3D(AudioSFXType.ChargeSplash, impactPoint, 0.9f);
                VirtualRailCameraRig.TriggerShake(0.35f);
            }

            // 1. Quét nổ lan hình cầu chuẩn Điều 4 (0 GC Alloc)
            LayerMask mask = (_cachedConfig != null && _cachedConfig.enemyLayer.value != 0) ? _cachedConfig.enemyLayer : ~0;
            int hitCount = Physics.OverlapSphereNonAlloc(impactPoint, splashRadius, _cachedOverlapColliders, mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                var col = _cachedOverlapColliders[i];
                if (col == null) continue;
                if (col.CompareTag("Player") || col.transform.root == transform.root) continue;

                var eh = col.GetComponentInParent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    bool isDirect = (target != null && (col.transform == target || col.transform.IsChildOf(target)));

                    // Kiểm tra khiên hộ thể Shieldbearer: nếu điểm nổ ở phía trước mặt quái (trong nón khiên)
                    if (eh.IsShieldActive && !isSmartBomb)
                    {
                        Vector3 toExplosion = (impactPoint - eh.transform.position).normalized;
                        bool hitShieldFront = Vector3.Dot(toExplosion, eh.transform.forward) > 0.1f;
                        if (hitShieldFront)
                        {
                            // Khiên cản hoàn toàn đòn đánh trực diện, kích hoạt phản hồi khiên lóe sáng
                            eh.TakeDamage(0, eh.transform.position, transform.forward);
                            continue;
                        }
                    }

                    int dmg = isSmartBomb ? 999 : (isDirect ? 100 : 50);
                    Vector3 hitDir = (eh.transform.position - impactPoint).normalized;
                    if (hitDir.sqrMagnitude < 0.001f) hitDir = transform.forward;
                    if (eh.TakeDamage(dmg, eh.transform.position, hitDir))
                    {
                        killCount++;
                    }
                    continue;
                }

                var enemy = col.GetComponentInParent<EnemyMovement>();
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    enemy.BlowUp();
                    killCount++;
                    continue;
                }

                var asteroid = col.GetComponentInParent<Asteroid>();
                if (asteroid != null && asteroid.gameObject.activeSelf)
                {
                    if (!isSmartBomb || asteroid.HazardType == AsteroidHazardType.Destructible)
                    {
                        asteroid.TakeDamage(isSmartBomb ? 999 : 100, impactPoint);
                    }
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

            // Thu hồi về Object Pool (0 GC Alloc, không dùng Destroy)
            Recycle();
        }
    }
}
