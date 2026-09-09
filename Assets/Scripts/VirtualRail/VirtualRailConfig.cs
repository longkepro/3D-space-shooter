using UnityEngine;

namespace VirtualRail
{
    [CreateAssetMenu(fileName = "VirtualRailConfig", menuName = "Virtual Rail/Config", order = 1)]
    public class VirtualRailConfig : ScriptableObject
    {
        [Header("1. Rail Movement")]
        [Tooltip("Tốc độ tiến dọc trục Z của đường ray")]
        public float forwardSpeed = 28f;
        [Tooltip("Hệ số tăng tốc khi boost")]
        public float boostMultiplier = 1.6f;

        [Header("2. Convergence (Cự ly hội tụ)")]
        [Tooltip("Khoảng cách Z hội tụ đạn đạo (120m - 200m)")]
        public float convergenceDistance = 150f;

        [Header("3. Viewport Frustum Ratios (Khung biên động theo Camera)")]
        [Tooltip("Bật tính toán khung biên tự động theo tỷ lệ Camera Frustum")]
        public bool useDynamicFrustumBounds = true;
        [Range(0.5f, 0.75f)]
        [Tooltip("Khung biên tàu (Inner Box): 60% - 65% Viewport")]
        public float shipViewportRatio = 0.75f;
        [Range(0.75f, 0.95f)]
        [Tooltip("Khung biên tâm ngắm (Outer Box): 80% - 85% Viewport")]
        public float reticleViewportRatio = 0.85f;

        [Header("4. Fallback Static Limits (Dự phòng khi không có Camera)")]
        public float reticleLimitX = 30f;
        public float reticleLimitY = 18f;
        public float shipLimitX = 22f;
        [Tooltip("Tốc độ bay tự do của tâm ngắm theo trục ngang X (m/s - Hướng A)")]
        public float reticleSpeedX = 140f;
        [Tooltip("Tốc độ bay tự do của tâm ngắm theo trục dọc Y (m/s - Hướng A)")]
        public float reticleSpeedY = 85f;
        [Tooltip("Bật ánh xạ trực tiếp (tự động kéo về tâm). Tắt (Hướng A) để tự do di chuyển, nhả phím đứng yên")]
        public bool useDirectAnalogMapping = false;

        [Header("5. Independent Control & Mouse Aim")]
        [Tooltip("Bật chế độ điều khiển độc lập: Tàu dùng phím/analog, Tâm ngắm dùng chuột")]
        public bool enableIndependentControls = true;
        [Tooltip("Bật điều khiển tâm ngắm bằng chuột")]
        public bool enableMouseAim = true;
        [Tooltip("Tự động xả đạn khi tâm ngắm/chuột di chuyển")]
        public bool autoFireOnAimMove = true;
        [Tooltip("Ngưỡng phát hiện di chuyển chuột tối thiểu (pixels/frame)")]
        public float aimMoveDeadzone = 0.5f;
        [Tooltip("Thời gian duy trì bắn tự động sau khi chuột dừng (giây) - chống giật cục micro-pause")]
        public float autoFireHoldTime = 0.08f;
        [Tooltip("Tốc độ bay tự do của tàu theo trục ngang X (m/s)")]
        public float shipSpeedX = 140f;
        [Tooltip("Tốc độ bay tự do của tàu theo trục dọc Y (m/s)")]
        public float shipSpeedY = 85f;

        [Header("5b. Android & Mobile Touch Controls")]
        [Tooltip("Bắt buộc hiển thị cần gạt ảo trên Unity Editor để kiểm thử")]
        public bool forceShowMobileUI = true;
        [Tooltip("Bán kính kéo của cần gạt ảo (pixels)")]
        public float joystickHandleRange = 65f;
        [Tooltip("Vùng chết của cần gạt ảo")]
        public float joystickDeadZone = 0.1f;
        [Tooltip("Độ nhạy di chuyển tâm ngắm của cần gạt phải")]
        public float mobileAimSensitivity = 1.0f;

        [Header("5c. Golden-Ratio Kinematics (Tỷ Lệ Vàng Động Học)")]
        [Tooltip("Bật động học tỷ lệ vàng: Vx = 50% Vz, Vy = 35% Vz, khống chế trôi dạt S_drift <= 2.5m")]
        public bool useGoldenRatioMotion = true;
        [Range(0.35f, 0.65f)]
        [Tooltip("Hệ số phân bổ trục ngang X so với tốc độ tiến Z (chuẩn 0.50)")]
        public float goldenRatioX = 0.50f;
        [Range(0.20f, 0.50f)]
        [Tooltip("Hệ số phân bổ trục dọc Y so với tốc độ tiến Z (chuẩn 0.35 cho khung 16:9)")]
        public float goldenRatioY = 0.35f;
        [Tooltip("Quãng đường trôi dạt an toàn tối đa (mét - chuẩn <= 2.5m)")]
        public float maxSafeDriftDistance = 2.5f;
        [Tooltip("Độ trễ đàn hồi cơ sở khi tàu bay chậm (giây)")]
        public float goldenRatioBaseLag = 0.10f;

        /// <summary>
        /// Tính toán độ trễ đàn hồi thích ứng (Adaptive Damping) để đảm bảo quãng đường trôi dạt S_drift <= maxSafeDriftDistance.
        /// </summary>
        public float CalculateAdaptiveLag(float currentForwardSpeed)
        {
            if (!useGoldenRatioMotion) return smoothDampLag;
            float safeSpeed = Mathf.Max(currentForwardSpeed, 1f);
            return Mathf.Min(goldenRatioBaseLag, maxSafeDriftDistance / safeSpeed);
        }

        [Header("6. Ship Tracking Settings (Chế độ phụ thuộc cũ)")]
        [Range(0.05f, 0.2f)]
        [Tooltip("Độ trễ lò xo suy giảm chấn (0.08s - 0.12s)")]
        public float smoothDampLag = 0.1f;

        [Header("6. Aerodynamic Rotation (Banking/Pitch/Yaw)")]
        public float rollFactor = 2.5f;
        public float maxRollAngle = 65f;
        public float pitchFactor = 2.0f;
        public float maxPitchAngle = 30f;
        public float yawFactor = 1.2f;
        public float maxYawAngle = 25f;
        public float rotationSlerpSpeed = 12f;

        [Header("7. Edge Angle Damping (Hãm góc kịch biên)")]
        public bool enableEdgeDamping = true;
        public float minEdgeRollAngle = 14f;

        [Header("8. Camera Settings (Cố định & Thoáng đãng như Star Fox gốc)")]
        [Tooltip("Khoảng cách lùi ra xa sau đuôi tàu (22m cho Classic Wide, 35m cho Telephoto Hướng 3)")]
        public float cameraDistance = 35f;
        [Tooltip("Độ cao đặt camera (5.5m cho Classic Wide, 8.0m cho Telephoto Hướng 3)")]
        public float cameraHeight = 8.0f;
        [Range(25f, 75f)]
        [Tooltip("Góc mở Field of View của Camera (60 độ cho Classic Wide, 40 độ cho Telephoto Hướng 3 giúp triệt tiêu góc nhìn xiên mép màn hình)")]
        public float cameraFOV = 40f;
        [Tooltip("Góc chúc cố định của camera dọc theo đường ray (độ) - giữ đường chân trời bất động")]
        public float fixedCameraPitch = 5.0f;
        [Tooltip("Độ dịch chuyển vị trí camera theo tâm ngắm")]
        public float cameraPanFactor = 0.05f;
        [Tooltip("Bật/tắt xoay liếc theo tàu (MẶC ĐỊNH TẮT để camera không bị lắc lư gây chóng mặt)")]
        public bool enableDynamicLookAt = false;
        [Range(0f, 1f)]
        public float cameraLookAtWeight = 0.2f;
        public float cameraSmoothSpeed = 8f;

        [Header("9. Aim Assist & Bullet Magnetism")]
        public float magnetismRadius = 2.5f;
        public float bulletSpeed = 220f;
        public LayerMask enemyLayer = ~0;

        [Header("10. Charge Shot & Homing")]
        public float chargeTime = 1.2f;
        public float lockConeAngle = 18f;
        public float lockMaxDistance = 250f;

        [Header("11. Segmented Laser Bolts (Hướng 1 - Đoạn đạn laser)")]
        [Tooltip("Bật chế độ bắn đạn laser thành từng đoạn (Direction 1). Nếu tắt sẽ quay lại tia tức thời (Layer 2 Undo)")]
        public bool useSegmentedLaser = true;
        [Tooltip("Tốc độ bay của đoạn đạn laser (m/s)")]
        public float laserBoltSpeed = 220f;
        [Tooltip("Chiều dài của mỗi đoạn đạn laser (m) - tăng lên để nhìn rõ và ấn tượng")]
        public float laserBoltLength = 8.5f;
        [Tooltip("Độ rộng của tia đạn laser - tăng lên để nhìn rõ trên màn hình")]
        public float laserBoltWidth = 0.85f;
        [Tooltip("Thời gian sống tối đa của viên đạn (giây)")]
        public float laserBoltLifetime = 2.0f;
        [Tooltip("Màu sắc của đoạn laser - màu Neon Cyan rực rỡ")]
        public Color laserColor = new Color(0.1f, 1f, 0.85f, 1f);
        [Tooltip("Vật liệu tùy chỉnh cho đoạn đạn laser")]
        public Material laserBoltMaterial;

        [Header("12. Wave Spawner & VFX Optimization")]
        [Tooltip("Bật bộ điều phối đợt tấn công đường ray (sinh quái đón đầu phía trước trục Z)")]
        public bool useRailWaveSpawner = true;
        [Tooltip("Khoảng thời gian giữa các đợt quái xuất hiện (giây)")]
        public float waveSpawnInterval = 3.5f;
        [Tooltip("Khoảng cách xuất hiện quái vật phía trước tàu (m)")]
        public float waveSpawnDistance = 220f;
        [Tooltip("Bật tối ưu hóa bộ đệm vụ nổ (0 GC Alloc)")]
        public bool useOptimizedVFXPool = true;

        [Header("13. Asteroid Streaming Spawner")]
        [Tooltip("Bật bộ điều phối thiên thạch đón đầu đường ray (0 GC Alloc)")]
        public bool useRailAsteroidSpawner = true;
        [Tooltip("Cự ly sinh thiên thạch phía trước tàu (m)")]
        public float asteroidSpawnDistance = 250f;
        [Tooltip("Khoảng cách bước Z giữa các cụm thiên thạch (m)")]
        public float asteroidZInterval = 18f;
        [Tooltip("Kích thước bộ đệm Object Pool thiên thạch")]
        public int asteroidPoolSize = 32;

        [Header("14. Combat & Counter-Play (TDD v1.0.0 Phần I)")]
        [Tooltip("Bật cơ chế lộn cánh phản xạ đạn 90 độ (Barrel Roll Deflection)")]
        public bool enableBarrelRollDeflection = true;
        [Tooltip("Thời gian lộn vòng Barrel Roll (giây)")]
        public float barrelRollDuration = 0.40f;
        [Tooltip("Bật bom thông minh quét sạch màn hình (Smart Bomb)")]
        public bool enableSmartBomb = true;
        [Tooltip("Số lượng bom thông minh ban đầu")]
        public int initialSmartBombs = 3;
        [Tooltip("Bán kính nổ lan của phát bắn tụ lực Charge Shot Splash (mét)")]
        public float chargeShotSplashRadius = 10f;
        [Tooltip("Bật sử dụng đạn vật lý Plasma Orb cho quái thay vì hitscan cũ")]
        public bool usePhysicalEnemyProjectiles = true;

        /// <summary>
        /// Khung biên Frustum bất đối xứng (do Camera đặt trên cao Y = 5.5m và chúc xuống 5 độ).
        /// </summary>
        [System.Serializable]
        public struct FrustumBounds
        {
            public float minX;
            public float maxX;
            public float minY;
            public float maxY;
            public Vector2 opticalCenter;

            public float Width => Mathf.Max(0.001f, maxX - minX);
            public float Height => Mathf.Max(0.001f, maxY - minY);
            public float HalfWidth => Width * 0.5f;
            public float HalfHeight => Height * 0.5f;
            public Vector2 Center => new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            public FrustumBounds(float minX, float maxX, float minY, float maxY, Vector2 opticalCenter)
            {
                this.minX = minX;
                this.maxX = maxX;
                this.minY = minY;
                this.maxY = maxY;
                this.opticalCenter = opticalCenter;
            }

            public Vector2 Clamp(Vector2 pos)
            {
                return new Vector2(
                    Mathf.Clamp(pos.x, minX, maxX),
                    Mathf.Clamp(pos.y, minY, maxY)
                );
            }
        }

        /// <summary>
        /// Tính toán khung biên Frustum chính xác tuyệt đối tại cự ly targetLocalZ so với Anchor.
        /// Sử dụng kỹ thuật Ray-Plane Intersection chiếu từ Viewport của Camera để triệt tiêu
        /// sai số do góc chúc fixedCameraPitch và độ cao cameraHeight.
        /// </summary>
        public FrustumBounds CalculateFrustumBounds(Camera cam, VirtualRailAnchor anchor, float targetLocalZ, float viewportRatio)
        {
            if (cam != null && anchor != null && useDynamicFrustumBounds)
            {
                float uMin = 0.5f - 0.5f * viewportRatio;
                float uMax = 0.5f + 0.5f * viewportRatio;
                float vMin = 0.5f - 0.5f * viewportRatio;
                float vMax = 0.5f + 0.5f * viewportRatio;

                Vector3 planePoint = anchor.transform.position + anchor.transform.forward * targetLocalZ;
                Plane plane = new Plane(anchor.transform.forward, planePoint);

                Ray rayBL = cam.ViewportPointToRay(new Vector3(uMin, vMin, 0f));
                Ray rayTR = cam.ViewportPointToRay(new Vector3(uMax, vMax, 0f));
                Ray rayTL = cam.ViewportPointToRay(new Vector3(uMin, vMax, 0f));
                Ray rayBR = cam.ViewportPointToRay(new Vector3(uMax, vMin, 0f));
                Ray rayCenter = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                if (plane.Raycast(rayBL, out float dBL) &&
                    plane.Raycast(rayTR, out float dTR) &&
                    plane.Raycast(rayTL, out float dTL) &&
                    plane.Raycast(rayBR, out float dBR) &&
                    plane.Raycast(rayCenter, out float dCenter))
                {
                    Vector3 pBL = anchor.ToLocalPoint(rayBL.GetPoint(dBL));
                    Vector3 pTR = anchor.ToLocalPoint(rayTR.GetPoint(dTR));
                    Vector3 pTL = anchor.ToLocalPoint(rayTL.GetPoint(dTL));
                    Vector3 pBR = anchor.ToLocalPoint(rayBR.GetPoint(dBR));
                    Vector3 pCenter = anchor.ToLocalPoint(rayCenter.GetPoint(dCenter));

                    float minX = Mathf.Min(Mathf.Min(pBL.x, pTL.x), Mathf.Min(pTR.x, pBR.x));
                    float maxX = Mathf.Max(Mathf.Max(pBL.x, pTL.x), Mathf.Max(pTR.x, pBR.x));
                    float minY = Mathf.Min(Mathf.Min(pBL.y, pTL.y), Mathf.Min(pTR.y, pBR.y));
                    float maxY = Mathf.Max(Mathf.Max(pBL.y, pTL.y), Mathf.Max(pTR.y, pBR.y));

                    return new FrustumBounds(minX, maxX, minY, maxY, new Vector2(pCenter.x, pCenter.y));
                }
            }

            // Fallback: Công thức lượng giác chiếu giải tích (Analytical Projection)
            return CalculateAnalyticalBounds(cam, targetLocalZ, viewportRatio);
        }

        /// <summary>
        /// Công thức chiếu giải tích chính xác khi chưa có Camera trong Scene hoặc chạy kiểm thử.
        /// </summary>
        public FrustumBounds CalculateAnalyticalBounds(Camera cam, float targetLocalZ, float viewportRatio)
        {
            float fovY = (cam != null ? cam.fieldOfView : cameraFOV) * Mathf.Deg2Rad;
            float aspect = (cam != null ? cam.aspect : (16f / 9f));
            float L = targetLocalZ + cameraDistance;
            float pitchRad = fixedCameraPitch * Mathf.Deg2Rad;
            float cosP = Mathf.Cos(pitchRad);
            float sinP = Mathf.Sin(pitchRad);

            float halfRatio = 0.5f * viewportRatio;
            float vPrimeTop = halfRatio * 2f * Mathf.Tan(fovY * 0.5f);
            float vPrimeBot = -vPrimeTop;

            float denomTop = cosP + vPrimeTop * sinP;
            float denomBot = cosP + vPrimeBot * sinP;

            float yMax = cameraHeight + (L * (cosP * vPrimeTop - sinP)) / denomTop;
            float yMin = cameraHeight + (L * (cosP * vPrimeBot - sinP)) / denomBot;
            float yCenter = cameraHeight - L * Mathf.Tan(pitchRad);

            float uPrimeRight = halfRatio * 2f * Mathf.Tan(fovY * 0.5f) * aspect;
            float xMax = (L * uPrimeRight) / denomTop;
            float xMin = -xMax;

            return new FrustumBounds(xMin, xMax, yMin, yMax, new Vector2(0f, yCenter));
        }

        /// <summary>
        /// Giữ tương thích ngược với code cũ (trả về bán kính đối xứng gần đúng).
        /// </summary>
        public Vector2 CalculateFrustumLimit(Camera cam, float distanceZ, float viewportRatio)
        {
            FrustumBounds bounds = CalculateAnalyticalBounds(cam, distanceZ - cameraDistance, viewportRatio);
            return new Vector2(bounds.HalfWidth, bounds.HalfHeight);
        }
    }
}
