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

    private void FireLaser()
    {
        foreach (var laser in _lasers)
        {
            laser.FireLaser(_hitPosition, _target);
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
        }
        var foundTarget = (_target != null);
        return foundTarget;
    }

}
