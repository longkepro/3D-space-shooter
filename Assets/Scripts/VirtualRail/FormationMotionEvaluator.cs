using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bộ tính toán động học giải tích cho chuyển động đội hình (0 GC Alloc).
    /// Áp dụng mẫu Strategy Pattern: Nhận cấu hình từ FormationMotionProfile và tính toán vị trí, vận tốc,
    /// góc xoay nghiêng cánh (Banking Roll) và chúc ngóc mũi (Pitch) chuẩn mực không gian 3D.
    /// </summary>
    public static class FormationMotionEvaluator
    {
        private const float TWO_PI = Mathf.PI * 2f;
        private const float VELOCITY_REF = 15f;

        /// <summary>
        /// Tính toán vị trí dịch chuyển không gian và góc quay mục tiêu tại thời điểm t (0 GC Alloc).
        /// </summary>
        public static void Evaluate(
            FormationMotionProfile profile,
            float lifetimeTimer,
            float fallbackSwaySpeed,
            float fallbackSwayAmplitudeX,
            float fallbackBankingAngle,
            Vector3 railRight,
            Vector3 railUp,
            Vector3 railForward,
            out Vector3 localDisplacement,
            out Quaternion targetRotation)
        {
            float x = 0f;
            float y = 0f;
            float vx = 0f;
            float vy = 0f;
            float rollAngle = 0f;
            float pitchAngle = 0f;

            // 1. Fallback an toàn nếu chưa gán Profile (Tương thích ngược 100% với các đội hình cũ)
            if (profile == null)
            {
                float freq = fallbackSwaySpeed > 0.01f ? fallbackSwaySpeed : 1.5f;
                float amp = fallbackSwayAmplitudeX > 0.01f ? fallbackSwayAmplitudeX : 12f;
                float maxRoll = fallbackBankingAngle > 0.01f ? fallbackBankingAngle : 25f;

                x = Mathf.Sin(lifetimeTimer * freq) * amp;
                y = 0f;
                vx = Mathf.Cos(lifetimeTimer * freq) * (freq * amp);
                vy = 0f;
                rollAngle = -Mathf.Sin(lifetimeTimer * freq) * maxRoll;
                pitchAngle = 0f;

                localDisplacement = railRight * x;
                targetRotation = Quaternion.LookRotation(-railForward, railUp) * Quaternion.Euler(0f, 0f, rollAngle);
                return;
            }

            // 2. Hệ số làm mềm lúc mới ráp nối (Soft-Start Ramping)
            float ramp = 1.0f;
            if (profile.enableSoftStart && profile.softStartDuration > 0.05f)
            {
                float progress = Mathf.Clamp01(lifetimeTimer / profile.softStartDuration);
                ramp = progress * progress * (3f - 2f * progress); // SmoothStep
            }

            // 3. Tính toán tọa độ và đạo hàm vận tốc tức thời
            if (profile.useCustomCurves)
            {
                float ux = (lifetimeTimer * profile.frequencyX) % 1.0f;
                float uy = (lifetimeTimer * profile.frequencyY) % 1.0f;
                if (ux < 0f) ux += 1.0f;
                if (uy < 0f) uy += 1.0f;

                x = profile.customCurveX.Evaluate(ux) * profile.amplitudeX * ramp;
                y = profile.customCurveY.Evaluate(uy) * profile.amplitudeY * ramp;

                // Xấp xỉ đạo hàm vận tốc tiếp tuyến
                const float dt = 0.03f;
                float nx = (ux + dt) % 1.0f;
                float ny = (uy + dt) % 1.0f;
                vx = ((profile.customCurveX.Evaluate(nx) - profile.customCurveX.Evaluate(ux)) / dt) * profile.amplitudeX * ramp;
                vy = ((profile.customCurveY.Evaluate(ny) - profile.customCurveY.Evaluate(uy)) / dt) * profile.amplitudeY * ramp;
            }
            else
            {
                float wx = profile.frequencyX * TWO_PI;
                float wy = profile.frequencyY * TWO_PI;
                float phase = profile.phaseShiftDeg * Mathf.Deg2Rad;

                switch (profile.motionType)
                {
                    case FormationMotionType.HorizontalSinusoid:
                        x = Mathf.Sin(lifetimeTimer * wx) * profile.amplitudeX * ramp;
                        y = 0f;
                        vx = Mathf.Cos(lifetimeTimer * wx) * (wx * profile.amplitudeX * ramp);
                        vy = 0f;
                        break;

                    case FormationMotionType.VerticalUndulation:
                        x = 0f;
                        y = Mathf.Sin(lifetimeTimer * wy) * profile.amplitudeY * ramp;
                        vx = 0f;
                        vy = Mathf.Cos(lifetimeTimer * wy) * (wy * profile.amplitudeY * ramp);
                        break;

                    case FormationMotionType.CorkscrewRoll:
                        x = Mathf.Cos(lifetimeTimer * wx) * profile.amplitudeX * ramp;
                        y = Mathf.Sin(lifetimeTimer * wy) * profile.amplitudeY * ramp;
                        vx = -Mathf.Sin(lifetimeTimer * wx) * (wx * profile.amplitudeX * ramp);
                        vy = Mathf.Cos(lifetimeTimer * wy) * (wy * profile.amplitudeY * ramp);
                        break;

                    case FormationMotionType.FigureEight:
                        x = Mathf.Sin(lifetimeTimer * wx) * profile.amplitudeX * ramp;
                        y = Mathf.Sin(lifetimeTimer * 2f * wx + phase) * (profile.amplitudeY * 0.5f * ramp);
                        vx = Mathf.Cos(lifetimeTimer * wx) * (wx * profile.amplitudeX * ramp);
                        vy = Mathf.Cos(lifetimeTimer * 2f * wx + phase) * (wx * profile.amplitudeY * ramp);
                        break;

                    case FormationMotionType.Lissajous3D:
                        x = Mathf.Sin(lifetimeTimer * wx) * profile.amplitudeX * ramp;
                        y = Mathf.Sin(lifetimeTimer * wy + phase) * profile.amplitudeY * ramp;
                        vx = Mathf.Cos(lifetimeTimer * wx) * (wx * profile.amplitudeX * ramp);
                        vy = Mathf.Cos(lifetimeTimer * wy + phase) * (wy * profile.amplitudeY * ramp);
                        break;

                    case FormationMotionType.StationaryHold:
                    default:
                        x = 0f;
                        y = 0f;
                        vx = 0f;
                        vy = 0f;
                        break;
                }
            }

            // 4. Tính toán góc nghiêng động học (Banking & Pitch) giải tích mượt mà (Analytic Smooth Dynamics)
            float maxVx = (profile.amplitudeX > 0.05f && profile.frequencyX > 0.01f) ? (profile.frequencyX * TWO_PI * profile.amplitudeX) : 10f;
            float maxVy = (profile.amplitudeY > 0.05f && profile.frequencyY > 0.01f) ? (profile.frequencyY * TWO_PI * profile.amplitudeY) : 10f;

            float normVx = Mathf.Clamp(vx / maxVx, -1.0f, 1.0f);
            float normVy = Mathf.Clamp(vy / maxVy, -1.0f, 1.0f);

            rollAngle = -normVx * profile.maxRollAngle;
            pitchAngle = normVy * profile.maxPitchAngle;

            // 5. Tổng hợp Vector vị trí và Quaternion xoay hướng
            localDisplacement = railRight * x + railUp * y;
            targetRotation = Quaternion.LookRotation(-railForward, railUp) * Quaternion.Euler(pitchAngle, 0f, rollAngle);
        }
    }
}
