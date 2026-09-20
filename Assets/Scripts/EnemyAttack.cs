using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private Laser[] _lasers;

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

    public void ApplyData(VirtualRail.EnemyData data)
    {
        if (data == null) return;
        _fireCooldown = data.fireCooldown;
        _burstCount = Mathf.Max(1, data.burstCount);
        _burstInterval = Mathf.Max(0.05f, data.burstInterval);
        _bulletSpeedOverride = data.projectileSpeed;
        _burstRemaining = 0;
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
            FireLaser();
        }
    }

    private bool TargetInfront()
    {
        if (_target == null) return false;
        Vector3 toTarget = _target.position - transform.position;
        return Vector3.Dot(transform.forward, toTarget) > 0f;
    }

    private bool HaveRaycastLineOfSight()
    {
        Vector3 attackDirection = _target.position - transform.position;
        foreach (var laser in _lasers)
        {
            if (laser == null) continue;
            if (Physics.Raycast(laser.transform.position, attackDirection, out RaycastHit hit, laser.Distance))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    Debug.DrawRay(laser.transform.position, attackDirection, Color.green);
                    _hitPosition = hit.transform.position;
                    return true;
                }
            }
        }
        return false;
    }

    private void FireLaser()
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

        bool usePhysical = _usePhysicalProjectiles;
        if (VirtualRail.VirtualRailAnchor.Instance != null && VirtualRail.VirtualRailAnchor.Instance.config != null)
        {
            usePhysical = _usePhysicalProjectiles && VirtualRail.VirtualRailAnchor.Instance.config.usePhysicalEnemyProjectiles;
        }

        if (usePhysical && VirtualRail.EnemyProjectilePool.Instance != null)
        {
            Vector3 toPlayer = _target.position - transform.position;
            bool isHeadOn = toPlayer.z < 0f;

            float bulletSpeed = (_bulletSpeedOverride > 5f)
                ? _bulletSpeedOverride
                : (isHeadOn ? 20f : 75f);

            foreach (var laser in _lasers)
            {
                if (laser == null) continue;
                Vector3 muzzlePos = laser.transform.position;
                Vector3 fireDir = (_target.position - muzzlePos).normalized;
                VirtualRail.EnemyProjectilePool.Instance.SpawnProjectile(muzzlePos, fireDir, bulletSpeed, isHeadOn);
            }
        }
        else
        {
            foreach (var laser in _lasers)
            {
                if (laser != null) laser.FireLaser(_hitPosition, _target);
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

            if (_target == null)
            {
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
