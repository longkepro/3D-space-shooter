using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Vòng đời trạng thái bốn pha của thực thể không gian (Four-Phase State Machine) theo chuẩn TDD v1.0.0 Phần II.4:
    /// 1. TELEGRAPH (0.4s - 0.8s): Báo hiệu sớm, âm thanh/vệt sáng ngoài màn hình, Collider TẮT.
    /// 2. APPROACH (1.0s - 1.5s): Lao vào hành lang bay, Collider BẬT, Hitbox trục Z kéo dài gấp 1.5 lần.
    /// 3. ACTION (2.0s - 3.0s): Đồng tốc tương đối với tàu, xả đạn, bộc lộ điểm yếu (cửa sổ vàng).
    /// 4. EXIT (1.0s): Peel-off lộn cánh 90 độ, bứt tốc thoát ly và tự động thu hồi về Object Pool.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyLifecycleFSM : MonoBehaviour, IPoolableEntity
    {
        public enum LifecyclePhase
        {
            Telegraph = 0,
            Approach = 1,
            Action = 2,
            Exit = 3
        }

        [Header("State Tracking")]
        [SerializeField] private LifecyclePhase _currentPhase = LifecyclePhase.Telegraph;
        [SerializeField] private float _phaseTimer = 0f;
        [SerializeField] private bool _isActiveFSM = true;

        [Header("Phase Durations")]
        [SerializeField] private float _telegraphDuration = 0.5f;
        [SerializeField] private float _approachDuration = 1.2f;
        [SerializeField] private float _actionDuration = 2.5f;
        [SerializeField] private float _exitDuration = 1.0f;

        private SpatialTrajectoryData _trajectory;
        private Collider _collider;
        private EnemyMovement _movement;
        private VirtualRailAnchor _anchor;
        private Vector3 _originalColliderSize = Vector3.one;
        private bool _isInitialized = false;

        public LifecyclePhase CurrentPhase => _currentPhase;
        public bool HasRearWeakSpot => _trajectory.hasRearWeakSpot;
        public float DamageMultiplier => _trajectory.damageMultiplier;

        private void Awake()
        {
            _collider = GetComponentInChildren<Collider>();
            _movement = GetComponent<EnemyMovement>();
            if (_collider != null && _collider is BoxCollider box)
            {
                _originalColliderSize = box.size;
            }
        }

        public void InitializeTrajectory(SpatialTrajectoryData trajectory, VirtualRailAnchor anchor)
        {
            _trajectory = trajectory;
            _anchor = anchor != null ? anchor : VirtualRailAnchor.Instance;
            _approachDuration = trajectory.approachDuration;
            _actionDuration = trajectory.actionDuration;
            _exitDuration = trajectory.exitDuration;
            _isActiveFSM = true;
            _isInitialized = true;

            // Đặt vị trí xuất phát
            transform.position = trajectory.spawnPosition;
            transform.rotation = trajectory.spawnRotation;

            // Bắt đầu từ pha 1: Telegraph
            EnterPhase(LifecyclePhase.Telegraph);
        }

        private void EnterPhase(LifecyclePhase nextPhase)
        {
            _currentPhase = nextPhase;
            _phaseTimer = 0f;

            switch (_currentPhase)
            {
                case LifecyclePhase.Telegraph:
                    // Collider TẮT trong pha Telegraph (chống đạn vô lý ngoài tầm nhìn)
                    if (_collider != null) _collider.enabled = false;
                    break;

                case LifecyclePhase.Approach:
                    // Collider BẬT khi đã tiến vào hành lang bay
                    if (_collider != null)
                    {
                        _collider.enabled = true;
                        // Mở rộng Hitbox trục Z gấp 1.5 lần chống hiện tượng đạn xuyên qua giữa 2 frame (TDD Phần II.3)
                        if (_collider is BoxCollider box)
                        {
                            box.size = new Vector3(_originalColliderSize.x, _originalColliderSize.y, _originalColliderSize.z * 1.5f);
                        }
                    }
                    break;

                case LifecyclePhase.Action:
                    // Phục hồi kích thước collider chuẩn
                    if (_collider != null && _collider is BoxCollider b)
                    {
                        b.size = _originalColliderSize;
                    }
                    break;

                case LifecyclePhase.Exit:
                    // Peel-off: Bẻ lái lộn cánh 90 độ vọt ra ngoài màn hình
                    transform.Rotate(0f, 0f, (UnityEngine.Random.value > 0.5f ? 90f : -90f));
                    break;
            }
        }

        private void Update()
        {
            if (!_isActiveFSM || !_isInitialized) return;

            _phaseTimer += Time.deltaTime;

            switch (_currentPhase)
            {
                case LifecyclePhase.Telegraph:
                    // Di chuyển mồi nhử hoặc chờ tín hiệu
                    transform.position += _trajectory.approachVelocity * (0.3f * Time.deltaTime);
                    if (_phaseTimer >= _telegraphDuration)
                    {
                        EnterPhase(LifecyclePhase.Approach);
                    }
                    break;

                case LifecyclePhase.Approach:
                    // Di chuyển lao vào hành lang bay
                    transform.position += _trajectory.approachVelocity * Time.deltaTime;
                    if (_phaseTimer >= _approachDuration)
                    {
                        EnterPhase(LifecyclePhase.Action);
                    }
                    break;

                case LifecyclePhase.Action:
                    // Đồng tốc tương đối với tàu người chơi theo trục Z đường ray
                    float forwardSpeed = (_anchor != null) ? _anchor.CurrentSpeed : 40f;
                    Vector3 forwardDir = (_anchor != null) ? _anchor.ForwardTangent : Vector3.forward;
                    transform.position += forwardDir * (forwardSpeed * Time.deltaTime);

                    // Dao động chiến đấu nhẹ để tạo cảm giác sống động
                    float wave = Mathf.Sin(_phaseTimer * 3f) * 1.5f;
                    Vector3 rightDir = (_anchor != null) ? _anchor.transform.right : Vector3.right;
                    transform.position += rightDir * (wave * Time.deltaTime);

                    if (_phaseTimer >= _actionDuration)
                    {
                        EnterPhase(LifecyclePhase.Exit);
                    }
                    break;

                case LifecyclePhase.Exit:
                    // Bứt tốc tối đa thoát ly ra khỏi khung hình
                    transform.position += _trajectory.exitVelocity * Time.deltaTime;
                    if (_phaseTimer >= _exitDuration)
                    {
                        Recycle();
                    }
                    break;
            }
        }

        public void Recycle()
        {
            _isActiveFSM = false;
            _isInitialized = false;
            if (_collider != null)
            {
                _collider.enabled = true;
                if (_collider is BoxCollider box) box.size = _originalColliderSize;
            }

            if (_movement != null)
            {
                _movement.Recycle();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
