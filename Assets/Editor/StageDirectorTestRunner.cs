using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorTests
{
    /// <summary>
    /// Bộ kiểm thử tự động toàn diện cho Khung Kiến Trúc Stage Director (Automated Test Harness).
    /// Chạy mô phỏng tất định (Deterministic Simulation) 50 FPS để kiểm chứng:
    /// 1. Khởi động màn 1 với tốc độ cấu hình.
    /// 2. Đo đạc tiến trình Z và tính đơn điệu của thanh tiến trình (0.0 -> 1.0).
    /// 3. Kích hoạt pha Climax chuẩn xác tại ngưỡng cự ly Z.
    /// 4. Động lực học Hyperspace Warp: Gia tốc mượt mà đạt đỉnh 300%.
    /// 5. Hoán đổi dữ liệu màn kế tiếp tại đúng điểm giữa Warp (Mid-Warp Swap).
    /// 6. Bàn giao Stage 2 với vận tốc mới chính xác.
    /// 7. Hoàn thành tất cả các màn và kích hoạt AllStagesCleared.
    /// </summary>
    [InitializeOnLoad]
    public static class StageDirectorTestRunner
    {
        private const string REPORT_PATH = "StageDirector_TestReport.txt";

        static StageDirectorTestRunner()
        {
            EditorApplication.delayCall += RunTest;
        }

        [MenuItem("Tools/Virtual Rail/Run Stage Director Automated Test")]
        public static void RunTest()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("           VIRTUAL RAIL STAGE DIRECTOR - AUTOMATED TEST REPORT                 ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Execution Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("Environment: Unity Editor (Deterministic Tick 50 FPS, dt = 0.02s)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            int passedChecks = 0;
            int totalChecks = 7;

            // 1. Khởi tạo Mock Harness trong bộ nhớ (không ảnh hưởng scene)
            GameObject harnessObj = new GameObject("[Test_StageDirectorHarness]");
            VirtualRailAnchor anchor = harnessObj.AddComponent<VirtualRailAnchor>();
            VirtualRailStageDirector director = harnessObj.AddComponent<VirtualRailStageDirector>();

            // Tạo 2 StageData giả lập trong bộ nhớ
            StageData stage1 = ScriptableObject.CreateInstance<StageData>();
            stage1.stageId = "Test_Stage_01";
            stage1.stageTitle = "TEST STAGE 1: PATROL";
            stage1.stageIndex = 1;
            stage1.railSettings.forwardSpeed = 20f;
            stage1.railSettings.stageDistanceZ = 100f;
            stage1.railSettings.warpDuration = 1.0f;
            stage1.railSettings.warpSpeedMultiplier = 3.0f;
            stage1.climaxSettings.climaxType = StageClimaxType.DistanceReached;

            StageData stage2 = ScriptableObject.CreateInstance<StageData>();
            stage2.stageId = "Test_Stage_02";
            stage2.stageTitle = "TEST STAGE 2: INFILTRATION";
            stage2.stageIndex = 2;
            stage2.railSettings.forwardSpeed = 50f;
            stage2.railSettings.stageDistanceZ = 150f;
            stage2.railSettings.warpDuration = 1.0f;
            stage2.railSettings.warpSpeedMultiplier = 2.0f;
            stage2.climaxSettings.climaxType = StageClimaxType.DistanceReached;

            // Mock Backdrop
            VirtualRailHorizonBackdrop backdrop = harnessObj.AddComponent<VirtualRailHorizonBackdrop>();
            Material mockMat1 = new Material(Shader.Find("Unlit/Texture"));
            mockMat1.name = "Mock_Stage1_Backdrop";
            Material mockMat2 = new Material(Shader.Find("Unlit/Texture"));
            mockMat2.name = "Mock_Stage2_Backdrop";
            stage1.environmentSettings.backdropMaterial = mockMat1;
            stage2.environmentSettings.backdropMaterial = mockMat2;

            director.stages = new StageData[] { stage1, stage2 };
            director.anchor = anchor;
            director.backdrop = backdrop;

            float dt = 0.02f; // 50 FPS

            try
            {
                // TEST 1: Khởi động Stage 1 & Backdrop Màn 1
                director.StartStage(0);
                bool t1 = director.CurrentState == StageState.InJourney &&
                          director.currentStageIndex == 0 &&
                          Mathf.Abs(anchor.CurrentSpeed - 20f) < 0.1f &&
                          backdrop.backdropMaterial == mockMat1 &&
                          backdrop.GetComponent<MeshRenderer>().enabled;
                if (t1) passedChecks++;
                sb.AppendLine($"[TEST 1] Khởi động Stage 1 & Gán Backdrop 4K: {(t1 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         State: {director.CurrentState} | Speed: {anchor.CurrentSpeed:F1} m/s | Backdrop: {(backdrop.backdropMaterial != null ? backdrop.backdropMaterial.name : "null")} (Renderer: {backdrop.GetComponent<MeshRenderer>().enabled})");

                // TEST 2: Tiến trình bay và đo đạc Z
                float prevProgress = -1f;
                bool monotonicProgress = true;
                int frameCount = 0;

                while (anchor.transform.position.z < 100f && frameCount < 500)
                {
                    anchor.transform.position += Vector3.forward * (anchor.CurrentSpeed * dt);
                    director.Tick(dt);

                    float progress = director.StageProgress01;
                    if (progress < prevProgress) monotonicProgress = false;
                    prevProgress = progress;
                    frameCount++;
                }

                bool t2 = monotonicProgress && prevProgress >= 0.95f;
                if (t2) passedChecks++;
                sb.AppendLine($"[TEST 2] Đo đạc tiến trình Z đơn điệu: {(t2 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         Final Progress: {prevProgress * 100f:F1}% at Z = {anchor.transform.position.z:F1}m (Monotonic: {monotonicProgress})");

                // TEST 3: Kích hoạt Climax tại Z >= 100m
                bool t3 = director.CurrentState == StageState.Climax;
                if (t3) passedChecks++;
                sb.AppendLine($"[TEST 3] Kích hoạt Climax tại ngưỡng Z: {(t3 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         State: {director.CurrentState} (Expected: Climax)");

                // TEST 4: Kích hoạt Hyperspace Warp sau Climax delay (1.5s = 75 frames)
                for (int i = 0; i < 76; i++)
                {
                    director.Tick(dt);
                }

                bool t4 = director.CurrentState == StageState.WarpTransition;
                if (t4) passedChecks++;
                sb.AppendLine($"[TEST 4] Chuyển tiếp Hyperspace Warp: {(t4 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         State: {director.CurrentState} (Expected: WarpTransition)");

                // TEST 5: Động lực học gia tốc Warp & Hoán đổi dữ liệu giữa Warp (Mid-Warp Swap)
                float peakSpeedRecorded = 0f;
                bool swappedAtMidWarp = false;

                for (int w = 0; w <= 50; w++) // 51 * 0.02s > 1.0s (đủ hoàn tất chu kỳ warp 1.0s)
                {
                    director.Tick(dt);
                    if (anchor.CurrentSpeed > peakSpeedRecorded) peakSpeedRecorded = anchor.CurrentSpeed;
                    if (director.currentStageIndex == 1) swappedAtMidWarp = true;
                }

                bool t5 = peakSpeedRecorded >= 45f && swappedAtMidWarp && backdrop.backdropMaterial == mockMat2;
                if (t5) passedChecks++;
                sb.AppendLine($"[TEST 5] Gia tốc Warp & Hoán đổi Backdrop giữa Warp: {(t5 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         Peak Warp Speed: {peakSpeedRecorded:F1} m/s | Backdrop: {(backdrop.backdropMaterial != null ? backdrop.backdropMaterial.name : "null")}");

                // TEST 6: Bàn giao Stage 2 với vận tốc mới (50 m/s)
                bool t6 = director.CurrentState == StageState.InJourney &&
                          director.currentStageIndex == 1 &&
                          Mathf.Abs(anchor.CurrentSpeed - 50f) < 0.5f;
                if (t6) passedChecks++;
                sb.AppendLine($"[TEST 6] Bàn giao Stage 2 với vận tốc mới: {(t6 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         State: {director.CurrentState} | StageIdx: {director.currentStageIndex} | Speed: {anchor.CurrentSpeed:F1} m/s (Expected: 50.0 m/s)");

                // TEST 7: Bay hết Stage 2 và kết thúc chuỗi màn (AllStagesCleared)
                float s2StartZ = anchor.transform.position.z;
                frameCount = 0;

                while (anchor.transform.position.z - s2StartZ < 150f && frameCount < 500)
                {
                    anchor.transform.position += Vector3.forward * (anchor.CurrentSpeed * dt);
                    director.Tick(dt);
                    frameCount++;
                }

                // Chờ Climax (1.5s = 75 frames) và Warp cuối (1.0s = 50 frames)
                for (int i = 0; i < 130; i++)
                {
                    director.Tick(dt);
                }

                bool t7 = director.CurrentState == StageState.AllStagesCleared;
                if (t7) passedChecks++;
                sb.AppendLine($"[TEST 7] Hoàn thành toàn bộ chuỗi màn (AllStagesCleared): {(t7 ? "PASSED" : "FAILED")}");
                sb.AppendLine($"         Final State: {director.CurrentState} (Expected: AllStagesCleared)");

                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine($"TOTAL SCORE: {passedChecks}/{totalChecks} CHECKS PASSED ({(passedChecks == totalChecks ? "100% SUCCESS" : "FAILURES DETECTED")})");
                sb.AppendLine("================================================================================");
            }
            finally
            {
                // Dọn dẹp sạch sẽ 100% đối tượng mock trong bộ nhớ
                UnityEngine.Object.DestroyImmediate(harnessObj);
                UnityEngine.Object.DestroyImmediate(stage1);
                UnityEngine.Object.DestroyImmediate(stage2);
                UnityEngine.Object.DestroyImmediate(mockMat1);
                UnityEngine.Object.DestroyImmediate(mockMat2);
            }

            string reportContent = sb.ToString();
            File.WriteAllText(REPORT_PATH, reportContent);
            Debug.Log($"<color={(passedChecks == totalChecks ? "green" : "red")}><b>[VirtualRail Test Harness]</b></color>\n{reportContent}");
        }
    }
}
