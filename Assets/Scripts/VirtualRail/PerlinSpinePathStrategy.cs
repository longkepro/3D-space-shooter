using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Chiến lược quy hoạch luồng an toàn bằng Cột Sống Perlin Noise liên tục (Continuous Perlin Safe Spine).
    /// Đảm bảo người chơi luôn có một hẻm luồn lách (Trench Run) rộng tối thiểu R_safe xuyên qua biển thiên thạch.
    /// Hoàn toàn 0 GC Allocation, sử dụng tính toán hình học Stack Value Types.
    /// </summary>
    public class PerlinSpinePathStrategy : ICorridorPathStrategy
    {
        private const float SEED_X = 13.37f;
        private const float SEED_Y = 42.69f;
        private const float CORRIDOR_MID_Y = 8.5f;

        /// <summary>
        /// Lấy tọa độ tâm của luồng bay an toàn tại vị trí Z trên đường ray.
        /// </summary>
        public Vector2 SampleSafeCenter(float z, VirtualRailConfig config)
        {
            float freq = (config != null) ? config.safePathWiggleFrequency : 0.012f;
            float ampX = (config != null) ? config.safePathWiggleAmplitudeX : 12f;
            float ampY = (config != null) ? config.safePathWiggleAmplitudeY : 4.5f;

            // Tính toán đường cong uốn lượn mượt mà dọc theo trục Z
            float normX = Mathf.PerlinNoise(z * freq + SEED_X, 0.25f);
            float normY = Mathf.PerlinNoise(0.75f, z * freq + SEED_Y);

            float safeX = (normX - 0.5f) * 2f * ampX;
            float safeY = CORRIDOR_MID_Y + (normY - 0.5f) * 2f * ampY;

            return new Vector2(safeX, safeY);
        }

        /// <summary>
        /// Sinh tọa độ 3D cho thiên thạch, tự động áp dụng lực đẩy từ tính (Repulsion)
        /// để đẩy dạt mọi thiên thạch ra ngoài đường ống an toàn bán kính R_safe.
        /// </summary>
        public Vector3 CalculateSpawnPosition(float targetZ, float corridorWidth, float corridorHeight, VirtualRailConfig config)
        {
            float halfW = corridorWidth * 0.5f;
            float minY = 1f;
            float maxY = minY + corridorHeight;

            Vector2 safeCenter = SampleSafeCenter(targetZ, config);
            float clearRadius = (config != null) ? config.safeCorridorRadius : 5.5f;

            // 1. Lấy mẫu vị trí ngẫu nhiên ban đầu trong hành lang
            float candX = Random.Range(-halfW, halfW);
            float candY = Random.Range(minY, maxY);

            // 2. Kiểm tra khoảng cách tới tim luồng an toàn (cộng thêm biên bán kính bao thiên thạch 2.5m)
            Vector2 diff = new Vector2(candX - safeCenter.x, candY - safeCenter.y);
            float distSq = diff.sqrMagnitude;
            const float ASTEROID_RADIUS_MARGIN = 2.5f;
            float totalClearDist = clearRadius + ASTEROID_RADIUS_MARGIN;
            float totalClearDistSq = totalClearDist * totalClearDist;

            // 3. Nếu rơi vào vùng cấm an toàn -> Đẩy dạt ra ngoài bán kính an toàn đảm bảo
            if (distSq < totalClearDistSq)
            {
                float dist = Mathf.Sqrt(distSq);
                Vector2 dir;
                if (dist > 0.001f)
                {
                    dir = diff / dist;
                }
                else
                {
                    dir = (Random.value > 0.5f) ? Vector2.right : Vector2.left;
                }

                candX = safeCenter.x + dir.x * totalClearDist;
                candY = safeCenter.y + dir.y * totalClearDist;

                // Kẹp trong giới hạn biên hành lang
                candX = Mathf.Clamp(candX, -halfW, halfW);
                candY = Mathf.Clamp(candY, minY, maxY);

                // Trong trường hợp kẹp biên đẩy ngược vào trong luồng an toàn, đổi sang hướng đối diện
                float postDistSq = (new Vector2(candX, candY) - safeCenter).sqrMagnitude;
                if (postDistSq < totalClearDistSq)
                {
                    candX = safeCenter.x - dir.x * totalClearDist;
                    candY = safeCenter.y - dir.y * totalClearDist;
                    candX = Mathf.Clamp(candX, -halfW, halfW);
                    candY = Mathf.Clamp(candY, minY, maxY);
                }
            }

            return new Vector3(candX, candY, targetZ);
        }
    }
}
