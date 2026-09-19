using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Động cơ tính điểm Combo chuyên sâu (Deep Scoring & Multiplier Engine) theo TDD v2.0.0 Phần III.
    /// Quản lý hệ số nhân động (1x -> 2x -> 4x -> 8x -> 16x -> 32x), thanh đếm lùi Multiplier Decay (3.5s),
    /// cơ chế thưởng lướt cận kề (Graze / Near-Miss), và chuỗi Splash Combo từ Charge Shot.
    /// Tuân thủ nghiêm ngặt 0 GC Allocation trong vòng lặp game loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class ComboScoringEngine : MonoBehaviour
    {
        public static ComboScoringEngine Instance { get; private set; }

        public static event Action<int, int> OnComboScored; // (killCount, totalHits)
        public static event Action<int> OnMultiplierChanged; // (currentMultiplier)
        public static event Action<Vector3, int> OnGrazeTriggered; // (worldPos, earnedPoints)
        public static event Action OnMultiplierDecayed;
        public static event Action OnMultiplierReset;

        public static readonly int[] MULTIPLIERS = new int[] { 1, 2, 4, 8, 16, 32 };
        public static readonly string[] MULTIPLIER_TEXTS = new string[] { "x1", "x2", "x4", "x8", "x16", "x32" };
        public static readonly Color[] MULTIPLIER_COLORS = new Color[]
        {
            new Color(0.65f, 0.75f, 0.85f, 0.70f), // 1x: Dim Silver / Cyan
            new Color(0.20f, 0.90f, 1.00f, 1.00f), // 2x: Bright Cyan
            new Color(0.20f, 1.00f, 0.45f, 1.00f), // 4x: Electric Emerald Green
            new Color(1.00f, 0.90f, 0.15f, 1.00f), // 8x: Solar Gold
            new Color(1.00f, 0.50f, 0.10f, 1.00f), // 16x: Plasma Orange
            new Color(0.95f, 0.25f, 1.00f, 1.00f)  // 32x: Cosmic Violet
        };

        [Header("Combo Scoring Settings")]
        [SerializeField] private int pointsPerHit = 50;
        [SerializeField] private float maxDecayTime = 3.5f;

        [Header("State Tracking (0 GC Alloc)")]
        private int _multiplierIndex = 0;
        private int _currentMultiplier = 1;
        private float _multiplierDecayTimer = 0f;
        private int _grazeStreak = 0;
        private int _totalSessionHits = 0;
        private int _totalSessionGrazes = 0;

        public int MultiplierIndex => _multiplierIndex;
        public int CurrentMultiplier => _currentMultiplier;
        public float MultiplierDecayTimer => _multiplierDecayTimer;
        public float MaxDecayTime => maxDecayTime;
        public float DecayRatio => (_currentMultiplier > 1 && maxDecayTime > 0.001f) ? Mathf.Clamp01(_multiplierDecayTimer / maxDecayTime) : 0f;
        public int GrazeStreak => _grazeStreak;
        public int TotalSessionHits => _totalSessionHits;
        public int TotalSessionGrazes => _totalSessionGrazes;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            GameEventManager.OnStartGame += ResetSession;
        }

        private void OnDisable()
        {
            GameEventManager.OnStartGame -= ResetSession;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Vòng lặp đếm lùi thanh duy trì Multiplier (TDD v2.0.0 Phần III.1)
            if (_currentMultiplier > 1)
            {
                _multiplierDecayTimer -= Time.deltaTime;
                if (_multiplierDecayTimer <= 0f)
                {
                    LevelDownMultiplier();
                }
            }
        }

        public void ResetSession()
        {
            _totalSessionHits = 0;
            _totalSessionGrazes = 0;
            _grazeStreak = 0;
            _multiplierIndex = 0;
            _currentMultiplier = 1;
            _multiplierDecayTimer = 0f;
            OnMultiplierChanged?.Invoke(_currentMultiplier);
        }

        /// <summary>
        /// Tăng bậc Multiplier (1x -> 2x -> 4x -> 8x -> 16x -> 32x).
        /// Kích hoạt khi: Charge Shot diệt >= 3 địch, hoặc đạt mỗi 10 Graze liên tiếp không dính đòn.
        /// </summary>
        public void LevelUpMultiplier()
        {
            if (_multiplierIndex < MULTIPLIERS.Length - 1)
            {
                _multiplierIndex++;
                _currentMultiplier = MULTIPLIERS[_multiplierIndex];
                _multiplierDecayTimer = maxDecayTime;
                OnMultiplierChanged?.Invoke(_currentMultiplier);
                Debug.Log($"<color=cyan><b>[MULTIPLIER LEVEL UP]</b></color> Hệ số nhân tăng lên: <b>{_currentMultiplier}x!</b> (Decay: {maxDecayTime}s)");
            }
            else
            {
                // Đang ở mốc cực đại 32x: Đầy lại 100% thanh thời gian
                _multiplierDecayTimer = maxDecayTime;
            }
        }

        /// <summary>
        /// Giảm 1 bậc Multiplier khi thanh thời gian chạm đáy (32x -> 16x -> 8x ...).
        /// </summary>
        public void LevelDownMultiplier()
        {
            if (_multiplierIndex > 0)
            {
                _multiplierIndex--;
                _currentMultiplier = MULTIPLIERS[_multiplierIndex];
                _multiplierDecayTimer = (_currentMultiplier > 1) ? maxDecayTime : 0f;
                OnMultiplierChanged?.Invoke(_currentMultiplier);
                OnMultiplierDecayed?.Invoke();
                Debug.Log($"<color=yellow><b>[MULTIPLIER DECAY]</b></color> Hết giờ duy trì! Multiplier tụt xuống: <b>{_currentMultiplier}x</b>");
            }
            else
            {
                _multiplierDecayTimer = 0f;
            }
        }

        /// <summary>
        /// Reset toàn bộ Multiplier về 1x và thanh thời gian về 0 khi tàu dính sát thương (TDD v2.0.0 Phần III.2).
        /// </summary>
        public void ResetMultiplierOnDamage()
        {
            if (_currentMultiplier > 1 || _grazeStreak > 0)
            {
                _multiplierIndex = 0;
                _currentMultiplier = 1;
                _multiplierDecayTimer = 0f;
                _grazeStreak = 0;
                OnMultiplierChanged?.Invoke(_currentMultiplier);
                OnMultiplierReset?.Invoke();
                Debug.Log("<color=red><b>[COMBO BREAK]</b></color> Tàu dính sát thương! Reset Multiplier về <b>1x</b>, đứt chuỗi Graze.");
            }
        }

        /// <summary>
        /// Ghi nhận diệt theo cụm (Splash Combo) từ phát bắn tụ lực (Charge Shot).
        /// Công thức chuẩn TDD: N + (N - 1)
        /// Diệt >= 3 tàu địch: Tự động Level Up Multiplier!
        /// </summary>
        public int RegisterSplashKill(int count)
        {
            if (count <= 0) return 0;

            int totalHits = (count == 1) ? 1 : (count + (count - 1));
            _totalSessionHits += totalHits;

            if (count >= 3)
            {
                LevelUpMultiplier();
            }
            else if (_currentMultiplier > 1)
            {
                _multiplierDecayTimer = Mathf.Min(_multiplierDecayTimer + 1.2f, maxDecayTime);
            }

            int bonusScore = totalHits * pointsPerHit * _currentMultiplier;
            GameEventManager.IncrementScore(bonusScore);

            OnComboScored?.Invoke(count, totalHits);

            Debug.Log($"<color=orange><b>[COMBO SPLASH]</b></color> Diệt {count} tàu địch -> Thưởng <b>+{totalHits} HITS!</b> (+{bonusScore} pts, Multiplier: {_currentMultiplier}x)");

            return totalHits;
        }

        /// <summary>
        /// Ghi nhận tiêu diệt đơn lẻ bằng đạn thường (Laser).
        /// Nhân điểm cơ sở theo Multiplier hiện tại và duy trì thanh thời gian (+0.5s).
        /// </summary>
        public void RegisterSingleKill(int basePoints)
        {
            if (_currentMultiplier > 1)
            {
                int bonus = basePoints * (_currentMultiplier - 1);
                if (bonus > 0)
                {
                    GameEventManager.IncrementScore(bonus);
                }
                _multiplierDecayTimer = Mathf.Min(_multiplierDecayTimer + 0.5f, maxDecayTime);
            }
        }

        /// <summary>
        /// Kích hoạt cơ chế Lướt Cận Kề (Graze / Near-Miss Mechanic - TDD v2.0.0 Phần III.2).
        /// Thưởng +500 điểm x Multiplier, hồi 30% (+1.05s) thanh thời gian Multiplier Decay.
        /// Cứ mỗi 10 Graze liên tiếp không mất máu -> Thưởng tăng ngay +1 bậc Multiplier!
        /// </summary>
        public void RegisterGraze(Vector3 worldPos, int basePoints = 500)
        {
            int earnedPoints = basePoints * _currentMultiplier;
            GameEventManager.IncrementScore(earnedPoints);
            _totalSessionGrazes++;
            _grazeStreak++;

            // Hồi 30% dung lượng thanh thời gian Multiplier Decay
            if (_currentMultiplier > 1)
            {
                _multiplierDecayTimer = Mathf.Min(_multiplierDecayTimer + (maxDecayTime * 0.30f), maxDecayTime);
            }

            // Thưởng tăng bậc Multiplier khi tích lũy mỗi 10 Graze chuẩn kỹ năng cao
            if (_grazeStreak > 0 && _grazeStreak % 10 == 0)
            {
                LevelUpMultiplier();
            }

            OnGrazeTriggered?.Invoke(worldPos, earnedPoints);
        }
    }
}
