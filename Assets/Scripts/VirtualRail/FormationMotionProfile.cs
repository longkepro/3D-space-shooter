using System;
using UnityEngine;

namespace VirtualRail
{
    public enum FormationMotionType
    {
        [Tooltip("Lượn sóng ngang hình Sin kinh điển (trục X)")]
        HorizontalSinusoid = 0,

        [Tooltip("Lượn sóng nhấp nhô trục đứng (trục Y)")]
        VerticalUndulation = 1,

        [Tooltip("Xoắn ốc cuộn tròn 3D quanh trục ray (trục X + Y quay tròn)")]
        CorkscrewRoll = 2,

        [Tooltip("Quỹ đạo số 8 vô cực (Lemniscate of Bernoulli)")]
        FigureEight = 3,

        [Tooltip("Dao động đa tần số 3D kết hợp (Lissajous Curve)")]
        Lissajous3D = 4,

        [Tooltip("Giữ vị trí ghìm chốt chặn cố định")]
        StationaryHold = 5
    }

    /// <summary>
    /// ScriptableObject định nghĩa Hồ Sơ Chuyển Động Độc Lập cho Đội Hình (Decoupled Formation Motion Profile).
    /// Áp dụng mẫu thiết kế Strategy Pattern: Tách rời hoàn toàn hình thái đội hình (Geometry) và động học bay (Motion Trajectory).
    /// Cho phép tái sử dụng 1 Profile cho hàng chục đội hình khác nhau, hoặc tùy biến đường bay 3D mượt mà (0 GC Alloc).
    /// </summary>
    [CreateAssetMenu(fileName = "NewMotionProfile", menuName = "Virtual Rail/Formation Motion Profile", order = 160)]
    public class FormationMotionProfile : ScriptableObject
    {
        [Header("Motion Profile Identity")]
        public string profileName = "Standard Motion Profile";

        [Header("Trajectory Archetype")]
        [Tooltip("Dạng quỹ đạo chuyển động chính")]
        public FormationMotionType motionType = FormationMotionType.HorizontalSinusoid;

        [Header("Spatial Amplitudes (Biên độ dao động mét)")]
        [Range(0f, 25f)]
        [Tooltip("Biên độ dao động ngang trục X (mét)")]
        public float amplitudeX = 12f;

        [Range(0f, 10f)]
        [Tooltip("Biên độ dao động đứng trục Y (mét) - Tự động được kẹp an toàn trong phạm vi nhìn thấy của Camera")]
        public float amplitudeY = 5f;

        [Header("Frequencies (Tần số dao động Hz)")]
        [Range(0.1f, 3.0f)]
        [Tooltip("Tần số dao động trục X (chu kỳ/giây)")]
        public float frequencyX = 1.2f;

        [Range(0.1f, 3.0f)]
        [Tooltip("Tần số dao động trục Y (chu kỳ/giây)")]
        public float frequencyY = 1.2f;

        [Header("Phase Offset (Độ lệch pha góc)")]
        [Range(0f, 360f)]
        [Tooltip("Độ lệch pha góc giữa trục X và Y (độ) - điều chỉnh độ nghiêng và hướng mở của elip hoặc hình số 8")]
        public float phaseShiftDeg = 90f;

        [Header("Centroid Orbit (Quay quanh tâm tàu chính)")]
        [Tooltip("Tốc độ tự quay tròn quanh Tàu Chỉ Huy (Slot 0) của toàn bộ các tàu cánh (độ/giây). 0 = Tắt, > 0 = Quay thuận chiều, < 0 = Quay ngược chiều.")]
        [Range(-180f, 180f)]
        public float centroidSpinSpeed = 0f;

        [Header("Angular Banking Dynamics (Nghiêng cánh & Ngóc đầu)")]
        [Range(0f, 60f)]
        [Tooltip("Góc nghiêng cánh tối đa theo vận tốc rẽ ngang (độ)")]
        public float maxRollAngle = 28f;

        [Range(0f, 45f)]
        [Tooltip("Góc chúc/ngóc mũi tối đa theo vận tốc đứng (độ)")]
        public float maxPitchAngle = 14f;

        [Range(1f, 15f)]
        [Tooltip("Tốc độ bám góc xoay nghiêng cánh (Slerp factor)")]
        public float rotationResponsiveness = 6.0f;

        [Header("Soft-Start Ramping (Làm mềm lúc ráp nối)")]
        [Tooltip("Bật cơ chế tăng dần biên độ lúc mới xuất hiện để tàu cánh ráp nối Bézier mượt mà")]
        public bool enableSoftStart = true;

        [Range(0.5f, 2.5f)]
        [Tooltip("Thời gian làm mềm biên độ (giây) tính từ lúc xuất kích")]
        public float softStartDuration = 1.2f;

        [Header("Deep Customization (Đồ thị AnimationCurve tự do)")]
        [Tooltip("Bật chế độ đọc đường cong tùy biến AnimationCurve thay thế hàm toán học")]
        public bool useCustomCurves = false;

        [Tooltip("Đường cong dao động trục X từ t = 0 đến 1 (Loop)")]
        public AnimationCurve customCurveX = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Tooltip("Đường cong dao động trục Y từ t = 0 đến 1 (Loop)")]
        public AnimationCurve customCurveY = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        /// <summary>
        /// Tạo preset mặc định tĩnh khi profile bị null (0 GC Alloc, Fallback an toàn).
        /// </summary>
        public static FormationMotionProfile CreateDefaultPreset(FormationMotionType type)
        {
            var p = CreateInstance<FormationMotionProfile>();
            p.motionType = type;
            switch (type)
            {
                case FormationMotionType.HorizontalSinusoid:
                    p.profileName = "Default Horizontal Sinusoid";
                    p.amplitudeX = 12f;
                    p.amplitudeY = 0f;
                    p.frequencyX = 1.5f;
                    p.maxRollAngle = 25f;
                    p.maxPitchAngle = 0f;
                    break;
                case FormationMotionType.VerticalUndulation:
                    p.profileName = "Default Vertical Undulation";
                    p.amplitudeX = 0f;
                    p.amplitudeY = 6f;
                    p.frequencyY = 1.2f;
                    p.maxRollAngle = 0f;
                    p.maxPitchAngle = 20f;
                    break;
                case FormationMotionType.CorkscrewRoll:
                    p.profileName = "Default Corkscrew 3D";
                    p.amplitudeX = 10f;
                    p.amplitudeY = 6f;
                    p.frequencyX = 1.0f;
                    p.frequencyY = 1.0f;
                    p.phaseShiftDeg = 90f;
                    p.maxRollAngle = 35f;
                    p.maxPitchAngle = 15f;
                    break;
                case FormationMotionType.FigureEight:
                    p.profileName = "Default Figure-8 Infinity";
                    p.amplitudeX = 14f;
                    p.amplitudeY = 5.5f;
                    p.frequencyX = 1.0f;
                    p.frequencyY = 2.0f;
                    p.phaseShiftDeg = 0f;
                    p.maxRollAngle = 30f;
                    p.maxPitchAngle = 15f;
                    break;
                case FormationMotionType.Lissajous3D:
                    p.profileName = "Default Lissajous Cosmic";
                    p.amplitudeX = 12f;
                    p.amplitudeY = 5f;
                    p.frequencyX = 1.2f;
                    p.frequencyY = 1.8f;
                    p.phaseShiftDeg = 45f;
                    p.maxRollAngle = 25f;
                    p.maxPitchAngle = 18f;
                    break;
                case FormationMotionType.StationaryHold:
                default:
                    p.profileName = "Default Stationary Hold";
                    p.amplitudeX = 0f;
                    p.amplitudeY = 0f;
                    p.maxRollAngle = 0f;
                    p.maxPitchAngle = 0f;
                    break;
            }
            return p;
        }
    }
}
