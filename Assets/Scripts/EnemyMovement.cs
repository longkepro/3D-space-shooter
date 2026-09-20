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

    [Header("Archetype Integration")]
    [SerializeField] private VirtualRail.EnemyData _archetypeData;

    private Transform _target;
    private bool _isBlowingUp = false;
    private float _nextAvoidanceTime = 0f;
    private Vector3 _cachedAvoidanceOffset = Vector3.zero;
    private Explosion _cachedExplosion;
    private VirtualRail.EnemyHealth _health;
    private bool _isFlankerStrafing = false;
    private float _flankerStrafeDir = 1f;

    public Action<EnemyMovement> OnRecycle;

    public VirtualRail.EnemyHealth Health => _health;
    public VirtualRail.EnemyData ArchetypeData => _archetypeData;

    private void Awake()
    {
        _cachedExplosion = GetComponent<Explosion>();
        _health = GetComponent<VirtualRail.EnemyHealth>();
    }

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

    public void ApplyArchetype(VirtualRail.EnemyData data)
    {
        _archetypeData = data;
        if (data == null) return;

        _movementSpeed = data.movementSpeed;
        _turnSpeed = data.turnSpeed;
        _points = data.scoreValue;

        if (_health == null) _health = GetComponent<VirtualRail.EnemyHealth>();
        if (_health != null)
        {
            _health.ApplyData(data);
        }

        var attack = GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.ApplyData(data);
        }

        // Cấu hình hành vi tạt sườn Flanker Strafing
        if (data.archetype == VirtualRail.EnemyArchetype.Flanker && data.flankerTactic == VirtualRail.FlankerTactic.StrafingRun)
        {
            _isFlankerStrafing = true;
            _flankerStrafeDir = (transform.position.x < 0f) ? 1f : -1f;
        }
        else
        {
            _isFlankerStrafing = false;
        }
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
        _isFlankerStrafing = false;
        if (_health != null)
        {
            _health.ResetState();
        }
    }

    public bool TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_health != null)
        {
            return _health.TakeDamage(damage, hitPoint, hitDirection);
        }
        BlowUp();
        return true;
    }

    private void Update()
    {
        if (!TargetPlayer()) return;

        if (!_isFlankerStrafing)
        {
            Pathfinding();
        }
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
        var anchor = VirtualRail.VirtualRailAnchor.Instance;

        // Dạng 1: Tạt sườn quấy rối (Strafing Run) - lao ngang cắt mặt người chơi ở tốc độ cao
        if (_isFlankerStrafing)
        {
            Vector3 strafeRight = (anchor != null) ? anchor.transform.right : transform.right;
            Vector3 railTangent = (anchor != null) ? anchor.ForwardTangent : Vector3.forward;
            Vector3 strafeVelocity = strafeRight * (_flankerStrafeDir * _movementSpeed * 1.4f) + railTangent * (_movementSpeed * 0.3f);

            transform.position += strafeVelocity * Time.deltaTime;
            Quaternion targetRot = Quaternion.LookRotation(strafeVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
            return;
        }

        bool useSync = anchor != null && anchor.CurrentSpeed > 0.1f && (anchor.config == null || anchor.config.enableEnemyRailSync);

        if (useSync)
        {
            Vector3 railFwd = anchor.ForwardTangent;
            Vector3 toEnemy = transform.position - anchor.transform.position;
            float distAlongRail = Vector3.Dot(toEnemy, railFwd);

            float holdDist = (anchor.config != null) ? anchor.config.enemyHoldDistance : 60f;
            float targetClosingSpeed = (anchor.config != null) ? anchor.config.enemyClosingSpeed : 20f;
            float railSpeed = anchor.CurrentSpeed;

            float closingSpeed = 0f;
            if (distAlongRail > holdDist)
            {
                // Giảm tốc mượt mà khi tiến gần cự ly ghìm
                closingSpeed = Mathf.Min(targetClosingSpeed, (distAlongRail - holdDist) * 1.5f);
            }
            else if (distAlongRail > 0f)
            {
                // Khóa đồng tốc với ray khi ở trong cự ly ghìm phía trước mũi tàu (closingSpeed = 0)
                closingSpeed = 0f;
            }
            else
            {
                // Đã bị vượt qua phía sau: giữ vận tốc để người chơi bỏ lại phía sau và despawn
                closingSpeed = targetClosingSpeed;
            }

            float forwardSpeed = railSpeed - closingSpeed;
            Vector3 forwardDelta = railFwd * (forwardSpeed * Time.deltaTime);

            // Bóc tách vận tốc lái né ngang (lateral steering) vuông góc với ray (0 GC Alloc)
            Vector3 steerVelocity = transform.forward * _movementSpeed;
            Vector3 lateralVelocity = steerVelocity - railFwd * Vector3.Dot(steerVelocity, railFwd);
            Vector3 lateralDelta = lateralVelocity * Time.deltaTime;

            transform.position += forwardDelta + lateralDelta;
        }
        else
        {
            transform.position += transform.forward * _movementSpeed * Time.deltaTime;
        }
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
            VirtualRail.ComboScoringEngine.Instance?.RegisterSingleKill(_points);
            var explosion = _cachedExplosion != null ? _cachedExplosion : GetComponent<Explosion>();
            if (explosion != null)
            {
                explosion.BlowUp();
            }
            else
            {
                // Fallback: Phát hiệu ứng vụ nổ từ VFXPool nếu prefab chưa gắn Explosion component
                var vfxPool = VirtualRail.VirtualRailVFXPool.Instance;
                if (vfxPool != null)
                {
#if UNITY_EDITOR
                    var expPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/BigExplosion.prefab");
                    if (expPrefab != null)
                    {
                        vfxPool.SpawnVFX(expPrefab, transform.position, Quaternion.identity, null, 3f);
                    }
#endif
                }
                SelfDestruct();
            }
        }
    }
}
