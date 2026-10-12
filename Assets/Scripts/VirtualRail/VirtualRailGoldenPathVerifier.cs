using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Thuật Toán Bảo Chứng Khả Thi Hình Học (Golden Path Raycast & Solvability Verifier - Task 4.2).
    /// Tuân thủ nghiêm ngặt ENDLESS_SCORING_SPECIFICATION.md (Mục 1.2):
    /// - Quét trước hành lang kiểm định W_ship x H_ship x 1.5 (15m x 5.25m) dọc theo Chunk mới sinh.
    /// - Mô phỏng tìm đường 3D dựa trên góc thoát hiểm khả thi theta <= 26.5 độ (tỷ lệ vận tốc né tránh 1:2).
    /// - Tự động phát hiện và loại bỏ "bức tường chết" (Unwinnable Scenarios) không có lối thoát.
    /// - Tự động giải phóng (Carve) luồng bay an toàn tuyệt đối nếu vật cản bịt kín lối đi.
    /// - 0 GC Allocation trong game loop nhờ mảng đệm tĩnh tiền cấp phát.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailGoldenPathVerifier : MonoBehaviour
    {
        public static VirtualRailGoldenPathVerifier Instance { get; private set; }

        [Header("Configuration")]
        public VirtualRailConfig config;

        [Header("Clearance Dimensions (W_ship x H_ship x 1.5)")]
        [Tooltip("Chiều rộng hộp quét an toàn (m - sải cánh tàu 10m * 1.5 = 15m)")]
        [SerializeField] private float clearanceWidth = 15f;

        [Tooltip("Chiều cao hộp quét an toàn (m - chiều cao tàu 3.5m * 1.5 = 5.25m)")]
        [SerializeField] private float clearanceHeight = 5.25f;

        [Tooltip("Bán kính hình cầu bao tương đương của vùng an toàn (m)")]
        [SerializeField] private float clearanceRadius = 6.0f;

        [Header("Escape Kinematics")]
        [Tooltip("Góc thoát hiểm tối đa cho phép (độ - chuẩn 26.5 độ cho tỷ lệ 1:2)")]
        [SerializeField] private float maxEscapeAngle = 26.5f;

        [Tooltip("Khoảng cách bước nhảy giữa các lát cắt kiểm định (m)")]
        [SerializeField] private float sliceInterval = 25f;

        [Header("Telemetry")]
        [SerializeField] private int totalVerifications = 0;
        [SerializeField] private int totalCarvedWalls = 0;
        [SerializeField] private bool lastVerificationPassed = true;

        // Bảng ứng viên 15 Slots (5 cột ngang x 3 tầng cao)
        private const int NUM_SLOTS = 15;
        private const int MAX_SLICES = 24; // 500m / 25m = 20 lát cắt (+ dự phòng)
        private readonly Vector2[] _slotOffsets = new Vector2[NUM_SLOTS];

        // Mảng bộ đệm kiểm định 0 GC Alloc
        private readonly bool[,] _navigableGrid = new bool[MAX_SLICES, NUM_SLOTS];
        private readonly bool[,] _reachableGrid = new bool[MAX_SLICES, NUM_SLOTS];

        public int TotalVerifications => totalVerifications;
        public int TotalCarvedWalls => totalCarvedWalls;
        public bool LastVerificationPassed => lastVerificationPassed;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeSlots();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (config == null && VirtualRailAnchor.Instance != null)
            {
                config = VirtualRailAnchor.Instance.config;
            }
        }

        private void InitializeSlots()
        {
            // 5 cột ngang: X = -16, -8, 0, 8, 16
            // 3 tầng cao: Y = 3.5, 8.5, 13.5
            float[] colX = new float[] { -16f, -8f, 0f, 8f, 16f };
            float[] rowY = new float[] { 3.5f, 8.5f, 13.5f };

            int index = 0;
            for (int r = 0; r < rowY.Length; r++)
            {
                for (int c = 0; c < colX.Length; c++)
                {
                    _slotOffsets[index++] = new Vector2(colX[c], rowY[r]);
                }
            }
        }

        /// <summary>
        /// Kiểm định tính khả thi hình học của một đoạn Chunk ray (Solvability Verification).
        /// Đảm bảo người chơi với phản xạ chuẩn góc 26.5 độ luôn có ít nhất 1 đường bay thoát hiểm thông suốt.
        /// </summary>
        public bool VerifyAndSecureChunk(float startArcDist, float endArcDist)
        {
            totalVerifications++;
            var cfg = config != null ? config : (VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance.config : null);
            bool enabled = cfg == null || cfg.enableGoldenPathVerification;
            if (!enabled) return true;

            int sliceCount = Mathf.Clamp(Mathf.CeilToInt((endArcDist - startArcDist) / sliceInterval), 2, MAX_SLICES);
            float maxTan = Mathf.Tan(maxEscapeAngle * Mathf.Deg2Rad);
            float maxDisplacement = sliceInterval * maxTan; // 25m * 0.5 = 12.5m

            // 1. Kiểm định tính thông thoáng của từng ô (Slot Navigability) trên các mặt cắt
            for (int s = 0; s < sliceCount; s++)
            {
                float arc = startArcDist + s * sliceInterval;
                Vector3 railPos = new Vector3(0f, 0f, arc);
                Quaternion railRot = Quaternion.identity;

                if (VirtualRailSplineChunker.Instance != null)
                {
                    VirtualRailSplineChunker.Instance.EvaluateFull(arc, out railPos, out _, out railRot, out _);
                }
                else
                {
                    railPos = new Vector3(0f, 0f, arc);
                }

                for (int k = 0; k < NUM_SLOTS; k++)
                {
                    Vector3 slotWorld = railPos + railRot * new Vector3(_slotOffsets[k].x, _slotOffsets[k].y, 0f);
                    _navigableGrid[s, k] = IsSlotClearOfObstacles(slotWorld, clearanceRadius);
                }
            }

            // 2. Thuật toán Quy hoạch động tìm đường thoát hiểm khả thi (Dynamic Programming Reachability)
            // Lát cắt đầu tiên: Tất cả các ô thông thoáng đều là điểm xuất phát hợp lệ
            for (int k = 0; k < NUM_SLOTS; k++)
            {
                _reachableGrid[0, k] = _navigableGrid[0, k];
            }

            // Chuyển tiếp giữa các lát cắt liên tiếp
            for (int s = 0; s < sliceCount - 1; s++)
            {
                for (int kNext = 0; kNext < NUM_SLOTS; kNext++)
                {
                    _reachableGrid[s + 1, kNext] = false;
                    if (!_navigableGrid[s + 1, kNext]) continue;

                    Vector2 nextOffset = _slotOffsets[kNext];

                    for (int kPrev = 0; kPrev < NUM_SLOTS; kPrev++)
                    {
                        if (!_reachableGrid[s, kPrev]) continue;

                        Vector2 prevOffset = _slotOffsets[kPrev];
                        float dist = Vector2.Distance(prevOffset, nextOffset);

                        if (dist <= maxDisplacement + 0.1f)
                        {
                            _reachableGrid[s + 1, kNext] = true;
                            break;
                        }
                    }
                }
            }

            // 3. Đánh giá kết quả tại lát cắt cuối cùng
            bool solvable = false;
            for (int k = 0; k < NUM_SLOTS; k++)
            {
                if (_reachableGrid[sliceCount - 1, k])
                {
                    solvable = true;
                    break;
                }
            }

            lastVerificationPassed = solvable;

            // 4. Cơ chế Bảo Chứng Tuyệt Đối: Nếu phát hiện Bức Tường Chết -> Tự động giải phóng hành lang Golden Path
            if (!solvable)
            {
                totalCarvedWalls++;
                CarveSafeGoldenPath(startArcDist, endArcDist);
                lastVerificationPassed = true;
                return true;
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra một điểm trong không gian có bị thiên thạch/vật cản che chắn hay không (0 GC Alloc).
        /// </summary>
        private bool IsSlotClearOfObstacles(Vector3 worldPoint, float clearRadius)
        {
            if (VirtualRailAsteroidSpawner.Instance == null) return true;

            var pool = VirtualRailAsteroidSpawner.Instance.GetActiveAsteroidsBuffer();
            if (pool == null) return true;

            float clearRadiusSq = clearRadius * clearRadius;

            for (int i = 0; i < pool.Count; i++)
            {
                var ast = pool[i];
                if (ast != null && ast.gameObject.activeSelf)
                {
                    float distSq = (ast.transform.position - worldPoint).sqrMagnitude;
                    // Bán kính bao thiên thạch chuẩn ~3.0m
                    float totalRadius = clearRadius + 3.0f;
                    if (distSq < totalRadius * totalRadius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tự động đẩy dạt các thiên thạch/vật cản chặn đường để bảo chứng tuyệt đối người chơi luôn có lối thoát.
        /// </summary>
        private void CarveSafeGoldenPath(float startArcDist, float endArcDist)
        {
            if (VirtualRailAsteroidSpawner.Instance == null) return;

            var pool = VirtualRailAsteroidSpawner.Instance.GetActiveAsteroidsBuffer();
            if (pool == null) return;

            var cfg = config != null ? config : (VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance.config : null);
            float safeCorridorRadius = (cfg != null) ? cfg.safeCorridorRadius : 6.0f;

            for (int i = 0; i < pool.Count; i++)
            {
                var ast = pool[i];
                if (ast != null && ast.gameObject.activeSelf)
                {
                    Vector3 pos = ast.transform.position;

                    // Tính toán cự ly tương đối dọc theo đường ray
                    float arc = pos.z;
                    Vector3 spinePos = new Vector3(0f, 8.5f, pos.z);
                    Vector3 spineRight = Vector3.right;

                    if (VirtualRailSplineChunker.Instance != null && VirtualRailAnchor.Instance != null && VirtualRailAnchor.Instance.UseProceduralSpline)
                    {
                        // Chiếu vị trí thiên thạch lên hệ quy chiếu Spline
                        arc = VirtualRailAnchor.Instance.AccumulatedArcDistance + (pos.z - VirtualRailAnchor.Instance.transform.position.z);
                        if (arc >= startArcDist - 20f && arc <= endArcDist + 20f)
                        {
                            VirtualRailSplineChunker.Instance.EvaluateFull(arc, out spinePos, out Vector3 tan, out Quaternion rot, out _);
                            spineRight = rot * Vector3.right;
                        }
                    }

                    if (arc >= startArcDist - 20f && arc <= endArcDist + 20f)
                    {
                        Vector3 diff = pos - spinePos;
                        float dist = diff.magnitude;
                        float minClearance = safeCorridorRadius + 3.5f;

                        if (dist < minClearance)
                        {
                            // Đẩy dạt thiên thạch ra phía ngoài vành đai an toàn
                            Vector3 pushDir = diff.sqrMagnitude > 0.01f ? diff.normalized : spineRight;
                            ast.transform.position = spinePos + pushDir * minClearance;
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (VirtualRailSplineChunker.Instance == null) return;

            // Vẽ trực quan khung kiểm định Golden Path Clearance Box trên Scene View
            Gizmos.color = new Color(1.0f, 0.84f, 0.0f, 0.45f); // Vàng kim (Gold)
            float currentDist = VirtualRailSplineChunker.Instance.AccumulatedDistance;

            for (float s = currentDist; s < currentDist + 300f; s += sliceInterval)
            {
                VirtualRailSplineChunker.Instance.EvaluateFull(s, out Vector3 center, out Vector3 tan, out Quaternion rot, out _);
                Matrix4x4 matrix = Matrix4x4.TRS(center + rot * new Vector3(0f, 8.5f, 0f), rot, Vector3.one);
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(clearanceWidth, clearanceHeight, 2.0f));
            }
        }
    }
}
