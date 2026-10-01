using System;
using UnityEngine;

namespace VirtualRail
{
    public enum LeaderDeathReaction
    {
        PanicScatter = 0,   // Hoảng loạn phân rã ra hai biên màn hình (chuẩn Star Fox 64)
        RetreatWarp = 1,    // Tăng tốc thoát ly khẩn cấp biến mất khỏi màn chơi
        KamikazeCharge = 2   // Cảm tử lao thẳng vào vị trí người chơi
    }

    [Serializable]
    public struct FormationSlotDefinition
    {
        [Tooltip("Tên gợi nhớ cho slot này (VD: Leader, Left Wing 1, Right Wing 1)")]
        public string slotName;

        [Tooltip("Tọa độ lệch tương đối (X, Y, Z) so với tâm đội hình")]
        public Vector3 localOffset;

        [Tooltip("Loại quái ưu tiên cho vị trí này")]
        public EnemyArchetype preferredArchetype;

        [Tooltip("Đánh dấu vị trí này là Tàu Chỉ Huy (Slot 0)")]
        public bool isLeader;

        [Range(0.5f, 2.5f)]
        [Tooltip("Hệ số phóng to kích thước của tàu tại slot này (VD: 1.35 cho chỉ huy)")]
        public float scaleMultiplier;

        [Tooltip("Prefab tùy biến riêng cho slot này nếu muốn khác biệt")]
        public GameObject customPrefabOverride;
    }

    /// <summary>
    /// ScriptableObject định nghĩa mẫu đội hình bay (Data-Driven Formation Pattern).
    /// Cho phép Game Designer thiết kế hình dáng, tọa độ và hành vi phi đội trực quan trong Unity Editor
    /// mà không cần viết thêm bất kỳ dòng code C# nào.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFormationPattern", menuName = "Virtual Rail/Formation Pattern", order = 150)]
    public class FormationPattern : ScriptableObject
    {
        [Header("Formation Identity")]
        public string patternName = "New Formation";

        [Header("Slot Configuration")]
        [Tooltip("Danh sách vị trí các tàu trong đội hình")]
        public FormationSlotDefinition[] slots = new FormationSlotDefinition[]
        {
            new FormationSlotDefinition { slotName = "Leader", localOffset = Vector3.zero, preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.35f },
            new FormationSlotDefinition { slotName = "Left Wing", localOffset = new Vector3(-8f, 0f, -8f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
            new FormationSlotDefinition { slotName = "Right Wing", localOffset = new Vector3(8f, 0f, -8f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f }
        };

        [Header("Flight Dynamics")]
        [Range(0.5f, 3.0f)]
        [Tooltip("Thời gian các tàu từ ngoài bay vào ráp nối đội hình theo đường cong Bézier (giây)")]
        public float assemblyTime = 1.2f;

        [Range(0.2f, 4.0f)]
        [Tooltip("Tần số lượn sóng ngang hình sin của cả đội hình")]
        public float swaySpeed = 1.5f;

        [Range(0f, 30f)]
        [Tooltip("Biên độ lượn ngang (mét)")]
        public float swayAmplitudeX = 12f;

        [Range(0f, 60f)]
        [Tooltip("Góc nghiêng cánh khi lượn trái/phải (độ)")]
        public float bankingAngle = 25f;

        [Tooltip("Cự ly ghìm giao chiến phía trước tàu người chơi (mét)")]
        public float holdDistance = 65f;

        [Tooltip("Vận tốc tiếp cận ban đầu khi áp sát từ xa (m/s)")]
        public float maxClosingSpeed = 25f;

        [Tooltip("Thời gian tồn tại tối đa của đội hình trước khi tự giải tán (giây)")]
        public float maxLifetime = 14f;

        [Header("Tactical Behavior")]
        [Tooltip("Phản ứng của phi đội cánh khi Tàu Chỉ Huy bị bắn hạ")]
        public LeaderDeathReaction leaderDeathReaction = LeaderDeathReaction.PanicScatter;

        public int ShipCount => slots != null ? slots.Length : 0;

        /// <summary>
        /// Trích xuất mảng Vector3 offset tĩnh phục vụ cho VirtualFormationAnchor (0 GC Alloc).
        /// </summary>
        public Vector3[] GetOffsetsArray()
        {
            if (slots == null || slots.Length == 0) return Array.Empty<Vector3>();
            Vector3[] offsets = new Vector3[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                offsets[i] = slots[i].localOffset;
            }
            return offsets;
        }
    }
}
