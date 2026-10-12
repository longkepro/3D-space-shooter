using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Giao diện HUD Buồng Lái Khoa Học Viễn Tưởng Hiện Đại (Modern Cockpit Holo HUD).
    /// Chuẩn Star Fox 64 Arcade, thiết kế 5 cụm chuyên trách toàn diện:
    /// - Cụm 1 (Top-Left): Thanh Khiên Shield (%), Thanh Tụ Lực Plasma Charge (Hồi 2.0s, Đầy mới cho bắn), Kho Bom Thông Minh (3 Bombs).
    /// - Cụm 2 (Top-Right): Điểm số Arcade số hóa 6 chữ số, Bộ đếm Hit Counter, Huy hiệu Combo Multiplier x2/x3/x4 kèm vòng đếm ngược.
    /// - Cụm 3 (Center): Hệ tâm ngắm Dual Reticle 3D & Khung khóa mục tiêu Lock-on.
    /// - Cụm 4 (Bottom-Left): Khung đàm thoại tác chiến bộ đàm Radio Comms (RadioCommsHUD).
    /// - Cụm 5 (Bottom-Right): Cấp độ hỏa lực Laser (MK-I / MK-II / Hyper) & Đồng hồ tốc độ phi hành số hóa.
    /// 
    /// Tuân thủ 100% Bộ Quy Tắc Phát Triển Dự Án (AGENTS.md / GEMINI.md):
    /// - Unidirectional Data Flow: Chỉ đọc dữ liệu từ Gameplay State và Events.
    /// - 0 GC Allocation trong Game Loop: Sử dụng Precomputed String Tables và Event-driven UI.
    /// - LateUpdate Synchronization: Đồng bộ trọn vẹn sau CameraRig và Ship Transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModernCockpitHUD : MonoBehaviour
    {
        public static ModernCockpitHUD Instance { get; private set; }

        [Header("System References")]
        public VirtualRailAnchor anchor;
        public VirtualRailShip ship;
        public VirtualRailWeapon weapon;
        public AimAssistModule aimAssist;
        public SmartBombModule smartBomb;
        public RadioCommsHUD radioComms;

        [Header("Cluster 1: Ship Integrity & Plasma Charge (Top-Left)")]
        public RectTransform clusterTopLeftRoot;
        public Image shieldBarFill;
        public Text shieldPercentText;
        public Image chargeBarFill;
        public Text chargeStatusText;
        public Text chargeTimerText;
        public Image[] bombIconImages;
        public Text bombCountText;

        [Header("Cluster 2: Arcade Score & Combo (Top-Right)")]
        public RectTransform clusterTopRightRoot;
        public Text scoreValueText;
        public Text hitCountValueText;
        public RectTransform comboBadgeRoot;
        public Text comboMultiplierText;
        public Image comboDecayRadial;

        [Header("Cluster 2 Endless Telemetry: Threat & Distance Tracker")]
        public Text threatLevelText;
        public Image threatProgressFill;
        public Text distanceTrackerText;

        [Header("Graze Indicator: Screen-Space Floating Numbers")]
        public RectTransform grazeFloatingRoot;
        public Text[] grazePopupTexts;

        [Header("Cluster 4: Tactical Comms Box (Bottom-Left)")]
        public RectTransform clusterBottomLeftRoot;

        [Header("Cluster 5: Flight Telemetry & Weapon (Bottom-Right)")]
        public RectTransform clusterBottomRightRoot;
        public Text weaponTierText;
        public Text speedometerValueText;
        public Image speedometerGaugeFill;

        [Header("Cluster 6: Climax Boss Health Bar (Top-Center)")]
        public RectTransform clusterBossRoot;
        public Image bossHealthBarFill;
        public Image bossGhostBarFill;
        public Image bossShieldBarFill;
        public Image bossShieldGhostBarFill;
        public Text bossNameText;
        public Text bossPhaseText;

        // 0 GC String Lookup Tables (Triệt tiêu 100% Heap Allocation)
        private static readonly string[] PERCENT_STRINGS = PrecomputePercentStrings();
        private static readonly string[] SPEED_STRINGS = PrecomputeSpeedStrings();
        private static readonly string[] HITS_STRINGS = PrecomputeHitsStrings();
        private static readonly string[] THREAT_STRINGS = PrecomputeThreatStrings();
        private static readonly string[] DIST_STRINGS = PrecomputeDistanceStrings();
        private static readonly string[] GRAZE_STRINGS = new string[]
        {
            "+500 [GRAZE!]",
            "+1,000 [GRAZE!]",
            "+2,000 [GRAZE!]",
            "+4,000 [GRAZE!]",
            "+8,000 [GRAZE!]",
            "+16,000 [GRAZE!]"
        };

        private static string[] PrecomputePercentStrings()
        {
            string[] table = new string[101];
            for (int i = 0; i <= 100; i++) table[i] = i + "%";
            return table;
        }

        private static string[] PrecomputeSpeedStrings()
        {
            string[] table = new string[351];
            for (int i = 0; i <= 350; i++) table[i] = i + " m/s";
            return table;
        }

        private static string[] PrecomputeHitsStrings()
        {
            string[] table = new string[1000];
            for (int i = 0; i < 1000; i++) table[i] = i.ToString("D3");
            return table;
        }

        private static string[] PrecomputeThreatStrings()
        {
            string[] table = new string[31];
            for (int i = 0; i <= 30; i++)
            {
                table[i] = (i < 10) ? $"THREAT LV.0{i}" : $"THREAT LV.{i}";
            }
            return table;
        }

        private static string[] PrecomputeDistanceStrings()
        {
            string[] table = new string[3001];
            for (int i = 0; i <= 3000; i++)
            {
                int meters = i * 10;
                table[i] = meters.ToString("00,000") + " m";
            }
            return table;
        }

        private struct GrazePopupSlot
        {
            public Text textObj;
            public RectTransform rectTransform;
            public float timer;
            public bool active;
        }

        private GrazePopupSlot[] _grazeSlots;
        private int _nextGrazeSlot = 0;
        private Camera _mainCamera;

        // Cache trạng thái nội bộ để tránh ghi đè UI thừa thãi
        private int _cachedHealthPercent = 100;
        private int _cachedScore = 0;
        private int _cachedHits = 0;
        public int CurrentScore => _cachedScore;
        public int CurrentHits => _cachedHits;
        private int _cachedBombs = 3;
        private int _lastSpeedInt = -1;
        private int _lastThreatLevel = -1;
        private int _lastDistanceStep = -1;
        private float _bindingRetryTime = 0f;

        // Màu sắc Sci-Fi Neon
        private readonly Color COLOR_CYAN = new Color(0f, 0.95f, 1f, 1f);
        private readonly Color COLOR_AMBER = new Color(1f, 0.68f, 0.1f, 1f);
        private readonly Color COLOR_AMBER_READY = new Color(1f, 0.88f, 0.2f, 1f);
        private readonly Color COLOR_GOLD = new Color(1f, 0.84f, 0f, 1f);
        private readonly Color COLOR_CRITICAL_RED = new Color(1f, 0.22f, 0.15f, 1f);

        private CanvasGroup _hudCanvasGroup;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _hudCanvasGroup = GetComponent<CanvasGroup>();
            if (_hudCanvasGroup == null) _hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            GameEventManager.OnUpdateHealthBar += HandleHealthBarUpdated;
            GameEventManager.OnIncrementScore += HandleScoreIncremented;
            GameEventManager.OnStartGame += HandleGameStarted;
            GameEventManager.OnPlayerDestroyed += HandlePlayerDestroyed;
            GameEventManager.OnRespawnPickup += HandleRespawnPickup;

            SmartBombModule.OnBombCountChanged += HandleBombCountChanged;
            ComboScoringEngine.OnMultiplierChanged += HandleMultiplierChanged;
            ComboScoringEngine.OnMultiplierDecayed += HandleMultiplierDecayed;
            ComboScoringEngine.OnMultiplierReset += HandleMultiplierReset;
            ComboScoringEngine.OnComboScored += HandleComboScored;
            ComboScoringEngine.OnGrazeTriggered += HandleGrazeTriggered;

            BossF3CorvetteController.OnBossSpawned += HandleBossSpawned;
            BossF3CorvetteController.OnBossHealthChanged += HandleBossHealthChanged;
            BossF3CorvetteController.OnBossShieldChanged += HandleBossShieldChanged;
            BossF3CorvetteController.OnBossShieldBroken += HandleBossShieldBroken;
            BossF3CorvetteController.OnBossTrenchCharging += HandleBossTrenchCharging;
            BossF3CorvetteController.OnBossHyperLaserCharging += HandleBossHyperLaserCharging;
            BossF3CorvetteController.OnBossStunned += HandleBossStunned;
            BossF3CorvetteController.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            GameEventManager.OnUpdateHealthBar -= HandleHealthBarUpdated;
            GameEventManager.OnIncrementScore -= HandleScoreIncremented;
            GameEventManager.OnStartGame -= HandleGameStarted;
            GameEventManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
            GameEventManager.OnRespawnPickup -= HandleRespawnPickup;

            SmartBombModule.OnBombCountChanged -= HandleBombCountChanged;
            ComboScoringEngine.OnMultiplierChanged -= HandleMultiplierChanged;
            ComboScoringEngine.OnMultiplierDecayed -= HandleMultiplierDecayed;
            ComboScoringEngine.OnMultiplierReset -= HandleMultiplierReset;
            ComboScoringEngine.OnComboScored -= HandleComboScored;
            ComboScoringEngine.OnGrazeTriggered -= HandleGrazeTriggered;

            BossF3CorvetteController.OnBossSpawned -= HandleBossSpawned;
            BossF3CorvetteController.OnBossHealthChanged -= HandleBossHealthChanged;
            BossF3CorvetteController.OnBossShieldChanged -= HandleBossShieldChanged;
            BossF3CorvetteController.OnBossShieldBroken -= HandleBossShieldBroken;
            BossF3CorvetteController.OnBossTrenchCharging -= HandleBossTrenchCharging;
            BossF3CorvetteController.OnBossHyperLaserCharging -= HandleBossHyperLaserCharging;
            BossF3CorvetteController.OnBossStunned -= HandleBossStunned;
            BossF3CorvetteController.OnBossDefeated -= HandleBossDefeated;
        }

        private void Start()
        {
            FindReferences();
            EnsureGrazePoolInitialized();
            UpdateWeaponTierText();
            UpdateBombDisplay(_cachedBombs);
            UpdateHealthUI(_cachedHealthPercent);
            UpdateScoreUI(_cachedScore);
            UpdateHitsUI(_cachedHits);

            EnsureBossClusterCreated();
            EnsureTelemetryAndGrazeCreated();

            if (BossF3CorvetteController.Instance != null && BossF3CorvetteController.Instance.CurrentPhase != BossBattlePhase.Defeated)
            {
                HandleBossSpawned(BossF3CorvetteController.Instance);
                HandleBossHealthChanged(BossF3CorvetteController.Instance.CurrentHealth, BossF3CorvetteController.Instance.MaxHealth, BossF3CorvetteController.Instance.CurrentPhase);
                if (BossF3CorvetteController.Instance.IsShieldOnline)
                {
                    var s = BossF3CorvetteController.Instance.GetComponentInChildren<VirtualRailHexShield>();
                    if (s != null) HandleBossShieldChanged(s.CurrentShieldHealth, s.MaxShieldHealth);
                }
            }
            else if (clusterBossRoot != null)
            {
                clusterBossRoot.gameObject.SetActive(false);
            }

            // Đảm bảo triệt tiêu toàn bộ cụm không thuộc ảnh mẫu thiết kế
            if (clusterBottomLeftRoot != null)
            {
                clusterBottomLeftRoot.gameObject.SetActive(false);
            }
            if (clusterBottomRightRoot != null)
            {
                clusterBottomRightRoot.gameObject.SetActive(false);
            }

            SuppressLegacyGameUI();

            // Kiểm tra trạng thái Menu chính để ẩn HUD khi ngoài game
            if (UIManager.Instance != null)
            {
                bool isGame = (UIManager.Instance.CurrentState == UIViewState.Gameplay || UIManager.Instance.CurrentState == UIViewState.Pause);
                SetHUDVisible(isGame);
            }
            else
            {
                GameObject mainMenu = GameObject.Find("Main Menu");
                bool isMenuOpen = (mainMenu != null && mainMenu.activeInHierarchy);
                SetHUDVisible(!isMenuOpen);
            }
        }

        public static void SuppressLegacyGameUI()
        {
            GameObject gameUI = GameObject.Find("Game UI");
            if (gameUI != null)
            {
                gameUI.SetActive(false);
            }
            if (UIManager.Instance != null)
            {
                GameObject mainMenu = GameObject.Find("Main Menu");
                if (mainMenu != null)
                {
                    mainMenu.SetActive(false);
                }
            }
        }

        public void SetHUDVisible(bool visible)
        {
            if (_hudCanvasGroup != null)
            {
                _hudCanvasGroup.alpha = visible ? 1f : 0f;
                _hudCanvasGroup.interactable = visible;
                _hudCanvasGroup.blocksRaycasts = visible;
            }
        }

        private void HandlePlayerDestroyed()
        {
            SetHUDVisible(false);
        }

        private void HandleRespawnPickup()
        {
            SetHUDVisible(true);
        }

        private void FindReferences()
        {
            if (anchor == null) anchor = VirtualRailAnchor.Instance;
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (ship == null) ship = FindAnyObjectByType<VirtualRailShip>();
            if (weapon == null && anchor != null) weapon = anchor.GetComponentInChildren<VirtualRailWeapon>();
            if (weapon == null) weapon = FindAnyObjectByType<VirtualRailWeapon>();
            if (aimAssist == null && anchor != null) aimAssist = anchor.GetComponentInChildren<AimAssistModule>();
            if (aimAssist == null) aimAssist = FindAnyObjectByType<AimAssistModule>();
            if (smartBomb == null && anchor != null) smartBomb = anchor.GetComponentInChildren<SmartBombModule>();
            if (smartBomb == null) smartBomb = FindAnyObjectByType<SmartBombModule>();
            if (radioComms == null) radioComms = GetComponentInChildren<RadioCommsHUD>();

            if (_mainCamera == null)
            {
                var camRig = (anchor != null) ? anchor.GetComponentInChildren<VirtualRailCameraRig>() : null;
                _mainCamera = (camRig != null && camRig.targetCamera != null) ? camRig.targetCamera : Camera.main;
            }

            if (grazeFloatingRoot == null)
            {
                Transform gRoot = transform.Find("GrazeFloatingContainer");
                if (gRoot != null) grazeFloatingRoot = gRoot.GetComponent<RectTransform>();
            }
            if ((grazePopupTexts == null || grazePopupTexts.Length == 0) && grazeFloatingRoot != null)
            {
                grazePopupTexts = grazeFloatingRoot.GetComponentsInChildren<Text>(true);
            }
            if (threatLevelText == null && clusterTopRightRoot != null)
            {
                threatLevelText = clusterTopRightRoot.Find("TelemetryBox/ThreatBadge/ThreatLabel")?.GetComponent<Text>();
            }
            if (threatProgressFill == null && clusterTopRightRoot != null)
            {
                threatProgressFill = clusterTopRightRoot.Find("TelemetryBox/ThreatBadge/ThreatProgress/Fill")?.GetComponent<Image>();
            }
            if (distanceTrackerText == null && clusterTopRightRoot != null)
            {
                distanceTrackerText = clusterTopRightRoot.Find("TelemetryBox/DistanceTracker/DistanceValue")?.GetComponent<Text>();
            }
        }

        private void LateUpdate()
        {
            if (_hudCanvasGroup != null && _hudCanvasGroup.alpha <= 0.001f) return;

            // Kiểm tra và retry binding định kỳ nếu đối tượng xuất hiện muộn
            if ((anchor == null || weapon == null || aimAssist == null) && Time.time >= _bindingRetryTime)
            {
                _bindingRetryTime = Time.time + 1.0f;
                FindReferences();
            }

            var cfg = (anchor != null) ? anchor.config : null;
            if (cfg != null && !cfg.enableModernHUD) return;

            UpdateChargeMeterUI(cfg);
            UpdateSpeedometerUI();
            UpdateShieldAlertPulsing();
            UpdateComboDecayUI();
            UpdateBossGhostBar();
            UpdateEndlessTelemetryUI();
            UpdateGrazeFloatingCombatText();
        }

        private void UpdateBossGhostBar()
        {
            if (bossGhostBarFill != null && bossHealthBarFill != null && bossGhostBarFill.fillAmount > bossHealthBarFill.fillAmount)
            {
                bossGhostBarFill.fillAmount = Mathf.MoveTowards(bossGhostBarFill.fillAmount, bossHealthBarFill.fillAmount, Time.deltaTime * 0.35f);
            }
            if (bossShieldGhostBarFill != null && bossShieldBarFill != null && bossShieldGhostBarFill.fillAmount > bossShieldBarFill.fillAmount)
            {
                bossShieldGhostBarFill.fillAmount = Mathf.MoveTowards(bossShieldGhostBarFill.fillAmount, bossShieldBarFill.fillAmount, Time.deltaTime * 0.45f);
            }
        }

        private void UpdateComboDecayUI()
        {
            var engine = ComboScoringEngine.Instance;
            if (engine == null) return;

            int mult = engine.CurrentMultiplier;
            int multIdx = Mathf.Clamp(engine.MultiplierIndex, 0, ComboScoringEngine.MULTIPLIER_COLORS.Length - 1);

            if (comboBadgeRoot != null)
            {
                bool showBadge = mult > 1;
                if (comboBadgeRoot.gameObject.activeSelf != showBadge)
                {
                    comboBadgeRoot.gameObject.SetActive(showBadge);
                }
            }

            if (comboDecayRadial != null)
            {
                float ratio = engine.DecayRatio;
                comboDecayRadial.fillAmount = ratio;

                if (mult > 1)
                {
                    Color baseCol = ComboScoringEngine.MULTIPLIER_COLORS[multIdx];
                    if (engine.MultiplierDecayTimer <= 1.0f && engine.MultiplierDecayTimer > 0f)
                    {
                        // Hiệu ứng nhịp đập cảnh báo sắp tụt bậc hệ số nhân
                        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 16f);
                        comboDecayRadial.color = Color.Lerp(COLOR_CRITICAL_RED, baseCol, pulse);
                    }
                    else
                    {
                        comboDecayRadial.color = baseCol;
                    }
                }
            }
        }

        #region CLUSTER 1: SHIP INTEGRITY & PLASMA CHARGE

        private void HandleHealthBarUpdated(int percentHealthRemaining)
        {
            _cachedHealthPercent = Mathf.Clamp(percentHealthRemaining, 0, 100);
            UpdateHealthUI(_cachedHealthPercent);
        }

        private void UpdateHealthUI(int percent)
        {
            if (shieldBarFill != null)
            {
                shieldBarFill.fillAmount = percent / 100f;
            }

            if (shieldPercentText != null)
            {
                shieldPercentText.text = PERCENT_STRINGS[Mathf.Clamp(percent, 0, 100)];
            }
        }

        private void UpdateShieldAlertPulsing()
        {
            if (shieldBarFill == null) return;

            if (_cachedHealthPercent <= 30 && _cachedHealthPercent > 0)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
                shieldBarFill.color = Color.Lerp(COLOR_CRITICAL_RED, Color.yellow, pulse);
            }
            else if (_cachedHealthPercent > 30)
            {
                shieldBarFill.color = COLOR_CYAN;
            }
        }

        /// <summary>
        /// Cập nhật Thanh Tụ Lực Plasma Charge theo thời gian hồi 2.0s và trạng thái nạp.
        /// Khi đầy 100% mới cho phép Charge Shot (Chuẩn điều chỉnh được người dùng phê duyệt).
        /// </summary>
        private void UpdateChargeMeterUI(VirtualRailConfig cfg)
        {
            if (chargeBarFill == null) return;

            float chargeTime = (cfg != null && cfg.chargeTime > 0.1f) ? cfg.chargeTime : 2.0f;
            float cooldownTime = (cfg != null && cfg.chargeCooldown > 0.1f) ? cfg.chargeCooldown : 2.0f;

            bool isWeaponInCooldown = (weapon != null && weapon.IsWeaponInCooldown);
            bool isAimAssistCharging = (aimAssist != null && aimAssist.IsCharging);

            if (isWeaponInCooldown)
            {
                // Đang trong thời gian hồi 2.0s sau khi phóng đạn
                float remaining = weapon.WeaponCooldownRemaining;
                float progress = Mathf.Clamp01(1.0f - (remaining / cooldownTime));
                chargeBarFill.fillAmount = progress;
                chargeBarFill.color = Color.Lerp(Color.red, COLOR_AMBER, progress);

                if (chargeStatusText != null) chargeStatusText.text = "RECHARGING...";
                if (chargeTimerText != null)
                {
                    int timerInt = Mathf.CeilToInt(remaining);
                    chargeTimerText.text = timerInt > 0 ? timerInt + "s" : "";
                }
            }
            else if (isAimAssistCharging)
            {
                // Người chơi đang giữ nút bắn để tụ lực
                float progress = (aimAssist != null) ? aimAssist.ChargeProgress : 0f;
                chargeBarFill.fillAmount = progress;

                if (aimAssist != null && aimAssist.IsFullyCharged)
                {
                    float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 14f);
                    chargeBarFill.color = COLOR_AMBER_READY * pulse;
                    if (chargeStatusText != null) chargeStatusText.text = "CHARGE READY (2.0s)";
                    if (chargeTimerText != null) chargeTimerText.text = "100%";
                }
                else
                {
                    chargeBarFill.color = COLOR_AMBER;
                    int percentInt = Mathf.RoundToInt(progress * 100f);
                    if (chargeStatusText != null) chargeStatusText.text = "CHARGING...";
                    if (chargeTimerText != null) chargeTimerText.text = percentInt + "%";
                }
            }
            else
            {
                // Sẵn sàng nạp tụ lực (Đã hồi xong)
                chargeBarFill.fillAmount = 1.0f;
                chargeBarFill.color = COLOR_AMBER_READY;
                if (chargeStatusText != null) chargeStatusText.text = "CHARGE READY (2.0s)";
                if (chargeTimerText != null) chargeTimerText.text = "100%";
            }
        }

        private void HandleBombCountChanged(int currentBombs)
        {
            _cachedBombs = Mathf.Max(0, currentBombs);
            UpdateBombDisplay(_cachedBombs);
        }

        private void UpdateBombDisplay(int bombCount)
        {
            if (bombIconImages != null)
            {
                for (int i = 0; i < bombIconImages.Length; i++)
                {
                    if (bombIconImages[i] != null)
                    {
                        bombIconImages[i].gameObject.SetActive(i < bombCount);
                    }
                }
            }

            if (bombCountText != null)
            {
                bombCountText.text = "x" + bombCount;
            }
        }

        #endregion

        #region CLUSTER 2: ARCADE SCORE & COMBO

        private void HandleGameStarted()
        {
            SuppressLegacyGameUI();
            SetHUDVisible(true);
            _cachedScore = 0;
            _cachedHits = 0;
            UpdateScoreUI(_cachedScore);
            UpdateHitsUI(_cachedHits);
            if (comboBadgeRoot != null) comboBadgeRoot.gameObject.SetActive(false);
        }

        private void HandleScoreIncremented(int points)
        {
            _cachedScore += points;
            UpdateScoreUI(_cachedScore);
        }

        private void HandleComboScored(int hitCount, int totalPoints)
        {
            _cachedHits += hitCount;
            UpdateHitsUI(_cachedHits);
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = score.ToString("000,000");
            }
        }

        private void UpdateHitsUI(int hits)
        {
            if (hitCountValueText != null)
            {
                hitCountValueText.text = (hits >= 0 && hits < 1000) ? HITS_STRINGS[hits] : hits.ToString("D3");
            }
        }

        private void HandleMultiplierChanged(int multiplier)
        {
            if (comboBadgeRoot != null)
            {
                comboBadgeRoot.gameObject.SetActive(multiplier > 1);
            }

            var engine = ComboScoringEngine.Instance;
            int idx = (engine != null) ? engine.MultiplierIndex : 0;
            idx = Mathf.Clamp(idx, 0, ComboScoringEngine.MULTIPLIER_TEXTS.Length - 1);

            if (comboMultiplierText != null)
            {
                comboMultiplierText.text = ComboScoringEngine.MULTIPLIER_TEXTS[idx];
                comboMultiplierText.color = ComboScoringEngine.MULTIPLIER_COLORS[idx];
            }

            if (comboDecayRadial != null)
            {
                comboDecayRadial.color = ComboScoringEngine.MULTIPLIER_COLORS[idx];
            }
        }

        private void HandleMultiplierDecayed()
        {
            if (comboDecayRadial != null) comboDecayRadial.fillAmount = 1f;
        }

        private void HandleMultiplierReset()
        {
            if (comboBadgeRoot != null)
            {
                comboBadgeRoot.gameObject.SetActive(false);
            }

            if (comboMultiplierText != null)
            {
                comboMultiplierText.text = "x1";
                comboMultiplierText.color = ComboScoringEngine.MULTIPLIER_COLORS[0];
            }

            if (comboDecayRadial != null)
            {
                comboDecayRadial.fillAmount = 0f;
            }
        }

        #endregion

        #region ENDLESS TELEMETRY & GRAZE COMBAT TEXT (0 GC ALLOC)

        private void EnsureGrazePoolInitialized()
        {
            if (_grazeSlots != null && _grazeSlots.Length > 0) return;
            if (grazePopupTexts != null && grazePopupTexts.Length > 0)
            {
                _grazeSlots = new GrazePopupSlot[grazePopupTexts.Length];
                for (int i = 0; i < grazePopupTexts.Length; i++)
                {
                    _grazeSlots[i] = new GrazePopupSlot
                    {
                        textObj = grazePopupTexts[i],
                        rectTransform = grazePopupTexts[i].GetComponent<RectTransform>(),
                        timer = 0f,
                        active = false
                    };
                    grazePopupTexts[i].gameObject.SetActive(false);
                }
            }
        }

        private void HandleGrazeTriggered(Vector3 worldPos, int points)
        {
            EnsureGrazePoolInitialized();
            if (_grazeSlots == null || _grazeSlots.Length == 0) return;

            int slotIdx = _nextGrazeSlot;
            _nextGrazeSlot = (_nextGrazeSlot + 1) % _grazeSlots.Length;

            ref var slot = ref _grazeSlots[slotIdx];

            int strIdx = 0;
            if (points >= 16000) strIdx = 5;
            else if (points >= 8000) strIdx = 4;
            else if (points >= 4000) strIdx = 3;
            else if (points >= 2000) strIdx = 2;
            else if (points >= 1000) strIdx = 1;
            else strIdx = 0;

            slot.textObj.text = GRAZE_STRINGS[strIdx];

            Vector2 localPoint = Vector2.zero;
            if (_mainCamera != null)
            {
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        grazeFloatingRoot != null ? grazeFloatingRoot : (RectTransform)transform,
                        screenPos,
                        null,
                        out localPoint
                    );
                }
            }

            slot.rectTransform.anchoredPosition = localPoint + new Vector2(0f, 25f);
            slot.rectTransform.localScale = new Vector3(1.3f, 1.3f, 1f);
            slot.timer = 0.75f;
            slot.active = true;

            Color col = COLOR_CYAN;
            col.a = 1f;
            slot.textObj.color = col;
            slot.textObj.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.GrazePass, 1.0f);
            }
        }

        private void UpdateGrazeFloatingCombatText()
        {
            if (_grazeSlots == null) return;

            for (int i = 0; i < _grazeSlots.Length; i++)
            {
                if (!_grazeSlots[i].active) continue;

                _grazeSlots[i].timer -= Time.deltaTime;
                if (_grazeSlots[i].timer <= 0f)
                {
                    _grazeSlots[i].active = false;
                    _grazeSlots[i].textObj.gameObject.SetActive(false);
                    continue;
                }

                Vector2 pos = _grazeSlots[i].rectTransform.anchoredPosition;
                pos.y += 50f * Time.deltaTime;
                _grazeSlots[i].rectTransform.anchoredPosition = pos;

                float progress = 1f - (_grazeSlots[i].timer / 0.75f);
                float alpha = Mathf.Clamp01(_grazeSlots[i].timer / 0.75f);
                float scale = Mathf.Lerp(1.3f, 1.0f, progress);
                _grazeSlots[i].rectTransform.localScale = new Vector3(scale, scale, 1f);

                Color c = _grazeSlots[i].textObj.color;
                c.a = alpha;
                _grazeSlots[i].textObj.color = c;
            }
        }

        private void UpdateEndlessTelemetryUI()
        {
            // 1. Cập nhật Threat Level & Progress Fill (DDA)
            var endlessDirector = VirtualRailEndlessDirector.Instance;
            if (endlessDirector != null)
            {
                int threat = Mathf.Clamp(endlessDirector.ThreatLevel, 1, 30);
                if (threat != _lastThreatLevel)
                {
                    _lastThreatLevel = threat;
                    if (threatLevelText != null)
                    {
                        threatLevelText.text = THREAT_STRINGS[threat];
                        if (threat <= 2)
                            threatLevelText.color = COLOR_CYAN;
                        else if (threat <= 5)
                            threatLevelText.color = COLOR_AMBER;
                        else
                            threatLevelText.color = COLOR_CRITICAL_RED;
                    }
                }

                if (threatProgressFill != null)
                {
                    threatProgressFill.fillAmount = endlessDirector.ThreatLevelProgress01;
                    if (threat <= 2)
                        threatProgressFill.color = COLOR_CYAN;
                    else if (threat <= 5)
                        threatProgressFill.color = COLOR_AMBER;
                    else
                        threatProgressFill.color = COLOR_CRITICAL_RED;
                }
            }

            // 2. Cập nhật Distance Tracker (Z-position hoặc Spline Arc Distance hoặc 64-bit Lifetime Distance theo mốc 10m)
            if (distanceTrackerText != null)
            {
                float currentZ = VirtualRailOriginShifter.Instance != null
                    ? (float)VirtualRailOriginShifter.Instance.LifetimeTraveledDistance
                    : ((anchor != null) ? (anchor.UseProceduralSpline ? anchor.AccumulatedArcDistance : anchor.transform.position.z) : 0f);
                int distMeters = Mathf.Max(0, Mathf.RoundToInt(currentZ));
                int step = Mathf.Clamp(distMeters / 10, 0, DIST_STRINGS.Length - 1);
                if (step != _lastDistanceStep)
                {
                    _lastDistanceStep = step;
                    distanceTrackerText.text = DIST_STRINGS[step];
                }
            }
        }

        #endregion

        #region CLUSTER 5: WEAPON TIER & SPEEDOMETER

        private void UpdateWeaponTierText()
        {
            if (weaponTierText == null) return;

            var cfg = (anchor != null) ? anchor.config : null;
            if (cfg != null && cfg.useSegmentedLaser)
            {
                weaponTierText.text = "TWIN LASER MK-II";
            }
            else
            {
                weaponTierText.text = "PLASMA LASER MK-I";
            }
        }

        private void UpdateSpeedometerUI()
        {
            float speed = (anchor != null) ? anchor.CurrentSpeed : 70f;
            int speedInt = Mathf.Clamp(Mathf.RoundToInt(speed), 0, 350);

            if (speedInt != _lastSpeedInt)
            {
                _lastSpeedInt = speedInt;
                if (speedometerValueText != null)
                {
                    speedometerValueText.text = SPEED_STRINGS[speedInt];
                }
            }

            if (speedometerGaugeFill != null)
            {
                speedometerGaugeFill.fillAmount = Mathf.Clamp01(speed / 240f);
            }
        }

        #endregion

        #region CLUSTER 6: CLIMAX BOSS HEALTH BAR

        private void HandleBossSpawned(BossF3CorvetteController boss)
        {
            EnsureBarSpritesAssigned();
            if (clusterBossRoot != null)
            {
                clusterBossRoot.gameObject.SetActive(true);
            }
            if (bossNameText != null)
            {
                bossNameText.text = "FEDERATION CORVETTE F3 // CRIMSON TITAN";
            }
            if (bossHealthBarFill != null) bossHealthBarFill.fillAmount = 1f;
            if (bossGhostBarFill != null) bossGhostBarFill.fillAmount = 1f;
            if (bossShieldBarFill != null) bossShieldBarFill.fillAmount = 1f;
            if (bossShieldGhostBarFill != null) bossShieldGhostBarFill.fillAmount = 1f;
            if (bossPhaseText != null)
            {
                bossPhaseText.text = "PHASE 1: DORSAL SHIELD ONLINE";
            }
        }

        private void HandleBossHealthChanged(int current, int max, BossBattlePhase phase)
        {
            if (clusterBossRoot != null && !clusterBossRoot.gameObject.activeSelf)
            {
                clusterBossRoot.gameObject.SetActive(true);
            }

            float ratio = (max > 0) ? Mathf.Clamp01((float)current / max) : 0f;
            if (bossHealthBarFill != null)
            {
                bossHealthBarFill.fillAmount = ratio;
            }

            if (bossPhaseText != null)
            {
                switch (phase)
                {
                    case BossBattlePhase.IntroBreach:
                        bossPhaseText.text = "WARNING: SIGNATURE DETECTED";
                        break;
                    case BossBattlePhase.Phase1_DeckStrafe:
                        if (bossShieldBarFill == null || bossShieldBarFill.fillAmount <= 0.001f)
                        {
                            bossPhaseText.text = "PHASE 1: DORSAL SHIELD ONLINE";
                        }
                        break;
                    case BossBattlePhase.Phase2_ReactorTrench:
                        bossPhaseText.text = "PHASE 2: REACTOR TRENCH EXPOSED";
                        break;
                    case BossBattlePhase.Phase3_MeltdownOverdrive:
                        bossPhaseText.text = "PHASE 3: MELTDOWN OVERDRIVE";
                        break;
                    case BossBattlePhase.Defeated:
                        bossPhaseText.text = "TARGET DESTROYED // WARP ENGAGED";
                        break;
                }
            }
        }

        private void HandleBossShieldChanged(int current, int max)
        {
            if (clusterBossRoot != null && !clusterBossRoot.gameObject.activeSelf)
            {
                clusterBossRoot.gameObject.SetActive(true);
            }

            float ratio = (max > 0) ? Mathf.Clamp01((float)current / max) : 0f;
            if (bossShieldBarFill != null)
            {
                bossShieldBarFill.fillAmount = ratio;
            }

            if (bossPhaseText != null && ratio > 0.001f)
            {
                int pct = Mathf.RoundToInt(ratio * 100f);
                bossPhaseText.text = $"PHASE 1: DORSAL SHIELD [{pct}%]";
            }
        }

        private void HandleBossShieldBroken()
        {
            if (bossShieldBarFill != null) bossShieldBarFill.fillAmount = 0f;
            if (bossShieldGhostBarFill != null) bossShieldGhostBarFill.fillAmount = 0f;
            if (bossPhaseText != null)
            {
                bossPhaseText.text = "PHASE 2: SHIELD DESTROYED // REACTOR EXPOSED";
                bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            }
        }

        private void HandleBossTrenchCharging(bool isCharging)
        {
            if (bossPhaseText == null) return;

            if (isCharging)
            {
                bossPhaseText.text = "PHASE 2: WARNING! THERMAL CORE OVERCHARGING! [STRIKE NOW!]";
                bossPhaseText.color = new Color(1f, 0.45f, 0.1f, 1f); // Blazing Neon Orange
            }
            else
            {
                BossBattlePhase currentPhase = (BossF3CorvetteController.Instance != null)
                    ? BossF3CorvetteController.Instance.CurrentPhase
                    : BossBattlePhase.Phase2_ReactorTrench;

                if (currentPhase == BossBattlePhase.Phase3_MeltdownOverdrive)
                {
                    bossPhaseText.text = "PHASE 3: MELTDOWN OVERDRIVE";
                }
                else
                {
                    bossPhaseText.text = "PHASE 2: REACTOR TRENCH EXPOSED";
                }
                bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            }
        }

        private void HandleBossHyperLaserCharging(bool isCharging)
        {
            if (bossPhaseText == null) return;

            if (isCharging)
            {
                bossPhaseText.text = "PHASE 3: WARNING! HYPER LASER OVERCHARGING! [EVADE NOW!]";
                bossPhaseText.color = new Color(0.35f, 0.75f, 1f, 1f); // Electric Cyan Warning
            }
            else
            {
                bossPhaseText.text = "PHASE 3: MELTDOWN OVERDRIVE";
                bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            }
        }

        private void HandleBossStunned(bool isStunned)
        {
            if (bossPhaseText == null) return;

            BossBattlePhase currentPhase = (BossF3CorvetteController.Instance != null)
                ? BossF3CorvetteController.Instance.CurrentPhase
                : BossBattlePhase.Phase2_ReactorTrench;

            if (isStunned)
            {
                string phasePrefix = (currentPhase == BossBattlePhase.Phase3_MeltdownOverdrive) ? "PHASE 3" : "PHASE 2";
                bossPhaseText.text = $"{phasePrefix}: THERMAL CORE OVERLOADED // BOSS STUNNED!";
                bossPhaseText.color = new Color(0.2f, 0.85f, 1f, 1f); // Neon Cyan Glitch
            }
            else
            {
                switch (currentPhase)
                {
                    case BossBattlePhase.Phase1_DeckStrafe:
                        bossPhaseText.text = "PHASE 1: DORSAL SHIELD ONLINE";
                        break;
                    case BossBattlePhase.Phase2_ReactorTrench:
                        bossPhaseText.text = "PHASE 2: REACTOR TRENCH EXPOSED";
                        break;
                    case BossBattlePhase.Phase3_MeltdownOverdrive:
                        bossPhaseText.text = "PHASE 3: MELTDOWN OVERDRIVE";
                        break;
                    default:
                        bossPhaseText.text = "PHASE 2: REACTOR TRENCH EXPOSED";
                        break;
                }
                bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            }
        }

        private void HandleBossDefeated()
        {
            if (bossHealthBarFill != null) bossHealthBarFill.fillAmount = 0f;
            if (bossGhostBarFill != null) bossGhostBarFill.fillAmount = 0f;
            if (bossShieldBarFill != null) bossShieldBarFill.fillAmount = 0f;
            if (bossShieldGhostBarFill != null) bossShieldGhostBarFill.fillAmount = 0f;
            if (clusterBossRoot != null)
            {
                clusterBossRoot.gameObject.SetActive(false);
            }
        }

        private static Sprite _proceduralBarSprite;

        private Sprite GetDefaultBarSprite()
        {
            if (shieldBarFill != null && shieldBarFill.sprite != null)
            {
                return shieldBarFill.sprite;
            }
            if (_proceduralBarSprite == null)
            {
                Texture2D whiteTex = Texture2D.whiteTexture;
                _proceduralBarSprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
            }
            return _proceduralBarSprite;
        }

        private void EnsureBarSpritesAssigned()
        {
            Sprite barSprite = GetDefaultBarSprite();
            if (barSprite == null) return;

            if (bossHealthBarFill != null)
            {
                if (bossHealthBarFill.sprite == null) bossHealthBarFill.sprite = barSprite;
                bossHealthBarFill.type = Image.Type.Filled;
            }
            if (bossGhostBarFill != null)
            {
                if (bossGhostBarFill.sprite == null) bossGhostBarFill.sprite = barSprite;
                bossGhostBarFill.type = Image.Type.Filled;
            }
            if (bossShieldBarFill != null)
            {
                if (bossShieldBarFill.sprite == null) bossShieldBarFill.sprite = barSprite;
                bossShieldBarFill.type = Image.Type.Filled;
            }
            if (bossShieldGhostBarFill != null)
            {
                if (bossShieldGhostBarFill.sprite == null) bossShieldGhostBarFill.sprite = barSprite;
                bossShieldGhostBarFill.type = Image.Type.Filled;
            }
        }

        private void EnsureBossClusterCreated()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (clusterBossRoot != null)
            {
                EnsureShieldBarUnder(clusterBossRoot);
                EnsureBarSpritesAssigned();
                return;
            }

            Transform existing = transform.Find("Cluster_Boss");
            if (existing != null)
            {
                clusterBossRoot = existing.GetComponent<RectTransform>();
                if (bossNameText == null) bossNameText = existing.Find("BossName")?.GetComponent<Text>();
                if (bossHealthBarFill == null) bossHealthBarFill = existing.Find("HealthBar/Fill")?.GetComponent<Image>();
                if (bossGhostBarFill == null) bossGhostBarFill = existing.Find("HealthBar/GhostFill")?.GetComponent<Image>();
                if (bossPhaseText == null) bossPhaseText = existing.Find("PhaseText")?.GetComponent<Text>();
                EnsureShieldBarUnder(clusterBossRoot);
                EnsureBarSpritesAssigned();
                return;
            }

            GameObject bossPanel = new GameObject("Cluster_Boss");
            bossPanel.transform.SetParent(transform, false);

            RectTransform rt = bossPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -22f);
            rt.sizeDelta = new Vector2(540f, 74f);

            Image panelBg = bossPanel.AddComponent<Image>();
            panelBg.color = new Color(0.02f, 0.08f, 0.14f, 0.88f);

            Outline outline = bossPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 1. Tên Boss
            GameObject nameObj = new GameObject("BossName");
            nameObj.transform.SetParent(bossPanel.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -4f);
            nameRt.sizeDelta = new Vector2(-24f, 16f);

            bossNameText = nameObj.AddComponent<Text>();
            bossNameText.font = font;
            bossNameText.fontSize = 12;
            bossNameText.fontStyle = FontStyle.Bold;
            bossNameText.alignment = TextAnchor.MiddleCenter;
            bossNameText.color = new Color(1f, 0.75f, 0.2f, 1f);
            bossNameText.text = "FEDERATION CORVETTE F3 // CRIMSON TITAN";

            // 2. Thanh Máu Thân Tàu (HealthBar)
            GameObject barObj = new GameObject("HealthBar");
            barObj.transform.SetParent(bossPanel.transform, false);
            RectTransform barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.5f);
            barRt.anchorMax = new Vector2(1f, 0.5f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.anchoredPosition = new Vector2(0f, -7f);
            barRt.sizeDelta = new Vector2(-28f, 11f);

            Image trackImg = barObj.AddComponent<Image>();
            trackImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            Sprite barSprite = GetDefaultBarSprite();

            // Ghost Fill
            GameObject ghostObj = new GameObject("GhostFill");
            ghostObj.transform.SetParent(barObj.transform, false);
            RectTransform ghostRt = ghostObj.AddComponent<RectTransform>();
            ghostRt.anchorMin = Vector2.zero;
            ghostRt.anchorMax = Vector2.one;
            ghostRt.offsetMin = Vector2.zero;
            ghostRt.offsetMax = Vector2.zero;

            bossGhostBarFill = ghostObj.AddComponent<Image>();
            bossGhostBarFill.sprite = barSprite;
            bossGhostBarFill.type = Image.Type.Filled;
            bossGhostBarFill.fillMethod = Image.FillMethod.Horizontal;
            bossGhostBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            bossGhostBarFill.fillAmount = 1f;
            bossGhostBarFill.color = new Color(1f, 0.85f, 0.35f, 0.7f);

            // Main Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            bossHealthBarFill = fillObj.AddComponent<Image>();
            bossHealthBarFill.sprite = barSprite;
            bossHealthBarFill.type = Image.Type.Filled;
            bossHealthBarFill.fillMethod = Image.FillMethod.Horizontal;
            bossHealthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            bossHealthBarFill.fillAmount = 1f;
            bossHealthBarFill.color = new Color(1f, 0.22f, 0.15f, 1f);

            // 3. Khởi tạo thanh khiên riêng
            EnsureShieldBarUnder(bossPanel.transform);

            // 4. Phase Subtitle Text
            GameObject phaseObj = new GameObject("PhaseText");
            phaseObj.transform.SetParent(bossPanel.transform, false);
            RectTransform phaseRt = phaseObj.AddComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0f, 0f);
            phaseRt.anchorMax = new Vector2(1f, 0f);
            phaseRt.pivot = new Vector2(0.5f, 0f);
            phaseRt.anchoredPosition = new Vector2(0f, 4f);
            phaseRt.sizeDelta = new Vector2(-24f, 14f);

            bossPhaseText = phaseObj.AddComponent<Text>();
            bossPhaseText.font = font;
            bossPhaseText.fontSize = 10;
            bossPhaseText.fontStyle = FontStyle.Normal;
            bossPhaseText.alignment = TextAnchor.MiddleCenter;
            bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            bossPhaseText.text = "PHASE 1: DORSAL SHIELD ONLINE";

            clusterBossRoot = rt;
            EnsureBarSpritesAssigned();
            bossPanel.SetActive(false);
        }

        private void EnsureShieldBarUnder(Transform parent)
        {
            if (parent == null) return;

            Sprite barSprite = GetDefaultBarSprite();

            Transform existingBar = parent.Find("ShieldBar");
            if (existingBar != null)
            {
                if (bossShieldBarFill == null) bossShieldBarFill = existingBar.Find("Fill")?.GetComponent<Image>();
                if (bossShieldGhostBarFill == null) bossShieldGhostBarFill = existingBar.Find("GhostFill")?.GetComponent<Image>();
                EnsureBarSpritesAssigned();
                return;
            }

            // Tự động sinh ShieldBar (Tầng trên của thanh máu)
            GameObject shieldObj = new GameObject("ShieldBar");
            shieldObj.transform.SetParent(parent, false);

            RectTransform sRt = shieldObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.5f);
            sRt.anchorMax = new Vector2(1f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0f, 6f);
            sRt.sizeDelta = new Vector2(-28f, 9f);

            Image sTrack = shieldObj.AddComponent<Image>();
            sTrack.color = new Color(0.04f, 0.10f, 0.16f, 0.95f);

            // Shield Ghost Fill
            GameObject sGhost = new GameObject("GhostFill");
            sGhost.transform.SetParent(shieldObj.transform, false);
            RectTransform sGhostRt = sGhost.AddComponent<RectTransform>();
            sGhostRt.anchorMin = Vector2.zero;
            sGhostRt.anchorMax = Vector2.one;
            sGhostRt.offsetMin = Vector2.zero;
            sGhostRt.offsetMax = Vector2.zero;

            bossShieldGhostBarFill = sGhost.AddComponent<Image>();
            bossShieldGhostBarFill.sprite = barSprite;
            bossShieldGhostBarFill.type = Image.Type.Filled;
            bossShieldGhostBarFill.fillMethod = Image.FillMethod.Horizontal;
            bossShieldGhostBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            bossShieldGhostBarFill.fillAmount = 1f;
            bossShieldGhostBarFill.color = new Color(0.2f, 0.85f, 1f, 0.55f);

            // Shield Main Fill
            GameObject sFill = new GameObject("Fill");
            sFill.transform.SetParent(shieldObj.transform, false);
            RectTransform sFillRt = sFill.AddComponent<RectTransform>();
            sFillRt.anchorMin = Vector2.zero;
            sFillRt.anchorMax = Vector2.one;
            sFillRt.offsetMin = Vector2.zero;
            sFillRt.offsetMax = Vector2.zero;

            bossShieldBarFill = sFill.AddComponent<Image>();
            bossShieldBarFill.sprite = barSprite;
            bossShieldBarFill.type = Image.Type.Filled;
            bossShieldBarFill.fillMethod = Image.FillMethod.Horizontal;
            bossShieldBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            bossShieldBarFill.fillAmount = 1f;
            bossShieldBarFill.color = new Color(0f, 0.95f, 1f, 1f); // Neon Cyan

            // Điều chỉnh vị trí HealthBar nếu cần
            Transform hb = parent.Find("HealthBar");
            if (hb != null)
            {
                RectTransform hbRt = hb.GetComponent<RectTransform>();
                if (hbRt != null)
                {
                    hbRt.anchoredPosition = new Vector2(0f, -7f);
                    hbRt.sizeDelta = new Vector2(-28f, 11f);
                }
            }

            EnsureBarSpritesAssigned();
        }

        private void EnsureTelemetryAndGrazeCreated()
        {
            Font font = (scoreValueText != null && scoreValueText.font != null) ? scoreValueText.font :
                        (hitCountValueText != null && hitCountValueText.font != null) ? hitCountValueText.font :
                        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Tự động sinh hoặc cấu hình TelemetryBox nếu chưa tồn tại
            if (clusterTopRightRoot != null && threatLevelText == null)
            {
                Transform existingBox = clusterTopRightRoot.Find("TelemetryBox");
                if (existingBox == null)
                {
                    clusterTopRightRoot.sizeDelta = new Vector2(355f, 335f);

                    GameObject telBox = new GameObject("TelemetryBox");
                    telBox.transform.SetParent(clusterTopRightRoot, false);
                    RectTransform tbRt = telBox.AddComponent<RectTransform>();
                    tbRt.anchorMin = new Vector2(1f, 1f);
                    tbRt.anchorMax = new Vector2(1f, 1f);
                    tbRt.pivot = new Vector2(1f, 1f);
                    tbRt.anchoredPosition = new Vector2(0f, -148f);
                    tbRt.sizeDelta = new Vector2(355f, 48f);

                    Image telBg = telBox.AddComponent<Image>();
                    telBg.color = new Color(0.02f, 0.08f, 0.14f, 0.88f);
                    // Mượn frame bracket sprite từ các cụm khác nếu có
                    Image existingBracket = transform.Find("Cluster_BottomLeft_Comms")?.GetComponent<Image>()
                                         ?? transform.Find("Cluster_Boss")?.GetComponent<Image>();
                    if (existingBracket != null && existingBracket.sprite != null)
                    {
                        telBg.sprite = existingBracket.sprite;
                        telBg.type = Image.Type.Sliced;
                    }

                    // Threat Badge
                    GameObject tb = new GameObject("ThreatBadge");
                    tb.transform.SetParent(telBox.transform, false);
                    RectTransform tbChildRt = tb.AddComponent<RectTransform>();
                    tbChildRt.anchorMin = new Vector2(0f, 0f);
                    tbChildRt.anchorMax = new Vector2(0.52f, 1f);
                    tbChildRt.offsetMin = Vector2.zero;
                    tbChildRt.offsetMax = Vector2.zero;

                    GameObject tlObj = new GameObject("ThreatLabel");
                    tlObj.transform.SetParent(tb.transform, false);
                    RectTransform tlRt = tlObj.AddComponent<RectTransform>();
                    tlRt.anchorMin = new Vector2(0f, 0.5f);
                    tlRt.anchorMax = new Vector2(1f, 0.5f);
                    tlRt.pivot = new Vector2(0f, 0.5f);
                    tlRt.anchoredPosition = new Vector2(16f, 7f);
                    tlRt.sizeDelta = new Vector2(-20f, 20f);
                    threatLevelText = tlObj.AddComponent<Text>();
                    threatLevelText.font = font;
                    threatLevelText.fontSize = 13;
                    threatLevelText.fontStyle = FontStyle.Bold;
                    threatLevelText.color = COLOR_CYAN;
                    threatLevelText.text = "THREAT LV.01";
                    threatLevelText.alignment = TextAnchor.MiddleLeft;

                    GameObject tpObj = new GameObject("ThreatProgress");
                    tpObj.transform.SetParent(tb.transform, false);
                    RectTransform tpRt = tpObj.AddComponent<RectTransform>();
                    tpRt.anchorMin = new Vector2(0f, 0.5f);
                    tpRt.anchorMax = new Vector2(1f, 0.5f);
                    tpRt.pivot = new Vector2(0f, 0.5f);
                    tpRt.anchoredPosition = new Vector2(16f, -10f);
                    tpRt.sizeDelta = new Vector2(-32f, 6f);
                    Image tpTrack = tpObj.AddComponent<Image>();
                    tpTrack.color = new Color(0.08f, 0.22f, 0.32f, 0.95f);

                    GameObject tpFillObj = new GameObject("Fill");
                    tpFillObj.transform.SetParent(tpObj.transform, false);
                    RectTransform tpfRt = tpFillObj.AddComponent<RectTransform>();
                    tpfRt.anchorMin = Vector2.zero;
                    tpfRt.anchorMax = Vector2.one;
                    tpfRt.offsetMin = Vector2.zero;
                    tpfRt.offsetMax = Vector2.zero;
                    threatProgressFill = tpFillObj.AddComponent<Image>();
                    threatProgressFill.type = Image.Type.Filled;
                    threatProgressFill.fillMethod = Image.FillMethod.Horizontal;
                    threatProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    threatProgressFill.fillAmount = 0.25f;
                    threatProgressFill.color = COLOR_CYAN;

                    // Mượn segmented sprite nếu có
                    Image existingSeg = transform.Find("Cluster_TopLeft/LaserChargeGauge/Fill")?.GetComponent<Image>()
                                     ?? transform.Find("Cluster_Boss/BossHealthBar/Fill")?.GetComponent<Image>();
                    if (existingSeg != null && existingSeg.sprite != null)
                    {
                        threatProgressFill.sprite = existingSeg.sprite;
                    }

                    // Distance Tracker
                    GameObject dt = new GameObject("DistanceTracker");
                    dt.transform.SetParent(telBox.transform, false);
                    RectTransform dtChildRt = dt.AddComponent<RectTransform>();
                    dtChildRt.anchorMin = new Vector2(0.52f, 0f);
                    dtChildRt.anchorMax = new Vector2(1f, 1f);
                    dtChildRt.offsetMin = Vector2.zero;
                    dtChildRt.offsetMax = Vector2.zero;

                    GameObject dlObj = new GameObject("DistLabel");
                    dlObj.transform.SetParent(dt.transform, false);
                    RectTransform dlRt = dlObj.AddComponent<RectTransform>();
                    dlRt.anchorMin = new Vector2(0f, 0.5f);
                    dlRt.anchorMax = new Vector2(0f, 0.5f);
                    dlRt.pivot = new Vector2(0f, 0.5f);
                    dlRt.anchoredPosition = new Vector2(10f, 0f);
                    dlRt.sizeDelta = new Vector2(45f, 22f);
                    Text dlTxt = dlObj.AddComponent<Text>();
                    dlTxt.font = font;
                    dlTxt.fontSize = 12;
                    dlTxt.fontStyle = FontStyle.Bold;
                    dlTxt.color = new Color(0.6f, 0.78f, 0.88f, 0.85f);
                    dlTxt.text = "DIST:";
                    dlTxt.alignment = TextAnchor.MiddleLeft;

                    GameObject dvObj = new GameObject("DistanceValue");
                    dvObj.transform.SetParent(dt.transform, false);
                    RectTransform dvRt = dvObj.AddComponent<RectTransform>();
                    dvRt.anchorMin = new Vector2(1f, 0.5f);
                    dvRt.anchorMax = new Vector2(1f, 0.5f);
                    dvRt.pivot = new Vector2(1f, 0.5f);
                    dvRt.anchoredPosition = new Vector2(-16f, 0f);
                    dvRt.sizeDelta = new Vector2(120f, 24f);
                    distanceTrackerText = dvObj.AddComponent<Text>();
                    distanceTrackerText.font = font;
                    distanceTrackerText.fontSize = 14;
                    distanceTrackerText.fontStyle = FontStyle.Bold;
                    distanceTrackerText.color = COLOR_CYAN;
                    distanceTrackerText.text = "00,000 m";
                    distanceTrackerText.alignment = TextAnchor.MiddleRight;

                    // Điều chỉnh vị trí Combo Dial
                    Transform cd = clusterTopRightRoot.Find("ComboRadialDial");
                    if (cd != null)
                    {
                        RectTransform cdRt = cd.GetComponent<RectTransform>();
                        if (cdRt != null)
                        {
                            cdRt.anchoredPosition = new Vector2(0f, -200f);
                            cdRt.sizeDelta = new Vector2(118f, 118f);
                        }
                    }
                }
                else
                {
                    threatLevelText = existingBox.Find("ThreatBadge/ThreatLabel")?.GetComponent<Text>();
                    threatProgressFill = existingBox.Find("ThreatBadge/ThreatProgress/Fill")?.GetComponent<Image>();
                    distanceTrackerText = existingBox.Find("DistanceTracker/DistanceValue")?.GetComponent<Text>();
                }
            }

            // 2. Tự động sinh GrazeFloatingContainer nếu chưa tồn tại
            if (grazeFloatingRoot == null)
            {
                Transform existingG = transform.Find("GrazeFloatingContainer");
                if (existingG == null)
                {
                    GameObject gContainer = new GameObject("GrazeFloatingContainer");
                    gContainer.transform.SetParent(transform, false);
                    grazeFloatingRoot = gContainer.AddComponent<RectTransform>();
                    grazeFloatingRoot.anchorMin = new Vector2(0.5f, 0.5f);
                    grazeFloatingRoot.anchorMax = new Vector2(0.5f, 0.5f);
                    grazeFloatingRoot.pivot = new Vector2(0.5f, 0.5f);
                    grazeFloatingRoot.anchoredPosition = Vector2.zero;
                    grazeFloatingRoot.sizeDelta = new Vector2(400f, 300f);

                    grazePopupTexts = new Text[3];
                    for (int gp = 0; gp < 3; gp++)
                    {
                        GameObject gTextObj = new GameObject($"GrazePopup_{gp}");
                        gTextObj.transform.SetParent(grazeFloatingRoot, false);
                        RectTransform gtRt = gTextObj.AddComponent<RectTransform>();
                        gtRt.anchorMin = new Vector2(0.5f, 0.5f);
                        gtRt.anchorMax = new Vector2(0.5f, 0.5f);
                        gtRt.pivot = new Vector2(0.5f, 0.5f);
                        gtRt.sizeDelta = new Vector2(250f, 40f);

                        Text gTxt = gTextObj.AddComponent<Text>();
                        gTxt.font = font;
                        gTxt.fontSize = 24;
                        gTxt.fontStyle = FontStyle.Bold;
                        gTxt.alignment = TextAnchor.MiddleCenter;
                        gTxt.color = COLOR_CYAN;
                        gTxt.text = "+500 [GRAZE!]";

                        Outline gOutline = gTextObj.AddComponent<Outline>();
                        gOutline.effectColor = new Color(0f, 0.35f, 0.55f, 0.85f);
                        gOutline.effectDistance = new Vector2(1.5f, -1.5f);

                        gTextObj.SetActive(false);
                        grazePopupTexts[gp] = gTxt;
                    }
                }
                else
                {
                    grazeFloatingRoot = existingG.GetComponent<RectTransform>();
                    grazePopupTexts = existingG.GetComponentsInChildren<Text>(true);
                }
            }

            EnsureGrazePoolInitialized();
        }

        #endregion
    }
}
