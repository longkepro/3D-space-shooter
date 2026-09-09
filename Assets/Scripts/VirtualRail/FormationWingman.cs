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
            PanicScatter  // Hoảng loạn phân rã khi mất chỉ huy
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

        public int SlotIndex => _slotIndex;
        public bool IsLeader => _isLeader;
        public WingmanState State => _state;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _collider = GetComponentInChildren<Collider>();
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

            transform.position = _spawnPos;

            // Tính toán 2 điểm điều khiển (Control Points) cho đường cong Cubic Bézier 3D
            Vector3 targetSlotWorld = _formationAnchor.transform.TransformPoint(_slotLocalOffset);
            Vector3 toTarget = targetSlotWorld - _spawnPos;

            // Điểm điều khiển 1: Uốn lượn theo hướng vung sườn ban đầu
            _ctrl1 = _spawnPos + Vector3.up * 8f + Vector3.right * (UnityEngine.Random.Range(-12f, 12f));
            // Điểm điều khiển 2: Nắn theo hướng vector đường ray tới gần Slot
            _ctrl2 = targetSlotWorld - anchor.transform.forward * 20f + Vector3.up * 4f;

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
        /// Kích hoạt cơ chế phân rã hoảng loạn (Panic Scatter) khi Tàu chỉ huy bị tiêu diệt.
        /// </summary>
        public void TriggerPanicScatter(Vector3 leaderPos, Vector3 railForward, float railSpeed)
        {
            if (_state == WingmanState.PanicScatter) return;

            _state = WingmanState.PanicScatter;
            _panicTimer = 0f;

            // Toán học Vector Ly tâm Thoát ly (TDD Phần III.3):
            // V_panic = Normalize(P_ship.xy - P_anchor.xy) * V_boost + Forward_rail * V_rail
            Vector2 shipXY = new Vector2(transform.position.x, transform.position.y);
            Vector2 leaderXY = new Vector2(leaderPos.x, leaderPos.y);
            Vector2 centrifugal2D = (shipXY - leaderXY).sqrMagnitude > 0.01f ? (shipXY - leaderXY).normalized : (transform.position.x > 0 ? Vector2.right : Vector2.left);

            float vBoost = 42f; // Gia tốc ly tâm dạt biên cực nhanh
            Vector3 centrifugal3D = new Vector3(centrifugal2D.x, centrifugal2D.y, 0f) * vBoost;

            _panicVelocity = centrifugal3D + railForward * (railSpeed + 15f);
            _panicRollDir = centrifugal2D.x >= 0 ? 1f : -1f;

            Debug.Log($"<color=orange><b>[PANIC SCATTER]</b></color> Tàu cánh Slot {_slotIndex} hoảng loạn bẻ lái dạt biên thoát ly!");
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

        private void OnDisable()
        {
            if (_formationAnchor != null)
            {
                _formationAnchor.NotifyShipDestroyed(this);
            }
        }

        public void Recycle()
        {
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
