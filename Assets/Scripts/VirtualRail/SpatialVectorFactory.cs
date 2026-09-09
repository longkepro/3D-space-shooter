using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Phân loại chuẩn hóa 10 Vector xuất hiện không gian theo TDD v1.0.0 Phần II.1:
    /// Nhóm 1: Vector Hình học Không gian (Camera-Relative Kinematics)
    ///   1. FrontalHeadOn: Trực diện chân trời (Z +300m ~ 400m)
    ///   2. RearAmbush: Đột kích sau lưng (Z -30m ~ -60m, vượt mặt hoặc bám đuôi, yếu điểm đuôi 200%)
    ///   3. LateralFlankingLeft / Right: Cắt ngang sườn 90 độ (0.8s - 1.5s)
    ///   4. OverheadDiveBomb: Bổ nhào từ trần mây (+Y), góc 45-70 độ
    ///   5. SubSurfaceBreach: Phóng trồi từ đáy sâu (-Y), ném đứng đạt đỉnh rồi rơi
    /// Nhóm 2: Môi trường & Kịch bản
    ///   6. BlindCurveReveal: Lộ diện qua góc khuất
    ///   7. DynamicKineticHazards: Vật cản sụp đổ động lực học
    ///   8. StructuralEmergence: Chui ra từ khoang hầm ngầm
    /// Nhóm 3: Phân rã thứ cấp
    ///   9. ClusterFracture: Vỡ mảnh thiên thạch / tàu mẹ (3-5 mảnh vỡ)
    /// Nhóm 4: Viễn tưởng
    ///   10. WarpInDecloak: Cổng không gian warp-in tại Z ≈ 80m - 120m
    /// </summary>
    public enum SpatialVectorType
    {
        FrontalHeadOn = 0,
        RearAmbush = 1,
        LateralFlankingLeft = 2,
        LateralFlankingRight = 3,
        OverheadDiveBomb = 4,
        SubSurfaceBreach = 5,
        BlindCurveReveal = 6,
        DynamicKineticHazards = 7,
        StructuralEmergence = 8,
        ClusterFracture = 9,
        WarpInDecloak = 10
    }

    /// <summary>
    /// Thông số quỹ đạo và động học của thực thể theo vector xuất hiện.
    /// </summary>
    public struct SpatialTrajectoryData
    {
        public SpatialVectorType vectorType;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public Vector3 approachVelocity;
        public float approachDuration;
        public Vector3 actionOffset; // Tọa độ tương đối so với Anchor khi ở pha Action
        public float actionDuration;
        public Vector3 exitVelocity;
        public float exitDuration;
        public bool hasRearWeakSpot;
        public float damageMultiplier;
    }

    /// <summary>
    /// Nhà máy tính toán điểm sinh theo hệ quy chiếu Frustum động (TDD Phần II.2).
    /// H_half = Z_target * tan(FOV / 2)
    /// W_half = H_half * Aspect
    /// </summary>
    public static class SpatialVectorFactory
    {
        public static SpatialTrajectoryData GenerateTrajectory(
            SpatialVectorType vectorType,
            VirtualRailAnchor anchor,
            Camera cam,
            float leadZ)
        {
            SpatialTrajectoryData data = new SpatialTrajectoryData
            {
                vectorType = vectorType,
                approachDuration = 1.2f,
                actionDuration = 2.5f,
                exitDuration = 1.0f,
                hasRearWeakSpot = false,
                damageMultiplier = 1.0f
            };

            if (anchor == null) return data;

            float fov = (cam != null) ? cam.fieldOfView : 40f;
            float aspect = (cam != null) ? cam.aspect : (16f / 9f);
            float fovRad = fov * Mathf.Deg2Rad * 0.5f;

            float railSpeed = anchor.CurrentSpeed;
            Vector3 railPos = anchor.transform.position;
            Vector3 railFwd = anchor.ForwardTangent;
            Vector3 railRight = anchor.transform.right;
            Vector3 railUp = anchor.transform.up;

            // Frustum dimensions at standard lead distance
            float safeLead = Mathf.Max(leadZ, 60f);
            float hHalf = safeLead * Mathf.Tan(fovRad);
            float wHalf = hHalf * aspect;
            const float mSafe = 8.0f; // Biên an toàn chống lộ mô hình

            switch (vectorType)
            {
                case SpatialVectorType.FrontalHeadOn:
                {
                    // Xuất phát từ mặt phẳng cắt xa Z ≈ +300m đến +350m
                    float zDist = 320f;
                    float hZ = zDist * Mathf.Tan(fovRad);
                    float wZ = hZ * aspect;
                    float offsetX = Random.Range(-wZ * 0.65f, wZ * 0.65f);
                    float offsetY = Random.Range(-hZ * 0.45f, hZ * 0.55f);

                    data.spawnPosition = railPos + railFwd * zDist + railRight * offsetX + railUp * offsetY;
                    data.spawnRotation = Quaternion.LookRotation(-railFwd);
                    // Lao thẳng đối đầu
                    data.approachVelocity = -railFwd * (railSpeed + 25f);
                    data.approachDuration = 1.4f;
                    data.actionOffset = new Vector3(offsetX * 0.5f, offsetY * 0.5f, 90f);
                    data.actionDuration = 2.5f;
                    // Thoát ly: Peel-off rẽ cánh 90 độ
                    data.exitVelocity = (railRight * Mathf.Sign(offsetX) + railUp * 0.3f - railFwd * 0.5f).normalized * 45f;
                    break;
                }

                case SpatialVectorType.RearAmbush:
                {
                    // Xuất phát từ vùng âm sau camera Z ≈ -40m
                    float offsetX = Random.Range(-wHalf * 0.45f, wHalf * 0.45f);
                    float offsetY = Random.Range(1f, hHalf * 0.6f);

                    data.spawnPosition = railPos - railFwd * 40f + railRight * offsetX + railUp * offsetY;
                    data.spawnRotation = Quaternion.LookRotation(railFwd);
                    // Vượt mặt với tốc độ cao hơn tàu người chơi (V_rail + 35 m/s)
                    data.approachVelocity = railFwd * (railSpeed + 35f);
                    data.approachDuration = 1.2f;
                    // Giữ cự ly ghìm phía trước mũi tàu (25m - 40m)
                    data.actionOffset = new Vector3(offsetX, offsetY, 35f);
                    data.actionDuration = 3.0f;
                    // Điểm yếu chí mạng phía đuôi nhận 200% sát thương
                    data.hasRearWeakSpot = true;
                    data.damageMultiplier = 2.0f;
                    // Thoát ly: Bứt tốc thẳng về phía trước
                    data.exitVelocity = railFwd * (railSpeed + 60f);
                    break;
                }

                case SpatialVectorType.LateralFlankingLeft:
                {
                    // Lao vuông góc 90 độ từ mép ngoài X bên trái cắt qua hành lang
                    float spawnX = -(wHalf + mSafe + 12f);
                    float targetY = Random.Range(2f, hHalf * 0.7f);
                    float zOffset = safeLead * 0.45f;

                    data.spawnPosition = railPos + railFwd * zOffset + railRight * spawnX + railUp * targetY;
                    data.spawnRotation = Quaternion.LookRotation(railRight);
                    // Cắt ngang với vận tốc 45 m/s (thời gian quét 1.0s)
                    data.approachVelocity = railRight * 42f + railFwd * railSpeed;
                    data.approachDuration = 1.0f;
                    data.actionOffset = new Vector3(0f, targetY, zOffset);
                    data.actionDuration = 1.2f;
                    data.exitVelocity = railRight * 48f + railFwd * railSpeed;
                    break;
                }

                case SpatialVectorType.LateralFlankingRight:
                {
                    // Lao vuông góc 90 độ từ mép ngoài X bên phải cắt qua hành lang
                    float spawnX = (wHalf + mSafe + 12f);
                    float targetY = Random.Range(2f, hHalf * 0.7f);
                    float zOffset = safeLead * 0.45f;

                    data.spawnPosition = railPos + railFwd * zOffset + railRight * spawnX + railUp * targetY;
                    data.spawnRotation = Quaternion.LookRotation(-railRight);
                    // Cắt ngang sang trái
                    data.approachVelocity = -railRight * 42f + railFwd * railSpeed;
                    data.approachDuration = 1.0f;
                    data.actionOffset = new Vector3(0f, targetY, zOffset);
                    data.actionDuration = 1.2f;
                    data.exitVelocity = -railRight * 48f + railFwd * railSpeed;
                    break;
                }

                case SpatialVectorType.OverheadDiveBomb:
                {
                    // Bổ nhào từ tầng mây (+Y) theo góc 45 - 70 độ
                    float spawnY = hHalf + mSafe + 18f;
                    float offsetX = Random.Range(-wHalf * 0.5f, wHalf * 0.5f);
                    float zOffset = safeLead * 0.6f;

                    data.spawnPosition = railPos + railFwd * (zOffset + 40f) + railRight * offsetX + railUp * spawnY;
                    Vector3 diveDir = (-railUp * 1.5f - railFwd * 0.8f).normalized;
                    data.spawnRotation = Quaternion.LookRotation(diveDir);
                    data.approachVelocity = diveDir * 50f + railFwd * railSpeed;
                    data.approachDuration = 1.1f;
                    data.actionOffset = new Vector3(offsetX, -2f, zOffset);
                    data.actionDuration = 2.0f;
                    // Ngóc mũi bay vút lên bầu trời
                    data.exitVelocity = (railUp * 1.8f + railFwd * 0.5f).normalized * 55f;
                    break;
                }

                case SpatialVectorType.SubSurfaceBreach:
                {
                    // Phóng trồi từ đáy sâu (-Y) theo động lực ném đứng
                    float spawnY = -(hHalf + mSafe + 15f);
                    float offsetX = Random.Range(-wHalf * 0.45f, wHalf * 0.45f);
                    float zOffset = safeLead * 0.4f;

                    data.spawnPosition = railPos + railFwd * zOffset + railRight * offsetX + railUp * spawnY;
                    data.spawnRotation = Quaternion.LookRotation(railUp);
                    // Vọt lên theo phương thẳng đứng (+Y)
                    data.approachVelocity = railUp * 38f + railFwd * railSpeed;
                    data.approachDuration = 1.0f;
                    // Đạt đỉnh cực đại (Apex) tại tâm khung nhìn
                    data.actionOffset = new Vector3(offsetX, 3f, zOffset);
                    data.actionDuration = 1.8f;
                    // Rơi ngược lại đáy sâu
                    data.exitVelocity = (-railUp * 35f + railFwd * railSpeed);
                    break;
                }

                case SpatialVectorType.BlindCurveReveal:
                {
                    // Lộ diện từ góc khuất
                    float offsetX = (Random.value > 0.5f ? 1f : -1f) * (wHalf * 0.7f);
                    data.spawnPosition = railPos + railFwd * (safeLead * 0.5f) + railRight * offsetX + railUp * 4f;
                    data.spawnRotation = Quaternion.LookRotation(-railFwd);
                    data.approachVelocity = railFwd * (railSpeed * 0.3f);
                    data.approachDuration = 0.8f;
                    data.actionOffset = new Vector3(offsetX * 0.6f, 4f, safeLead * 0.4f);
                    data.actionDuration = 2.2f;
                    data.exitVelocity = (railRight * Mathf.Sign(offsetX) * 35f);
                    break;
                }

                case SpatialVectorType.DynamicKineticHazards:
                {
                    // Vật cản sụp đổ chắn ngang
                    float dir = Random.value > 0.5f ? 1f : -1f;
                    data.spawnPosition = railPos + railFwd * (safeLead * 0.6f) + railRight * (dir * (wHalf + 10f)) + railUp * 12f;
                    data.spawnRotation = Quaternion.Euler(0, 0, dir * 45f);
                    data.approachVelocity = (-railRight * dir * 28f - railUp * 8f);
                    data.approachDuration = 1.3f;
                    data.actionOffset = new Vector3(0f, 2f, safeLead * 0.4f);
                    data.actionDuration = 2.0f;
                    data.exitVelocity = -railUp * 25f;
                    break;
                }

                case SpatialVectorType.StructuralEmergence:
                {
                    // Chui ra từ hầm ngầm / cửa khoang
                    float spawnX = Random.Range(-8f, 8f);
                    data.spawnPosition = railPos + railFwd * (safeLead * 0.7f) + railRight * spawnX - railUp * 10f;
                    data.spawnRotation = Quaternion.LookRotation(railFwd * 0.5f + railUp * 0.8f);
                    data.approachVelocity = (railUp * 22f + railFwd * (railSpeed * 0.8f));
                    data.approachDuration = 1.2f;
                    data.actionOffset = new Vector3(spawnX, 4f, safeLead * 0.5f);
                    data.actionDuration = 2.5f;
                    data.exitVelocity = (railRight * 30f + railFwd * railSpeed);
                    break;
                }

                case SpatialVectorType.ClusterFracture:
                {
                    // Phân rã thứ cấp từ vụ nổ lớn
                    Vector3 randDir = Random.onUnitSphere;
                    data.spawnPosition = railPos + railFwd * (safeLead * 0.5f) + randDir * 3f;
                    data.spawnRotation = Random.rotation;
                    data.approachVelocity = randDir * 20f + railFwd * (railSpeed * 0.5f);
                    data.approachDuration = 0.8f;
                    data.actionOffset = randDir * 8f + new Vector3(0, 0, safeLead * 0.4f);
                    data.actionDuration = 1.8f;
                    data.exitVelocity = randDir * 35f;
                    break;
                }

                case SpatialVectorType.WarpInDecloak:
                default:
                {
                    // Cổng không gian & Tàng hình (Z ≈ 80m - 120m)
                    float zDist = Random.Range(85f, 115f);
                    float offsetX = Random.Range(-wHalf * 0.5f, wHalf * 0.5f);
                    float offsetY = Random.Range(1f, hHalf * 0.6f);

                    data.spawnPosition = railPos + railFwd * zDist + railRight * offsetX + railUp * offsetY;
                    data.spawnRotation = Quaternion.LookRotation(-railFwd);
                    // Dịch chuyển tức thời tới cự ly chiến đấu
                    data.approachVelocity = Vector3.zero;
                    data.approachDuration = 0.5f; // Hiện hiệu ứng warp
                    data.actionOffset = new Vector3(offsetX, offsetY, zDist);
                    data.actionDuration = 3.0f;
                    // Thoát ly: Bẻ ngoặt 90 độ văng ra ngoài
                    data.exitVelocity = (railRight * Mathf.Sign(offsetX) * 45f + railFwd * 10f);
                    break;
                }
            }

            return data;
        }
    }
}
