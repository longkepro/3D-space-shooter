using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private Laser[] _lasers;
    [SerializeField] private VirtualRail.VirtualRailMuzzle[] _muzzles;

    private Transform _target;
    private Vector3 _hitPosition;

    [Header("Projectile Fire (TDD v1.0.0 Phần I.3)")]
    [SerializeField] private bool _usePhysicalProjectiles = true;
    [SerializeField] private float _fireCooldown = 1.2f;
    [SerializeField] private int _burstCount = 1;
    [SerializeField] private float _burstInterval = 0.15f;
    [SerializeField] private float _bulletSpeedOverride = 0f;

    private float _nextFireTime = 0f;
    private int _burstRemaining = 0;
    private float _nextBurstTime = 0f;
    private float _nextFindTime = 0f;

    private float _baseFireCooldown = 1.2f;
    private float _cooldownScale = 1.0f;
    private const float MIN_FIRE_COOLDOWN_CAP = 0.60f; // Trần hãm an toàn: giữa 2 loạt đạn luôn có khoảng trống tối thiểu 0.6s

    private void OnEnable()
    {
        _nextFindTime = 0f;
    }

    private void Awake()
    {
        EnsureMuzzles();
    }

    private void EnsureMuzzles()
    {
        if (_muzzles == null || _muzzles.Length == 0)
        {
            _muzzles = GetComponentsInChildren<VirtualRail.VirtualRailMuzzle>(true);
        }
        if ((_muzzles == null || _muzzles.Length == 0) && (_lasers == null || _lasers.Length == 0))
        {
            _lasers = GetComponentsInChildren<Laser>(true);
        }
    }

    public void ApplyData(VirtualRail.EnemyData data)
    {
        if (data == null) return;
        _baseFireCooldown = data.fireCooldown;
        _fireCooldown = Mathf.Max(MIN_FIRE_COOLDOWN_CAP, _baseFireCooldown * _cooldownScale);
        _burstCount = Mathf.Max(1, data.burstCount);
        _burstInterval = Mathf.Max(0.05f, data.burstInterval);
        _bulletSpeedOverride = data.projectileSpeed;
        _burstRemaining = 0;
    }

    /// <summary>
    /// Điều chỉnh hệ số giãn cách bắn đạn thích ứng theo DDA Threat Level (0 GC Alloc).
    /// Giảm thời gian chờ giữa các loạt bắn để tạo mật độ đạn dày đặc hơn, có trần hãm an toàn tối thiểu 0.6s.
    /// </summary>
    public void SetFireCooldownScale(float scale)
    {
        _cooldownScale = Mathf.Clamp(scale, 0.45f, 1.0f);
        _fireCooldown = Mathf.Max(MIN_FIRE_COOLDOWN_CAP, _baseFireCooldown * _cooldownScale);
    }

    public void SetBulletSpeedOverride(float speed)
    {
        _bulletSpeedOverride = speed;
    }

    private void Update()
    {
        if (!TargetPlayer()) return;

        // Xử lý các phát bắn liên tiếp trong chuỗi Burst Fire (0 GC Alloc)
        if (_burstRemaining > 0 && Time.time >= _nextBurstTime)
        {
            ExecuteDischarge();
            _burstRemaining--;
            _nextBurstTime = Time.time + _burstInterval;
        }

        if (_burstRemaining <= 0 && TargetInfront() && HaveRaycastLineOfSight())
        {
            FireWeapons();
        }
    }

    private bool TargetInfront()
    {
        if (_target == null) return false;
        Vector3 toTarget = _target.position - transform.position;
        return Vector3.Dot(transform.forward, toTarget) > 0f;
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player")) return true;
        Transform t = col.transform;
        if (t.root != null && t.root.CompareTag("Player")) return true;
        if (t.GetComponentInParent<VirtualRail.VirtualRailShip>() != null) return true;
        return false;
    }

    private bool HaveRaycastLineOfSight()
    {
        if (_target == null) return false;
        Vector3 toTarget = _target.position - transform.position;
        float distSq = toTarget.sqrMagnitude;
        float maxRange = 260f;
        if (distSq > maxRange * maxRange) return false;

        // 1. Nếu có muzzles riêng
        if (_muzzles != null && _muzzles.Length > 0)
        {
            for (int i = 0; i < _muzzles.Length; i++)
            {
                if (_muzzles[i] == null) continue;
                Vector3 origin = _muzzles[i].Position;
                Vector3 aimDir = (_target.position - origin).normalized;
                if (Physics.Raycast(origin, aimDir, out RaycastHit hit, maxRange))
                {
                    if (IsPlayerCollider(hit.collider) || hit.collider.CompareTag("Enemy") || hit.collider.transform.root == transform.root)
                    {
                        _hitPosition = hit.point;
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        // 2. Fallback lấy vị trí từ _lasers
        if (_lasers != null && _lasers.Length > 0)
        {
            for (int i = 0; i < _lasers.Length; i++)
            {
                if (_lasers[i] == null) continue;
                Vector3 origin = _lasers[i].transform.position;
                Vector3 aimDir = (_target.position - origin).normalized;
                if (Physics.Raycast(origin, aimDir, out RaycastHit hit, maxRange))
                {
                    if (IsPlayerCollider(hit.collider) || hit.collider.CompareTag("Enemy") || hit.collider.transform.root == transform.root)
                    {
                        _hitPosition = hit.point;
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        // 3. Fallback bắn từ tâm tàu nếu chưa gán muzzles/lasers
        Vector3 shipAimDir = toTarget.normalized;
        if (Physics.Raycast(transform.position, shipAimDir, out RaycastHit centerHit, maxRange))
        {
            return IsPlayerCollider(centerHit.collider) || centerHit.collider.CompareTag("Enemy") || centerHit.collider.transform.root == transform.root;
        }
        return true;
    }

    private void FireWeapons()
    {
        if (Time.time < _nextFireTime) return;
        _nextFireTime = Time.time + _fireCooldown;

        // Bắn phát đầu tiên và lên lịch các phát tiếp theo trong loạt burst
        ExecuteDischarge();
        if (_burstCount > 1)
        {
            _burstRemaining = _burstCount - 1;
            _nextBurstTime = Time.time + _burstInterval;
        }
    }

    private void ExecuteDischarge()
    {
        if (_target == null) return;
        if (VirtualRail.EnemyProjectilePool.Instance == null) return;

        Vector3 toPlayer = _target.position - transform.position;
        bool isHeadOn = toPlayer.z < 0f;

        float bulletSpeed = (_bulletSpeedOverride > 5f)
            ? _bulletSpeedOverride
            : (isHeadOn ? 20f : 75f);

        // 1. Ưu tiên bắn từ VirtualRailMuzzle nếu có
        if (_muzzles != null && _muzzles.Length > 0)
        {
            for (int i = 0; i < _muzzles.Length; i++)
            {
                if (_muzzles[i] == null) continue;
                Vector3 muzzlePos = _muzzles[i].Position;
                Vector3 fireDir = (_target.position - muzzlePos).normalized;
                VirtualRail.EnemyProjectilePool.Instance.SpawnProjectile(muzzlePos, fireDir, bulletSpeed, isHeadOn);
                _muzzles[i].PlayFireEffects();
            }
            return;
        }

        // 2. Fallback lấy vị trí từ _lasers (chỉ mượn transform.position, tuyệt đối KHÔNG gọi FireLaser)
        if (_lasers != null)
        {
            for (int i = 0; i < _lasers.Length; i++)
            {
                if (_lasers[i] == null) continue;
                Vector3 muzzlePos = _lasers[i].transform.position;
                Vector3 fireDir = (_target.position - muzzlePos).normalized;
                VirtualRail.EnemyProjectilePool.Instance.SpawnProjectile(muzzlePos, fireDir, bulletSpeed, isHeadOn);
            }
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            _target = VirtualRail.VirtualRailAnchor.PlayerShipTransform != null
                ? VirtualRail.VirtualRailAnchor.PlayerShipTransform
                : VirtualRail.VirtualRailAnchor.PlayerTransform;

            if (_target == null && Time.time >= _nextFindTime)
            {
                _nextFindTime = Time.time + 1.0f;
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }
        return _target != null;
    }

}
