using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Ð?ng co tính di?m Combo (Hit Count Engine) theo chu?n TDD v1.0.0 Ph?n I.6.
    /// Công th?c thu?ng c?p s? c?ng khi tiêu di?t c?m k? d?ch b?ng Charge Shot:
    /// TotalHits = N_killed + (N_killed - 1)
    /// </summary>
    public class ComboScoringEngine : MonoBehaviour
    {
        public static ComboScoringEngine Instance { get; private set; }

        public static event Action<int, int> OnComboScored; // (killCount, totalHits)

        [Header("Combo Scoring Settings")]
        [SerializeField] private int pointsPerHit = 50;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Ghi nh?n dòn tiêu di?t theo c?m (Splash Combo) t? phát b?n t? l?c.
        /// </summary>
        public int RegisterSplashKill(int count)
        {
            if (count <= 0) return 0;

            int totalHits;
            if (count == 1)
            {
                totalHits = 1;
            }
            else
            {
                // Công th?c TDD: N + (N - 1)
                totalHits = count + (count - 1);
            }

            int bonusScore = totalHits * pointsPerHit;
            GameEventManager.IncrementScore(bonusScore);

            OnComboScored?.Invoke(count, totalHits);

            Debug.Log($"<color=orange><b>[COMBO SPLASH]</b></color> Di?t {count} tàu d?ch -> Thu?ng <b>+{totalHits} HITS!</b> (+{bonusScore} pts)");

            return totalHits;
        }
    }
}
