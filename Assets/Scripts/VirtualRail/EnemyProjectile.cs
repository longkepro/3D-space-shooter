using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Thực thể đạn của kẻ địch (Plasma Orb / Dart).
    /// Tuân thủ TDD v1.0.0 & TDD v2.0.0:
    /// - Quản lý vận tốc tương đối V_closing = ||V_bullet - V_player||.
    /// - Đồng bộ hóa với trục tiến đường ray (Local Track Synchronization).
    /// - Hitbox dung thứ bất đối xứng (30% - 35% kích thước thị giác).
    /// - Thuật toán bù trừ cự ly (Distance Scale Compensation).
    /// - Tương tác phản xạ khi người chơi thực hiện Barrel Roll Deflection (bẻ lệch 90 độ).
    /// - Hỗ trợ cờ HasGrazed cho cơ chế Graze / Near-Miss (TDD v2.0.0 Phần III.2).
    /// - Tự động thu hồi qua Object Pool (0 GC Alloc).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyProjectile : MonoBehaviour, IPoolableEntity
    {
        [Header("Projectile Settings")]
        public float damage = 1f;
        public float maxLifetime = 5.0f;
        public float visualBaseRadius = 1.2f;
        public float hitboxRadius = 0.45f; // ~35% của visual radius

        [Header("Visual Components")]
        public Transform visualTransform;
        public TrailRenderer trailRenderer;
        public SphereCollider sphereCollider;
        public ParticleSystem sparkParticles;

        [Header("Impact Effects (0 GC Allocation Pool)")]
        [SerializeField] private GameObject impactVFXPrefab;

        /// <summary>
        /// Cờ đánh dấu viên đạn đã kích hoạt Graze với thân tàu người chơi hay chưa (TDD v2.0.0 Phần III.2).
        /// Reset mỗi khi viên đạn được tái sinh từ Pool.
        /// </summary>
        public bool HasGrazed { get; set; }

        private Vector3 _velocity;
        private float _spawnTime;
        private bool _isDeflected = false;
        private Transform _playerTransform;
        private VirtualRailAnchor _anchor;

        private void Awake()
        {
            if (sphereCollider == null)
            {
                sphereCollider = GetComponent<SphereCollider>();
                if (sphereCollider == null) sphereCollider = gameObject.AddComponent<SphereCollider>();
            }
            sphereCollider.isTrigger = true;
            sphereCollider.radius = hitboxRadius;

            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
            else if (visualTransform == null)
            {
                visualTransform = transform;
            }

            if (trailRenderer == null)
            {
                trailRenderer = GetComponentInChildren<TrailRenderer>();
            }

            if (sparkParticles == null)
            {
                sparkParticles = GetComponentInChildren<ParticleSystem>();
            }
        }

        public void Initialize(Vector3 startPosition, Vector3 direction, float speed, bool isHeadOn, VirtualRailAnchor anchor)
        {
            _anchor = anchor != null ? anchor : VirtualRailAnchor.Instance;
            _playerTransform = VirtualRailAnchor.PlayerShipTransform != null
                ? VirtualRailAnchor.PlayerShipTransform
                : VirtualRailAnchor.PlayerTransform;

            transform.position = startPosition;
            _isDeflected = false;
            HasGrazed = false;
            _spawnTime = Time.time;

            if (sphereCollider != null)
            {
                sphereCollider.enabled = true;
                sphereCollider.radius = hitboxRadius;
            }

            // Đồng bộ hóa với trục đường ray:
            // V_bullet_world = V_rail_tangent * V_rail + V_bullet_local
            Vector3 railVelocity = (_anchor != null) ? _anchor.ForwardTangent * _anchor.CurrentSpeed : Vector3.zero;
            Vector3 localVelocity = direction.normalized * speed;

            _velocity = localVelocity + (isHeadOn ? Vector3.zero : railVelocity);
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }

            if (sparkParticles != null)
            {
                sparkParticles.Clear();
                sparkParticles.Play();
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += _velocity * dt;

            // Bù trừ cự ly cho kích thước thị giác (Distance Scale Compensation):
            // Scale_visual = Clamp(1.0 + (Distance / D_ref) * K_scale, 1.0, 3.5)
            if (visualTransform != null && _playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                float scale = Mathf.Clamp(1.0f + (dist / 120f) * 1.2f, 1.0f, 3.5f);
                visualTransform.localScale = Vector3.one * (visualBaseRadius * scale);
            }

            // Kiểm tra vòng đời hoặc trôi quá xa về sau lưng tàu
            if (Time.time >= _spawnTime + maxLifetime)
            {
                Recycle();
                return;
            }

            if (_anchor != null && transform.position.z < _anchor.transform.position.z - 35f)
            {
                Recycle();
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDeflected) return;

            // 1. Nếu chạm vào vỏ bọc GrazeTrigger (R = 1.8m), KHÔNG gây sát thương, để GrazeTrigger xử lý tính điểm
            if (other.GetComponent<VirtualRailGrazeTrigger>() != null)
            {
                return;
            }

            // 2. Kiểm tra va chạm với Tàu người chơi (Inner Hurtbox)
            var ship = other.GetComponentInParent<VirtualRailShip>();
            if (ship != null)
            {
                if (ship.IsDeflecting)
                {
                    // ==================== BARREL ROLL DEFLECTION ====================
                    // Bẻ ngoặt vector hướng bay văng ra ngoài 90 độ, miễn nhiễm sát thương hoàn toàn!
                    Deflect(ship.transform.position);
                    return;
                }

                // Gây sát thương nếu không ở trạng thái lộn cánh phản xạ
                var shield = ship.GetComponentInChildren<Shield>();
                if (shield == null) shield = other.GetComponentInChildren<Shield>();
                if (shield != null)
                {
                    shield.TakeDamage(Mathf.RoundToInt(damage));
                }

                // Kích hoạt phản hồi sóng lục giác trên tấm khiên nếu có
                var hexShield = ship.GetComponentInChildren<VirtualRailHexShield>();
                if (hexShield != null)
                {
                    hexShield.TriggerHitFlash();
                }

                TriggerImpactFX(transform.position);
                Recycle();
                return;
            }

            // Nếu chạm bom hoặc khiên năng lượng khác
            if (other.CompareTag("Player"))
            {
                var shield = other.GetComponent<Shield>();
                if (shield != null)
                {
                    shield.TakeDamage(Mathf.RoundToInt(damage));
                }

                var hexShield = other.GetComponentInChildren<VirtualRailHexShield>();
                if (hexShield != null)
                {
                    hexShield.TriggerHitFlash();
                }

                TriggerImpactFX(transform.position);
                Recycle();
            }
        }

        /// <summary>
        /// Bẻ lệch đạn 90 độ văng ra ngoài khi người chơi thực hiện Barrel Roll Deflection.
        /// </summary>
        public void Deflect(Vector3 shipPosition)
        {
            _isDeflected = true;

            // Bẻ hướng vuông góc 90 độ văng ra xa thân tàu
            Vector3 awayDir = (transform.position - shipPosition).normalized;
            Vector3 deflectDir = Vector3.Cross(_velocity.normalized, Vector3.up);
            if (Vector3.Dot(deflectDir, awayDir) < 0f) deflectDir = -deflectDir;

            deflectDir = (deflectDir + awayDir * 0.6f).normalized;
            _velocity = deflectDir * (Mathf.Max(_velocity.magnitude, 35f) * 1.6f);
            transform.rotation = Quaternion.LookRotation(_velocity);

            if (sphereCollider != null)
            {
                sphereCollider.enabled = false; // Đạn đã bị hất văng, không gây sát thương nữa
            }

            // Tự thu hồi sau 0.8s khi đã văng ra ngoài
            CancelInvoke(nameof(Recycle));
            Invoke(nameof(Recycle), 0.8f);
        }

        public void DestroyByBomb()
        {
            TriggerImpactFX(transform.position);
            Recycle();
        }

        public void TriggerImpactFX(Vector3 pos)
        {
            if (impactVFXPrefab != null && VirtualRailVFXPool.Instance != null)
            {
                VirtualRailVFXPool.Instance.SpawnVFX(impactVFXPrefab, pos, Quaternion.identity, null, 1.2f);
            }
        }

        public void SetImpactVFXPrefab(GameObject vfxPrefab)
        {
            impactVFXPrefab = vfxPrefab;
        }

        public void Recycle()
        {
            CancelInvoke(nameof(Recycle));
            if (sparkParticles != null)
            {
                sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
            gameObject.SetActive(false);
        }
    }
}
