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
            Assembling,     // Đang bay theo đường cong Bézier nhập đội
            LockedInSlot,   // Đã khóa cứng đồng điệu theo Slot của Anchor
            PanicScatter,   // Hoảng loạn phân rã khi mất chỉ huy
            RetreatWarp,    // Bứt tốc khẩn cấp thoát ly khỏi màn chơi khi mất chỉ huy
            KamikazeCharge, // Khóa mũi lao thẳng cảm tử vào người chơi khi mất chỉ huy
            FreeCombat,     // Tự do chiến đấu khi mất chỉ huy và mất khiên (không tự hủy)
            Exiting         // Đang thực hiện động tác thoát ly rời đi điện ảnh
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

        // Retreat Warp kinematics state (Cinematic 3-Phase Retreat Arc)
        private Vector3 _warpVelocity;
        private float _warpTimer = 0f;
        private float _warpDuration = 3.5f;
        private float _retreatSide = 1f;
        private Vector3 _retreatBaseScale = Vector3.one;
        private bool _hasPlayedWarpSFX = false;
        private Vector3 _retreatForwardCached = Vector3.forward;
        private Vector3 _retreatRightCached = Vector3.right;
        private Vector3 _retreatUpCached = Vector3.up;
        private float _retreatBaseSpeedCached = 30f;

        // Kamikaze Charge pursuit kinematics state
        private Transform _kamikazeTarget;
        private float _kamikazeTimer = 0f;
        private float _kamikazeDuration = 3.2f;
        private float _kamikazeTelegraphDuration = 0.45f;
        private float _kamikazeTurnRate = 2.8f;
        private Vector3 _railForwardCached = Vector3.forward;
        private float _railSpeedCached = 30f;

        // Cinematic Exit kinematics state
        private Vector3 _exitVelocity;
        private float _exitTimer = 0f;
        private float _exitDuration = 1.8f;
        private float _exitRollRate = 0f;
        private float _exitRollDir = 1f;

        private EnemyMovement _movement;
        private Collider _collider;
        private EnemyHealth _health;

        // Morphing state độc lập cho từng tàu (0 GC Alloc, chống lỗi lệch index khi có tàu chết giữa chừng)
        private Vector3 _morphFromOffset;
        private Vector3 _morphToOffset;
        private int _targetMorphSlotIndex;

        public int SlotIndex => _slotIndex;
        public bool IsLeader => _isLeader;
        public WingmanState State => _state;
        public Vector3 SlotLocalOffset => _slotLocalOffset;

        public void SetDynamicSlotOffset(Vector3 dynamicOffset)
        {
            _slotLocalOffset = dynamicOffset;
        }

        public void UpdateSlotDefinition(int newSlotIndex, Vector3 newOffset)
        {
            _slotIndex = newSlotIndex;
            _slotLocalOffset = newOffset;
        }

        /// <summary>
        /// Khởi tạo mục tiêu biến hình cho tàu cánh (0 GC Alloc).
        /// </summary>
        public void StartMorph(Vector3 fromOffset, Vector3 toOffset, int targetSlotIndex)
        {
            _morphFromOffset = fromOffset;
            _morphToOffset = toOffset;
            _targetMorphSlotIndex = targetSlotIndex;
        }

        /// <summary>
        /// Cập nhật tiến trình lướt chuyển vị trí khi đang biến hình kết hợp tách độ sâu Z né va chạm.
        /// </summary>
        public void UpdateMorph(float s, float depthArch)
        {
            Vector3 currentSlot = Vector3.Lerp(_morphFromOffset, _morphToOffset, s);
            float deltaX = _morphToOffset.x - _morphFromOffset.x;
            if (Mathf.Abs(deltaX) > 1.0f)
            {
                float zOffset = (deltaX > 0f ? -3.5f : 3.5f) * depthArch;
                currentSlot.z += zOffset;
            }
            _slotLocalOffset = currentSlot;
        }

        /// <summary>
        /// Hoàn tất biến hình, chính thức cập nhật slot index và offset cố định mới.
        /// </summary>
        public void CompleteMorph()
        {
            _slotIndex = _targetMorphSlotIndex;
            _slotLocalOffset = _morphToOffset;
        }

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _collider = GetComponentInChildren<Collider>();
            _health = GetComponent<EnemyHealth>();
            _retreatBaseScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        }

        public void InitializeSlot(
            int slotIndex,
            bool isLeader,
            VirtualFormationAnchor anchor,
            Vector3 slotOffset,
            Vector3 spawnPosition,
            float assemblyTime,
            FormationEntryStyle entryStyle = FormationEntryStyle.FlankIn)
        {
            this.enabled = true;
            _slotIndex = slotIndex;
            _isLeader = isLeader;
            _formationAnchor = anchor;
            _slotLocalOffset = slotOffset;
            _spawnPos = spawnPosition;

            // Chế độ Khóa Tức Thì (Instant Locked Slot - 0s Assembly): bỏ qua pha bổ nhào, vào vị trí chiến đấu ngay
            if (assemblyTime <= 0.05f)
            {
                _state = WingmanState.LockedInSlot;
                _assemblyDuration = 0f;
                _assemblyTimer = 0f;
                Vector3 instantPos = _formationAnchor != null ? _formationAnchor.GetSlotWorldPosition(_slotIndex, _slotLocalOffset) : _spawnPos;
                transform.position = instantPos;
                if (_formationAnchor != null) transform.rotation = _formationAnchor.transform.rotation;
                if (_health == null) _health = GetComponent<EnemyHealth>();
                if (_health != null) _health.SetShieldActive(!_isLeader);
                if (_movement != null) _movement.enabled = false;
                if (_collider != null) _collider.enabled = true;
                return;
            }

            _assemblyDuration = Mathf.Max(assemblyTime, 0.5f);
            _assemblyTimer = 0f;
            _state = WingmanState.Assembling;

            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false); // Pha Assembling khiên luôn tắt

            transform.position = _spawnPos;

            // Tính toán 2 điểm điều khiển (Control Points) cho đường cong Cubic Bézier 3D theo Entry Style
            Vector3 targetSlotWorld = _formationAnchor.GetSlotWorldPosition(_slotIndex, _slotLocalOffset);
            Vector3 railFwd = anchor != null ? anchor.transform.forward : Vector3.forward;
            Vector3 railRight = anchor != null ? anchor.transform.right : Vector3.right;
            Vector3 railUp = anchor != null ? anchor.transform.up : Vector3.up;

            switch (entryStyle)
            {
                case FormationEntryStyle.OverheadDive:
                    // Bổ nhào từ tầng mây +Y: ctrl1 cắm dốc xuống cực mạnh, ctrl2 võng lên đón slot
                    _ctrl1 = _spawnPos - railUp * 24f - railFwd * 6f;
                    _ctrl2 = targetSlotWorld - railUp * 8f - railFwd * 16f;
                    break;

                case FormationEntryStyle.RearFlyby:
                    // Phóng vọt từ sau lưng lên phía trước vượt mặt Arwing
                    _ctrl1 = _spawnPos + railFwd * 35f + railUp * 8f;
                    _ctrl2 = targetSlotWorld + railFwd * 15f + railUp * 4f;
                    break;

                case FormationEntryStyle.FrontalHorizon:
                    // Lao thẳng từ chân trời xa phía trước rồi xòe ra đón slot
                    _ctrl1 = _spawnPos - railFwd * 25f;
                    _ctrl2 = targetSlotWorld + railFwd * 12f;
                    break;

                case FormationEntryStyle.FlankIn:
                default:
                    // Uốn lượn từ 2 bên sườn
                    _ctrl1 = _spawnPos + railUp * 8f + railRight * (UnityEngine.Random.Range(-12f, 12f));
                    _ctrl2 = targetSlotWorld - railFwd * 20f + railUp * 4f;
                    break;
            }

            // Tắt EnemyMovement để toàn quyền kiểm soát vị trí bằng Bézier và Slot
            if (_movement != null) _movement.enabled = false;

            // Trong lúc ráp nối, vẫn có thể bắn hạ (Risk vs Reward: Chiến thuật an toàn sớm)
            if (_collider != null) _collider.enabled = true;
        }

        private void Update()
        {
            // Trạng thái FreeCombat đã được bàn giao toàn bộ cho EnemyMovement, FormationWingman không can thiệp
            if (_state == WingmanState.FreeCombat) return;

            // Các trạng thái phản ứng hậu chỉ huy (PanicScatter, RetreatWarp, KamikazeCharge)
            // hoạt động độc lập không phụ thuộc vào _formationAnchor để đảm bảo hoàn thành hoạt ảnh
            switch (_state)
            {
                case WingmanState.PanicScatter:
                    UpdatePanicScatter();
                    return;

                case WingmanState.RetreatWarp:
                    UpdateRetreatWarp();
                    return;

                case WingmanState.KamikazeCharge:
                    UpdateKamikazeCharge();
                    return;
            }

            if (_formationAnchor == null) return;

            switch (_state)
            {
                case WingmanState.Assembling:
                    UpdateAssembly();
                    break;

                case WingmanState.LockedInSlot:
                    UpdateLockedSlot();
                    break;

                case WingmanState.Exiting:
                    UpdateExiting();
                    break;

                case WingmanState.FreeCombat:
                    // Đã bàn giao cho EnemyMovement
                    break;
            }
        }

        private void UpdateAssembly()
        {
            _assemblyTimer += Time.deltaTime;
            float u = Mathf.Clamp01(_assemblyTimer / _assemblyDuration);

            // Tọa độ đích động của Slot (Slot di chuyển liên tục theo Anchor)
            Vector3 dynamicSlotWorld = _formationAnchor.GetSlotWorldPosition(_slotIndex, _slotLocalOffset);

            // Công thức Cubic Bézier 3D: P(u) = (1-u)^3 P0 + 3(1-u)^2 u P1 + 3(1-u) u^2 P2 + u^3 P3
            Vector3 currentPos = EvaluateCubicBezier(_spawnPos, _ctrl1, _ctrl2, dynamicSlotWorld, u);
            Vector3 nextPos = EvaluateCubicBezier(_spawnPos, _ctrl1, _ctrl2, dynamicSlotWorld, Mathf.Min(u + 0.05f, 1f));

            transform.position = currentPos;
            Vector3 delta = nextPos - currentPos;
            if (delta.sqrMagnitude > 0.001f)
            {
                Vector3 travelDir = delta.normalized;
                Vector3 railRight = (_formationAnchor != null) ? _formationAnchor.transform.right : Vector3.right;
                Vector3 railUp = (_formationAnchor != null) ? _formationAnchor.transform.up : Vector3.up;

                // Tính góc nghiêng cánh Banking Roll theo độ cong ôm cua ngang
                float lateralDot = Vector3.Dot(travelDir, railRight);
                float bankRoll = -Mathf.Clamp(lateralDot * 50f, -40f, 40f);

                Quaternion lookRot = Quaternion.LookRotation(travelDir, railUp);
                Quaternion targetRot = lookRot * Quaternion.Euler(0f, 0f, bankRoll);

                // Nếu đội hình ở chế độ StabilizedBoresight, mượt mà nội suy về góc ổn định phẳng khi gần tới slot
                bool isStabilized = _formationAnchor != null && _formationAnchor.ActivePattern != null && _formationAnchor.ActivePattern.attitudeMode == FormationAttitudeMode.StabilizedBoresight;
                if (isStabilized && u > 0.6f)
                {
                    float blend = (u - 0.6f) / 0.4f;
                    targetRot = Quaternion.Slerp(targetRot, _formationAnchor.transform.rotation, blend);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
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
            // Khóa cứng tọa độ tịnh tiến và góc xoay vào Slot của Formation Anchor (Pha 2 Synchronized Lock)
            Vector3 targetWorld = _formationAnchor.GetSlotWorldPosition(_slotIndex, _slotLocalOffset);
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

        private void UpdateRetreatWarp()
        {
            _warpTimer += Time.deltaTime;
            float t = _warpTimer;

            if (t < 0.8f)
            {
                // =========================================================================
                // PHA 1 (0.0s - 0.8s): Tactical Fan-out Break Turn (Tẽ đội hình & Lật cánh)
                // =========================================================================
                float u1 = t / 0.8f;
                float lateralSpeed = _retreatSide * (8f + _slotIndex * 2f) * (1f - u1 * 0.5f);
                float verticalSpeed = 3f * (1f - u1);
                float fwdSpeed = _retreatBaseSpeedCached + 12f;

                Vector3 stepVel = _retreatForwardCached * fwdSpeed +
                                  _retreatRightCached * lateralSpeed +
                                  _retreatUpCached * verticalSpeed;

                transform.position += stepVel * Time.deltaTime;

                float targetRoll = -_retreatSide * 42f * Mathf.Sin(u1 * Mathf.PI * 0.5f);
                if (stepVel.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(stepVel.normalized, _retreatUpCached);
                    Quaternion targetRot = lookRot * Quaternion.Euler(0f, 0f, targetRoll);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
                }
            }
            else if (t < 2.9f)
            {
                // =========================================================================
                // PHA 2 (0.8s - 2.9s): Evasive Jinking / S-Turn Weaving (Lượn S né đạn tương tác)
                // =========================================================================
                float t2 = t - 0.8f;
                float freq = 4.2f + (_slotIndex * 0.35f);
                float vx = Mathf.Cos(t2 * freq) * 6.5f * _retreatSide;
                float vy = Mathf.Sin(t2 * freq * 0.5f) * 3.5f;
                float fwdSpeed = _retreatBaseSpeedCached + 12f;

                Vector3 stepVel = _retreatForwardCached * fwdSpeed +
                                  _retreatRightCached * vx +
                                  _retreatUpCached * vy;

                transform.position += stepVel * Time.deltaTime;

                float weaveRoll = -vx * 5.0f;
                if (stepVel.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(stepVel.normalized, _retreatUpCached);
                    Quaternion targetRot = lookRot * Quaternion.Euler(0f, 0f, weaveRoll);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
                }
            }
            else
            {
                // =========================================================================
                // PHA 3 (2.9s - 3.5s): Relativistic Warp Stretch & Hyperdrive Jump
                // =========================================================================
                float warpU = Mathf.Clamp01((t - 2.9f) / 0.6f);
                float warpAccel = 1.0f + (warpU * warpU * 8.0f);
                float fwdSpeed = (_retreatBaseSpeedCached + 12f) * warpAccel;

                Vector3 stepVel = _retreatForwardCached * fwdSpeed;
                transform.position += stepVel * Time.deltaTime;

                Quaternion targetRot = Quaternion.LookRotation(_retreatForwardCached, _retreatUpCached);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);

                // Kéo giãn relativistic scale trục Z, bóp nhỏ trục X/Y
                float stretchZ = Mathf.Lerp(1.0f, 3.2f, warpU * warpU);
                float pinchXY = Mathf.Lerp(1.0f, 0.45f, warpU);
                transform.localScale = new Vector3(_retreatBaseScale.x * pinchXY, _retreatBaseScale.y * pinchXY, _retreatBaseScale.z * stretchZ);

                if (!_hasPlayedWarpSFX)
                {
                    _hasPlayedWarpSFX = true;
                    AudioManager.Instance?.PlaySFX3D(AudioSFXType.EngineBoost, transform.position, 0.9f, 1.35f);
                }

                if (t >= _warpDuration)
                {
                    transform.localScale = _retreatBaseScale; // Khôi phục tỷ lệ gốc trước khi trả về pool
                    Recycle();
                }
            }
        }

        private void UpdateKamikazeCharge()
        {
            _kamikazeTimer += Time.deltaTime;

            Transform target = _kamikazeTarget != null ? _kamikazeTarget : VirtualRailAnchor.PlayerShipTransform;
            Vector3 toPlayer = target != null ? (target.position - transform.position) : -_railForwardCached;
            float sqrDist = toPlayer.sqrMagnitude;

            // 1. Pha Telegraph (Khựng lại 0.45s, rung cánh chao đảo báo hiệu để người chơi chuẩn bị)
            if (_kamikazeTimer < _kamikazeTelegraphDuration)
            {
                // Đồng tốc theo ray trong lúc khựng lại để giữ nguyên cự ly
                transform.position += _railForwardCached * (_railSpeedCached * Time.deltaTime);

                // Rung chao đảo cánh cảnh báo (Visual Telegraph Wobble)
                float wobble = Mathf.Sin(_kamikazeTimer * 30f) * 15f;
                Vector3 lookDir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : -_railForwardCached;
                Quaternion lookRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = lookRot * Quaternion.Euler(0f, 0f, wobble);
                return;
            }

            // 2. Kiểm tra va chạm trực diện với Arwing trong cự ly nguy hiểm (< 3.0m)
            if (sqrDist <= 9.0f && target != null)
            {
                var shield = target.GetComponentInChildren<Shield>();
                if (shield == null) shield = target.GetComponentInParent<Shield>();
                if (shield != null)
                {
                    shield.TakeDamage(2); // Cú húc cảm tử gây 2 điểm sát thương khiên
                }

                VirtualRailCameraRig.TriggerShake(0.35f);
                AudioManager.Instance?.PlaySFX3D(AudioSFXType.ExplosionSmall, transform.position, 1.0f);

                if (_movement != null)
                {
                    _movement.BlowUp();
                }
                else
                {
                    Recycle();
                }
                return;
            }

            // 3. Điều hướng bám đuổi Arwing (Turn rate 2.8rad/s vừa phải: nếu Arwing lượn gấp/Barrel Roll sẽ né thoát)
            if (sqrDist > 0.01f)
            {
                Vector3 desiredDir = toPlayer.normalized;
                Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _kamikazeTurnRate * Time.deltaTime);
            }

            // 4. Vận tốc tiếp cận cân bằng:
            // Tốc độ rút ngắn cự ly dọc trục ray là ~26m/s (khoảng 2.5s từ 65m để người chơi kịp xả đạn)
            // Kèm thành phần lượn ngang ép tâm đuổi theo vị trí Arwing
            Vector3 lateralDir = Vector3.ProjectOnPlane(toPlayer, _railForwardCached);
            Vector3 lateralStep = lateralDir.sqrMagnitude > 0.01f ? lateralDir.normalized * 12f : Vector3.zero;
            Vector3 closingVel = _railForwardCached * (_railSpeedCached - 26f) + lateralStep;

            transform.position += closingVel * Time.deltaTime;

            // 5. Quá thời gian sinh tồn (bay trượt qua người chơi ra sau lưng): Tự nổ
            if (_kamikazeTimer >= _kamikazeDuration)
            {
                if (_movement != null)
                {
                    _movement.BlowUp();
                }
                else
                {
                    Recycle();
                }
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

            // 4. Bàn giao quyền điều khiển hoàn toàn: tắt component FormationWingman
            this.enabled = false;

            Debug.Log($"<color=green><b>[BREAK SHIELD & ENGAGE]</b></color> Tàu cánh Slot {_slotIndex} mất khiên và tiếp tục chiến đấu!");
        }

        /// <summary>
        /// Kích hoạt cơ chế phân rã hoảng loạn (Panic Scatter) khi Tàu chỉ huy bị tiêu diệt.
        /// </summary>
        public void TriggerPanicScatter(Vector3 leaderPos, Vector3 railForward, float railSpeed)
        {
            if (_state == WingmanState.PanicScatter ||
                _state == WingmanState.RetreatWarp ||
                _state == WingmanState.KamikazeCharge ||
                _state == WingmanState.FreeCombat) return;

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
        /// Triển khai Cung đường Rút lui Điện ảnh 3 Pha (3-Phase Cinematic Retreat Arc):
        /// - Pha 1 (0.0s - 0.8s): Tẽ đội hình & nghiêng cánh thoát hiểm (Tactical Fan-out Break Turn)
        /// - Pha 2 (0.8s - 2.9s): Lượn sóng né đạn S-Turn tương tác trong tầm ngắm Arwing
        /// - Pha 3 (2.9s - 3.5s): Giãn dài không-thời gian (Relativistic Warp Stretch) & nhảy siêu không gian
        /// </summary>
        public void TriggerRetreatWarp(Vector3 railForward, float railSpeed)
        {
            if (_state == WingmanState.RetreatWarp ||
                _state == WingmanState.PanicScatter ||
                _state == WingmanState.KamikazeCharge ||
                _state == WingmanState.FreeCombat) return;

            _state = WingmanState.RetreatWarp;
            _warpTimer = 0f;
            _warpDuration = 3.5f;
            _hasPlayedWarpSFX = false;

            if (_retreatBaseScale == Vector3.zero)
            {
                _retreatBaseScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
            }

            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false);

            // Xác định hướng tẽ thoát hiểm theo vị trí Slot
            _retreatSide = (_slotIndex % 2 == 0) ? 1f : -1f;
            if (Mathf.Abs(_slotLocalOffset.x) > 0.5f)
            {
                _retreatSide = Mathf.Sign(_slotLocalOffset.x);
            }

            _retreatBaseSpeedCached = (railSpeed > 10f) ? railSpeed : 30f;
            _retreatForwardCached = (railForward.sqrMagnitude > 0.01f) ? railForward.normalized : Vector3.forward;

            if (_formationAnchor != null)
            {
                _retreatRightCached = _formationAnchor.transform.right;
                _retreatUpCached = _formationAnchor.transform.up;
            }
            else
            {
                _retreatRightCached = Vector3.Cross(Vector3.up, _retreatForwardCached).normalized;
                _retreatUpCached = Vector3.Cross(_retreatForwardCached, _retreatRightCached).normalized;
            }

            Debug.Log($"<color=cyan><b>[RETREAT WARP ARC]</b></color> Tàu cánh Slot {_slotIndex} bắt đầu rút lui điện ảnh 3 pha về chân trời!");
        }

        /// <summary>
        /// Kích hoạt cơ chế cảm tử (Kamikaze Charge) lao thẳng vào người chơi khi mất Tàu Chỉ Huy.
        /// </summary>
        public void TriggerKamikazeCharge(Transform playerTarget)
        {
            if (_state == WingmanState.KamikazeCharge ||
                _state == WingmanState.RetreatWarp ||
                _state == WingmanState.PanicScatter ||
                _state == WingmanState.FreeCombat) return;

            _state = WingmanState.KamikazeCharge;
            _kamikazeTimer = 0f;
            _kamikazeDuration = 3.2f;
            _kamikazeTelegraphDuration = 0.45f;
            _kamikazeTurnRate = 2.8f;
            _kamikazeTarget = playerTarget != null ? playerTarget : VirtualRailAnchor.PlayerShipTransform;

            var rail = VirtualRailAnchor.Instance;
            _railForwardCached = (rail != null) ? rail.ForwardTangent : Vector3.forward;
            _railSpeedCached = (rail != null && rail.CurrentSpeed > 10f) ? rail.CurrentSpeed : 30f;

            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false);

            Debug.Log($"<color=red><b>[KAMIKAZE CHARGE]</b></color> Tàu cánh Slot {_slotIndex} khựng lại 0.45s rồi lao cảm tử vào người chơi với tốc độ cân bằng!");
        }

        /// <summary>
        /// Kích hoạt cơ chế thoát ly rời đi điện ảnh (Cinematic Exit) khi phi đội hết hạn tồn tại.
        /// </summary>
        public void TriggerExit(
            FormationExitStyle exitStyle,
            float duration,
            Vector3 railForward,
            Vector3 railRight,
            Vector3 railUp,
            float railSpeed)
        {
            if (_state == WingmanState.Exiting ||
                _state == WingmanState.PanicScatter ||
                _state == WingmanState.RetreatWarp ||
                _state == WingmanState.KamikazeCharge ||
                _state == WingmanState.FreeCombat) return;

            _state = WingmanState.Exiting;
            _exitTimer = 0f;
            _exitDuration = Mathf.Max(0.5f, duration);

            // Dập tắt lá chắn và vô hiệu hóa collider để không cản trở tàu người chơi khi đang thoát ly
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.SetShieldActive(false);
            if (_collider != null) _collider.enabled = false;

            float side = (_slotIndex % 2 == 0) ? 1f : -1f;

            switch (exitStyle)
            {
                case FormationExitStyle.ClimbOut:
                    // Cả đội ngóc mũi 45 độ vút lên nóc màn hình với vận tốc cao
                    Vector3 climbDir = (railUp * 1.6f + railForward * 1.5f).normalized;
                    _exitVelocity = climbDir * (railSpeed + 45f) + railRight * (side * 4f);
                    _exitRollDir = side;
                    _exitRollRate = 90f;
                    break;

                case FormationExitStyle.SplitFlyPast:
                    // Tẽ đôi sang 2 cánh, lộn vòng Barrel Roll bay ngược về phía sau/hai bên người chơi
                    Vector3 splitDir = (railRight * side * 1.8f - railForward * 1.2f + railUp * 0.4f).normalized;
                    _exitVelocity = splitDir * (railSpeed + 50f);
                    _exitRollDir = side;
                    _exitRollRate = 540f;
                    break;

                case FormationExitStyle.DiveUnder:
                    // Cắm dốc xuống đáy sâu
                    Vector3 diveDir = (-railUp * 1.8f + railForward * 1.2f).normalized;
                    _exitVelocity = diveDir * (railSpeed + 45f) + railRight * (side * 3f);
                    _exitRollDir = side;
                    _exitRollRate = 60f;
                    break;

                case FormationExitStyle.InstantRecycle:
                default:
                    Recycle();
                    return;
            }
        }

        private void UpdateExiting()
        {
            _exitTimer += Time.deltaTime;
            transform.position += _exitVelocity * Time.deltaTime;

            if (_exitVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 railUp = (_formationAnchor != null) ? _formationAnchor.transform.up : Vector3.up;
                Quaternion lookRot = Quaternion.LookRotation(_exitVelocity.normalized, railUp);
                float rollAngle = _exitRollDir * _exitRollRate * _exitTimer;
                transform.rotation = lookRot * Quaternion.Euler(0f, 0f, rollAngle);
            }

            if (_exitTimer >= _exitDuration)
            {
                Recycle();
            }
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
            this.enabled = true;
            _state = WingmanState.Assembling;
            _formationAnchor = null;
            _assemblyTimer = 0f;
            _panicTimer = 0f;
            _warpTimer = 0f;
            _warpVelocity = Vector3.zero;
            _kamikazeTimer = 0f;
            _kamikazeTarget = null;
            _railForwardCached = Vector3.forward;
            _railSpeedCached = 30f;
            _exitTimer = 0f;
            _exitVelocity = Vector3.zero;
            _morphFromOffset = Vector3.zero;
            _morphToOffset = Vector3.zero;
            _targetMorphSlotIndex = 0;
            if (_retreatBaseScale != Vector3.zero)
            {
                transform.localScale = _retreatBaseScale;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
            _hasPlayedWarpSFX = false;
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

            // Nếu đang chuyển sang FreeCombat (bàn giao cho EnemyMovement), bảo lưu state để không reset nhầm
            if (_state != WingmanState.FreeCombat)
            {
                ResetState();
            }
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
