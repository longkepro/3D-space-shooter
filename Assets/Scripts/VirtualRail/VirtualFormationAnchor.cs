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
        private float _lifetimeTimer = 0f;
        private float _maxFormationLifetime = 12f;
        private bool _isLeaderKilled = false;

        public FormationType Type => _formationType;
        public bool IsActive => gameObject.activeSelf;

        public static Vector3[] GetSlotOffsets(FormationType type)
        {
            switch (type)
            {
                case FormationType.WedgeV:
                    return new Vector3[]
                    {
                        new Vector3(0f, 0f, 0f),      // Slot 0: Leader
                        new Vector3(-7f, 0f, -8f),    // Slot 1: Left Wing 1
                        new Vector3(7f, 0f, -8f),     // Slot 2: Right Wing 1
                        new Vector3(-14f, 0f, -16f),  // Slot 3: Left Wing 2
                        new Vector3(14f, 0f, -16f)    // Slot 4: Right Wing 2
                    };

                case FormationType.EchelonLine:
                    return new Vector3[]
                    {
                        new Vector3(0f, 0f, 0f),      // Slot 0: Leader
                        new Vector3(-10f, 0f, -4f),   // Slot 1: Left Wing
                        new Vector3(10f, 0f, -4f)     // Slot 2: Right Wing
                    };

                case FormationType.SpiralStream:
                default:
                    return new Vector3[]
                    {
                        new Vector3(0f, 0f, 0f),
                        new Vector3(6f, 3f, -10f),
                        new Vector3(0f, 6f, -20f),
                        new Vector3(-6f, 3f, -30f),
                        new Vector3(-6f, -3f, -40f)
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

                var wingman = enemy.GetComponent<FormationWingman>();
                if (wingman == null) wingman = enemy.gameObject.AddComponent<FormationWingman>();

                bool isLeader = (i == 0);
                if (isLeader) _leader = wingman;

                // Tạo điểm xuất phát ngẫu nhiên ngoài màn hình cho từng tàu
                Vector3 spawnOffset = new Vector3(
                    (i % 2 == 0 ? 1f : -1f) * UnityEngine.Random.Range(20f, 40f),
                    UnityEngine.Random.Range(-10f, 25f),
                    UnityEngine.Random.Range(30f, 80f)
                );
                Vector3 shipSpawnPos = anchorStartPosition + spawnOffset;

                wingman.InitializeSlot(i, isLeader, this, slotOffsets[i], shipSpawnPos, _assemblyTime);
                _activeWingmen.Add(wingman);

                enemy.ResetState();
                enemy.SetTarget(VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform : _railAnchor.transform);
                enemy.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
            Debug.Log($"<color=cyan><b>[FORMATION ANCHOR]</b></color> Khởi tạo đội hình {_formationType} với {countToAssign} tàu chiến! Pha 1 Ráp nối Bézier bắt đầu.");
        }

        private void Update()
        {
            if (_railAnchor == null) return;

            _lifetimeTimer += Time.deltaTime;

            // 1. Tịnh tiến dọc theo đường ray (đồng tốc với tiến trình ray)
            float speed = _railAnchor.CurrentSpeed;
            transform.position += _railAnchor.ForwardTangent * (speed * Time.deltaTime);

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
                TriggerLeaderDestroyed(ship.transform.position);
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
        /// Kích hoạt cơ chế hoảng loạn phân rã toàn đội khi mất Tàu Chỉ Huy (TDD Phần III.3).
        /// </summary>
        private void TriggerLeaderDestroyed(Vector3 leaderPos)
        {
            Debug.Log("<color=red><b>[LEADER KILLED!]</b></color> Chỉ huy Slot 0 đã bị tiêu diệt! Toàn bộ phi đội cánh rơi vào trạng thái hoảng loạn PANIC SCATTER!");

            Vector3 railFwd = (_railAnchor != null) ? _railAnchor.ForwardTangent : Vector3.forward;
            float railSpeed = (_railAnchor != null) ? _railAnchor.CurrentSpeed : 40f;

            for (int i = 0; i < _activeWingmen.Count; i++)
            {
                if (_activeWingmen[i] != null && _activeWingmen[i].gameObject.activeSelf)
                {
                    _activeWingmen[i].TriggerPanicScatter(leaderPos, railFwd, railSpeed);
                }
            }

            _activeWingmen.Clear();
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
