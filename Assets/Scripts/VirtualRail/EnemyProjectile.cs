using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Th?c th? d?n c?a k? d?ch (Plasma Orb / Dart).
    /// Tuân th? TDD v1.0.0 Ph?n I:
    /// - Qu?n lý v?n t?c tuong d?i V_closing = ||V_bullet - V_player||.
    /// - Ð?ng b? hóa v?i tr?c ti?n du?ng ray (Local Track Synchronization).
    /// - Hitbox dung th? b?t d?i x?ng (30% - 35% kích thu?c th? giác).
    /// - Thu?t toán bù tr? c? ly (Distance Scale Compensation).
    /// - Tuong tác ph?n x? khi ngu?i choi th?c hi?n Barrel Roll Deflection (b? l?ch 90 d?).
    /// - T? d?ng thu h?i qua Object Pool (0 GC Alloc).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyProjectile : MonoBehaviour, IPoolableEntity
    {
        [Header("Projectile Settings")]
        public float damage = 1f;
        public float maxLifetime = 5.0f;
        public float visualBaseRadius = 1.2f;
        public float hitboxRadius = 0.45f; // ~35% c?a visual radius

        [Header("Visual Components")]
        public Transform visualTransform;
        public TrailRenderer trailRenderer;
        public SphereCollider sphereCollider;

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
        }

        public void Initialize(Vector3 startPosition, Vector3 direction, float speed, bool isHeadOn, VirtualRailAnchor anchor)
        {
            _anchor = anchor != null ? anchor : VirtualRailAnchor.Instance;
            _playerTransform = VirtualRailAnchor.PlayerShipTransform != null
                ? VirtualRailAnchor.PlayerShipTransform
                : VirtualRailAnchor.PlayerTransform;

            transform.position = startPosition;
            _isDeflected = false;
            _spawnTime = Time.time;

            if (sphereCollider != null)
            {
                sphereCollider.enabled = true;
                sphereCollider.radius = hitboxRadius;
            }

            // Ð?ng b? hóa v?i tr?c du?ng ray:
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

            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += _velocity * dt;

            // Bù tr? c? ly cho kích thu?c th? giác (Distance Scale Compensation):
            // Scale_visual = Clamp(1.0 + (Distance / D_ref) * K_scale, 1.0, 3.5)
            if (visualTransform != null && _playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                float scale = Mathf.Clamp(1.0f + (dist / 120f) * 1.2f, 1.0f, 3.5f);
                visualTransform.localScale = Vector3.one * (visualBaseRadius * scale);
            }

            // Ki?m tra vòng d?i ho?c trôi quá xa v? sau lung tàu
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

            // Ki?m tra va ch?m v?i Tàu ngu?i choi
            var ship = other.GetComponentInParent<VirtualRailShip>();
            if (ship != null)
            {
                if (ship.IsDeflecting)
                {
                    // ==================== BARREL ROLL DEFLECTION ====================
                    // B? ngo?t vector hu?ng bay vang ra ngoài 90 d?, mi?n nhi?m sát thuong hoàn toàn!
                    Deflect(ship.transform.position);
                    return;
                }

                // Gây sát thuong n?u không ? tr?ng thái l?n cánh ph?n x?
                var shield = ship.GetComponentInChildren<Shield>();
                if (shield == null) shield = other.GetComponentInChildren<Shield>();
                if (shield != null)
                {
                    shield.TakeDamage(Mathf.RoundToInt(damage));
                }

                Recycle();
                return;
            }

            // N?u ch?m bom ho?c khiên nang lu?ng khác
            if (other.CompareTag("Player"))
            {
                var shield = other.GetComponent<Shield>();
                if (shield != null)
                {
                    shield.TakeDamage(Mathf.RoundToInt(damage));
                }
                Recycle();
            }
        }

        /// <summary>
        /// B? l?ch d?n 90 d? vang ra ngoài khi ngu?i choi th?c hi?n Barrel Roll Deflection.
        /// </summary>
        public void Deflect(Vector3 shipPosition)
        {
            _isDeflected = true;

            // B? hu?ng vuông góc 90 d? vang ra xa thân tàu
            Vector3 awayDir = (transform.position - shipPosition).normalized;
            Vector3 deflectDir = Vector3.Cross(_velocity.normalized, Vector3.up);
            if (Vector3.Dot(deflectDir, awayDir) < 0f) deflectDir = -deflectDir;

            deflectDir = (deflectDir + awayDir * 0.6f).normalized;
            _velocity = deflectDir * (Mathf.Max(_velocity.magnitude, 35f) * 1.6f);
            transform.rotation = Quaternion.LookRotation(_velocity);

            if (sphereCollider != null)
            {
                sphereCollider.enabled = false; // Ð?n dã b? h?t vang, không gây sát thuong n?a
            }

            // T? thu h?i sau 0.8s khi dã vang ra ngoài
            CancelInvoke(nameof(Recycle));
            Invoke(nameof(Recycle), 0.8f);
        }

        public void DestroyByBomb()
        {
            var vfxPool = VirtualRailVFXPool.Instance;
            if (vfxPool != null)
            {
                // Có th? sinh hi?u ?ng tia l?a nh? n?u c?n
            }
            Recycle();
        }

        public void Recycle()
        {
            CancelInvoke(nameof(Recycle));
            gameObject.SetActive(false);
        }
    }
}
