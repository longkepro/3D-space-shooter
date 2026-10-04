using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Thực thể thành viên phi đội (Wingman / Leader) theo chuẩn TDD v1.0.0 Phần III:
    /// - Pha 1: Ráp nối tự do bằng đường cong Cubic Bézier 3D (1.0s - 1.5s).
    /// - Pha 2: Khóa cứng đồng bộ theo ma trận Slot của VirtualFormationAnchor.
    /// - Phân rã hoảng loạn (Panic Scatter): Khi chỉ huy (Slot 0) bị bắn hạ,
    ///   nhận vector ly tâm, lộn vòng Barrel Roll và thoát ly khẩn cấp ra 2 biên màn hình.
    /// </summary>
    [DisallowMultipleComponent]
    public class FormationWingman : MonoBehaviour, IPoolableEntity
    {
        public enum WingmanState
        {
            Assembling,   // Đang bay theo đường cong Bézier nhập đội
            LockedInSlot, // Đã khóa cứng đồng điệu theo Slot của Anchor
            PanicScatter, // Hoảng loạn phân rã khi mất chỉ huy
            FreeCombat    // Tự do chiến đấu khi mất chỉ huy và mất khiên (không tự hủy)
        }

        [Header("Wingman Info")]
        [SerializeField] private int _slotIndex = 0;
        [SerializeField] private bool _isLeader = false;
        [SerializeField] private WingmanState _state = WingmanState.Assembling;

        private VirtualFormationAnchor _formationAnchor;
        private Vector3 _slotLocalOffset;
        private Vector3 _spawnPos;
        private Vector3 _ctrl1;
        private Vector3 _ctrl2;
        private float _assemblyTimer = 0f;
        private float _assemblyDuration = 1.2f;

        // Panic scatter state
        private Vector3 _panicVelocity;
        private float _panicTimer = 0f;
        private float _panicDuration = 1.5f;
        private float _panicRollDir = 1f;

        private EnemyMovement _movement;
        private Collider _collider;
        private EnemyHealth _health;

        public int SlotIndex => _slotIndex;
        public bool IsLeader => _isLeader;
        public WingmanState State => _state;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _collider = GetComponentInChildren<Collider>();
            _health = GetComponent<EnemyHealth>();
        }

        public void InitializeSlot(
            int slotIndex,
            bool isLeader,
            VirtualFormationAnchor anchor,
            Vector3 slotOffset,
            Vector3 spawnPosition,
            float assemblyTime)
        {
            _slotIndex = slotIndex;
            _isLeader = isLeader;
            _formationAnchor = anchor;
            _slotLocalOffset = slotOffset;
            _spawnPos = spawnPosition;
            _assemblyDuration = Mathf.Max(assemblyTime, 0.5f);
            _assemblyTimer = 0f;
            _state = WingmanState.Assembling;

            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false); // Pha Assembling khiên luôn tắt

            transform.position = _spawnPos;

            // Tính toán 2 điểm điều khiển (Control Points) cho đường cong Cubic Bézier 3D
            Vector3 targetSlotWorld = _formationAnchor.transform.TransformPoint(_slotLocalOffset);
            Vector3 toTarget = targetSlotWorld - _spawnPos;

            // Điểm điều khiển 1: Uốn lượn theo hướng vung sườn ban đầu
            _ctrl1 = _spawnPos + Vector3.up * 8f + Vector3.right * (UnityEngine.Random.Range(-12f, 12f));
            // Điểm điều khiển 2: Nắn theo hướng vector đường ray tới gần Slot
            _ctrl2 = targetSlotWorld - anchor.transform.forward * 20f + Vector3.up * 4f;

            // Tắt EnemyMovement để toàn quyền kiểm soát vị trí bằng Bézier và Slot
            if (_movement != null) _movement.enabled = false;

            // Trong lúc ráp nối, vẫn có thể bắn hạ (Risk vs Reward: Chiến thuật an toàn sớm)
            if (_collider != null) _collider.enabled = true;
        }

        private void Update()
        {
            if (_formationAnchor == null) return;

            switch (_state)
            {
                case WingmanState.Assembling:
                    UpdateAssembly();
                    break;

                case WingmanState.LockedInSlot:
                    UpdateLockedSlot();
                    break;

                case WingmanState.PanicScatter:
                    UpdatePanicScatter();
                    break;
            }
        }

        private void UpdateAssembly()
        {
            _assemblyTimer += Time.deltaTime;
            float u = Mathf.Clamp01(_assemblyTimer / _assemblyDuration);

            // Tọa độ đích động của Slot (Slot di chuyển liên tục theo Anchor)
            Vector3 dynamicSlotWorld = _formationAnchor.transform.TransformPoint(_slotLocalOffset);

            // Công thức Cubic Bézier 3D: P(u) = (1-u)^3 P0 + 3(1-u)^2 u P1 + 3(1-u) u^2 P2 + u^3 P3
            Vector3 currentPos = EvaluateCubicBezier(_spawnPos, _ctrl1, _ctrl2, dynamicSlotWorld, u);
            Vector3 nextPos = EvaluateCubicBezier(_spawnPos, _ctrl1, _ctrl2, dynamicSlotWorld, Mathf.Min(u + 0.05f, 1f));

            transform.position = currentPos;
            if ((nextPos - currentPos).sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(nextPos - currentPos), 10f * Time.deltaTime);
            }

            if (u >= 1.0f)
            {
                _state = WingmanState.LockedInSlot;
                OnEnteredFormationSlot();
            }
        }

        private void OnEnteredFormationSlot()
        {
            // Kiểm tra và kích hoạt lá chắn bảo vệ nếu là Tàu Lá Chắn (ShieldBearer)
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null && _health.Data != null && _health.Data.hasFormationShield)
            {
                // Bắt buộc đội hình phải có Tàu Chỉ Huy còn sống
                bool hasLeader = _formationAnchor != null && _formationAnchor.HasActiveLeader;
                if (hasLeader)
                {
                    _health.SetShieldActive(true);
                }
                else
                {
                    _health.SetShieldActive(false);
                    Debug.Log($"[Formation] Tàu lá chắn Slot {_slotIndex} không bật khiên do đội hình không có Chỉ Huy!");
                }
            }
        }

        private void UpdateLockedSlot()
        {
            // Khóa cứng tọa độ và góc xoay vào Slot của Formation Anchor (Pha 2 Synchronized Lock)
            Vector3 targetWorld = _formationAnchor.transform.TransformPoint(_slotLocalOffset);
            transform.position = targetWorld;
            transform.rotation = _formationAnchor.transform.rotation;
        }

        private void UpdatePanicScatter()
        {
            _panicTimer += Time.deltaTime;

            // Di chuyển theo vector gia tốc ly tâm
            transform.position += _panicVelocity * Time.deltaTime;

            // Lộn vòng Barrel Roll hoảng loạn quanh trục Z
            transform.Rotate(0f, 0f, _panicRollDir * 720f * Time.deltaTime, Space.Self);

            if (_panicTimer >= _panicDuration)
            {
                Recycle();
            }
        }

        /// <summary>
        /// Kích hoạt cơ chế giữ nguyên đội hình (Maintain Formation) khi Tàu Chỉ Huy bị hạ gục.
        /// Đội hình KHÔNG tan rã, tàu cánh tiếp tục khóa vị trí theo Anchor, lá chắn dập tắt để tạo cửa sổ sát thương.
        /// </summary>
        public void OnLeaderDestroyedMaintainFormation()
        {
            // Vẫn giữ nguyên trạng thái khóa vị trí theo Anchor (LockedSlot)
            // Không gán _formationAnchor = null

            // Dập tắt lá chắn bảo vệ của ShieldBearer để thưởng cho người chơi khi đã bắn hạ Leader
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null)
            {
                _health.SetShieldActive(false);
            }

            Debug.Log($"<color=cyan><b>[MAINTAIN FORMATION]</b></color> Chỉ huy hy sinh nhưng tàu cánh Slot {_slotIndex} tiếp tục giữ vững vị trí đội hình!");
        }

        /// <summary>
        /// Kích hoạt cơ chế phá khiên và tiếp tục giao chiến tự do (Break Shield & Engage).
        /// Khi Tàu Chỉ Huy bị hạ, khiên dập tắt ngay, quái KHÔNG tự hủy và tiếp tục bay bắn người chơi.
        /// </summary>
        public void TriggerBreakShieldAndEngage(Transform playerTarget)
        {
            _state = WingmanState.FreeCombat;
            _formationAnchor = null;

            // 1. Dập tắt 100% lá chắn bảo vệ
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null)
            {
                _health.SetShieldActive(false);
                // Cung cấp 0.5s grace period để không chết oan do vụ nổ của Chỉ Huy ở cạnh bên
                _health.TriggerInvulnerability(0.5f);
            }

            // 2. Kích hoạt lại EnemyMovement để quái tiếp tục hành trình bay theo ray
            if (_movement == null) _movement = GetComponent<EnemyMovement>();
            if (_movement != null)
            {
                _movement.enabled = true;
                _movement.SetTarget(playerTarget != null ? playerTarget : (VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : null));
            }

            // 3. Kích hoạt lại hệ thống bắn đạn EnemyAttack
            var attack = GetComponent<EnemyAttack>();
            if (attack != null)
            {
                attack.enabled = true;
            }

            Debug.Log($"<color=green><b>[BREAK SHIELD & ENGAGE]</b></color> Tàu cánh Slot {_slotIndex} mất khiên và tiếp tục chiến đấu!");
        }

        /// <summary>
        /// Kích hoạt cơ chế phân rã hoảng loạn (Panic Scatter) khi Tàu chỉ huy bị tiêu diệt.
        /// </summary>
        public void TriggerPanicScatter(Vector3 leaderPos, Vector3 railForward, float railSpeed)
        {
            if (_state == WingmanState.PanicScatter) return;

            _state = WingmanState.PanicScatter;
            _panicTimer = 0f;

            // Dập tắt lá chắn ngay lập tức khi Tàu Chỉ Huy bị tiêu diệt
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null)
            {
                _health.SetShieldActive(false);
            }

            // Toán học Vector Ly tâm Thoát ly (TDD Phần III.3):
            // V_panic = Normalize(P_ship.xy - P_anchor.xy) * V_boost + Forward_rail * V_rail
            Vector2 shipXY = new Vector2(transform.position.x, transform.position.y);
            Vector2 leaderXY = new Vector2(leaderPos.x, leaderPos.y);
            Vector2 centrifugal2D = (shipXY - leaderXY).sqrMagnitude > 0.01f ? (shipXY - leaderXY).normalized : (transform.position.x > 0 ? Vector2.right : Vector2.left);

            float vBoost = 42f; // Gia tốc ly tâm dạt biên cực nhanh
            Vector3 centrifugal3D = new Vector3(centrifugal2D.x, centrifugal2D.y, 0f) * vBoost;

            _panicVelocity = centrifugal3D + railForward * (railSpeed + 15f);
            _panicRollDir = centrifugal2D.x >= 0 ? 1f : -1f;

            Debug.Log($"<color=orange><b>[PANIC SCATTER]</b></color> Tàu cánh Slot {_slotIndex} mất khiên và hoảng loạn bẻ lái dạt biên thoát ly!");
        }

        /// <summary>
        /// Kích hoạt cơ chế rút lui khẩn cấp (Retreat Warp) khi mất Tàu Chỉ Huy.
        /// </summary>
        public void TriggerRetreatWarp(Vector3 railForward, float railSpeed)
        {
            if (_state == WingmanState.PanicScatter) return;

            _state = WingmanState.PanicScatter;
            _panicTimer = 0f;
            _panicDuration = 0.8f;

            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false);

            _panicVelocity = -railForward * (railSpeed + 45f) + transform.up * 15f;
            _panicRollDir = 0f;

            Debug.Log($"<color=cyan><b>[RETREAT WARP]</b></color> Tàu cánh Slot {_slotIndex} tăng tốc thoát ly khẩn cấp!");
        }

        /// <summary>
        /// Kích hoạt cơ chế cảm tử (Kamikaze Charge) lao thẳng vào người chơi khi mất Tàu Chỉ Huy.
        /// </summary>
        public void TriggerKamikazeCharge(Transform playerTarget)
        {
            if (_state == WingmanState.PanicScatter) return;

            _state = WingmanState.PanicScatter;
            _panicTimer = 0f;
            _panicDuration = 2.5f;

            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false);

            Vector3 toPlayer = (playerTarget != null) ? (playerTarget.position - transform.position).normalized : -transform.forward;
            _panicVelocity = toPlayer * 45f;
            _panicRollDir = 1f;

            Debug.Log($"<color=red><b>[KAMIKAZE CHARGE]</b></color> Tàu cánh Slot {_slotIndex} lao thẳng cảm tử vào người chơi!");
        }

        private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0; // (1-t)^3 * P0
            p += 3f * uu * t * p1; // 3*(1-t)^2 * t * P1
            p += 3f * u * tt * p2; // 3*(1-t) * t^2 * P2
            p += ttt * p3;         // t^3 * P3
            return p;
        }

        public void ResetState()
        {
            _state = WingmanState.Assembling;
            _formationAnchor = null;
            _assemblyTimer = 0f;
            _panicTimer = 0f;
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null)
            {
                _health.SetShieldActive(false);
            }
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_formationAnchor != null)
            {
                _formationAnchor.NotifyShipDestroyed(this);
            }
            ResetState();
        }

        public void Recycle()
        {
            ResetState();

            if (_movement != null)
            {
                _movement.enabled = true;
                _movement.Recycle();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
