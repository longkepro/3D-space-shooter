using TMPro;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{


    [SerializeField] private Laser[] _lasers;

    private Transform _target;
    private Vector3 _hitPosition;

    private void Update()
    {
        if (!TargetPlayer()) return;

        if (TargetInfront() && HaveRaycastLineOfSight())
        {
            FireLaser();
        }
    }

    private bool TargetInfront()
    {
        Vector3 directionToTarget = transform.position - _target.position;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        if (Mathf.Abs(angle) > 90 && Mathf.Abs(angle) < 270)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    private bool HaveRaycastLineOfSight()
    {
        Vector3 attackDirection = _target.position - transform.position;
        foreach (var laser in _lasers)
        {
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

    [Header("Projectile Fire (TDD v1.0.0 Phần I.3)")]
    [SerializeField] private bool _usePhysicalProjectiles = true;
    [SerializeField] private float _fireCooldown = 1.2f;
    private float _nextFireTime = 0f;

    private void FireLaser()
    {
        bool usePhysical = _usePhysicalProjectiles;
        if (VirtualRail.VirtualRailAnchor.Instance != null && VirtualRail.VirtualRailAnchor.Instance.config != null)
        {
            usePhysical = _usePhysicalProjectiles && VirtualRail.VirtualRailAnchor.Instance.config.usePhysicalEnemyProjectiles;
        }

        if (usePhysical && VirtualRail.EnemyProjectilePool.Instance != null)
        {
            if (Time.time < _nextFireTime) return;
            _nextFireTime = Time.time + _fireCooldown;

            Vector3 toPlayer = _target.position - transform.position;
            // Nếu người chơi đang bay tới đối đầu theo trục Z
            bool isHeadOn = toPlayer.z < 0f;

            // Toán học vận tốc tương đối TDD:
            // - Head-on: V_bullet = 20 m/s (V_closing = 60 m/s, Reaction Window 1.5s)
            // - Rear-chaser: V_bullet = 75 m/s (bắt kịp tàu người chơi)
            float bulletSpeed = isHeadOn ? 20f : 75f;

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
