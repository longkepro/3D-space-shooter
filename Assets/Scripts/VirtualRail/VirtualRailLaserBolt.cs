using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Quản lý một đoạn đạn laser độc lập (Segmented Laser Bolt - Hướng 1).
    /// Đạn có chiều dài và độ dày nổi bật, phát sáng rực rỡ (Neon Glow + Point Light),
    /// và phát hiện va chạm liên tục (Continuous Collision Detection) tránh xuyên thấu mục tiêu.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class VirtualRailLaserBolt : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private Light _pointLight;
        private Vector3 _currentPos;
        private Vector3 _direction;
        private float _speed = 220f;
        private float _length = 9.0f;
        private float _distanceTraveled = 0f;
        private float _lifetime = 2.0f;
        private Transform _source;
        private LayerMask _hitMask;
        private float _collisionRadius = 1.0f;
        private Vector3 _inheritedVelocity;
        private int _damage = 20;

        private Color _cachedColor;
        private float _cachedWidth = -1f;
        private Gradient _gradient = new Gradient();

        public event Action<VirtualRailLaserBolt> OnDespawn;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();

            _pointLight = GetComponent<Light>();
            if (_pointLight == null) _pointLight = gameObject.AddComponent<Light>();
            _pointLight.type = LightType.Point;
            _pointLight.range = 16f;
            _pointLight.intensity = 3.0f;
            _pointLight.shadows = LightShadows.None;
            _pointLight.enabled = false;
        }

        private void ConfigureLineRenderer()
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.numCapVertices = 6;
            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.alignment = LineAlignment.View;
        }

        public void SetupVisual(Material mat, Color color, float width)
        {
            if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();
            if (mat != null && _lineRenderer.sharedMaterial != mat) _lineRenderer.sharedMaterial = mat;

            if (Mathf.Abs(_cachedWidth - width) > 0.001f)
            {
                _cachedWidth = width;
                _lineRenderer.startWidth = width;
                _lineRenderer.endWidth = width * 0.9f;
            }

            if (_cachedColor != color)
            {
                _cachedColor = color;
                _gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(color, 0.0f),
                        new GradientColorKey(color, 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(1.0f, 1.0f) // Sáng rõ 100% không bị mờ đuôi
                    }
                );
                _lineRenderer.colorGradient = _gradient;

                if (_pointLight != null)
                {
                    _pointLight.color = color;
                    _pointLight.range = Mathf.Max(14f, width * 18f);
                }
            }
        }

        public void Spawn(Vector3 startPos, Vector3 dir, float speed, float length, float maxLife, Transform source, LayerMask hitMask, float collisionRadius = 1.0f, Vector3 inheritedVelocity = default, int damage = 20)
        {
            _currentPos = startPos;
            _direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
            _speed = speed;
            _length = Mathf.Max(1.0f, length);
            _distanceTraveled = 0f;
            _lifetime = maxLife;
            _source = source;
            _hitMask = hitMask;
            _collisionRadius = collisionRadius > 0.05f ? collisionRadius : 1.0f;
            _inheritedVelocity = inheritedVelocity;
            _damage = damage;

            if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, startPos);
            _lineRenderer.enabled = true;

            if (_pointLight != null)
            {
                _pointLight.transform.position = startPos;
                _pointLight.enabled = true;
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 velocity = _direction * _speed + _inheritedVelocity;
            Vector3 displacement = velocity * dt;
            float stepDist = displacement.magnitude;
            Vector3 moveDir = stepDist > 0.0001f ? (displacement / stepDist) : _direction;
            Vector3 nextPos = _currentPos + displacement;

            // Continuous Collision Detection (SphereCast với bán kính hành lang _collisionRadius phủ kín khoảng hở)
            RaycastHit hit;
            if (Physics.SphereCast(_currentPos, _collisionRadius, moveDir, out hit, stepDist, _hitMask, QueryTriggerInteraction.Ignore))
            {
                // Bỏ qua nếu va chạm trúng chính tàu người chơi
                bool isSelf = _source != null && (hit.transform == _source || hit.transform.IsChildOf(_source) || hit.transform.root == _source.root || hit.transform.CompareTag("Player"));
                if (!isSelf)
                {
                    _currentPos = hit.point;
                    UpdatePositions(moveDir);
                    OnHit(hit);
                    return;
                }
            }

            _currentPos = nextPos;
            _distanceTraveled += stepDist;
            _lifetime -= dt;

            UpdatePositions(moveDir);

            // Tự động thu hồi khi hết thời gian sống hoặc bay vượt quá tầm bắn
            if (_lifetime <= 0f || _distanceTraveled >= 600f)
            {
                Deactivate();
            }
        }

        private void UpdatePositions(Vector3 moveDir)
        {
            // Đuôi đạn mọc dần từ nòng súng khi mới phóng ra, sau đó duy trì chiều dài cố định L
            float tailDist = Mathf.Min(_length, _distanceTraveled);
            Vector3 tailPos = _currentPos - moveDir * tailDist;

            _lineRenderer.SetPosition(0, _currentPos); // Head
            _lineRenderer.SetPosition(1, tailPos);    // Tail

            if (_pointLight != null)
            {
                _pointLight.transform.position = _currentPos;
            }
        }

        private void OnHit(RaycastHit hit)
        {
            // 0. Xử lý va chạm Lá chắn Năng lượng (Procedural Hex Shield)
            var hexShield = hit.transform.GetComponentInParent<VirtualRailHexShield>();
            if (hexShield != null && hexShield.IsActive)
            {
                hexShield.TakeDamage(_damage);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX2D(AudioSFXType.ShieldDamage, 0.85f);
                }
                Deactivate();
                return;
            }

            // 1. Thu thập vòng năng lượng / vật phẩm
            var ring = hit.transform.GetComponentInParent<CollectibleRing>();
            if (ring != null)
            {
                ring.Collect();
            }
            else if (hit.transform.CompareTag("Pickup"))
            {
                var pickup = hit.transform.GetComponentInParent<Pickup>();
                if (pickup != null) pickup.Collect();
            }
            else
            {
                // 2. Xử lý sát thương kẻ địch (ưu tiên EnemyHealth)
                var enemyHealth = hit.transform.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(_damage, hit.point, _direction);
                }
                else
                {
                    var enemy = hit.transform.GetComponentInParent<EnemyMovement>();
                    if (enemy != null) enemy.BlowUp();
                }

                // 3. Xử lý sát thương thiên thạch
                var asteroid = hit.transform.GetComponentInParent<Asteroid>();
                if (asteroid != null)
                {
                    asteroid.TakeDamage(_damage, hit.point);
                }
            }

            // Kích nổ / đẩy thiên thạch và vật thể vật lý
            var explosion = hit.transform.GetComponentInParent<Explosion>();
            if (explosion != null)
            {
                Transform src = _source != null ? _source : transform;
                explosion.AddForce(hit.point, src);
            }

            Deactivate();
        }

        public void Deactivate()
        {
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            if (_pointLight != null) _pointLight.enabled = false;
            gameObject.SetActive(false);
            OnDespawn?.Invoke(this);
        }
    }
}
