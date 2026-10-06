using System;
using UnityEngine;

namespace VirtualRail
{
    public enum LeaderDeathReaction
    {
        BreakShieldAndEngage = 0, // Mất khiên bảo vệ, thoát đội hình và tiếp tục chiến đấu tự do (chuẩn arcade)
        PanicScatter = 1,         // Hoảng loạn phân rã ra hai biên màn hình
        RetreatWarp = 2,          // Tăng tốc thoát ly khẩn cấp biến mất khỏi màn chơi
        KamikazeCharge = 3,       // Cảm tử lao thẳng vào vị trí người chơi
        MaintainFormation = 4     // Giữ nguyên đội hình, các tàu còn lại tiếp tục bay theo đội hình cho đến khi bị tiêu diệt hết hoặc hết thời gian
    }

    public enum FormationEntryStyle
    {
        FlankIn = 0,         // Từ 2 bên sườn bay vào (cơ bản)
        OverheadDive = 1,    // Từ tầng mây +Y bổ nhào dốc xuống, lượn phẳng rồi dàn đội hình
        RearFlyby = 2,       // Từ phía sau lưng người chơi phóng vọt lên trước mặt rồi dàn đội hình
        FrontalHorizon = 3   // Từ chân trời xa xôi phía trước bay thẳng tới rồi xòe ra
    }

    public enum FormationExitStyle
    {
        ClimbOut = 0,        // Cả đội ngóc mũi 45 độ, tăng tốc xé gió vút lên nóc màn hình
        SplitFlyPast = 1,    // Đội hình tẽ đôi sang 2 cánh, lộn vòng lướt qua phía sau tàu người chơi
        DiveUnder = 2,       // Cắm mũi chúc xuống đáy sâu thoát ly
        InstantRecycle = 3   // Tắt ngấm trực tiếp (cơ chế cũ - legacy fallback)
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
        [Tooltip("Hệ số kích thước của tàu tại slot này (1.0 = kích thước chuẩn)")]
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
            new FormationSlotDefinition { slotName = "Leader", localOffset = Vector3.zero, preferredArchetype = EnemyArchetype.SquadLeader, isLeader = true, scaleMultiplier = 1.0f },
            new FormationSlotDefinition { slotName = "Left Wing", localOffset = new Vector3(-8f, 0f, 0f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f },
            new FormationSlotDefinition { slotName = "Right Wing", localOffset = new Vector3(8f, 0f, 0f), preferredArchetype = EnemyArchetype.ShieldBearer, isLeader = false, scaleMultiplier = 1.0f }
        };

        [Header("Flight Dynamics & Motion Profile")]
        [Tooltip("Hồ sơ chuyển động quỹ đạo độc lập. Kéo thả Profile vào đây để tùy biến kiểu bay (Xoắn ốc, Số 8, Nhấp nhô...). Nếu để trống, hệ thống tự động dùng thông số mặc định bên dưới.")]
        public FormationMotionProfile motionProfile;

        [Range(0.5f, 3.0f)]
        [Tooltip("Thời gian các tàu từ ngoài bay vào ráp nối đội hình theo đường cong Bézier (giây)")]
        public float assemblyTime = 1.2f;

        [Header("Cinematic Entry & Exit (Bổ Nhào & Thoát Ly Điện Ảnh)")]
        [Tooltip("Kiểu xuất hiện ráp nối đội hình")]
        public FormationEntryStyle entryStyle = FormationEntryStyle.FlankIn;

        [Tooltip("Kiểu thoát ly rời đi khi hết thời gian tồn tại thay vì biến mất đột ngột")]
        public FormationExitStyle exitStyle = FormationExitStyle.ClimbOut;

        [Range(0.8f, 3.5f)]
        [Tooltip("Thời gian thực hiện động tác thoát ly rời đi (giây)")]
        public float exitDuration = 1.8f;

        [Header("Legacy Fallback Parameters (Khi chưa gán Motion Profile)")]
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

        [Header("Dynamic Formation Morphing (Biến hình đội hình)")]
        [Tooltip("Đội hình tiếp theo muốn phi đội tự động biến hình lướt chuyển sang. Nếu để null, đội hình giữ nguyên dạng ban đầu.")]
        public FormationPattern nextMorphPattern = null;

        [Range(1.0f, 15.0f)]
        [Tooltip("Thời gian phi đội bay ở dạng cũ trước khi bắt đầu biến hình chuyển đội hình (giây)")]
        public float morphDelay = 4.0f;

        [Range(0.5f, 4.0f)]
        [Tooltip("Thời gian các tàu lướt từ slot cũ sang slot mới của đội hình tiếp theo (giây)")]
        public float morphDuration = 1.8f;

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
