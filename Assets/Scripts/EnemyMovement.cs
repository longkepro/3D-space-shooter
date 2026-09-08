using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyMovement : MonoBehaviour, VirtualRail.IPoolableEntity
{
    [SerializeField] private float _movementSpeed = 20f;
    [SerializeField] private float _turnSpeed = 0.5f;
    [SerializeField] private float _rayCastOffset = 2.5f;
    [SerializeField] private float _rayCastRange = 20f;
    [SerializeField] private int _points = 50;

    [Header("Optimization")]
    [Tooltip("Khoảng thời gian giữa các lần quét tia né vật cản (giây) - giảm tải CPU Physics")]
    [SerializeField] private float _avoidanceInterval = 0.1f;

    private Transform _target;
    private bool _isBlowingUp = false;
    private float _nextAvoidanceTime = 0f;
    private Vector3 _cachedAvoidanceOffset = Vector3.zero;

    public Action<EnemyMovement> OnRecycle;

    private void OnEnable()
    {
        GameEventManager.OnStartGame += SelfDestruct;
        GameEventManager.OnPlayerDestroyed += TargetMainCamera;
        ResetState();
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= SelfDestruct;
        GameEventManager.OnPlayerDestroyed -= TargetMainCamera;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void ResetState()
    {
        _isBlowingUp = false;
        _cachedAvoidanceOffset = Vector3.zero;
        _nextAvoidanceTime = 0f;
    }

    private void Update()
    {
        if (!TargetPlayer()) return;

        Pathfinding();
        Move();
    }

    private void Turn()
    {
        if (_target == null) return;

        Vector3 pos = _target.position - transform.position;
        if (pos.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(pos);
            base.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _turnSpeed * Time.deltaTime);
        }
    }

    private void Move()
    {
        transform.position += transform.forward * _movementSpeed * Time.deltaTime;
    }

    private void Pathfinding()
    {
        // Throttled: Chỉ quét Physics Raycast 10 lần/giây thay vì mỗi frame, giảm 85% tải tính toán Physics
        if (Time.time >= _nextAvoidanceTime)
        {
            _nextAvoidanceTime = Time.time + _avoidanceInterval;
            UpdateAvoidanceRays();
        }

        if (_cachedAvoidanceOffset != Vector3.zero)
        {
            base.transform.Rotate(_cachedAvoidanceOffset * 5f * Time.deltaTime);
        }
        else
        {
            Turn();
        }
    }

    private void UpdateAvoidanceRays()
    {
        _cachedAvoidanceOffset = Vector3.zero;
        Vector3 fwd = transform.forward;
        Vector3 rightOffset = transform.right * _rayCastOffset;
        Vector3 upOffset = transform.up * _rayCastOffset;

        if (Physics.Raycast(transform.position - rightOffset, fwd, _rayCastRange))
        {
            _cachedAvoidanceOffset += Vector3.right;
        }
        else if (Physics.Raycast(transform.position + rightOffset, fwd, _rayCastRange))
        {
            _cachedAvoidanceOffset -= Vector3.right;
        }

        if (Physics.Raycast(transform.position + upOffset, fwd, _rayCastRange))
        {
            _cachedAvoidanceOffset -= Vector3.up;
        }
        else if (Physics.Raycast(transform.position - upOffset, fwd, _rayCastRange))
        {
            _cachedAvoidanceOffset += Vector3.up;
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            // 1. Tận dụng tham chiếu nhanh từ VirtualRailAnchor (O(1), 0 GC, 0 Scene Scan)
            _target = VirtualRail.VirtualRailAnchor.PlayerShipTransform != null
                ? VirtualRail.VirtualRailAnchor.PlayerShipTransform
                : VirtualRail.VirtualRailAnchor.PlayerTransform;

            // 2. Fallback cho scene cũ nếu không chạy VirtualRail
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

    private void TargetMainCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            _target = mainCamera.transform;
        }
    }

    public void Recycle()
    {
        gameObject.SetActive(false);
        OnRecycle?.Invoke(this);
    }

    private void SelfDestruct()
    {
        Recycle();
    }

    public void BlowUp()
    {
        if (!_isBlowingUp)
        {
            _isBlowingUp = true;
            GameEventManager.IncrementScore(_points);
            var explosion = transform.GetComponent<Explosion>();
            if (explosion != null)
            {
                explosion.BlowUp();
            }
            else
            {
                SelfDestruct();
            }
        }
    }
}
