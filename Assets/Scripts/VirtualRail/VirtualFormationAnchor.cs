using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Đầu mối ảo quản lý đội hình bay (Virtual Formation Anchor) theo chuẩn TDD v1.0.0 Phần III:
    /// - Quản lý ma trận Slot độc lập với tàu vật lý.
    /// - Thực hiện các chuyển động uốn lượn, nghiêng cánh đồng bộ của cả phi đội.
    /// - Quản lý quan hệ thứ bậc Leader (Slot 0) vs Wingmen (Slot 1, 2, ...).
    /// - Kích hoạt hiệu ứng Tán loạn (Panic Scatter) khi chỉ huy bị bắn hạ.
    /// </summary>
    public class VirtualFormationAnchor : MonoBehaviour, IPoolableEntity
    {
        [Serializable]
        public struct FormationCustomizationParams
        {
            public float spreadMultiplier;            // Dãn cách X, Y: kẹp an toàn [0.75, 1.4]
            public bool mirrorHorizontal;             // Đảo ngược trục X (X -> -X)
            public EnemyArchetype? archetypeOverride; // Nâng cấp loại quái toàn đội
            public FormationMotionProfile motionProfileOverride; // Hoán đổi quỹ đạo bay
            public FormationEntryStyle? entryStyleOverride;      // Hoán đổi kiểu xuất hiện
            public LeaderDeathReaction? reactionOverride;        // Ghi đè phản ứng mất sếp (Kamikaze Charge)
            public FormationPattern morphTargetOverride;         // Đội hình đích biến hình chuyển tiếp
            public float morphDelay;                             // Thời gian chờ trước khi biến hình
            public float morphDuration;                          // Thời gian lướt đổi slot

            public static FormationCustomizationParams Default => new FormationCustomizationParams
            {
                spreadMultiplier = 1.0f,
                mirrorHorizontal = false,
                archetypeOverride = null,
                motionProfileOverride = null,
                entryStyleOverride = null,
                reactionOverride = null,
                morphTargetOverride = null,
                morphDelay = 4.0f,
                morphDuration = 1.8f
            };
        }

        public enum FormationType
        {
            WedgeV = 0,       // Đội hình chữ V chuẩn Star Fox (5 tàu)
            EchelonLine = 1,  // Đội hình dàn hàng ngang quét màn hình (3 tàu)
            SpiralStream = 2  // Đội hình rồng bay xoắn ốc (5 tàu)
        }

        [Header("Formation Settings")]
        [SerializeField] private FormationType _formationType = FormationType.WedgeV;
        [SerializeField] private float _assemblyTime = 1.2f;
        [SerializeField] private float _swaySpeed = 1.5f;
        [SerializeField] private float _swayAmplitudeX = 12f;
        [SerializeField] private float _bankingAngle = 25f;

        private VirtualRailAnchor _railAnchor;
        private readonly List<FormationWingman> _activeWingmen = new List<FormationWingman>();
        private FormationWingman _leader;
        private FormationPattern _activePattern = null;
        private FormationCustomizationParams _activeCustomParams = FormationCustomizationParams.Default;
        private float _lifetimeTimer = 0f;
        private float _maxFormationLifetime = 12f;
        private bool _isLeaderKilled = false;
        private Vector3 _baseRayPosition;

        // Dynamic Formation Morphing state
        private bool _isMorphing = false;
        private float _morphTimer = 0f;
        private float _morphDuration = 1.8f;
        private FormationPattern _targetMorphPattern = null;
        private bool _hasMorphed = false;

        // Cinematic Exit state
        private bool _isExiting = false;
        private float _exitTimer = 0f;
        private float _exitDuration = 1.8f;

        public FormationType Type => _formationType;
        public FormationPattern ActivePattern => _activePattern;
        public bool IsActive => gameObject.activeSelf;
        public bool HasActiveLeader => _leader != null && !_isLeaderKilled && _leader.gameObject.activeSelf;
        public FormationWingman Leader => _leader;
        public bool IsMorphing => _isMorphing;
        public bool IsExiting => _isExiting;

        /// <summary>
        /// Tính toán tọa độ không gian thế giới của một Slot Offset theo hệ quy chiếu tịnh tiến của đường ray (0 GC Alloc).
        /// Hỗ trợ cơ chế Quay quanh tâm tàu chính (Centroid Orbit / Pinwheel Spin) khi slotIndex > 0 và profile có cài đặt centroidSpinSpeed.
        /// Slot Offset KHÔNG bị xoay theo góc nghiêng của Anchor để triệt tiêu hoàn toàn hiệu ứng quăng quật cánh (Zero Lever Arm Whiplash).
        /// </summary>
        public Vector3 GetSlotWorldPosition(int slotIndex, Vector3 slotLocalOffset)
        {
            Vector3 effectiveOffset = slotLocalOffset;

            // Cơ chế Centroid Orbit: Các tàu cánh (slotIndex > 0) quay tròn quanh tâm Leader (Slot 0)
            if (slotIndex > 0 && _activePattern != null && _activePattern.motionProfile != null)
            {
                float spinSpeed = _activePattern.motionProfile.centroidSpinSpeed;
                if (Mathf.Abs(spinSpeed) > 0.01f)
                {
                    float angleRad = (spinSpeed * _lifetimeTimer) * Mathf.Deg2Rad;
                    float cosA = Mathf.Cos(angleRad);
                    float sinA = Mathf.Sin(angleRad);

                    // Xoay 2D quanh trục Z trong mặt phẳng cục bộ XY của đội hình
                    float rx = slotLocalOffset.x * cosA - slotLocalOffset.y * sinA;
                    float ry = slotLocalOffset.x * sinA + slotLocalOffset.y * cosA;
                    effectiveOffset = new Vector3(rx, ry, slotLocalOffset.z);
                }
            }

            // Tùy biến Dãn cách (Spread) và Đảo gương (Mirror) động theo DDA
            if (_activeCustomParams.spreadMultiplier > 0.01f && Mathf.Abs(_activeCustomParams.spreadMultiplier - 1.0f) > 0.001f)
            {
                effectiveOffset.x *= _activeCustomParams.spreadMultiplier;
                effectiveOffset.y *= _activeCustomParams.spreadMultiplier;
            }

            if (_activeCustomParams.mirrorHorizontal)
            {
                effectiveOffset.x = -effectiveOffset.x;
            }

            if (_railAnchor != null)
            {
                return transform.position
                    + _railAnchor.transform.right * effectiveOffset.x
                    + _railAnchor.transform.up * effectiveOffset.y
                    + _railAnchor.ForwardTangent * effectiveOffset.z;
            }
            return transform.position + effectiveOffset;
        }

        public Vector3 GetSlotWorldPosition(Vector3 slotLocalOffset)
        {
            return GetSlotWorldPosition(-1, slotLocalOffset);
        }

        public static Vector3[] GetSlotOffsets(FormationType type)
        {
            switch (type)
            {
                case FormationType.WedgeV:
                    return new Vector3[]
                    {
                        new Vector3(0f, -2.0f, 0f),     // Slot 0: Leader
                        new Vector3(-8f, 2.0f, 0f),     // Slot 1: Left Wing 1
                        new Vector3(8f, 2.0f, 0f),      // Slot 2: Right Wing 1
                        new Vector3(-16f, 6.0f, 0f),    // Slot 3: Left Wing 2
                        new Vector3(16f, 6.0f, 0f)      // Slot 4: Right Wing 2
                    };

                case FormationType.EchelonLine:
                    return new Vector3[]
                    {
                        new Vector3(0f, 0f, 0f),        // Slot 0: Leader
                        new Vector3(-12f, 2.5f, 0f),    // Slot 1: Left Wing
                        new Vector3(12f, -2.5f, 0f)     // Slot 2: Right Wing
                    };

                case FormationType.SpiralStream:
                default:
                    return new Vector3[]
                    {
                        new Vector3(0f, 0f, 0f),        // Slot 0: Head
                        new Vector3(7f, 4f, 0f),        // Slot 1: Segment 1
                        new Vector3(12f, 0f, 0f),       // Slot 2: Segment 2
                        new Vector3(7f, -4f, 0f),       // Slot 3: Segment 3
                        new Vector3(0f, -8f, 0f)        // Slot 4: Segment 4
                    };
            }
        }

        public void InitializeFormation(
            FormationType type,
            VirtualRailAnchor railAnchor,
            Vector3 anchorStartPosition,
            List<EnemyMovement> assignedEnemies,
            float assemblyDuration)
        {
            _formationType = type;
            _activePattern = null;
            _railAnchor = railAnchor != null ? railAnchor : VirtualRailAnchor.Instance;
            _assemblyTime = assemblyDuration;
            _lifetimeTimer = 0f;
            _isLeaderKilled = false;
            _isMorphing = false;
            _morphTimer = 0f;
            _targetMorphPattern = null;
            _hasMorphed = false;
            _isExiting = false;
            _exitTimer = 0f;
            _activeWingmen.Clear();
            _leader = null;

            _baseRayPosition = anchorStartPosition;
            transform.position = anchorStartPosition;
            transform.rotation = Quaternion.LookRotation(-_railAnchor.ForwardTangent);

            Vector3[] slotOffsets = GetSlotOffsets(_formationType);
            int countToAssign = Mathf.Min(slotOffsets.Length, assignedEnemies.Count);

            for (int i = 0; i < countToAssign; i++)
            {
                EnemyMovement enemy = assignedEnemies[i];
                if (enemy == null) continue;

                enemy.ResetState();
                var fsm = enemy.GetComponent<EnemyLifecycleFSM>();
                if (fsm != null)
                {
                    fsm.ResetState();
                    fsm.enabled = false;
                }

                var wingman = enemy.GetComponent<FormationWingman>();
                if (wingman == null) wingman = enemy.gameObject.AddComponent<FormationWingman>();
                wingman.enabled = true;

                bool isLeader = (i == 0);
                if (isLeader) _leader = wingman;

                // Tạo điểm xuất phát ngẫu nhiên ngoài màn hình cho từng tàu (chân trời 120m - 140m)
                Vector3 spawnOffset = new Vector3(
                    (i % 2 == 0 ? 1f : -1f) * UnityEngine.Random.Range(20f, 35f),
                    UnityEngine.Random.Range(-5f, 15f),
                    UnityEngine.Random.Range(10f, 30f)
                );
                Vector3 shipSpawnPos = anchorStartPosition + spawnOffset;

                wingman.InitializeSlot(i, isLeader, this, slotOffsets[i], shipSpawnPos, _assemblyTime, FormationEntryStyle.FlankIn);
                _activeWingmen.Add(wingman);

                enemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : _railAnchor.transform);
                enemy.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
            Debug.Log($"<color=cyan><b>[FORMATION ANCHOR]</b></color> Khởi tạo đội hình {_formationType} với {countToAssign} tàu chiến! Pha 1 Ráp nối Bézier bắt đầu.");
        }

        /// <summary>
        /// Khởi tạo đội hình theo chuẩn DATA-DRIVEN FORMATION PATTERN (ScriptableObject).
        /// Cho phép tùy biến số lượng tàu, vị trí Slot Offset, Archetype và phản ứng khi mất sếp (0 GC Alloc).
        /// </summary>
        public void InitializeFormation(
            FormationPattern pattern,
            VirtualRailAnchor railAnchor,
            Vector3 anchorStartPosition,
            List<EnemyMovement> assignedEnemies,
            float overrideAssemblyTime = -1f)
        {
            InitializeFormation(pattern, railAnchor, anchorStartPosition, assignedEnemies, FormationCustomizationParams.Default, overrideAssemblyTime);
        }

        public void InitializeFormation(
            FormationPattern pattern,
            VirtualRailAnchor railAnchor,
            Vector3 anchorStartPosition,
            List<EnemyMovement> assignedEnemies,
            FormationCustomizationParams customParams,
            float overrideAssemblyTime = -1f)
        {
            if (pattern == null || pattern.slots == null || pattern.slots.Length == 0)
            {
                InitializeFormation(FormationType.WedgeV, railAnchor, anchorStartPosition, assignedEnemies, 1.2f);
                return;
            }

            _activePattern = pattern;
            _activeCustomParams = customParams;
            _railAnchor = railAnchor != null ? railAnchor : VirtualRailAnchor.Instance;
            _assemblyTime = overrideAssemblyTime >= 0f ? overrideAssemblyTime : pattern.assemblyTime;
            _swaySpeed = pattern.swaySpeed;
            _swayAmplitudeX = pattern.swayAmplitudeX;
            _bankingAngle = pattern.bankingAngle;
            _maxFormationLifetime = pattern.maxLifetime > 2f ? pattern.maxLifetime : 14f;
            _lifetimeTimer = 0f;
            _isLeaderKilled = false;
            _isMorphing = false;
            _morphTimer = 0f;
            _targetMorphPattern = null;
            _hasMorphed = false;
            _isExiting = false;
            _exitTimer = 0f;
            _activeWingmen.Clear();
            _leader = null;

            _baseRayPosition = anchorStartPosition;
            transform.position = anchorStartPosition;
            transform.rotation = Quaternion.LookRotation(-_railAnchor.ForwardTangent);

            int countToAssign = Mathf.Min(pattern.slots.Length, assignedEnemies.Count);
            FormationEntryStyle entryStyle = customParams.entryStyleOverride.HasValue
                ? customParams.entryStyleOverride.Value
                : pattern.entryStyle;

            Vector3 railUp = _railAnchor.transform.up;
            Vector3 railRight = _railAnchor.transform.right;
            Vector3 railFwd = _railAnchor.ForwardTangent;

            for (int i = 0; i < countToAssign; i++)
            {
                EnemyMovement enemy = assignedEnemies[i];
                if (enemy == null) continue;

                enemy.ResetState();
                var fsm = enemy.GetComponent<EnemyLifecycleFSM>();
                if (fsm != null)
                {
                    fsm.ResetState();
                    fsm.enabled = false;
                }

                var slotDef = pattern.slots[i];
                var wingman = enemy.GetComponent<FormationWingman>();
                if (wingman == null) wingman = enemy.gameObject.AddComponent<FormationWingman>();
                wingman.enabled = true;

                bool isLeader = slotDef.isLeader || (i == 0);
                if (isLeader) _leader = wingman;

                float scale = slotDef.scaleMultiplier > 0.01f ? slotDef.scaleMultiplier : 1.0f;
                enemy.transform.localScale = Vector3.one * scale;

                // Áp dụng Archetype chỉ định cho slot hoặc override theo DDA
                EnemyArchetype targetArch = (customParams.archetypeOverride.HasValue && !isLeader)
                    ? customParams.archetypeOverride.Value
                    : slotDef.preferredArchetype;

                if (targetArch != EnemyArchetype.Scout || isLeader)
                {
                    enemy.ApplyArchetype(EnemyData.CreateDefaultPreset(targetArch));
                }

                // Điểm xuất phát tính theo FormationEntryStyle
                float side = (i % 2 == 0 ? 1f : -1f);
                Vector3 spawnOffset;

                switch (entryStyle)
                {
                    case FormationEntryStyle.OverheadDive:
                        // Bổ nhào từ trần mây +Y (cách mặt đất/ray 28-35m)
                        spawnOffset = railUp * (28f + (i * 2.5f)) + railRight * (side * (8f + i * 3f)) + railFwd * (18f + i * 4f);
                        break;

                    case FormationEntryStyle.RearFlyby:
                        // Từ sau lưng người chơi phóng vượt lên
                        spawnOffset = -railFwd * (45f + i * 5f) + railRight * (side * (8f + i * 2f)) - railUp * 3f;
                        break;

                    case FormationEntryStyle.FrontalHorizon:
                        // Từ chân trời xa xôi phía trước
                        spawnOffset = railFwd * (50f + i * 8f) + railRight * (side * 4f) + railUp * (i % 2 == 0 ? 3f : -3f);
                        break;

                    case FormationEntryStyle.FlankIn:
                    default:
                        // Xuất phát từ 2 bên sườn
                        spawnOffset = railRight * (side * UnityEngine.Random.Range(20f, 35f))
                            + railUp * UnityEngine.Random.Range(-5f, 15f)
                            + railFwd * UnityEngine.Random.Range(10f, 30f);
                        break;
                }

                Vector3 shipSpawnPos = anchorStartPosition + spawnOffset;

                wingman.InitializeSlot(i, isLeader, this, slotDef.localOffset, shipSpawnPos, _assemblyTime, entryStyle);
                _activeWingmen.Add(wingman);

                enemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : _railAnchor.transform);
                enemy.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
            Debug.Log($"<color=cyan><b>[FORMATION ANCHOR]</b></color> Khởi tạo đội hình DATA-DRIVEN [{pattern.patternName}] với {countToAssign} tàu chiến! Pha 1 Ráp nối Bézier bắt đầu.");
        }

        /// <summary>
        /// Kích hoạt cơ chế biến hình chuyển đổi từ đội hình hiện tại sang đội hình mới (0 GC Alloc).
        /// Các tàu cánh còn sống lướt chuyển vị trí theo đường cong né nhau trên trục sâu Z (Depth Separation Curve).
        /// </summary>
        public void StartMorphing(FormationPattern targetPattern, float duration)
        {
            if (targetPattern == null || _activeWingmen.Count == 0 || _isMorphing) return;
            if (targetPattern.slots == null || targetPattern.slots.Length == 0) return;

            _targetMorphPattern = targetPattern;
            _morphDuration = Mathf.Max(0.5f, duration);
            _morphTimer = 0f;
            _isMorphing = true;

            int shipCount = _activeWingmen.Count;
            for (int i = 0; i < shipCount; i++)
            {
                FormationWingman wingman = _activeWingmen[i];
                if (wingman == null) continue;

                // Tàu i gán vào slot i của đội hình mới (kẹp an toàn nếu targetPattern ít slot hơn)
                int targetSlotIdx = Mathf.Min(i, targetPattern.slots.Length - 1);
                Vector3 targetOffset = targetPattern.slots[targetSlotIdx].localOffset;
                wingman.StartMorph(wingman.SlotLocalOffset, targetOffset, targetSlotIdx);
            }

            Debug.Log($"<color=yellow><b>[FORMATION MORPHING]</b></color> Bắt đầu biến hình phi đội từ [{(_activePattern != null ? _activePattern.patternName : "Default")}] sang [{targetPattern.patternName}] trong {_morphDuration}s!");
        }

        private void Update()
        {
            if (_railAnchor == null) return;

            _lifetimeTimer += Time.deltaTime;

            // 1. Tịnh tiến dọc theo đường ray với vận tốc tiếp cận tương đối (Closing Velocity)
            float railSpeed = _railAnchor.CurrentSpeed;
            bool useSync = _railAnchor.config == null || _railAnchor.config.enableEnemyRailSync;

            float closingSpeed = 0f;
            if (useSync)
            {
                Vector3 toFormation = _baseRayPosition - _railAnchor.transform.position;
                float distToPlayer = Vector3.Dot(toFormation, _railAnchor.ForwardTangent);
                float targetHoldDist = (_activePattern != null) ? _activePattern.holdDistance : ((_railAnchor.config != null) ? _railAnchor.config.formationHoldDistance : 65f);
                float maxClosingSpeed = (_activePattern != null) ? _activePattern.maxClosingSpeed : ((_railAnchor.config != null) ? _railAnchor.config.formationClosingSpeed : 25f);

                if (distToPlayer > targetHoldDist)
                {
                    // Giảm tốc mượt mà khi tiến sát cự ly ghìm
                    closingSpeed = Mathf.Min(maxClosingSpeed, (distToPlayer - targetHoldDist) * 1.5f);
                }
                else
                {
                    // Đã đạt cự ly ghìm giao chiến: khóa đồng tốc hoàn toàn với ray (closingSpeed = 0)
                    closingSpeed = 0f;
                }
            }
            else
            {
                closingSpeed = (_railAnchor.config != null) ? _railAnchor.config.formationClosingSpeed : 35f;
            }

            float actualSpeed = Mathf.Max(20f, railSpeed - closingSpeed);
            _baseRayPosition += _railAnchor.ForwardTangent * (actualSpeed * Time.deltaTime);

            // 2. Chuyển động uốn lượn / quỹ đạo độc lập thông qua FormationMotionEvaluator (0 GC Alloc)
            FormationMotionProfile profile = _activeCustomParams.motionProfileOverride != null 
                ? _activeCustomParams.motionProfileOverride 
                : ((_activePattern != null) ? _activePattern.motionProfile : null);
            FormationMotionEvaluator.Evaluate(
                profile,
                _lifetimeTimer,
                _swaySpeed,
                _swayAmplitudeX,
                _bankingAngle,
                _railAnchor.transform.right,
                _railAnchor.transform.up,
                _railAnchor.ForwardTangent,
                out Vector3 localDisplacement,
                out Quaternion targetRot
            );

            transform.position = _baseRayPosition + localDisplacement;

            // Xử lý góc xoay theo FormationAttitudeMode (Tư thế Mũi & Cánh)
            FormationAttitudeMode attitudeMode = (_activePattern != null) ? _activePattern.attitudeMode : FormationAttitudeMode.StabilizedBoresight;
            Quaternion desiredRotation;

            switch (attitudeMode)
            {
                case FormationAttitudeMode.StabilizedBoresight:
                    // Mũi và cánh giữ nguyên tuyệt đối: Khóa cứng hướng đối đầu trực diện đường ray, Roll = 0, Pitch = 0
                    desiredRotation = Quaternion.LookRotation(-_railAnchor.ForwardTangent, _railAnchor.transform.up);
                    break;

                case FormationAttitudeMode.TargetLockPlayer:
                    // Mũi tàu khóa ngắm bám theo vị trí người chơi
                    Transform playerT = VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : _railAnchor.transform;
                    Vector3 toPlayer = (playerT != null) ? (playerT.position - transform.position).normalized : -_railAnchor.ForwardTangent;
                    desiredRotation = Quaternion.LookRotation(toPlayer, _railAnchor.transform.up);
                    break;

                case FormationAttitudeMode.KinematicBanking:
                default:
                    // Dùng góc xoay nghiêng cánh và ngóc/chúc mũi từ Evaluator
                    desiredRotation = targetRot;
                    break;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 8f * Time.deltaTime);

            // 3. Tự động kích hoạt biến hình đội hình (Dynamic Formation Morphing)
            FormationPattern nextPattern = _activeCustomParams.morphTargetOverride != null
                ? _activeCustomParams.morphTargetOverride
                : ((_activePattern != null) ? _activePattern.nextMorphPattern : null);

            float delay = _activeCustomParams.morphDelay > 0.1f
                ? _activeCustomParams.morphDelay
                : ((_activePattern != null) ? _activePattern.morphDelay : 4.0f);

            float dur = _activeCustomParams.morphDuration > 0.1f
                ? _activeCustomParams.morphDuration
                : ((_activePattern != null) ? _activePattern.morphDuration : 1.8f);

            if (nextPattern != null && !_hasMorphed && !_isMorphing)
            {
                if (_lifetimeTimer >= delay)
                {
                    StartMorphing(nextPattern, dur);
                }
            }

            // Xử lý tiến trình biến hình (Morphing Transition với Depth Separation Curve né nhau)
            if (_isMorphing)
            {
                _morphTimer += Time.deltaTime;
                float u = Mathf.Clamp01(_morphTimer / _morphDuration);
                float s = u * u * (3f - 2f * u); // SmoothStep

                // Parabol né nhau trên trục sâu Z (Depth Separation Curve): h(u) = 4 * u * (1 - u)
                float depthArch = 4f * u * (1f - u);

                for (int i = 0; i < _activeWingmen.Count; i++)
                {
                    FormationWingman wingman = _activeWingmen[i];
                    if (wingman == null || !wingman.gameObject.activeSelf) continue;

                    wingman.UpdateMorph(s, depthArch);
                }

                if (u >= 1.0f)
                {
                    _isMorphing = false;
                    _hasMorphed = true;
                    _activePattern = _targetMorphPattern;

                    // Hoàn tất biến hình và cập nhật slot chính thức cho các wingman
                    for (int i = 0; i < _activeWingmen.Count; i++)
                    {
                        if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                        {
                            _activeWingmen[i].CompleteMorph();
                        }
                    }

                    Debug.Log($"<color=green><b>[FORMATION MORPHING COMPLETE]</b></color> Hoàn tất biến hình phi đội sang [{_targetMorphPattern.patternName}]!");
                }
            }

            // 4. Xử lý thoát ly rời đi khi hết thời gian tồn tại hoặc bị người chơi vượt qua
            if (_isExiting)
            {
                _exitTimer += Time.deltaTime;
                if (_exitTimer >= _exitDuration || _activeWingmen.Count == 0)
                {
                    Recycle();
                }
                return;
            }

            if (_lifetimeTimer >= _maxFormationLifetime || _baseRayPosition.z < _railAnchor.transform.position.z - 40f)
            {
                FormationExitStyle exitStyle = (_activePattern != null) ? _activePattern.exitStyle : FormationExitStyle.ClimbOut;
                if (exitStyle != FormationExitStyle.InstantRecycle && _activeWingmen.Count > 0)
                {
                    StartExitSequence(exitStyle);
                }
                else
                {
                    Recycle();
                }
            }
        }

        /// <summary>
        /// Xử lý thông điệp khi một thành viên trong phi đội bị bắn hạ hoặc thu hồi.
        /// </summary>
        public void NotifyShipDestroyed(FormationWingman ship)
        {
            if (ship == null) return;

            _activeWingmen.Remove(ship);

            if (ship.IsLeader && !_isLeaderKilled)
            {
                _isLeaderKilled = true;
                _isMorphing = false;
                LeaderDeathReaction reaction = _activeCustomParams.reactionOverride.HasValue
                    ? _activeCustomParams.reactionOverride.Value
                    : ((_activePattern != null) ? _activePattern.leaderDeathReaction : LeaderDeathReaction.BreakShieldAndEngage);

                if (reaction == LeaderDeathReaction.MaintainFormation)
                {
                    Debug.Log($"<color=cyan><b>[LEADER KILLED - MAINTAIN FORMATION]</b></color> Chỉ huy Slot 0 đã bị tiêu diệt! Phi đội gồm {_activeWingmen.Count} tàu còn lại VẪN GIỮ NGUYÊN CỰ LY ĐỘI HÌNH VÀ TIẾP TỤC CHIẾN ĐẤU!");
                    for (int i = 0; i < _activeWingmen.Count; i++)
                    {
                        if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                        {
                            _activeWingmen[i].OnLeaderDestroyedMaintainFormation();
                        }
                    }
                }
                else
                {
                    TriggerLeaderDestroyed(ship.transform.position);
                    return;
                }
            }
            else
            {
                Debug.Log($"[Formation] Tàu cánh Slot {ship.SlotIndex} bị hạ gục. Đội hình vẫn giữ nguyên cự ly!");
            }

            if (_activeWingmen.Count == 0)
            {
                Recycle();
            }
        }

        /// <summary>
        /// Kích hoạt cơ chế phản ứng toàn đội khi mất Tàu Chỉ Huy theo LeaderDeathReaction.
        /// </summary>
        private void TriggerLeaderDestroyed(Vector3 leaderPos)
        {
            LeaderDeathReaction reaction = _activeCustomParams.reactionOverride.HasValue
                ? _activeCustomParams.reactionOverride.Value
                : ((_activePattern != null) ? _activePattern.leaderDeathReaction : LeaderDeathReaction.BreakShieldAndEngage);
            Debug.Log($"<color=red><b>[LEADER KILLED!]</b></color> Chỉ huy Slot 0 đã bị tiêu diệt! Toàn bộ phi đội thực hiện phản ứng: {reaction}!");

            Vector3 railFwd = (_railAnchor != null) ? _railAnchor.ForwardTangent : Vector3.forward;
            float railSpeed = (_railAnchor != null) ? _railAnchor.CurrentSpeed : 40f;
            Transform playerTarget = VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : (_railAnchor != null ? _railAnchor.transform : null);

            for (int i = 0; i < _activeWingmen.Count; i++)
            {
                if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                {
                    switch (reaction)
                    {
                        case LeaderDeathReaction.BreakShieldAndEngage:
                            _activeWingmen[i].TriggerBreakShieldAndEngage(playerTarget);
                            break;
                        case LeaderDeathReaction.RetreatWarp:
                            _activeWingmen[i].TriggerRetreatWarp(railFwd, railSpeed);
                            break;
                        case LeaderDeathReaction.KamikazeCharge:
                            _activeWingmen[i].TriggerKamikazeCharge(playerTarget);
                            break;
                        case LeaderDeathReaction.PanicScatter:
                        default:
                            _activeWingmen[i].TriggerPanicScatter(leaderPos, railFwd, railSpeed);
                            break;
                    }
                }
            }

            _activeWingmen.Clear();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Kích hoạt chuỗi động tác thoát ly điện ảnh cho cả phi đội khi hết thời gian tồn tại (0 GC Alloc).
        /// </summary>
        public void StartExitSequence(FormationExitStyle exitStyle)
        {
            if (_isExiting) return;

            _isExiting = true;
            _exitTimer = 0f;
            _exitDuration = (_activePattern != null) ? _activePattern.exitDuration : 1.8f;
            _isMorphing = false;

            Vector3 railFwd = (_railAnchor != null) ? _railAnchor.ForwardTangent : Vector3.forward;
            Vector3 railRight = (_railAnchor != null) ? _railAnchor.transform.right : Vector3.right;
            Vector3 railUp = (_railAnchor != null) ? _railAnchor.transform.up : Vector3.up;
            float railSpeed = (_railAnchor != null) ? _railAnchor.CurrentSpeed : 40f;

            Debug.Log($"<color=cyan><b>[FORMATION EXIT]</b></color> Phi đội [{(_activePattern != null ? _activePattern.patternName : _formationType.ToString())}] bắt đầu thoát ly điện ảnh ({exitStyle})!");

            for (int i = 0; i < _activeWingmen.Count; i++)
            {
                if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                {
                    _activeWingmen[i].TriggerExit(exitStyle, _exitDuration, railFwd, railRight, railUp, railSpeed);
                }
            }
        }

        /// <summary>
        /// Ép buộc đội hình giải tán và rút lui ngay lập tức để giải phóng sàn đấu cho wave sau (0 GC Alloc).
        /// </summary>
        public void ForceRetreat()
        {
            Vector3 railFwd = (_railAnchor != null) ? _railAnchor.ForwardTangent : Vector3.forward;
            float railSpeed = (_railAnchor != null) ? _railAnchor.CurrentSpeed : 40f;

            for (int i = 0; i < _activeWingmen.Count; i++)
            {
                if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                {
                    _activeWingmen[i].TriggerRetreatWarp(railFwd, railSpeed);
                }
            }
            _activeWingmen.Clear();
            gameObject.SetActive(false);
        }

        public void Recycle()
        {
            _isMorphing = false;
            _isExiting = false;
            _exitTimer = 0f;
            for (int i = 0; i < _activeWingmen.Count; i++)
            {
                if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                {
                    _activeWingmen[i].Recycle();
                }
            }
            _activeWingmen.Clear();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Dịch chuyển hệ quy chiếu của Đội hình ảo trong quá trình Tái định vị thế giới (Task 4.3).
        /// Triệt tiêu 100% lỗi bay lệch hoặc mất tích quái vật khi anchor tịnh tiến 4000m về gốc.
        /// </summary>
        public void ShiftOrigin(Vector3 shift)
        {
            _baseRayPosition -= shift;
            transform.position -= shift;
            for (int i = 0; i < _activeWingmen.Count; i++)
            {
                if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                {
                    _activeWingmen[i].ShiftOrigin(shift);
                }
            }
        }
    }
}
