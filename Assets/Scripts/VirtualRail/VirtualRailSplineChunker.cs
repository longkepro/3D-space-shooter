using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Cấu trúc dữ liệu đại diện cho một đoạn ray Hermite Spline bậc 5 (Quintic Hermite Spline).
    /// Đảm bảo tính trơn hình học cấp 2 (C2 Continuity) tuyệt đối tại hai đầu tiếp giáp:
    /// - C0: Tọa độ tiếp giáp (Position continuity).
    /// - C1: Tiếp tuyến vận tốc (Velocity / tangent continuity).
    /// - C2: Pháp tuyến độ cong và gia tốc (Curvature / acceleration continuity).
    /// 0 GC Alloc trong game loop nhờ biểu diễn dạng Struct thuần túy.
    /// </summary>
    [System.Serializable]
    public struct SplineChunk
    {
        public int chunkIndex;
        public float startArcDistance;
        public float endArcDistance;
        public float length;

        // Điều kiện biên tại u = 0
        public Vector3 p0;
        public Vector3 v0; // Tangent T0 * length
        public Vector3 a0; // Curvature K0 * length^2

        // Điều kiện biên tại u = 1
        public Vector3 p1;
        public Vector3 v1; // Tangent T1 * length
        public Vector3 a1; // Curvature K1 * length^2

        // Hệ số đa thức bậc 5 theo phương pháp Horner:
        // P(u) = c0 + c1*u + c2*u^2 + c3*u^3 + c4*u^4 + c5*u^5
        public Vector3 c0;
        public Vector3 c1;
        public Vector3 c2;
        public Vector3 c3;
        public Vector3 c4;
        public Vector3 c5;

        /// <summary>
        /// Tính toán hệ số đa thức Hermite bậc 5 từ các điều kiện biên C0, C1, C2.
        /// </summary>
        public void ComputeCoefficients()
        {
            Vector3 deltaP = p1 - p0 - v0 - 0.5f * a0;
            Vector3 deltaV = v1 - v0 - a0;
            Vector3 deltaA = a1 - a0;

            c0 = p0;
            c1 = v0;
            c2 = 0.5f * a0;
            c3 = 10f * deltaP - 4f * deltaV + 0.5f * deltaA;
            c4 = -15f * deltaP + 7f * deltaV - deltaA;
            c5 = 6f * deltaP - 3f * deltaV + 0.5f * deltaA;
        }

        /// <summary>
        /// Ước lượng vị trí P(u), đạo hàm bậc nhất P'(u) và đạo hàm bậc hai P''(u)
        /// theo giải thuật Horner tối ưu hóa tốc độ tính toán (0 GC Alloc).
        /// </summary>
        public void Evaluate(float u, out Vector3 position, out Vector3 velocity, out Vector3 acceleration)
        {
            u = Mathf.Clamp01(u);
            position = ((((c5 * u + c4) * u + c3) * u + c2) * u + c1) * u + c0;
            velocity = (((5f * c5 * u + 4f * c4) * u + 3f * c3) * u + 2f * c2) * u + c1;
            acceleration = ((20f * c5 * u + 12f * c4) * u + 6f * c3) * u + 2f * c2;
        }
    }

    /// <summary>
    /// Bộ Ghép Nối Đường Ray Vô Hạn (Procedural Hermite Spline Chunker - Task 4.1).
    /// Tuân thủ nghiêm ngặt ENDLESS_SCORING_SPECIFICATION.md (Mục 1.1):
    /// - Sinh các đoạn ray thủ tục theo từng đoạn Chunk L = 500m.
    /// - Duy trì Look-ahead Ring Buffer tối thiểu 3 Chunks đồng thời:
    ///   [0] Chunk N-1 (Phía sau): Nơi thu hồi và dọn dẹp thực thể.
    ///   [1] Chunk N   (Hiện tại): Vị trí phi thuyền đang tác chiến.
    ///   [2] Chunk N+1 (Phía trước): Tiền tính toán đón đầu tọa độ sinh quái / chướng ngại vật.
    /// - Đảm bảo tính trơn C2 tuyệt đối tại giao diện tiếp nối các Chunk.
    /// - Khống chế bán kính cong R_min >= 300m (Curvature kappa <= 0.0033 m^-1).
    /// - Nghiêng thân ray khí động học tự động (Aerodynamic Track Banking: phi_bank trong [-35 deg, +35 deg]).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailSplineChunker : MonoBehaviour
    {
        public static VirtualRailSplineChunker Instance { get; private set; }

        [Header("1. Chunk Configuration")]
        [Tooltip("Chiều dài cung tiêu chuẩn của mỗi Chunk ray (m - chuẩn 500m)")]
        [SerializeField] private float chunkLength = 500f;

        [Tooltip("Bán kính cong tối thiểu cho phép (m - chuẩn 300m để đảm bảo độ êm ái khi bay)")]
        [SerializeField] private float minCurvatureRadius = 300f;

        [Header("2. Aerodynamic Track Banking")]
        [Tooltip("Góc nghiêng ray tối đa (độ - chuẩn 35 độ)")]
        [SerializeField] private float maxBankAngle = 35f;

        [Tooltip("Hệ số khuếch đại góc nghiêng theo độ cong ngang (Gain)")]
        [SerializeField] private float bankingGain = 10500f;

        [Header("3. Flight Corridor Constraints")]
        [Tooltip("Biên độ dạt ngang tối đa của hành lang ray (m)")]
        [SerializeField] private float maxCorridorX = 120f;

        [Tooltip("Cao độ tối thiểu của hành lang ray (m)")]
        [SerializeField] private float minCorridorY = -25f;

        [Tooltip("Cao độ tối đa của hành lang ray (m)")]
        [SerializeField] private float maxCorridorY = 55f;

        [Header("4. Runtime Telemetry")]
        [SerializeField] private float accumulatedDistance = 0f;
        [SerializeField] private int currentChunkId = 0;
        [SerializeField] private float currentBankAngle = 0f;

        // Look-ahead Ring Buffer gồm đúng 3 Chunks (0 GC Alloc)
        private readonly SplineChunk[] _chunks = new SplineChunk[3];
        private int _chunkCounter = 0;
        private bool _isInitialized = false;

        public float ChunkLength => chunkLength;
        public float MinCurvatureRadius => minCurvatureRadius;
        public float AccumulatedDistance => accumulatedDistance;
        public int CurrentChunkId => currentChunkId;
        public float CurrentBankAngle => currentBankAngle;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                Initialize(transform.position, transform.forward);
            }
        }

        /// <summary>
        /// Khởi tạo toàn bộ Ring Buffer 3 Chunks từ vị trí và hướng xuất phát ban đầu.
        /// </summary>
        public void Initialize(Vector3 startOrigin, Vector3 startForward)
        {
            if (startForward.sqrMagnitude < 0.001f)
            {
                startForward = Vector3.forward;
            }
            startForward.Normalize();

            accumulatedDistance = 0f;
            _chunkCounter = 0;

            // Chunk [0]: Đoạn phía sau (-500m -> 0m) làm đoạn dọn dẹp/lead-in
            SplineChunk chunkBehind = new SplineChunk
            {
                chunkIndex = _chunkCounter++,
                startArcDistance = -chunkLength,
                endArcDistance = 0f,
                length = chunkLength,
                p0 = startOrigin - startForward * chunkLength,
                v0 = startForward * chunkLength,
                a0 = Vector3.zero,
                p1 = startOrigin,
                v1 = startForward * chunkLength,
                a1 = Vector3.zero
            };
            chunkBehind.ComputeCoefficients();
            _chunks[0] = chunkBehind;

            // Chunk [1]: Đoạn hiện tại (0m -> 500m) đoạn bay xuất phát thẳng/êm ái
            SplineChunk chunkCurrent = new SplineChunk
            {
                chunkIndex = _chunkCounter++,
                startArcDistance = 0f,
                endArcDistance = chunkLength,
                length = chunkLength,
                p0 = chunkBehind.p1,
                v0 = chunkBehind.v1,
                a0 = chunkBehind.a1,
                p1 = chunkBehind.p1 + startForward * chunkLength,
                v1 = startForward * chunkLength,
                a1 = Vector3.zero
            };
            chunkCurrent.ComputeCoefficients();
            _chunks[1] = chunkCurrent;

            // Chunk [2]: Đoạn phía trước (500m -> 1000m) sinh thủ tục đón đầu
            SplineChunk chunkAhead = GenerateNextChunk(chunkCurrent, _chunkCounter++);
            _chunks[2] = chunkAhead;

            currentChunkId = _chunks[1].chunkIndex;
            _isInitialized = true;
        }

        /// <summary>
        /// Tịnh tiến cự ly dọc theo đường ray vô hạn.
        /// Tự động xoay vòng Ring Buffer khi vượt qua mốc tiếp giáp của Chunk hiện tại.
        /// </summary>
        public void Advance(float deltaDistance)
        {
            if (!_isInitialized) return;

            accumulatedDistance += deltaDistance;

            // Khi người chơi chạm tới điểm cuối của Chunk [1], dịch chuyển Ring Buffer:
            // [0] trở thành [1] cũ
            // [1] trở thành [2] cũ
            // [2] sinh Chunk mới tiếp theo (Chunk N+1)
            if (accumulatedDistance >= _chunks[1].endArcDistance)
            {
                _chunks[0] = _chunks[1];
                _chunks[1] = _chunks[2];
                _chunks[2] = GenerateNextChunk(_chunks[1], _chunkCounter++);
                currentChunkId = _chunks[1].chunkIndex;
            }
        }

        /// <summary>
        /// Sinh đoạn Chunk tiếp theo từ điểm cuối của Chunk tiền nhiệm.
        /// Đảm bảo tính trơn C2 tuyệt đối tại giao tiếp (p0 = p1_prev, v0 = v1_prev, a0 = a1_prev).
        /// Khống chế góc lượn để bán kính cong R >= 300m và giữ ray trong hành lang an toàn.
        /// </summary>
        private SplineChunk GenerateNextChunk(SplineChunk prev, int nextIndex)
        {
            Vector3 p0 = prev.p1;
            Vector3 v0 = prev.v1;
            Vector3 a0 = prev.a1;

            Vector3 t0 = v0.sqrMagnitude > 0.001f ? v0.normalized : Vector3.forward;

            // 1. Phân tích góc lái hiện tại
            float curYaw = Mathf.Atan2(t0.x, t0.z) * Mathf.Rad2Deg;
            float curPitch = -Mathf.Asin(Mathf.Clamp(t0.y, -0.99f, 0.99f)) * Mathf.Rad2Deg;

            // 2. Lực hồi quy tâm hành lang (Corridor Centering Bias)
            float targetYaw = 0f;
            if (p0.x > maxCorridorX * 0.5f)
            {
                targetYaw = Random.Range(-20f, -8f); // Đang lệch phải -> Lượn trái
            }
            else if (p0.x < -maxCorridorX * 0.5f)
            {
                targetYaw = Random.Range(8f, 20f);  // Đang lệch trái -> Lượn phải
            }
            else
            {
                targetYaw = Random.Range(-16f, 16f); // Nằm trong tâm -> Lượn tự do nhẹ nhàng
            }

            float targetPitch = 0f;
            if (p0.y > maxCorridorY * 0.7f)
            {
                targetPitch = Random.Range(6f, 14f);  // Quá cao -> Chúc mũi xuống
            }
            else if (p0.y < minCorridorY * 0.7f)
            {
                targetPitch = Random.Range(-14f, -6f); // Quá thấp -> Ngóc mũi lên
            }
            else
            {
                targetPitch = Random.Range(-8f, 8f);  // Cao độ an toàn -> Lượn dốc nhẹ
            }

            Vector3 desiredDir = Quaternion.Euler(targetPitch, targetYaw, 0f) * Vector3.forward;

            // 3. Khống chế giới hạn góc đổi hướng trong 500m để R_min >= 300m
            // deltaTheta_max = L / R_min = 500 / 300 = 1.67 rad (95 độ). Ta chọn trần an toàn 22 độ (~1300m)
            float maxTurnDegrees = 22f;
            Vector3 t1 = Vector3.RotateTowards(t0, desiredDir, maxTurnDegrees * Mathf.Deg2Rad, 0f).normalized;

            // 4. Vị trí điểm cuối P1 và vận tốc V1
            Vector3 avgTangent = (t0 + t1).normalized;
            Vector3 p1 = p0 + avgTangent * chunkLength;
            Vector3 v1 = t1 * chunkLength;

            // 5. Gia tốc tiếp giáp A1 (C2 Curvature Continuity)
            Vector3 a1 = (t1 - t0) * chunkLength;
            float maxCurvature = 1f / Mathf.Max(50f, minCurvatureRadius);
            float maxA = maxCurvature * (chunkLength * chunkLength);
            if (a1.sqrMagnitude > maxA * maxA)
            {
                a1 = a1.normalized * maxA;
            }

            SplineChunk chunk = new SplineChunk
            {
                chunkIndex = nextIndex,
                startArcDistance = prev.endArcDistance,
                endArcDistance = prev.endArcDistance + chunkLength,
                length = chunkLength,
                p0 = p0,
                v0 = v0,
                a0 = a0,
                p1 = p1,
                v1 = v1,
                a1 = a1
            };
            chunk.ComputeCoefficients();
            return chunk;
        }

        /// <summary>
        /// Đánh giá toàn diện trạng thái ray tại khoảng cách cung s (Position, Tangent, Rotation, BankAngle).
        /// Hoàn toàn 0 GC Alloc.
        /// </summary>
        public void EvaluateFull(float s, out Vector3 position, out Vector3 tangent, out Quaternion rotation, out float bankAngle)
        {
            // Xác định Chunk chứa khoảng cách cung s trong Ring Buffer
            int targetIndex = 1;
            if (s < _chunks[1].startArcDistance)
            {
                targetIndex = 0;
            }
            else if (s > _chunks[1].endArcDistance)
            {
                targetIndex = 2;
            }

            SplineChunk chunk = _chunks[targetIndex];
            float u = (s - chunk.startArcDistance) / chunk.length;
            u = Mathf.Clamp01(u);

            chunk.Evaluate(u, out position, out Vector3 velocity, out Vector3 acceleration);

            tangent = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector3.forward;

            // Vector độ cong K (Curvature Vector vuông góc với Tangent)
            Vector3 kVec = (acceleration - Vector3.Dot(tangent, acceleration) * tangent) / (chunk.length * chunk.length);

            // Khống chế độ cong theo R_min >= 300m
            float maxK = 1f / Mathf.Max(50f, minCurvatureRadius);
            if (kVec.sqrMagnitude > maxK * maxK)
            {
                kVec = kVec.normalized * maxK;
            }

            // Pháp tuyến ngang (Horizontal Right Binormal)
            Vector3 rightHoriz = Vector3.Cross(Vector3.up, tangent);
            if (rightHoriz.sqrMagnitude < 0.0001f)
            {
                rightHoriz = Vector3.right;
            }
            else
            {
                rightHoriz.Normalize();
            }

            // Độ cong theo phương ngang gây nghiêng cánh ray
            float lateralK = Vector3.Dot(kVec, rightHoriz);
            bankAngle = Mathf.Clamp(-lateralK * bankingGain, -maxBankAngle, maxBankAngle);

            // Vector Up vuông góc cục bộ kết hợp góc nghiêng Aerodynamic Track Banking
            Vector3 upProj = Vector3.Cross(tangent, rightHoriz).normalized;
            Vector3 normal = Quaternion.AngleAxis(bankAngle, tangent) * upProj;

            rotation = Quaternion.LookRotation(tangent, normal);

            if (targetIndex == 1)
            {
                currentBankAngle = bankAngle;
            }
        }

        public Vector3 EvaluatePosition(float s)
        {
            EvaluateFull(s, out Vector3 pos, out _, out _, out _);
            return pos;
        }

        public Vector3 EvaluateTangent(float s)
        {
            EvaluateFull(s, out _, out Vector3 tan, out _, out _);
            return tan;
        }

        public Quaternion EvaluateRotation(float s)
        {
            EvaluateFull(s, out _, out _, out Quaternion rot, out _);
            return rot;
        }

        public float EvaluateBankAngle(float s)
        {
            EvaluateFull(s, out _, out _, out _, out float bank);
            return bank;
        }

        /// <summary>
        /// Truy vấn vị trí và góc xoay đón đầu phía trước người chơi (dành riêng cho Spawner quái/vật phẩm).
        /// </summary>
        public Vector3 GetAheadPoint(float leadDistance, out Quaternion rotation)
        {
            float targetS = accumulatedDistance + leadDistance;
            EvaluateFull(targetS, out Vector3 pos, out _, out rotation, out _);
            return pos;
        }

        /// <summary>
        /// Kiểm tra xem một vật thể có nằm tụt lại phía sau người chơi quá cự ly an toàn hay không (cho despawn pool).
        /// </summary>
        public bool IsPointBehind(Vector3 worldPoint, float margin = 50f)
        {
            if (VirtualRailAnchor.Instance == null) return false;
            Vector3 local = VirtualRailAnchor.Instance.ToLocalPoint(worldPoint);
            return local.z < -margin;
        }

        /// <summary>
        /// Lấy thông tin một Chunk trong Ring Buffer (0 = Behind, 1 = Current, 2 = Ahead).
        /// </summary>
        public SplineChunk GetChunk(int bufferIndex)
        {
            if (bufferIndex < 0 || bufferIndex >= 3) return _chunks[1];
            return _chunks[bufferIndex];
        }

        /// <summary>
        /// Tái lập toàn bộ ray vô hạn về gốc mới (khi Respawn hoặc bắt đầu lại lượt chơi).
        /// </summary>
        public void ResetSpline(Vector3 startOrigin, Vector3 initialDirection)
        {
            Initialize(startOrigin, initialDirection);
        }

        private void OnDrawGizmos()
        {
            if (!_isInitialized) return;

            // Vẽ trực quan 3 Chunks trên Scene View Unity
            // [0] Behind: Xám đậm
            // [1] Current: Xanh lá tươi
            // [2] Ahead: Xanh Cyan
            Color[] chunkColors = new Color[]
            {
                new Color(0.4f, 0.4f, 0.4f, 0.6f),
                Color.green,
                Color.cyan
            };

            for (int c = 0; c < 3; c++)
            {
                Gizmos.color = chunkColors[c];
                SplineChunk chunk = _chunks[c];
                int steps = 25;
                Vector3 prevPoint = chunk.p0;

                for (int i = 1; i <= steps; i++)
                {
                    float u = (float)i / steps;
                    chunk.Evaluate(u, out Vector3 currPoint, out Vector3 vel, out _);
                    Gizmos.DrawLine(prevPoint, currPoint);

                    // Vẽ các thanh ray ngang mô phỏng đường ray không gian
                    if (i % 5 == 0)
                    {
                        Vector3 tan = vel.normalized;
                        Vector3 right = Vector3.Cross(Vector3.up, tan).normalized * 4f;
                        Gizmos.DrawLine(currPoint - right, currPoint + right);
                    }

                    prevPoint = currPoint;
                }
            }
        }
    }
}
