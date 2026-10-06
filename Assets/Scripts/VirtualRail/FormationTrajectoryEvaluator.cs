using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Trạng thái không gian (Pose) của một slot phi thuyền trong phi đội.
    /// Thiết kế dạng Struct thuần túy (0 GC Alloc) để tái sử dụng tối ưu giữa Runtime và Scene View Editor.
    /// </summary>
    [Serializable]
    public struct SlotPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool isLeader;
        public int slotIndex;
        public string slotName;
        public EnemyArchetype preferredArchetype;
        public bool isAssembled;
        public Vector3 spawnPos;
        public Vector3 ctrl1;
        public Vector3 ctrl2;
        public Vector3 targetSlotWorld;
    }

    /// <summary>
    /// Lõi toán học tất định mô phỏng quỹ đạo bay đội hình (Formation Trajectory Evaluator).
    /// SINGLE SOURCE OF TRUTH: Tái sử dụng 100% công thức giữa Runtime (VirtualFormationAnchor/FormationWingman)
    /// và Unity Scene View Previewer.
    /// </summary>
    public static class FormationTrajectoryEvaluator
    {
        /// <summary>
        /// Hàm nội suy Cubic Bézier 3D chuẩn xác.
        /// P(u) = (1-u)^3 P0 + 3(1-u)^2 u P1 + 3(1-u) u^2 P2 + u^3 P3
        /// </summary>
        public static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            u = Mathf.Clamp01(u);
            float oneMinusU = 1f - u;
            float oneMinusU2 = oneMinusU * oneMinusU;
            float oneMinusU3 = oneMinusU2 * oneMinusU;
            float u2 = u * u;
            float u3 = u2 * u;

            return (oneMinusU3 * p0) +
                   (3f * oneMinusU2 * u * p1) +
                   (3f * oneMinusU * u2 * p2) +
                   (u3 * p3);
        }

        /// <summary>
        /// Tính toán tọa độ và góc xoay của Đầu mối Đội hình (Anchor) tại mốc thời gian t.
        /// </summary>
        public static void EvaluateAnchor(
            FormationPattern pattern,
            float time,
            Vector3 startPos,
            Quaternion baseRotation,
            Vector3 railForward,
            Vector3 railRight,
            out Vector3 anchorPos,
            out Quaternion anchorRot)
        {
            float swaySpeed = pattern != null ? pattern.swaySpeed : 1.5f;
            float swayAmplitudeX = pattern != null ? pattern.swayAmplitudeX : 12f;
            float bankingAngle = pattern != null ? pattern.bankingAngle : 25f;

            // 1. Dao động hình sin ngang
            float sway = Mathf.Sin(time * swaySpeed) * swayAmplitudeX;
            Vector3 swayOffset = railRight * sway;
            anchorPos = startPos + swayOffset;

            // 2. Góc nghiêng cánh khi lượn (Banking Roll)
            float rollAngle = -Mathf.Sin(time * swaySpeed) * bankingAngle;
            anchorRot = baseRotation * Quaternion.Euler(0f, 0f, rollAngle);
        }

        /// <summary>
        /// Tạo điểm xuất phát deterministic ngoài màn hình cho từng slot theo FormationEntryStyle (chân trời 120m - 140m).
        /// </summary>
        public static Vector3 GetDeterministicSpawnOffset(int slotIndex, FormationEntryStyle entryStyle = FormationEntryStyle.FlankIn)
        {
            float side = (slotIndex % 2 == 0 ? 1f : -1f);
            switch (entryStyle)
            {
                case FormationEntryStyle.OverheadDive:
                    return new Vector3(side * (8f + slotIndex * 3f), 28f + (slotIndex * 2.5f), 18f + (slotIndex * 4f));
                case FormationEntryStyle.RearFlyby:
                    return new Vector3(side * (8f + slotIndex * 2f), -3f, -(45f + slotIndex * 5f));
                case FormationEntryStyle.FrontalHorizon:
                    return new Vector3(side * 4f, (slotIndex % 2 == 0 ? 3f : -3f), 50f + (slotIndex * 8f));
                case FormationEntryStyle.FlankIn:
                default:
                    float x = side * (24f + (slotIndex * 3.5f));
                    float y = (slotIndex % 3 == 0) ? 6f : -3f;
                    float z = 18f + (slotIndex * 4f);
                    return new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Đánh giá toàn bộ Pose của tất cả các tàu trong đội hình tại mốc thời gian t.
        /// Trả về số lượng slot hợp lệ. Đảm bảo 0 GC Allocation khi dùng mảng outPoses tái sử dụng.
        /// </summary>
        public static int EvaluateAllSlots(
            FormationPattern pattern,
            float time,
            Vector3 startPos,
            Quaternion baseRotation,
            Vector3 railForward,
            Vector3 railRight,
            SlotPose[] outPoses)
        {
            if (pattern == null || pattern.slots == null || pattern.slots.Length == 0 || outPoses == null)
            {
                return 0;
            }

            int count = Mathf.Min(pattern.slots.Length, outPoses.Length);
            float assemblyDuration = Mathf.Max(pattern.assemblyTime, 0.5f);
            FormationEntryStyle entryStyle = pattern.entryStyle;

            // 1. Tính toán vị trí tâm Anchor tại thời điểm time
            EvaluateAnchor(pattern, time, startPos, baseRotation, railForward, railRight, out Vector3 anchorPos, out Quaternion anchorRot);

            for (int i = 0; i < count; i++)
            {
                var slotDef = pattern.slots[i];
                bool isLeader = slotDef.isLeader || (i == 0);
                float scaleVal = slotDef.scaleMultiplier > 0.01f ? slotDef.scaleMultiplier : 1.0f;

                // Tọa độ mục tiêu của Slot trong World Space tại anchorPos
                Vector3 targetSlotWorld = anchorPos + (anchorRot * slotDef.localOffset);

                // Điểm xuất phát ngoài màn hình
                Vector3 spawnOffset = GetDeterministicSpawnOffset(i, entryStyle);
                Vector3 spawnPos = startPos + spawnOffset;

                // 2 điểm điều khiển Bézier theo Entry Style
                Vector3 ctrl1;
                Vector3 ctrl2;

                switch (entryStyle)
                {
                    case FormationEntryStyle.OverheadDive:
                        ctrl1 = spawnPos - Vector3.up * 24f - railForward * 6f;
                        ctrl2 = targetSlotWorld - Vector3.up * 8f - railForward * 16f;
                        break;
                    case FormationEntryStyle.RearFlyby:
                        ctrl1 = spawnPos + railForward * 35f + Vector3.up * 8f;
                        ctrl2 = targetSlotWorld + railForward * 15f + Vector3.up * 4f;
                        break;
                    case FormationEntryStyle.FrontalHorizon:
                        ctrl1 = spawnPos - railForward * 25f;
                        ctrl2 = targetSlotWorld + railForward * 12f;
                        break;
                    case FormationEntryStyle.FlankIn:
                    default:
                        ctrl1 = spawnPos + Vector3.up * 8f + railRight * (isLeader ? 0f : (i % 2 == 0 ? 10f : -10f));
                        ctrl2 = targetSlotWorld - (anchorRot * Vector3.forward * 20f) + Vector3.up * 4f;
                        break;
                }

                Vector3 currentPos;
                Quaternion currentRot;
                bool isAssembled;

                if (time < assemblyDuration)
                {
                    // Pha 1: Đang bay ráp nối Bézier
                    float u = Mathf.Clamp01(time / assemblyDuration);
                    currentPos = EvaluateCubicBezier(spawnPos, ctrl1, ctrl2, targetSlotWorld, u);

                    float nextU = Mathf.Min(u + 0.04f, 1f);
                    Vector3 nextPos = EvaluateCubicBezier(spawnPos, ctrl1, ctrl2, targetSlotWorld, nextU);

                    Vector3 dir = nextPos - currentPos;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        float lateralDot = Vector3.Dot(dir.normalized, railRight);
                        float bankRoll = -Mathf.Clamp(lateralDot * 50f, -40f, 40f);
                        currentRot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 0f, bankRoll);
                    }
                    else
                    {
                        currentRot = anchorRot;
                    }
                    isAssembled = false;
                }
                else
                {
                    // Pha 2: Đã khóa cứng trong đội hình (Locked in Slot)
                    currentPos = targetSlotWorld;
                    currentRot = anchorRot;
                    isAssembled = true;
                }

                outPoses[i] = new SlotPose
                {
                    position = currentPos,
                    rotation = currentRot,
                    scale = Vector3.one * scaleVal,
                    isLeader = isLeader,
                    slotIndex = i,
                    slotName = string.IsNullOrEmpty(slotDef.slotName) ? $"Slot {i}" : slotDef.slotName,
                    preferredArchetype = slotDef.preferredArchetype,
                    isAssembled = isAssembled,
                    spawnPos = spawnPos,
                    ctrl1 = ctrl1,
                    ctrl2 = ctrl2,
                    targetSlotWorld = targetSlotWorld
                };
            }

            return count;
        }
    }
}
