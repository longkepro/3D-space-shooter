using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Giao diện mẫu Strategy định nghĩa các thuật toán quy hoạch không gian
    /// và làn bay an toàn trong hành lang đường ray ảo (Corridor Safe Path).
    /// Đảm bảo tính mở rộng cao, không phụ thuộc vào GameObject/Physics, đạt chuẩn 0 GC Alloc.
    /// </summary>
    public interface ICorridorPathStrategy
    {
        /// <summary>
        /// Lấy tọa độ tâm của luồng bay an toàn (Safe Trench Center) tại vị trí Z.
        /// </summary>
        Vector2 SampleSafeCenter(float z, VirtualRailConfig config);

        /// <summary>
        /// Tính toán tọa độ xuất hiện (X, Y, Z) cho một chướng ngại vật/thiên thạch tại targetZ,
        /// tự động áp dụng các quy tắc đẩy dạt (repulsion) để chừa luồng an toàn cho người chơi.
        /// </summary>
        Vector3 CalculateSpawnPosition(float targetZ, float corridorWidth, float corridorHeight, VirtualRailConfig config);
    }
}
