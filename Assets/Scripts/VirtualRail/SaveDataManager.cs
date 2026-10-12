using System;
using System.IO;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Các bậc xếp hạng phi công (Pilot Rank Tiers) theo chuẩn Star Fox 64 & Endless Scoring Specification.
    /// </summary>
    public enum PilotRankTier
    {
        RankD = 0, // Flight Cadet (< 15k pts & < 1.5km)
        RankC = 1, // Survivor Pilot (>= 15k pts or >= 1.5km)
        RankB = 2, // Veteran Pilot (>= 30k pts or >= 3km)
        RankA = 3, // Elite Wing (>= 60k pts or >= 6km)
        RankS = 4  // Cosmic Ace (>= 100k pts or >= 10km)
    }

    /// <summary>
    /// Cấu trúc dữ liệu lưu trữ bền vững chế độ Endless Survival (Data Schema v1.0).
    /// </summary>
    [Serializable]
    public class EndlessSaveData
    {
        [Header("Pilot Profile")]
        public string pilotCallsign = "FOX McCLOUD";
        public string lastPlayedTimestampUtc = string.Empty;

        [Header("Best Records (Single Run)")]
        public int highScore = 0;
        public float maxDistanceMeters = 0f;
        public int maxThreatLevel = 1;
        public int maxHitsInSingleRun = 0;
        public int maxGrazesInSingleRun = 0;
        public int peakMultiplierAchieved = 1;

        [Header("Cumulative Lifetime Statistics")]
        public int totalRunsPlayed = 0;
        public int totalEnemiesDowned = 0;
        public int totalGrazesPerformed = 0;
        public float totalDistanceTraveled = 0f;
        public int bossIncursionsDefeatedTotal = 0;

        [Header("Endless Ace Medals Collection")]
        public int goldMedalCount = 0;      // Rank S (Cosmic Ace)
        public int silverMedalCount = 0;    // Rank A (Elite Wing)
        public int bronzeMedalCount = 0;    // Rank B (Veteran Pilot)
        public int survivorRibbonCount = 0; // Rank C (Survivor Pilot)

        [Header("Honor Badges & Milestones")]
        public bool hasReachedHyperspaceWarp = false;   // Đạt cự ly >= 15,000m
        public bool hasDefeatedBossIncursion = false;   // Đã bắn hạ Boss F3 Corvette
        public bool hasAchievedMaxMultiplier = false;   // Đã chạm mốc 32x Cosmic Multiplier
        public bool hasAchievedRankS = false;           // Đã đạt Rank S ít nhất 1 lần
    }

    /// <summary>
    /// Hệ thống Quản Lý Lưu Trữ Dữ Liệu Bền Vững (SaveDataManager) theo Task 3.5.
    /// Quản lý độc lập toàn bộ kỷ lục cá nhân, viễn trắc tích lũy, bộ sưu tập huân chương Ace Medals và hồ sơ phi công.
    /// Sử dụng JSON tuần tự hóa nguyên tử (Atomic File Replacement) kèm sao lưu dự phòng PlayerPrefs.
    /// Tuyệt đối 0 GC Allocation trong Game Loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveDataManager : MonoBehaviour
    {
        private static SaveDataManager _instance;
        public static SaveDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SaveDataManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[SaveDataManager]");
                        _instance = go.AddComponent<SaveDataManager>();
                    }
                }
                return _instance;
            }
        }

        private const string SAVE_FILE_NAME = "starfox_endless_save.json";
        private const string PREF_BACKUP_KEY = "StarFox_Endless_Save_Backup_v1";

        [SerializeField] private EndlessSaveData _data = new EndlessSaveData();
        public EndlessSaveData Data => _data;

        public static event Action<EndlessSaveData> OnSaveDataLoaded;
        public static event Action<EndlessSaveData> OnSaveDataSaved;
        public static event Action<string, int> OnRecordBroken; // (recordType, value)

        private string _filePath;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAndLoad();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAndLoad()
        {
            if (_isInitialized) return;
            _filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            LoadData();
            _isInitialized = true;
        }

        /// <summary>
        /// Tải toàn bộ dữ liệu từ tệp tin JSON hoặc PlayerPrefs fallback.
        /// </summary>
        public void LoadData()
        {
            _data = null;

            // 1. Nạp từ tệp tin JSON cục bộ
            if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        _data = JsonUtility.FromJson<EndlessSaveData>(json);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"<color=yellow>[SaveDataManager]</color> Không thể đọc tệp lưu cục bộ: {e.Message}. Sẽ thử tải từ PlayerPrefs.");
                }
            }

            // 2. Dự phòng nạp từ PlayerPrefs nếu tệp chưa có hoặc lỗi
            if (_data == null && PlayerPrefs.HasKey(PREF_BACKUP_KEY))
            {
                try
                {
                    string backupJson = PlayerPrefs.GetString(PREF_BACKUP_KEY, string.Empty);
                    if (!string.IsNullOrEmpty(backupJson))
                    {
                        _data = JsonUtility.FromJson<EndlessSaveData>(backupJson);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"<color=yellow>[SaveDataManager]</color> Không thể đọc PlayerPrefs backup: {e.Message}");
                }
            }

            // 3. Khởi tạo dữ liệu mặc định ban đầu nếu chưa từng có
            if (_data == null)
            {
                _data = new EndlessSaveData();
                SaveData();
            }

            OnSaveDataLoaded?.Invoke(_data);
            Debug.Log($"<color=cyan><b>[SaveDataManager]</b></color> Đã nạp dữ liệu phi công: <b>{_data.pilotCallsign}</b> | Kỷ lục điểm: <b>{_data.highScore:N0}</b> | Quãng đường: <b>{_data.maxDistanceMeters:N0}m</b> | Huân chương Rank S: <b>{_data.goldMedalCount}</b>");
        }

        /// <summary>
        /// Lưu trữ an toàn nguyên tử (Atomic Write) dữ liệu vào tệp tin và PlayerPrefs.
        /// </summary>
        public void SaveData()
        {
            if (_data == null) return;

            // Đồng bộ Callsign từ SettingsManager nếu có
            if (SettingsManager.Instance != null && !string.IsNullOrEmpty(SettingsManager.Instance.pilotCallsign))
            {
                _data.pilotCallsign = SettingsManager.Instance.pilotCallsign;
            }

            _data.lastPlayedTimestampUtc = DateTime.UtcNow.ToString("o");

            try
            {
                string json = JsonUtility.ToJson(_data, true);

                if (string.IsNullOrEmpty(_filePath))
                {
                    _filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
                }

                // Ghi nguyên tử qua tệp tạm tránh hỏng hóc khi mất điện đột ngột
                string tempPath = _filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
                File.Move(tempPath, _filePath);

                // Ghi bản sao lưu dự phòng vào PlayerPrefs
                PlayerPrefs.SetString(PREF_BACKUP_KEY, json);
                PlayerPrefs.Save();

                OnSaveDataSaved?.Invoke(_data);
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[SaveDataManager]</color> Lỗi ghi tệp lưu: {e.Message}");
            }
        }

        /// <summary>
        /// Ghi nhận kết quả của một lượt chạy vừa kết thúc, cập nhật thống kê trọn đời và kiểm tra phá kỷ lục.
        /// </summary>
        public bool RecordRunResult(int score, float distance, int threat, int hits, int grazes, int peakMultiplier, PilotRankTier rank)
        {
            if (_data == null) _data = new EndlessSaveData();

            bool isNewRecord = false;

            // 1. Cập nhật thống kê tích lũy trọn đời (Cumulative Lifetime Stats)
            _data.totalRunsPlayed++;
            _data.totalEnemiesDowned += hits;
            _data.totalGrazesPerformed += grazes;
            _data.totalDistanceTraveled += distance;

            // 2. Trao tặng Huân chương Ace Medals theo phân bậc Rank
            switch (rank)
            {
                case PilotRankTier.RankS:
                    _data.goldMedalCount++;
                    _data.hasAchievedRankS = true;
                    break;
                case PilotRankTier.RankA:
                    _data.silverMedalCount++;
                    break;
                case PilotRankTier.RankB:
                    _data.bronzeMedalCount++;
                    break;
                case PilotRankTier.RankC:
                    _data.survivorRibbonCount++;
                    break;
            }

            // 3. Cập nhật các cột mốc danh dự (Honor Milestones)
            if (distance >= 15000f)
            {
                _data.hasReachedHyperspaceWarp = true;
            }
            if (peakMultiplier >= 32)
            {
                _data.hasAchievedMaxMultiplier = true;
            }

            // 4. Kiểm tra và ghi nhận các kỷ lục mới trong 1 lượt chạy (Single-Run Bests)
            if (score > _data.highScore)
            {
                _data.highScore = score;
                isNewRecord = true;
                OnRecordBroken?.Invoke("HIGH SCORE", score);
                Debug.Log($"<color=yellow><b>[NEW RECORD]</b></color> Phá kỷ lục điểm cao mới: <b>{score:N0} PTS!</b>");
            }

            if (distance > _data.maxDistanceMeters)
            {
                _data.maxDistanceMeters = distance;
                OnRecordBroken?.Invoke("DISTANCE", Mathf.RoundToInt(distance));
            }

            if (threat > _data.maxThreatLevel)
            {
                _data.maxThreatLevel = threat;
            }

            if (hits > _data.maxHitsInSingleRun)
            {
                _data.maxHitsInSingleRun = hits;
            }

            if (grazes > _data.maxGrazesInSingleRun)
            {
                _data.maxGrazesInSingleRun = grazes;
            }

            if (peakMultiplier > _data.peakMultiplierAchieved)
            {
                _data.peakMultiplierAchieved = peakMultiplier;
            }

            // 5. Lưu ngay dữ liệu xuống đĩa
            SaveData();

            return isNewRecord;
        }

        /// <summary>
        /// Ghi nhận sự kiện tiêu diệt Boss F3 Corvette trong chế độ Endless.
        /// </summary>
        public void RecordBossDefeated()
        {
            if (_data == null) _data = new EndlessSaveData();
            _data.bossIncursionsDefeatedTotal++;
            _data.hasDefeatedBossIncursion = true;
            SaveData();
            Debug.Log($"<color=lime><b>[SaveDataManager]</b></color> Đã ghi nhận diệt Boss Incursion! Tổng Boss hạ gục: {_data.bossIncursionsDefeatedTotal}.");
        }

        /// <summary>
        /// Kiểm tra xem điểm số có vượt qua kỷ lục hiện tại hay không.
        /// </summary>
        public bool IsHighScore(int score)
        {
            return _data != null && score > _data.highScore;
        }

        /// <summary>
        /// Xóa sạch toàn bộ dữ liệu lưu và khôi phục về trạng thái khởi nguyên.
        /// </summary>
        public void ResetSaveData()
        {
            _data = new EndlessSaveData();
            SaveData();
            Debug.Log("<color=yellow><b>[SaveDataManager]</b></color> ĐÃ XÓA SẠCH VÀ ĐẶT LẠI TOÀN BỘ DỮ LIỆU LƯU VỀ MẶC ĐỊNH!");
        }

        public string GetSaveFilePath()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            }
            return _filePath;
        }
    }
}
