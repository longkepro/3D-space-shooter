using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VirtualRail;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ 1-Click chuyển đổi qua lại giữa Chế độ Chiến dịch Gốc (4.5 phút) và Chế độ Sinh tồn Vô tận (Endless Mode)
    /// cho Stage 1 (Tuân thủ Quy tắc An toàn 3 Tầng Bảo lưu - AGENTS.md Điều 3).
    /// </summary>
    public static class Stage1EndlessToggle
    {
        private const string STAGE_01_PATH = "Assets/Scripts/VirtualRail/StageData_01.asset";

        [MenuItem("Tools/Virtual Rail/Stage 1 Endless Mode/[1] Bật Endless Mode cho Stage 1 (ON)", false, 200)]
        public static void EnableStage1EndlessMode()
        {
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(STAGE_01_PATH);
            if (stage == null)
            {
                Debug.LogError($"[EndlessToggle] Không tìm thấy asset tại: {STAGE_01_PATH}");
                return;
            }

            Undo.RecordObject(stage, "Enable Endless Mode Stage 1");

            // 1. Kích hoạt cờ Endless Survival trên StageData
            stage.progressionSettings.isEndlessSurvival = true;
            stage.progressionSettings.completionMode = StageCompletionMode.EndlessSurvival;

            // 2. Tự động thu thập toàn bộ 46 Mẫu Đội hình (20 Hình học + 26 Chữ cái Alphabet)
            List<FormationPattern> allFormations = LoadAllFormations();
            if (allFormations.Count > 0)
            {
                stage.enemySettings.stageFormationPool = allFormations.ToArray();
            }

            stage.enemySettings.enableFormations = true;
            stage.enemySettings.formationIntervalWaves = 1;
            stage.enemySettings.spawnLeadDistance = 230f; // Cự ly chân trời chuẩn dải 220m - 240m

            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();

            // 3. Đảm bảo Scene hiện tại đã gắn Virtual Rail Rig và Stage Director (Zero Setup)
            if (Object.FindAnyObjectByType<VirtualRailStageDirector>() == null)
            {
                VirtualRailModeToggle.SwitchToVirtualRail();
            }

            Debug.Log($"<color=lime><b>[STAGE 1 ENDLESS MODE - ON]</b></color> Đã kích hoạt Endless Survival Mode cho Stage 1! Đã nạp {allFormations.Count} mẫu đội hình (20 Geometric + 26 Alphabet). Horizon Lead: 220m-240m, DDA: Sẵn sàng!");
        }

        [MenuItem("Tools/Virtual Rail/Stage 1 Endless Mode/[2] Khôi phục Kịch bản Gốc Stage 1 (OFF - Undo)", false, 201)]
        public static void DisableStage1EndlessMode()
        {
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(STAGE_01_PATH);
            if (stage == null)
            {
                Debug.LogError($"[EndlessToggle] Không tìm thấy asset tại: {STAGE_01_PATH}");
                return;
            }

            Undo.RecordObject(stage, "Disable Endless Mode Stage 1");

            // 1. Phục hồi về Chế độ Chiến dịch 4.5 phút nguyên bản
            stage.progressionSettings.isEndlessSurvival = false;
            stage.progressionSettings.completionMode = StageCompletionMode.TimeDuration;
            stage.progressionSettings.durationMinutes = 4.5f;
            stage.progressionSettings.targetDistanceZ = 34680f;

            stage.enemySettings.enableFormations = true;
            stage.enemySettings.formationIntervalWaves = 3;
            stage.enemySettings.spawnLeadDistance = 220f;

            // 2. Khôi phục danh sách đội hình gốc (7 mẫu cơ bản)
            List<FormationPattern> originalPool = LoadOriginalFormations();
            if (originalPool.Count > 0)
            {
                stage.enemySettings.stageFormationPool = originalPool.ToArray();
            }

            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();

            // 3. Tắt EndlessDirector nếu đang chạy trong runtime hoặc Scene
            var endlessDirector = Object.FindAnyObjectByType<VirtualRailEndlessDirector>();
            if (endlessDirector != null)
            {
                endlessDirector.StopEndlessSession();
                endlessDirector.enabled = false;
            }

            Debug.Log("<color=yellow><b>[STAGE 1 ENDLESS MODE - OFF / UNDO]</b></color> Đã hoàn nguyên Stage 1 về Kịch bản Chiến dịch 4.5 phút nguyên bản 100%!");
        }

        private static List<FormationPattern> LoadAllFormations()
        {
            var list = new List<FormationPattern>();
            var set = new HashSet<string>();

            // 1. Thư mục Formations_Geometric (20 mẫu)
            AddFormationsFromFolder("Assets/Scripts/VirtualRail/Formations_Geometric", list, set);

            // 2. Thư mục Formations_Alphabet (26 mẫu)
            AddFormationsFromFolder("Assets/Scripts/VirtualRail/Formations_Alphabet", list, set);

            // 3. Mẫu bổ sung ở thư mục gốc VirtualRail nếu có
            AddFormationsFromFolder("Assets/Scripts/VirtualRail", list, set);

            return list;
        }

        private static void AddFormationsFromFolder(string folderPath, List<FormationPattern> list, HashSet<string> set)
        {
            string[] guids = AssetDatabase.FindAssets("t:FormationPattern", new[] { folderPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (set.Contains(path)) continue;
                set.Add(path);

                var p = AssetDatabase.LoadAssetAtPath<FormationPattern>(path);
                if (p != null)
                {
                    list.Add(p);
                }
            }
        }

        private static List<FormationPattern> LoadOriginalFormations()
        {
            string[] originalNames = new[]
            {
                "Formation_AccretionJet",
                "Formation_Diamond4",
                "Formation_DysonSwarm",
                "Formation_EchelonLine",
                "Formation_EinsteinLens",
                "Formation_LagrangeTriad",
                "Formation_PulsarDipole"
            };

            var list = new List<FormationPattern>();
            for (int i = 0; i < originalNames.Length; i++)
            {
                string path = $"Assets/Scripts/VirtualRail/{originalNames[i]}.asset";
                var p = AssetDatabase.LoadAssetAtPath<FormationPattern>(path);
                if (p != null) list.Add(p);
            }
            return list;
        }
    }
}
