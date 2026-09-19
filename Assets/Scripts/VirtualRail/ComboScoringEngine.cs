using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Động cơ tính điểm Combo (Hit Count Engine) theo chuẩn TDD v1.0.0 & v2.0.0.
    /// Quản lý chuỗi tiêu diệt bằng Charge Shot, hệ số nhân Multiplier và cơ chế Graze.
    /// Tuân thủ nghiêm ngặt 0 GC Allocation trong vòng lặp game loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class ComboScoringEngine : MonoBehaviour
    {
        public static ComboScoringEngine Instance { get; private set; }

        public static event Action<int, int> OnComboScored; // (killCount, totalHits)
        public static event Action<int> OnMultiplierChanged;
        public static event Action<Vector3, int> OnGrazeTriggered;

        [Header("Combo Scoring Settings")]
        [SerializeField] private int pointsPerHit = 50;

        [Header("State Tracking (0 GC Alloc)")]
        private int _totalSessionHits = 0;
        private int _currentMultiplier = 1;
        private float _multiplierDecayTimer = 0f;

        public int TotalSessionHits => _totalSessionHits;
        public int CurrentMultiplier => _currentMultiplier;
        public float MultiplierDecayTimer => _multiplierDecayTimer;

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

        public void ResetSession()
        {
            _totalSessionHits = 0;
            _currentMultiplier = 1;
            _multiplierDecayTimer = 0f;
            OnMultiplierChanged?.Invoke(_currentMultiplier);
        }

        /// <summary>
        /// Ghi nhận diệt theo cụm (Splash Combo) từ phát bắn tụ lực (Charge Shot).
        /// Công thức chuẩn TDD: N + (N - 1)
        /// </summary>
        public int RegisterSplashKill(int count)
        {
            if (count <= 0) return 0;

            int totalHits = (count == 1) ? 1 : (count + (count - 1));
            _totalSessionHits += totalHits;

            int bonusScore = totalHits * pointsPerHit * _currentMultiplier;
            GameEventManager.IncrementScore(bonusScore);

            OnComboScored?.Invoke(count, totalHits);

            Debug.Log($"<color=orange><b>[COMBO SPLASH]</b></color> Diệt {count} tàu địch -> Thưởng <b>+{totalHits} HITS!</b> (+{bonusScore} pts, Multiplier: {_currentMultiplier}x)");

            return totalHits;
        }

        /// <summary>
        /// Kích hoạt cơ chế Lướt Cận Kề (Graze / Near-Miss Mechanic).
        /// Sẵn sàng kết nối với Outer Graze Box (R = 1.8m).
        /// </summary>
        public void RegisterGraze(Vector3 worldPos, int basePoints = 500)
        {
            int earnedPoints = basePoints * _currentMultiplier;
            GameEventManager.IncrementScore(earnedPoints);
            OnGrazeTriggered?.Invoke(worldPos, earnedPoints);
        }
    }
}