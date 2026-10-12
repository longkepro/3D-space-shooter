using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    [Serializable]
    public struct LeaderboardEntry
    {
        public int rank;
        public string callsign;
        public int score;
        public int stage;
        public string difficulty;

        public LeaderboardEntry(int rank, string callsign, int score, int stage, string difficulty)
        {
            this.rank = rank;
            this.callsign = callsign;
            this.score = score;
            this.stage = stage;
            this.difficulty = difficulty;
        }
    }

    [Serializable]
    public class LeaderboardDataWrapper
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// Quản lý Bảng Vinh Danh Điểm Cao (Hall Of Fame / Leaderboard Manager).
    /// Bám sát giao diện bảng kỷ lục ảnh mẫu (Rank, Callsign, Score, Stage, Difficulty).
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        private const string PREF_KEY = "StarFox_HallOfFame_v1";
        private const int MAX_ENTRIES = 10;

        private List<LeaderboardEntry> _cachedEntries = new List<LeaderboardEntry>();

        public event Action OnLeaderboardUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadLeaderboard();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public IReadOnlyList<LeaderboardEntry> GetEntries()
        {
            if (_cachedEntries.Count == 0)
            {
                LoadLeaderboard();
            }
            return _cachedEntries;
        }

        public void LoadLeaderboard()
        {
            _cachedEntries.Clear();
            string json = PlayerPrefs.GetString(PREF_KEY, string.Empty);

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    LeaderboardDataWrapper wrapper = JsonUtility.FromJson<LeaderboardDataWrapper>(json);
                    if (wrapper != null && wrapper.entries != null && wrapper.entries.Count > 0)
                    {
                        _cachedEntries.AddRange(wrapper.entries);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Leaderboard] Lỗi đọc JSON: " + e.Message);
                }
            }

            if (_cachedEntries.Count == 0)
            {
                SeedDefaultEntries();
                SaveLeaderboard();
            }
            else
            {
                SortAndReRank();
            }
        }

        public void SaveLeaderboard()
        {
            SortAndReRank();
            LeaderboardDataWrapper wrapper = new LeaderboardDataWrapper { entries = _cachedEntries };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PREF_KEY, json);
            PlayerPrefs.Save();
            OnLeaderboardUpdated?.Invoke();
        }

        public bool IsHighScore(int score)
        {
            if (_cachedEntries.Count < MAX_ENTRIES) return true;
            return score > _cachedEntries[_cachedEntries.Count - 1].score;
        }

        public void AddEntry(string callsign, int score, int stage, string difficulty = "NORMAL")
        {
            if (string.IsNullOrEmpty(callsign)) callsign = "STAR FOX";
            _cachedEntries.Add(new LeaderboardEntry(0, callsign.ToUpper(), score, stage, difficulty.ToUpper()));
            SortAndReRank();
            SaveLeaderboard();
        }

        private void SortAndReRank()
        {
            _cachedEntries.Sort((a, b) => b.score.CompareTo(a.score));
            if (_cachedEntries.Count > MAX_ENTRIES)
            {
                _cachedEntries.RemoveRange(MAX_ENTRIES, _cachedEntries.Count - MAX_ENTRIES);
            }
            for (int i = 0; i < _cachedEntries.Count; i++)
            {
                LeaderboardEntry e = _cachedEntries[i];
                e.rank = i + 1;
                _cachedEntries[i] = e;
            }
        }

        private void SeedDefaultEntries()
        {
            // Seed danh sách phi công huyền thoại chuẩn Star Fox & Matt từ ảnh mẫu
            _cachedEntries.Add(new LeaderboardEntry(1, "MATT", 123939, 6, "EASY"));
            _cachedEntries.Add(new LeaderboardEntry(2, "FOX", 98500, 5, "HARD"));
            _cachedEntries.Add(new LeaderboardEntry(3, "FALCO", 84200, 4, "NORMAL"));
            _cachedEntries.Add(new LeaderboardEntry(4, "PEPPY", 72600, 3, "NORMAL"));
            _cachedEntries.Add(new LeaderboardEntry(5, "SLIPPY", 61400, 3, "EASY"));
            _cachedEntries.Add(new LeaderboardEntry(6, "WOLF", 59000, 2, "EXPERT"));
            _cachedEntries.Add(new LeaderboardEntry(7, "LEON", 48200, 2, "HARD"));
            _cachedEntries.Add(new LeaderboardEntry(8, "ROB 64", 39100, 1, "NORMAL"));
            _cachedEntries.Add(new LeaderboardEntry(9, "KATT", 28500, 1, "EASY"));
            _cachedEntries.Add(new LeaderboardEntry(10, "BILL", 18200, 1, "EASY"));
        }
    }
}
