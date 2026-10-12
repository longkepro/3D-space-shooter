using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Hệ Thống Tái Định Vị Thế Giới (World Origin Shifter - Task 4.3).
    /// Tuân thủ nghiêm ngặt ENDLESS_SCORING_SPECIFICATION.md (Mục 1.3):
    /// - Khi quãng đường tích lũy đạt mỗi mốc chu kỳ 4,000m, kích hoạt dịch chuyển ngầm trong đúng 1 frame.
    /// - Dịch chuyển toàn bộ thực thể sống (Tàu, Spline, Kẻ địch, Đạn đạo, Thiên thạch, Trùm) ngược về gốc (0, 0, 0).
    /// - Triệt tiêu hoàn toàn hiện tượng rung lắc camera (Floating-point Precision Jitter) khi bay xa > 10,000m.
    /// - Biến đo quãng đường tích lũy và bảng điểm sử dụng số thực 64-bit (double) độc lập tuyệt đối.
    /// - 0 GC Allocation trong game loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailOriginShifter : MonoBehaviour
    {
        public static VirtualRailOriginShifter Instance { get; private set; }

        public static event System.Action<Vector3> OnWorldOriginShifted;

        [Header("Configuration")]
        public VirtualRailConfig config;
        public VirtualRailAnchor anchor;

        [Header("Shifting Parameters")]
        [Tooltip("Chu kỳ quãng đường kích hoạt dịch chuyển thế giới ngược về gốc (m - chuẩn 4,000m)")]
        [SerializeField] private float shiftThreshold = 4000f;

        [Header("64-bit Telemetry (Floating Origin Immune)")]
        [SerializeField] private double lifetimeTraveledDistance = 0.0;
        [SerializeField] private float distanceSinceLastShift = 0f;
        [SerializeField] private int totalShiftsPerformed = 0;

        public double LifetimeTraveledDistance => lifetimeTraveledDistance;
        public float DistanceSinceLastShift => distanceSinceLastShift;
        public int TotalShiftsPerformed => totalShiftsPerformed;
        public float ShiftThreshold => shiftThreshold;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (anchor == null)
            {
                anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : GetComponent<VirtualRailAnchor>();
            }
            if (config == null && anchor != null)
            {
                config = anchor.config;
            }
            if (config != null && config.originShiftThreshold > 500f)
            {
                shiftThreshold = config.originShiftThreshold;
            }
        }

        private void Update()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance;
            if (anchor == null || !anchor.IsGameActive) return;

            var cfg = config != null ? config : anchor.config;
            bool enabled = cfg == null || cfg.enableWorldOriginShifting;
            if (!enabled) return;

            float deltaDist = anchor.CurrentSpeed * Time.deltaTime;
            lifetimeTraveledDistance += deltaDist;
            distanceSinceLastShift += deltaDist;

            // Kiểm tra ngưỡng 4,000m chu kỳ để kích hoạt phép tịnh tiến thế giới
            if (distanceSinceLastShift >= shiftThreshold)
            {
                PerformOriginShift();
            }
        }

        /// <summary>
        /// Thực thi phép biến đổi tọa độ thế giới trong đúng 1 frame (Atomic 1-Frame World Origin Shift).
        /// Toàn bộ thực thể chuyển dịch đồng thời theo vector -anchor.transform.position (0 GC Alloc).
        /// </summary>
        public void PerformOriginShift()
        {
            if (anchor == null) return;

            Vector3 shiftVector = anchor.transform.position;
            // Nếu anchor đã ở sát gốc tọa độ, không cần dịch chuyển
            if (shiftVector.sqrMagnitude < 25f) return;

            // 1. Dịch chuyển Rail Anchor về gốc không gian
            anchor.transform.position -= shiftVector;

            // 2. Dịch chuyển các đoạn Spline Chunks trong Ring Buffer (C2 Preserving)
            if (VirtualRailSplineChunker.Instance != null)
            {
                VirtualRailSplineChunker.Instance.ShiftOrigin(shiftVector);
            }

            // 3. Dịch chuyển bầy quái vật và đội hình bay
            if (VirtualRailWaveSpawner.Instance != null)
            {
                VirtualRailWaveSpawner.Instance.ShiftOrigin(shiftVector);
            }

            // 4. Dịch chuyển biển thiên thạch
            if (VirtualRailAsteroidSpawner.Instance != null)
            {
                VirtualRailAsteroidSpawner.Instance.ShiftOrigin(shiftVector);
            }

            // 5. Dịch chuyển toàn bộ đạn địch
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.ShiftOrigin(shiftVector);
            }

            // 6. Dịch chuyển toàn bộ đạn laser người chơi
            if (VirtualRailLaserPool.Instance != null)
            {
                VirtualRailLaserPool.Instance.ShiftOrigin(shiftVector);
            }

            // 7. Dịch chuyển các vòng năng lượng / vật phẩm thu thập
            if (VirtualRailCollectiblePool.Instance != null)
            {
                VirtualRailCollectiblePool.Instance.ShiftOrigin(shiftVector);
            }

            // 8. Dịch chuyển Chiến hạm Trùm Corvette F3 nếu đang giao tranh
            if (BossF3CorvetteController.Instance != null)
            {
                BossF3CorvetteController.Instance.ShiftOrigin(shiftVector);
            }

            // 9. Phát sự kiện cho các module ngoại vi khác tự đồng bộ
            OnWorldOriginShifted?.Invoke(shiftVector);

            distanceSinceLastShift = 0f;
            totalShiftsPerformed++;

            Debug.Log($"<color=cyan><b>[VirtualRailOriginShifter]</b></color> ĐÃ TÁI ĐỊNH VỊ THẾ GIỚI LẦN #{totalShiftsPerformed}: " +
                      $"Dịch chuyển {-shiftVector}. Quãng đường 64-bit tích lũy: {lifetimeTraveledDistance:N1}m.");
        }

        /// <summary>
        /// Khởi tạo lại khi người chơi Respawn hoặc bắt đầu lượt chạy mới.
        /// </summary>
        public void ResetDistance()
        {
            lifetimeTraveledDistance = 0.0;
            distanceSinceLastShift = 0f;
            totalShiftsPerformed = 0;
        }
    }
}
