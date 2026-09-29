using System;
using UnityEngine;

namespace VirtualRail
{
    public enum StageState
    {
        Inactive,
        Intro,
        InJourney,
        Climax,
        WarpTransition,
        StageCompleted,
        AllStagesCleared
    }

    /// <summary>
    /// Nhạc trưởng điều phối chu trình màn chơi (Stage Director - Chuẩn Star Fox 64 Arcade).
    /// Quản lý chu trình 3 màn chơi và chuyển giao liền mạch không tải lại Scene (Seamless In-Place Transition):
    /// - 0 GC Allocation trong Game Loop.
    /// - Luồng dữ liệu 1 chiều (Unidirectional Data Flow): StageData -> Director -> Subsystems.
    /// - Đồng bộ mượt mà ánh sáng, Skybox, tốc độ ray, bảng quái và thiên thạch.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailStageDirector : MonoBehaviour
    {
        public static VirtualRailStageDirector Instance { get; private set; }

        [Header("Stage Sequence")]
        [Tooltip("Danh sách các màn chơi theo thứ tự tuần tự")]
        public StageData[] stages;
        public int currentStageIndex = 0;

        [Header("State Tracking")]
        [SerializeField] private StageState currentState = StageState.Inactive;
        public StageState CurrentState => currentState;

        [Header("Subsystem References")]
        public VirtualRailAnchor anchor;
        public VirtualRailWaveSpawner waveSpawner;
        public VirtualRailAsteroidSpawner asteroidSpawner;
        public VirtualRailHorizonBackdrop backdrop;
        public VirtualRailSpaceDust spaceDust;
        public Light directionalLight;
        public StageNotificationUI notificationUI;

        // Runtime Tracking (0 GC Alloc primitives)
        private float _stageStartZ = 0f;
        private float _warpTimer = 0f;
        private float _warpDuration = 2.5f;
        private float _normalSpeed = 28f;
        private float _warpPeakSpeed = 85f;
        private bool _isMidWarpSwapped = false;
        private float _climaxTimer = 0f;

        public StageData CurrentStageData => (stages != null && currentStageIndex >= 0 && currentStageIndex < stages.Length) ? stages[currentStageIndex] : null;

        public float StageProgress01
        {
            get
            {
                var stage = CurrentStageData;
                if (stage == null || anchor == null || stage.railSettings.stageDistanceZ <= 0.001f) return 0f;
                float distTraveled = anchor.transform.position.z - _stageStartZ;
                return Mathf.Clamp01(distTraveled / stage.railSettings.stageDistanceZ);
            }
        }

        public float StageDistanceRemaining
        {
            get
            {
                var stage = CurrentStageData;
                if (stage == null || anchor == null) return 0f;
                float distTraveled = anchor.transform.position.z - _stageStartZ;
                return Mathf.Max(0f, stage.railSettings.stageDistanceZ - distTraveled);
            }
        }

        // Global Events
        public static event Action<StageData> OnStageStarted;
        public static event Action<StageData> OnStageClimaxReached;
        public static event Action<StageData, StageData> OnWarpTransitionStarted;
        public static event Action OnAllStagesCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            FindReferences();
        }

        private void Start()
        {
            FindReferences();

            // Nếu tàu đã bắt đầu chạy hoặc đang trong Play mode, tự động kích hoạt Stage 0
            if (anchor != null && anchor.CurrentSpeed > 0.5f && currentState == StageState.Inactive)
            {
                StartStage(0);
            }
        }

        private void OnEnable()
        {
            GameEventManager.OnStartGame += HandleGameStarted;
            GameEventManager.OnPlayerDestroyed += HandlePlayerDestroyed;
        }

        private void OnDisable()
        {
            GameEventManager.OnStartGame -= HandleGameStarted;
            GameEventManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
        }

        private void FindReferences()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance != null ? VirtualRailAnchor.Instance : FindAnyObjectByType<VirtualRailAnchor>();
            if (waveSpawner == null) waveSpawner = VirtualRailWaveSpawner.Instance != null ? VirtualRailWaveSpawner.Instance : FindAnyObjectByType<VirtualRailWaveSpawner>();
            if (asteroidSpawner == null) asteroidSpawner = VirtualRailAsteroidSpawner.Instance != null ? VirtualRailAsteroidSpawner.Instance : FindAnyObjectByType<VirtualRailAsteroidSpawner>();
            if (backdrop == null) backdrop = FindAnyObjectByType<VirtualRailHorizonBackdrop>();
            if (spaceDust == null) spaceDust = FindAnyObjectByType<VirtualRailSpaceDust>();
            if (notificationUI == null) notificationUI = StageNotificationUI.Instance != null ? StageNotificationUI.Instance : FindAnyObjectByType<StageNotificationUI>();

            if (directionalLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        directionalLight = lights[i];
                        break;
                    }
                }
            }
        }

        private void HandleGameStarted()
        {
            StartStage(0);
        }

        private void HandlePlayerDestroyed()
        {
            currentState = StageState.Inactive;
        }

        /// <summary>
        /// Bắt đầu nạp và khởi chạy một màn chơi cụ thể.
        /// </summary>
        public void StartStage(int stageIdx)
        {
            if (stages == null || stages.Length == 0) return;

            currentStageIndex = Mathf.Clamp(stageIdx, 0, stages.Length - 1);
            StageData stage = stages[currentStageIndex];
            if (stage == null) return;

            if (anchor != null)
            {
                _stageStartZ = anchor.transform.position.z;
            }

            _normalSpeed = stage.railSettings.forwardSpeed > 1f ? stage.railSettings.forwardSpeed : 28f;
            _warpDuration = stage.railSettings.warpDuration > 0.5f ? stage.railSettings.warpDuration : 2.5f;
            _warpPeakSpeed = _normalSpeed * (stage.railSettings.warpSpeedMultiplier > 1f ? stage.railSettings.warpSpeedMultiplier : 3.0f);

            // 1. Áp dụng Môi trường & Khí quyển
            ApplyEnvironment(stage.environmentSettings);

            // 2. Áp dụng Tốc độ đường ray
            if (anchor != null)
            {
                anchor.CurrentSpeed = _normalSpeed;
            }

            // 3. Áp dụng Bảng quái vật & Vector xuất hiện
            if (waveSpawner != null)
            {
                waveSpawner.ApplyStagePalette(stage.enemySettings);
            }

            // 4. Áp dụng Thiên thạch & Chướng ngại vật
            if (asteroidSpawner != null)
            {
                asteroidSpawner.ApplyStageHazardSettings(stage.hazardSettings);
            }

            // 5. Cập nhật UI thông báo phong cách Arcade Star Fox 64
            if (notificationUI != null)
            {
                notificationUI.SetStageNumber(stage.stageIndex);
                notificationUI.ShowNotification(stage.stageTitle, stage.stageSubtitle, 3.5f);
            }

            currentState = StageState.InJourney;
            OnStageStarted?.Invoke(stage);
        }

        private void Update()
        {
            if (anchor == null) return;

            // Tự động bắt đầu màn nếu tàu bắt đầu di chuyển
            if (currentState == StageState.Inactive && anchor.CurrentSpeed > 0.5f)
            {
                StartStage(currentStageIndex);
            }

            switch (currentState)
            {
                case StageState.InJourney:
                    UpdateJourneyState();
                    break;

                case StageState.Climax:
                    UpdateClimaxState();
                    break;

                case StageState.WarpTransition:
                    UpdateWarpTransitionState();
                    break;
            }

            // Cập nhật thanh tiến trình Z cho HUD (0 GC Alloc)
            if (notificationUI != null && currentState == StageState.InJourney)
            {
                notificationUI.UpdateProgressBar(StageProgress01);
            }
        }

        private void UpdateJourneyState()
        {
            StageData stage = CurrentStageData;
            if (stage == null) return;

            float traveled = anchor.transform.position.z - _stageStartZ;
            if (traveled >= stage.railSettings.stageDistanceZ)
            {
                TriggerClimax();
            }
        }

        private void TriggerClimax()
        {
            currentState = StageState.Climax;
            _climaxTimer = 0f;

            StageData stage = CurrentStageData;
            OnStageClimaxReached?.Invoke(stage);

            // Nếu màn hoàn thành đơn thuần bằng quãng đường (DistanceReached), chờ 1.5s tạo nhịp thở rồi Warp
            if (stage != null && stage.climaxSettings.climaxType == StageClimaxType.DistanceReached)
            {
                // Sau một khoảng ngắn sẽ kích hoạt Warp
            }
        }

        private void UpdateClimaxState()
        {
            _climaxTimer += Time.deltaTime;

            StageData stage = CurrentStageData;
            if (stage != null && stage.climaxSettings.climaxType == StageClimaxType.DistanceReached)
            {
                if (_climaxTimer >= 1.5f)
                {
                    StartWarpTransition();
                }
            }
        }

        /// <summary>
        /// Kích hoạt chu trình nhảy vọt Hyperspace Warp để chuyển màn (0 GC Alloc).
        /// </summary>
        public void StartWarpTransition()
        {
            if (currentState == StageState.WarpTransition) return;

            currentState = StageState.WarpTransition;
            _warpTimer = 0f;
            _isMidWarpSwapped = false;

            StageData currentStage = CurrentStageData;
            int nextIdx = currentStageIndex + 1;
            StageData nextStage = (stages != null && nextIdx < stages.Length) ? stages[nextIdx] : null;

            if (notificationUI != null)
            {
                string nextTarget = nextStage != null ? nextStage.stageTitle : "MISSION COMPLETE";
                notificationUI.ShowNotification("WARP SPEED ENGAGED", nextTarget, _warpDuration);
            }

            OnWarpTransitionStarted?.Invoke(currentStage, nextStage);
        }

        private void UpdateWarpTransitionState()
        {
            _warpTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_warpTimer / Mathf.Max(0.1f, _warpDuration));

            // Đường cong vận tốc Warp: Tăng vọt lên cực đại ở giữa, rồi hạ cánh êm đềm ở cuối
            float speedFactor;
            if (progress < 0.5f)
            {
                float tIn = progress * 2f; // 0 -> 1
                speedFactor = Mathf.SmoothStep(0f, 1f, tIn);
                if (anchor != null)
                {
                    anchor.CurrentSpeed = Mathf.Lerp(_normalSpeed, _warpPeakSpeed, speedFactor);
                }
            }
            else
            {
                // Điểm giữa Warp: Hoán đổi môi trường sang Màn kế tiếp ngay trong vệt sáng cực đại
                if (!_isMidWarpSwapped)
                {
                    SwapToNextStageData();
                    _isMidWarpSwapped = true;
                }

                float tOut = (progress - 0.5f) * 2f; // 0 -> 1
                speedFactor = Mathf.SmoothStep(1f, 0f, tOut);
                if (anchor != null)
                {
                    anchor.CurrentSpeed = Mathf.Lerp(_normalSpeed, _warpPeakSpeed, speedFactor);
                }
            }

            // Hoàn tất chu trình Warp
            if (progress >= 1.0f)
            {
                CompleteWarpTransition();
            }
        }

        private void SwapToNextStageData()
        {
            int nextIndex = currentStageIndex + 1;

            if (stages == null || nextIndex >= stages.Length)
            {
                // Đã hoàn thành tất cả các màn!
                currentState = StageState.AllStagesCleared;
                if (notificationUI != null)
                {
                    notificationUI.ShowNotification("ALL STAGES CLEARED!", "EXCELLENT FLIGHT RECORD", 5.0f);
                }
                OnAllStagesCompleted?.Invoke();
                return;
            }

            currentStageIndex = nextIndex;
            StageData nextStage = stages[currentStageIndex];
            if (nextStage == null) return;

            if (anchor != null)
            {
                _stageStartZ = anchor.transform.position.z;
            }

            _normalSpeed = nextStage.railSettings.forwardSpeed > 1f ? nextStage.railSettings.forwardSpeed : 28f;

            // 1. Hoán đổi Môi trường
            ApplyEnvironment(nextStage.environmentSettings);

            // 2. Hoán đổi Bảng quái vật
            if (waveSpawner != null)
            {
                waveSpawner.ApplyStagePalette(nextStage.enemySettings);
            }

            // 3. Hoán đổi Thiên thạch
            if (asteroidSpawner != null)
            {
                asteroidSpawner.ApplyStageHazardSettings(nextStage.hazardSettings);
            }

            if (notificationUI != null)
            {
                notificationUI.SetStageNumber(nextStage.stageIndex);
            }

            OnStageStarted?.Invoke(nextStage);
        }

        private void CompleteWarpTransition()
        {
            if (currentState == StageState.AllStagesCleared) return;

            currentState = StageState.InJourney;
            if (anchor != null)
            {
                anchor.CurrentSpeed = _normalSpeed;
            }
        }

        /// <summary>
        /// Áp dụng các thông số thị giác môi trường mượt mà (Skybox, Fog, Backdrop, Ánh sáng, Bụi sao).
        /// </summary>
        public void ApplyEnvironment(StageEnvironmentSettings env)
        {
            // Skybox
            if (env.skyboxMaterial != null)
            {
                RenderSettings.skybox = env.skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            // Ambient Light
            RenderSettings.ambientLight = env.ambientLightColor;

            // Fog
            RenderSettings.fog = env.enableFog;
            if (env.enableFog)
            {
                RenderSettings.fogColor = env.fogColor;
                RenderSettings.fogDensity = env.fogDensity;
            }

            // Directional Light
            if (directionalLight != null)
            {
                directionalLight.color = env.directionalLightColor;
                directionalLight.intensity = env.lightIntensity;
            }

            // Backdrop
            if (backdrop != null && env.backdropMaterial != null)
            {
                backdrop.SetBackdropMaterial(env.backdropMaterial);
            }

            // Space Dust
            if (spaceDust != null)
            {
                spaceDust.SetDustSettings(env.spaceDustColor, env.spaceDustCount);
            }
        }
    }
}
