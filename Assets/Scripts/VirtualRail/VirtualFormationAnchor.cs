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
        private float _lifetimeTimer = 0f;
        private float _maxFormationLifetime = 12f;
        private bool _isLeaderKilled = false;

        public FormationType Type => _formationType;
        public FormationPattern ActivePattern => _activePattern;
        public bool IsActive => gameObject.activeSelf;
        public bool HasActiveLeader => _leader != null && !_isLeaderKilled && _leader.gameObject.activeSelf;
        public FormationWingman Leader => _leader;

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
            _activeWingmen.Clear();
            _leader = null;

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

                wingman.InitializeSlot(i, isLeader, this, slotOffsets[i], shipSpawnPos, _assemblyTime);
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
            List<EnemyMovement> assignedEnemies)
        {
            if (pattern == null || pattern.slots == null || pattern.slots.Length == 0)
            {
                InitializeFormation(FormationType.WedgeV, railAnchor, anchorStartPosition, assignedEnemies, 1.2f);
                return;
            }

            _activePattern = pattern;
            _railAnchor = railAnchor != null ? railAnchor : VirtualRailAnchor.Instance;
            _assemblyTime = pattern.assemblyTime;
            _swaySpeed = pattern.swaySpeed;
            _swayAmplitudeX = pattern.swayAmplitudeX;
            _bankingAngle = pattern.bankingAngle;
            _maxFormationLifetime = pattern.maxLifetime > 2f ? pattern.maxLifetime : 14f;
            _lifetimeTimer = 0f;
            _isLeaderKilled = false;
            _activeWingmen.Clear();
            _leader = null;

            transform.position = anchorStartPosition;
            transform.rotation = Quaternion.LookRotation(-_railAnchor.ForwardTangent);

            int countToAssign = Mathf.Min(pattern.slots.Length, assignedEnemies.Count);

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

                // Áp dụng Archetype chỉ định cho slot nếu có
                if (slotDef.preferredArchetype != EnemyArchetype.Scout || isLeader)
                {
                    enemy.ApplyArchetype(EnemyData.CreateDefaultPreset(slotDef.preferredArchetype));
                }

                // Điểm xuất phát ngẫu nhiên ngoài màn hình cho từng tàu (chân trời 120m - 140m)
                Vector3 spawnOffset = new Vector3(
                    (i % 2 == 0 ? 1f : -1f) * UnityEngine.Random.Range(20f, 35f),
                    UnityEngine.Random.Range(-5f, 15f),
                    UnityEngine.Random.Range(10f, 30f)
                );
                Vector3 shipSpawnPos = anchorStartPosition + spawnOffset;

                wingman.InitializeSlot(i, isLeader, this, slotDef.localOffset, shipSpawnPos, _assemblyTime);
                _activeWingmen.Add(wingman);

                enemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : _railAnchor.transform);
                enemy.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
            Debug.Log($"<color=cyan><b>[FORMATION ANCHOR]</b></color> Khởi tạo đội hình DATA-DRIVEN [{pattern.patternName}] với {countToAssign} tàu chiến! Pha 1 Ráp nối Bézier bắt đầu.");
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
                Vector3 toFormation = transform.position - _railAnchor.transform.position;
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
            transform.position += _railAnchor.ForwardTangent * (actualSpeed * Time.deltaTime);

            // 2. Chuyển động uốn lượn hình sin và nghiêng cánh (Banking Roll)
            float sway = Mathf.Sin(_lifetimeTimer * _swaySpeed) * _swayAmplitudeX;
            Vector3 swayOffset = _railAnchor.transform.right * (Mathf.Cos(_lifetimeTimer * _swaySpeed) * _swayAmplitudeX * 0.5f * Time.deltaTime);
            transform.position += swayOffset;

            // Nghiêng cánh theo hướng lượn
            float rollAngle = -Mathf.Sin(_lifetimeTimer * _swaySpeed) * _bankingAngle;
            Quaternion targetRot = Quaternion.LookRotation(-_railAnchor.ForwardTangent, _railAnchor.transform.up) * Quaternion.Euler(0, 0, rollAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * Time.deltaTime);

            // 3. Tự động thu hồi khi hết thời gian tồn tại hoặc bị người chơi vượt qua
            if (_lifetimeTimer >= _maxFormationLifetime || transform.position.z < _railAnchor.transform.position.z - 40f)
            {
                Recycle();
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
                LeaderDeathReaction reaction = (_activePattern != null) ? _activePattern.leaderDeathReaction : LeaderDeathReaction.BreakShieldAndEngage;

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
            LeaderDeathReaction reaction = (_activePattern != null) ? _activePattern.leaderDeathReaction : LeaderDeathReaction.BreakShieldAndEngage;
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
    }
}
