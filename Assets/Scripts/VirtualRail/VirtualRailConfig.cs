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
        [Tooltip("Bật hội tụ độ sâu động theo mục tiêu (Dynamic Depth Convergence). Tự động chụm nòng súng vào cự ly thực tế của kẻ địch thay vì cố định 150m")]
        public bool enableDynamicDepthConvergence = true;
        [Tooltip("Bán kính hình cầu quét tìm mục tiêu độ sâu dọc theo tia ngắm (mét - chuẩn 2.5m khớp với kích thước khung ngắm 56px)")]
        public float dynamicConvergenceRadius = 2.5f;
        [Tooltip("Cự ly tối đa mà tia ngắm quét tìm mục tiêu độ sâu động (mét)")]
        public float maxConvergenceScanDistance = 250f;

        [Header("2b. Dual Reticle System (Hệ tâm ngắm kép chuẩn Star Fox 64)")]
        [Tooltip("Bật hệ thống tâm ngắm kép: Tâm ngắm xa (Far Reticle) điều khiển bởi chuột/cần gạt và Tâm ngắm gần (Near Reticle) neo trước mũi tàu")]
        public bool enableDualReticle = true;
        [Tooltip("Cự ly của tâm ngắm gần trước mũi tàu (mét - chuẩn 35m)")]
        public float nearReticleDistance = 35f;
        [Tooltip("Kích thước tâm ngắm xa (pixels)")]
        public float farReticleSize = 56f;
        [Tooltip("Kích thước tâm ngắm gần (pixels)")]
        public float nearReticleSize = 28f;

        [Header("2c. Gimbaled Turret Weapons (Ụ súng xoay cơ học & Khóa tâm động)")]
        [Tooltip("Bật cơ chế ụ súng xoay 2 bậc tự do: Nòng súng tự động xoay đuổi theo con trỏ chuột trong nón hỏa lực")]
        public bool enableGimbalWeapon = true;
        [Tooltip("Vận tốc xoay góc của ụ súng (độ/giây - chuẩn 220 độ/giây)")]
        public float gimbalTrackingSpeed = 220f;
        [Tooltip("Góc quay tối đa của ụ súng so với mũi tàu (độ - chuẩn 35 độ)")]
        public float maxGimbalAngle = 35f;
        [Tooltip("Bán kính pixel màn hình để xác định 2 tâm đã khóa khớp nhau (chuẩn 12 pixels)")]
        public float gimbalLockThreshold = 12f;

        [Header("2d. Micro-Aim Alignment (Khí động học ngước mũi theo tâm)")]
        [Tooltip("Bật ngước/liếc mũi tàu theo góc ngắm. Giúp nòng súng luôn thẳng trục với tâm ngắm và tâm gần lồng khớp vào tâm xa")]
        public bool enableMicroAimAlignment = true;
        [Range(0.1f, 1.0f)]
        [Tooltip("Trọng số ảnh hưởng của góc ngắm lên mũi tàu")]
        public float microAimWeight = 1.0f;
        [Tooltip("Góc ngước chúc tối đa của mũi tàu theo tâm ngắm (độ)")]
        public float maxMicroAimPitch = 15f;
        [Tooltip("Góc liếc ngang tối đa của mũi tàu theo tâm ngắm (độ)")]
        public float maxMicroAimYaw = 18f;

        [Header("2d. Depth-Responsive Sci-Fi Reticle (Tâm ngắm phản hồi độ sâu 3D)")]
        [Tooltip("Bật tâm ngắm đơn Sci-Fi phản hồi độ sâu: Tự động co giãn theo khoảng cách mục tiêu và hiển thị đo đạc cự ly")]
        public bool enableDepthResponsiveReticle = true;
        [Tooltip("Bật hiển thị cự ly mục tiêu số hóa (Distance Telemetry HUD)")]
        public bool enableDistanceTelemetry = true;
        [Tooltip("Tỷ lệ thu nhỏ tối thiểu của tâm ngắm khi mục tiêu ở xa (chuẩn 0.7)")]
        public float minReticleScale = 0.7f;
        [Tooltip("Tỷ lệ phóng to tối đa của tâm ngắm khi mục tiêu ở gần (chuẩn 1.4)")]
        public float maxReticleScale = 1.4f;
        [Tooltip("Tốc độ chuyển đổi kích thước tâm ngắm (Lerp speed)")]
        public float reticleScaleLerpSpeed = 12f;

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
        [Tooltip("Bật cơ chế tự động bắn khi con trỏ chuột chạm/nằm trong vùng tâm ngắm (Hover-to-Fire Hướng 1)")]
        public bool enableMouseHoverFire = true;
        [Tooltip("Bán kính vùng chạm của chuột quanh tâm ngắm (pixels)")]
        public float reticleMouseTouchRadius = 45f;
        [Tooltip("Ngưỡng phát hiện di chuyển chuột tối thiểu (pixels/frame)")]
        public float aimMoveDeadzone = 0.5f;
        [Tooltip("Thời gian duy trì bắn tự động sau khi chuột dừng (giây) - chống giật cục micro-pause")]
        public float autoFireHoldTime = 0.08f;
        [Tooltip("Tốc độ bay tự do của tàu theo trục ngang X (m/s)")]
        public float shipSpeedX = 140f;
        [Tooltip("Tốc độ bay tự do của tàu theo trục dọc Y (m/s)")]
        public float shipSpeedY = 85f;

        [Header("5b. Android & Mobile Touch Controls")]
        [Tooltip("Bật/Tắt tổng thể cụm UI cảm ứng Mobile (Master Switch)")]
        public bool enableMobileUI = true;
        [Tooltip("Bắt buộc hiển thị cần gạt ảo trên Unity Editor để kiểm thử")]
        public bool forceShowMobileUI = false;
        [Tooltip("Phím tắt bàn phím để bật/tắt nhanh giao diện Mobile UI khi đang chơi (Mặc định F1)")]
        public KeyCode toggleMobileUIKey = KeyCode.F1;
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
        [Tooltip("Bật phễu từ tính uốn nhẹ đường đạn về kẻ địch")]
        public bool enableBulletMagnetism = false;
        public float magnetismRadius = 2.5f;
        [Tooltip("Góc lệch tối đa mà phễu từ tính được phép uốn nắn hướng đạn (độ)")]
        public float magnetismMaxAngle = 6.0f;
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
        [Tooltip("Bán kính quét va chạm của đạn laser (m) - chuẩn 1.0m giúp 2 nòng phủ kín hoàn toàn khoảng hở 3m giữa 2 cánh tàu")]
        public float laserBoltCollisionRadius = 1.0f;
        [Tooltip("Bật thừa kế vận tốc né ngang của tàu vào viên đạn khi xả đạn liên hoàn")]
        public bool enableLateralVelocityInheritance = false;
        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ thừa kế vận tốc né ngang của tàu (chuẩn 0.85)")]
        public float lateralVelocityWeight = 0.85f;
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
        public float asteroidZInterval = 50f;
        [Tooltip("Kích thước bộ đệm Object Pool thiên thạch")]
        public int asteroidPoolSize = 32;
        [Tooltip("Bật thuật toán quy hoạch làn bay an toàn đảm bảo (Guaranteed Safe Corridor Trench)")]
        public bool enableSafeCorridorPath = true;
        [Tooltip("Bán kính vùng ống an toàn không có thiên thạch (mét) - Sải cánh tàu 2.5m + đệm 3m")]
        public float safeCorridorRadius = 5.5f;
        [Tooltip("Tần số sóng uốn lượn của luồng an toàn dọc theo trục Z")]
        public float safePathWiggleFrequency = 0.012f;
        [Tooltip("Biên độ uốn lượn chiều ngang X của luồng an toàn (mét)")]
        public float safePathWiggleAmplitudeX = 12f;
        [Tooltip("Biên độ uốn lượn chiều cao Y của luồng an toàn (mét)")]
        public float safePathWiggleAmplitudeY = 4.5f;

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

        [Header("15. Spatial Vectors & 4-Phase Lifecycle FSM (TDD v1.0.0 Phần II)")]
        [Tooltip("Bật hệ thống 10 Vector xuất hiện không gian đa hướng")]
        public bool useSpatialVectors = true;
        [Tooltip("Bật máy trạng thái vòng đời 4 pha (Telegraph -> Approach -> Action -> Exit)")]
        public bool enable4PhaseLifecycle = true;
        [Tooltip("Thời gian pha báo hiệu Telegraph (giây)")]
        public float telegraphDuration = 0.5f;
        [Tooltip("Thời gian pha tiếp cận Approach (giây)")]
        public float approachDuration = 1.2f;
        [Tooltip("Thời gian pha tác chiến Action (giây)")]
        public float actionDuration = 2.5f;
        [Tooltip("Thời gian pha thoát ly Exit (giây)")]
        public float exitDuration = 1.0f;

        [Header("16. Formation Choreography & Panic Scatter (TDD v1.0.0 Phần III)")]
        [Tooltip("Bật biên đạo phi đội hình bay chữ V/Hàng ngang/Xoắn ốc")]
        public bool enableFormations = true;
        [Tooltip("Cự ly xuất hiện ban đầu của phi đội hình bay phía trước tàu (mét) - Chuẩn Star Fox 64: 100m - 120m")]
        public float formationSpawnDistance = 110f;
        [Tooltip("Vận tốc tiếp cận tương đối của tàu người chơi với đội hình (m/s) - Tàu đuổi kịp đội hình dần để tác chiến")]
        public float formationClosingSpeed = 35f;
        [Tooltip("Thời gian ráp nối tự do bằng đường cong Cubic Bézier 3D (giây)")]
        public float formationAssemblyTime = 1.2f;
        [Tooltip("Bật cơ chế phân rã hoảng loạn Panic Scatter khi mất tàu chỉ huy")]
        public bool enablePanicScatter = true;
        [Tooltip("Tần suất xuất hiện đợt đội hình (cứ sau mỗi X đợt quái)")]
        public int formationIntervalWaves = 3;

        [Header("17. Enemy Velocity Sync & Engagement Distance (Option 1)")]
        [Tooltip("Bật cơ chế đồng bộ vận tốc ray và cự ly ghìm giao chiến cho kẻ địch (Star Fox 64 Arcade Pacing)")]
        public bool enableEnemyRailSync = true;
        [Tooltip("Cự ly ghìm giao chiến phía trước tàu người chơi (mét - chuẩn 55m - 65m). Khi đạt cự ly này, kẻ địch khóa đồng tốc với ray để tạo cửa sổ tác chiến")]
        public float enemyHoldDistance = 60f;
        [Tooltip("Vận tốc tiếp cận tương đối của kẻ địch khi ở xa (m/s - chuẩn 18m/s - 25m/s). Kẻ địch bay tới mượt mà thay vì lao thẳng vào tàu")]
        public float enemyClosingSpeed = 20f;
        [Tooltip("Cự ly ghìm đội hình bay phía trước tàu người chơi (mét - chuẩn 60m - 70m)")]
        public float formationHoldDistance = 65f;

        [Header("18. Enemy & Obstacle Modularization (TDD v1.0.0 Phần I & II)")]
        [Tooltip("Bật phân loại kẻ địch chuyên sâu (Scout, Flanker, Interceptor, SquadLeader)")]
        public bool enableModularEnemies = true;
        [Tooltip("Bật phân loại thiên thạch có máu (Destructible) vs bất hoại (Indestructible)")]
        public bool enableDestructibleAsteroids = true;
        [Tooltip("Tỉ lệ xuất hiện thiên thạch bất hoại Monolith (0.0 - 1.0)")]
        [Range(0f, 1f)] public float indestructibleAsteroidChance = 0.25f;
        [Tooltip("Bật xuất hiện Vòng Năng Lượng / Vật phẩm thu thập (Collectible Rings)")]
        public bool enableCollectibleRings = true;
        [Tooltip("Xác suất rơi/sinh Vòng năng lượng dọc luồng bay (0.0 - 1.0)")]
        [Range(0f, 1f)] public float ringSpawnRate = 0.35f;

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
