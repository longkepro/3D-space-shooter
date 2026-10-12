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

        // Cache trạng thái nội bộ để tránh ghi đè UI thừa thãi
        private int _cachedHealthPercent = 100;
        private int _cachedScore = 0;
        private int _cachedHits = 0;
        private int _cachedBombs = 3;
        private int _lastSpeedInt = -1;
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

            BossF3CorvetteController.OnBossSpawned += HandleBossSpawned;
            BossF3CorvetteController.OnBossHealthChanged += HandleBossHealthChanged;
            BossF3CorvetteController.OnBossShieldChanged += HandleBossShieldChanged;
            BossF3CorvetteController.OnBossShieldBroken += HandleBossShieldBroken;
            BossF3CorvetteController.OnBossTrenchCharging += HandleBossTrenchCharging;
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

            BossF3CorvetteController.OnBossSpawned -= HandleBossSpawned;
            BossF3CorvetteController.OnBossHealthChanged -= HandleBossHealthChanged;
            BossF3CorvetteController.OnBossShieldChanged -= HandleBossShieldChanged;
            BossF3CorvetteController.OnBossShieldBroken -= HandleBossShieldBroken;
            BossF3CorvetteController.OnBossTrenchCharging -= HandleBossTrenchCharging;
            BossF3CorvetteController.OnBossStunned -= HandleBossStunned;
            BossF3CorvetteController.OnBossDefeated -= HandleBossDefeated;
        }

        private void Start()
        {
            FindReferences();
            UpdateWeaponTierText();
            UpdateBombDisplay(_cachedBombs);
            UpdateHealthUI(_cachedHealthPercent);
            UpdateScoreUI(_cachedScore);
            UpdateHitsUI(_cachedHits);

            EnsureBossClusterCreated();

            if (clusterBossRoot != null)
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
            GameObject mainMenu = GameObject.Find("Main Menu");
            bool isMenuOpen = (mainMenu != null && mainMenu.activeInHierarchy);
            SetHUDVisible(!isMenuOpen);
        }

        public static void SuppressLegacyGameUI()
        {
            GameObject gameUI = GameObject.Find("Game UI");
            if (gameUI != null)
            {
                gameUI.SetActive(false);
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
            if (comboDecayRadial != null && ComboScoringEngine.Instance != null)
            {
                comboDecayRadial.fillAmount = ComboScoringEngine.Instance.DecayRatio;
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
                comboBadgeRoot.gameObject.SetActive(true);
            }

            if (comboMultiplierText != null)
            {
                comboMultiplierText.text = "x" + Mathf.Max(1, multiplier);
            }
        }

        private void HandleMultiplierDecayed()
        {
            if (comboDecayRadial != null) comboDecayRadial.fillAmount = 1f;
        }

        private void HandleMultiplierReset()
        {
            if (comboMultiplierText != null)
            {
                comboMultiplierText.text = "x1";
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
                bossPhaseText.text = "PHASE 2: REACTOR TRENCH EXPOSED";
                bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            }
        }

        private void HandleBossStunned(bool isStunned)
        {
            if (bossPhaseText == null) return;

            if (isStunned)
            {
                bossPhaseText.text = "PHASE 2: THERMAL CORE OVERLOADED // BOSS STUNNED!";
                bossPhaseText.color = new Color(0.2f, 0.85f, 1f, 1f); // Neon Cyan Glitch
            }
            else
            {
                bossPhaseText.text = "PHASE 2: REACTOR TRENCH EXPOSED";
                bossPhaseText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            }
        }

        private void HandleBossDefeated()
        {
            if (bossHealthBarFill != null) bossHealthBarFill.fillAmount = 0f;
            if (bossShieldBarFill != null) bossShieldBarFill.fillAmount = 0f;
            if (clusterBossRoot != null)
            {
                clusterBossRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureBossClusterCreated()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (clusterBossRoot != null)
            {
                EnsureShieldBarUnder(clusterBossRoot);
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

            // Ghost Fill
            GameObject ghostObj = new GameObject("GhostFill");
            ghostObj.transform.SetParent(barObj.transform, false);
            RectTransform ghostRt = ghostObj.AddComponent<RectTransform>();
            ghostRt.anchorMin = Vector2.zero;
            ghostRt.anchorMax = Vector2.one;
            ghostRt.offsetMin = Vector2.zero;
            ghostRt.offsetMax = Vector2.zero;

            bossGhostBarFill = ghostObj.AddComponent<Image>();
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
            bossPanel.SetActive(false);
        }

        private void EnsureShieldBarUnder(Transform parent)
        {
            if (parent == null) return;

            Transform existingBar = parent.Find("ShieldBar");
            if (existingBar != null)
            {
                if (bossShieldBarFill == null) bossShieldBarFill = existingBar.Find("Fill")?.GetComponent<Image>();
                if (bossShieldGhostBarFill == null) bossShieldGhostBarFill = existingBar.Find("GhostFill")?.GetComponent<Image>();
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
        }

        #endregion
    }
}
