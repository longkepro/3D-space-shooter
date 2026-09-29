using System;
using UnityEngine;

namespace VirtualRail
{
    public enum StageClimaxType
    {
        DistanceReached,
        EliminateWaveCount,
        DefeatBoss
    }

    [Serializable]
    public struct StageRailSettings
    {
        [Tooltip("Tốc độ tiến dọc trục Z của đường ray (m/s)")]
        public float forwardSpeed;

        [Tooltip("Quãng đường Z của màn chơi trước khi bước vào cao trào (m)")]
        public float stageDistanceZ;

        [Tooltip("Thời gian lướt nhảy vọt Hyperspace Warp khi chuyển màn (giây)")]
        public float warpDuration;

        [Tooltip("Hệ số tăng tốc khi lướt Hyperspace Warp")]
        public float warpSpeedMultiplier;

        public static StageRailSettings Default => new StageRailSettings
        {
            forwardSpeed = 28f,
            stageDistanceZ = 1500f,
            warpDuration = 2.5f,
            warpSpeedMultiplier = 3.2f
        };
    }

    [Serializable]
    public struct StageEnvironmentSettings
    {
        [Tooltip("Skybox riêng của màn chơi")]
        public Material skyboxMaterial;

        [Tooltip("Màn chiếu chân trời HD trực diện (Horizon Backdrop)")]
        public Material backdropMaterial;

        [Tooltip("Ảnh nền chân trời 4K trực tiếp (Kéo thả trực tiếp Texture2D không cần tạo Material riêng)")]
        public Texture2D backdropTexture;

        [Range(0.1f, 3.0f)]
        [Tooltip("Độ sáng của bức ảnh nền (Exposure Boost - chuẩn 1.0)")]
        public float backdropExposure;

        [Tooltip("Màu nhuộm sắc độ bức ảnh nền (Color Tint)")]
        public Color backdropTint;

        [Tooltip("Màu ánh sáng môi trường Ambient Light")]
        public Color ambientLightColor;

        [Tooltip("Bật sương mù khí quyển (Fog)")]
        public bool enableFog;

        [Tooltip("Màu sương mù khí quyển")]
        public Color fogColor;

        [Tooltip("Mật độ sương mù khí quyển")]
        public float fogDensity;

        [Tooltip("Màu ánh sáng mặt trời Directional Light")]
        public Color directionalLightColor;

        [Tooltip("Cường độ ánh sáng mặt trời Directional Light")]
        public float lightIntensity;

        [Tooltip("Màu sắc bụi sao không gian (Space Dust)")]
        public Color spaceDustColor;

        [Tooltip("Số lượng hạt bụi sao không gian")]
        public int spaceDustCount;

        public static StageEnvironmentSettings Default => new StageEnvironmentSettings
        {
            skyboxMaterial = null,
            backdropMaterial = null,
            backdropTexture = null,
            backdropExposure = 1.0f,
            backdropTint = Color.white,
            ambientLightColor = new Color(0.2f, 0.25f, 0.35f, 1f),
            enableFog = false,
            fogColor = new Color(0.05f, 0.08f, 0.15f, 1f),
            fogDensity = 0.0015f,
            directionalLightColor = Color.white,
            lightIntensity = 1.0f,
            spaceDustColor = new Color(0.85f, 0.95f, 1.0f, 0.85f),
            spaceDustCount = 220
        };
    }

    [Serializable]
    public struct StageEnemySettings
    {
        [Tooltip("Bảng danh mục Prefab quái riêng của màn chơi (nếu null sẽ dùng mặc định)")]
        public GameObject[] stageEnemyPrefabs;

        [Tooltip("Chu kỳ xuất hiện đợt quái (giây)")]
        public float waveInterval;

        [Tooltip("Cự ly sinh quái phía trước mũi tàu (m)")]
        public float spawnLeadDistance;

        [Tooltip("Tần suất xuất hiện đợt bay đội hình (mỗi X đợt)")]
        public int formationIntervalWaves;

        [Tooltip("Vận tốc tiếp cận tương đối của quái (m/s)")]
        public float enemyClosingSpeed;

        [Tooltip("Cự ly ghìm giao chiến của quái phía trước tàu (m)")]
        public float enemyHoldDistance;

        [Tooltip("Các hướng xuất hiện được kích hoạt trong màn này (nếu null sẽ dùng cả 7 hướng)")]
        public SpatialVectorType[] allowedVectors;

        public static StageEnemySettings Default => new StageEnemySettings
        {
            stageEnemyPrefabs = null,
            waveInterval = 3.5f,
            spawnLeadDistance = 220f,
            formationIntervalWaves = 3,
            enemyClosingSpeed = 20f,
            enemyHoldDistance = 60f,
            allowedVectors = null
        };
    }

    [Serializable]
    public struct StageHazardSettings
    {
        [Tooltip("Bật xuất hiện thiên thạch trong màn này")]
        public bool enableAsteroids;

        [Tooltip("Tỉ lệ thiên thạch bất hoại Monolith (0.0 - 1.0)")]
        public float indestructibleChance;

        [Tooltip("Cự ly sinh thiên thạch phía trước tàu (m)")]
        public float asteroidSpawnDistance;

        [Tooltip("Khoảng cách bước Z giữa các cụm thiên thạch (m)")]
        public float asteroidZInterval;

        [Tooltip("Bật Vòng Năng Lượng / Vật phẩm thu thập")]
        public bool enableCollectibleRings;

        [Tooltip("Tỉ lệ xuất hiện Vòng Năng Lượng")]
        public float ringSpawnRate;

        public static StageHazardSettings Default => new StageHazardSettings
        {
            enableAsteroids = true,
            indestructibleChance = 0.25f,
            asteroidSpawnDistance = 250f,
            asteroidZInterval = 50f,
            enableCollectibleRings = true,
            ringSpawnRate = 0.35f
        };
    }

    [Serializable]
    public struct StageClimaxSettings
    {
        [Tooltip("Kiểu kết thúc/cao trào của màn")]
        public StageClimaxType climaxType;

        [Tooltip("Số đợt quái cần tiêu diệt nếu là EliminateWaveCount")]
        public int targetWaveCount;

        [Tooltip("Prefab Boss (nếu có)")]
        public GameObject bossPrefab;

        public static StageClimaxSettings Default => new StageClimaxSettings
        {
            climaxType = StageClimaxType.DistanceReached,
            targetWaveCount = 10,
            bossPrefab = null
        };
    }

    /// <summary>
    /// Bản thiết kế dữ liệu cho từng màn chơi độc lập (Data-Driven Stage Architecture).
    /// Tuân thủ nguyên tắc Open-Closed (SOLID) và 0 GC Alloc.
    /// Mỗi màn là 1 Asset độc lập, cho phép nhân bản (Ctrl+D) và tùy biến không giới hạn.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "Virtual Rail/Stage Data", order = 2)]
    public class StageData : ScriptableObject
    {
        [Header("Stage Identity")]
        public string stageId = "Stage_01";
        public string stageTitle = "STAGE 1: ASTEROID BELT";
        public string stageSubtitle = "TRENCH RUN - INITIALIZE";
        public int stageIndex = 1;

        [Header("1. Rail & Kinematics")]
        public StageRailSettings railSettings = StageRailSettings.Default;

        [Header("2. Environment & Atmosphere")]
        public StageEnvironmentSettings environmentSettings = StageEnvironmentSettings.Default;

        [Header("3. Enemy & Wave Palette")]
        public StageEnemySettings enemySettings = StageEnemySettings.Default;

        [Header("4. Hazards & Collectibles")]
        public StageHazardSettings hazardSettings = StageHazardSettings.Default;

        [Header("5. Climax & Completion")]
        public StageClimaxSettings climaxSettings = StageClimaxSettings.Default;
    }
}
