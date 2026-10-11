using System;
using UnityEngine;

namespace VirtualRail
{
    public enum StageCompletionMode
    {
        DistanceTraveled = 0,    // Hoàn thành theo quãng đường ray Z (mét)
        TimeDuration = 1,        // Hoàn thành theo thời lượng thực tế (phút)
        EliminationQuota = 2,    // Tiêu diệt đủ số lượng quái chỉ định
        DefeatBoss = 3,          // Chỉ hoàn thành khi Boss bị tiêu diệt
        SurvivalUntilBoss = 4,   // Hết thời gian/cự ly -> Chuyển sang kích hoạt trận đấu Boss
        EndlessSurvival = 5      // Chế độ Sinh tồn Vô Hạn (Endless Survival Mode)
    }

    public enum TimelineTriggerMode
    {
        TimeInSeconds = 0,       // Kích hoạt theo giây từ lúc bắt đầu màn
        DistanceInMeters = 1     // Kích hoạt theo mét Z từ lúc bắt đầu màn
    }

    public enum StageTimelineEventType
    {
        RadioTransmission = 0,   // Thông điệp đàm thoại / Cảnh báo nguy hiểm
        EnvironmentShift = 1,    // Thay đổi sương mù / ánh sáng / Skybox
        SpawnScriptedSquad = 2,  // Triệu gọi cụm quái / đội hình kịch bản
        SpeedChange = 3,         // Tăng tốc hoặc giảm tốc đường ray
        Checkpoint = 4,          // Lưu trạm Checkpoint hồi sinh
        SpawnBoss = 5            // Kích hoạt trận đấu Boss
    }

    public enum WaveWaitMode
    {
        Immediate = 0,               // Chạy ngay lập tức (không chờ wave trước kết thúc)
        WaitForClearOrTimeout = 1,   // Chờ wave trước sạch hoặc quá timeout an toàn
        WaitForStrictClear = 2       // Bắt buộc tiêu diệt sạch wave trước mới spawn
    }

    public enum StageClimaxType
    {
        DistanceReached = 0,
        EliminateWaveCount = 1,
        DefeatBoss = 2,
        TimeElapsed = 3
    }

    [Serializable]
    public struct StageProgressionSettings
    {
        [Tooltip("Chế độ hoàn thành màn chơi (Thời gian, Cự ly, Diệt quái, Diệt Boss, Vô hạn)")]
        public StageCompletionMode completionMode;

        [Tooltip("Bật chế độ Sinh tồn Vô hạn (Endless Survival Mode) cho màn chơi này")]
        public bool isEndlessSurvival;

        public bool IsEndlessMode => completionMode == StageCompletionMode.EndlessSurvival || isEndlessSurvival;

        [Range(0.5f, 60.0f)]
        [Tooltip("Thời lượng màn chơi tính theo PHÚT (VD: 15.0 cho màn 15 phút)")]
        public float durationMinutes;

        [Tooltip("Quãng đường ray mục tiêu (m) nếu chạy theo cự ly")]
        public float targetDistanceZ;

        [Tooltip("Tốc độ tiến cơ sở dọc trục Z của đường ray (m/s)")]
        public float baseForwardSpeed;

        [Tooltip("Thời gian lướt nhảy vọt Hyperspace Warp khi chuyển màn (giây)")]
        public float warpDuration;

        [Tooltip("Hệ số tăng tốc khi lướt Hyperspace Warp")]
        public float warpSpeedMultiplier;

        [Tooltip("Số lượng quái cần tiêu diệt nếu chọn EliminationQuota")]
        public int targetEliminationCount;

        public static StageProgressionSettings Default => new StageProgressionSettings
        {
            completionMode = StageCompletionMode.TimeDuration,
            durationMinutes = 15.0f,
            targetDistanceZ = 25200f,
            baseForwardSpeed = 28f,
            warpDuration = 2.5f,
            warpSpeedMultiplier = 3.2f,
            targetEliminationCount = 50
        };
    }

    [Serializable]
    public struct StageTimelineEvent
    {
        [Tooltip("Tên gợi nhớ của sự kiện kịch bản")]
        public string eventName;

        [Tooltip("Cách thức kích hoạt: Theo thời gian (giây) hoặc theo cự ly (mét Z)")]
        public TimelineTriggerMode triggerMode;

        [Tooltip("Mốc kích hoạt (giây nếu là Time, mét Z nếu là Distance)")]
        public float triggerValue;

        [Tooltip("Loại sự kiện")]
        public StageTimelineEventType eventType;

        [Header("Radio Transmission")]
        public string messageTitle;
        public string messageSubtitle;
        public float messageDuration;

        [Header("Scripted Spawning")]
        public GameObject customPrefab;
        public SpatialVectorType vectorType;
        public int squadCount;
        [Tooltip("Mẫu đội hình bay Data-Driven (nếu gán, sẽ ưu tiên dùng mẫu này thay vì lính lẻ)")]
        public FormationPattern scriptedFormationPattern;

        [Header("Wave Pacing & Gating")]
        [Tooltip("Chế độ chờ đợt quái trước dọn sạch trước khi kích hoạt đợt này")]
        public WaveWaitMode waitMode;
        [Tooltip("Thời gian chờ tối đa (giây) nếu chọn WaitForClearOrTimeout trước khi ép wave cũ rút lui (mặc định 14s)")]
        public float maxWaitTimeout;

        [Header("Speed & Environment Control")]
        public float targetSpeed;
        public bool changeFog;
        public bool enableFog;
        public Color fogColor;
        public float fogDensity;
        public bool changeSkybox;
        public Material newSkybox;
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
            stageDistanceZ = 25200f,
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
    public struct StageAudioSettings
    {
        [Tooltip("Nhạc nền hành trình của màn chơi (BGM)")]
        public AudioClip stageBgm;

        [Tooltip("Nhạc nền cao trào / trận đấu Boss của màn chơi (BGM)")]
        public AudioClip bossBgm;

        [Range(0f, 1f)]
        [Tooltip("Âm lượng nhạc nền riêng cho màn này")]
        public float musicVolume;

        public static StageAudioSettings Default => new StageAudioSettings
        {
            stageBgm = null,
            bossBgm = null,
            musicVolume = 1.0f
        };
    }

    [Serializable]
    public struct StageEnemySettings
    {
        [Tooltip("Bảng danh mục Prefab quái riêng của màn chơi (Chỉ sinh những quái có trong danh sách này, rỗng = không sinh quái)")]
        public GameObject[] stageEnemyPrefabs;

        [Tooltip("Bật sinh đợt quái tuần hoàn ngẫu nhiên ở chế độ nền (Tắt để kích hoạt chế độ Level Design thuần Kịch bản Timeline)")]
        public bool enablePeriodicWaves;

        [Tooltip("Chu kỳ xuất hiện đợt quái (giây)")]
        public float waveInterval;

        [Tooltip("Cự ly sinh quái phía trước mũi tàu (m)")]
        public float spawnLeadDistance;

        [Tooltip("Tần suất xuất hiện đợt bay đội hình (mỗi X đợt, <= 0 là tắt)")]
        public int formationIntervalWaves;

        [Tooltip("Vận tốc tiếp cận tương đối của quái (m/s)")]
        public float enemyClosingSpeed;

        [Tooltip("Cự ly ghìm giao chiến của quái phía trước tàu (m)")]
        public float enemyHoldDistance;

        [Tooltip("Các hướng xuất hiện được kích hoạt trong màn này (Chỉ dùng các vector trong danh sách này, rỗng = không dùng vector nâng cao)")]
        public SpatialVectorType[] allowedVectors;

        [Tooltip("Cho phép quái bắn đạn hay không trong màn này")]
        public bool allowShooting;

        [Tooltip("Vận tốc đạn của quái riêng màn này (m/s)")]
        public float bulletSpeed;

        [Tooltip("Bật xuất hiện đội hình lượn")]
        public bool enableFormations;

        [Tooltip("Danh sách các mẫu đội hình Data-Driven sử dụng trong màn này (nếu có)")]
        public FormationPattern[] stageFormationPool;

        public static StageEnemySettings Default => new StageEnemySettings
        {
            stageEnemyPrefabs = null,
            enablePeriodicWaves = true,
            waveInterval = 3.5f,
            spawnLeadDistance = 220f,
            formationIntervalWaves = 3,
            enemyClosingSpeed = 20f,
            enemyHoldDistance = 60f,
            allowedVectors = null,
            allowShooting = true,
            bulletSpeed = 22f,
            enableFormations = true,
            stageFormationPool = null
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

        [Tooltip("Lượng máu của Boss riêng màn này")]
        public int bossHealth;

        [Tooltip("Ghìm dừng tốc độ tàu ray khi bước vào trận đấu Boss")]
        public bool holdRailOnBoss;

        public static StageClimaxSettings Default => new StageClimaxSettings
        {
            climaxType = StageClimaxType.DistanceReached,
            targetWaveCount = 10,
            bossPrefab = null,
            bossHealth = 100,
            holdRailOnBoss = true
        };
    }

    /// <summary>
    /// Bản thiết kế dữ liệu cho từng màn chơi độc lập (Pure Data-Driven Stage Architecture).
    /// Tuân thủ nguyên tắc Open-Closed (SOLID) và 0 GC Alloc.
    /// Mỗi màn là 1 Asset độc lập, cho phép nhân bản (Ctrl+D) và tùy biến 100% thời gian, kịch bản, gameplay.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "Virtual Rail/Stage Data", order = 2)]
    public class StageData : ScriptableObject
    {
        public string stageId = "Stage_01";
        public string stageTitle = "STAGE 1: OPERATION METEOR BREACH";
        public string stageSubtitle = "SECTOR ALPHA - INITIALIZE";
        public int stageIndex = 1;

        public StageProgressionSettings progressionSettings = StageProgressionSettings.Default;

        public StageRailSettings railSettings = StageRailSettings.Default;

        public StageEnvironmentSettings environmentSettings = StageEnvironmentSettings.Default;

        public StageEnemySettings enemySettings = StageEnemySettings.Default;

        public StageHazardSettings hazardSettings = StageHazardSettings.Default;

        public StageClimaxSettings climaxSettings = StageClimaxSettings.Default;
        public StageAudioSettings audioSettings = StageAudioSettings.Default;

        public StageTimelineEvent[] timelineEvents;

        /// <summary>
        /// Vận tốc cơ sở của đường ray (ưu tiên progressionSettings, fallback sang railSettings).
        /// </summary>
        public float GetBaseForwardSpeed()
        {
            if (progressionSettings.baseForwardSpeed > 0.5f) return progressionSettings.baseForwardSpeed;
            if (railSettings.forwardSpeed > 0.5f) return railSettings.forwardSpeed;
            return 28f;
        }

        /// <summary>
        /// Cự ly ray mục tiêu (m).
        /// </summary>
        public float GetTargetDistanceZ()
        {
            if (progressionSettings.targetDistanceZ > 10f) return progressionSettings.targetDistanceZ;
            if (railSettings.stageDistanceZ > 10f) return railSettings.stageDistanceZ;
            return 25200f;
        }

        /// <summary>
        /// Thời lượng mục tiêu tính bằng giây.
        /// </summary>
        public float GetTargetDurationSeconds()
        {
            if (progressionSettings.durationMinutes > 0.05f)
            {
                return progressionSettings.durationMinutes * 60f;
            }
            return GetTargetDistanceZ() / Mathf.Max(1f, GetBaseForwardSpeed());
        }

        /// <summary>
        /// Thời gian lướt Warp khi xong màn (giây).
        /// </summary>
        public float GetWarpDuration()
        {
            if (progressionSettings.warpDuration > 0.5f) return progressionSettings.warpDuration;
            if (railSettings.warpDuration > 0.5f) return railSettings.warpDuration;
            return 2.5f;
        }

        /// <summary>
        /// Hệ số tăng tốc Warp.
        /// </summary>
        public float GetWarpSpeedMultiplier()
        {
            if (progressionSettings.warpSpeedMultiplier > 1f) return progressionSettings.warpSpeedMultiplier;
            if (railSettings.warpSpeedMultiplier > 1f) return railSettings.warpSpeedMultiplier;
            return 3.2f;
        }
    }
}
