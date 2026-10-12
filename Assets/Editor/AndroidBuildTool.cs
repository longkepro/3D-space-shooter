using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ 1-Click tự động xuất file cài đặt Android APK (Automated APK Exporter).
    /// Tự động thiết lập cấu hình chuẩn (Package Name, ARMv7 + ARM64, Min SDK).
    /// </summary>
    [InitializeOnLoad]
    public static class AndroidBuildTool
    {
        private const string FLAG_FILE = "Temp/trigger_android_build.flag";
        private const string STATUS_FILE = "Temp/android_build_status.txt";

        static AndroidBuildTool()
        {
            EditorApplication.update += CheckAutoBuildTrigger;
        }

        private static void CheckAutoBuildTrigger()
        {
            if (File.Exists(FLAG_FILE))
            {
                try
                {
                    File.Delete(FLAG_FILE);
                }
                catch { }

                EditorApplication.update -= CheckAutoBuildTrigger;
                BuildAndroidAPK();
            }
        }

        [MenuItem("Tools/Virtual Rail/Android/1. Build Android APK (1-Click)")]
        public static void BuildAndroidAPK()
        {
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            string apkPath = Path.Combine(outputDir, "StarFox3D.apk");

            File.WriteAllText(STATUS_FILE, $"BUILDING: Started at {DateTime.Now:HH:mm:ss}");
            Debug.Log($"<color=cyan><b>[Android Build]</b> Bắt đầu quá trình xuất file APK tới: {apkPath}...</color>");

            try
            {
                // 1. Cấu hình PlayerSettings cho Android
                PlayerSettings.companyName = "Longke";
                PlayerSettings.productName = "StarFox3D";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.longke.starfox3d");

                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

                // Tắt App Bundle để xuất file .apk cài đặt trực tiếp
                EditorUserBuildSettings.buildAppBundle = false;

                // 2. Chuyển Active Build Target nếu cần
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    Debug.Log("<color=yellow><b>[Android Build]</b> Đang chuyển Platform sang Android...</color>");
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }

                // 3. Danh sách Scene đưa vào Build
                string[] scenes = new string[] { "Assets/Scenes/Game.unity" };

                // 4. Cấu hình BuildPlayerOptions
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                // 5. Tiến hành Build
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                    string successMsg = $"SUCCESS: {apkPath} ({sizeMb} MB) at {DateTime.Now:HH:mm:ss}";
                    File.WriteAllText(STATUS_FILE, successMsg);
                    Debug.Log($"<color=green><b>[Android Build THÀNH CÔNG]</b></color> File APK đã hoàn thành: <b>{apkPath}</b> ({sizeMb} MB)");
                    EditorUtility.RevealInFinder(apkPath);
                }
                else
                {
                    string failMsg = $"FAILED: {summary.result}, Errors: {summary.totalErrors} at {DateTime.Now:HH:mm:ss}";
                    File.WriteAllText(STATUS_FILE, failMsg);
                    Debug.LogError($"<color=red><b>[Android Build THẤT BẠI]</b></color> Kết quả: {summary.result}, Tổng lỗi: {summary.totalErrors}");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"EXCEPTION: {ex.Message} at {DateTime.Now:HH:mm:ss}";
                File.WriteAllText(STATUS_FILE, errorMsg);
                Debug.LogError($"[Android Build Exception] {ex}");
            }
        }

        [MenuItem("Tools/Virtual Rail/Android/2. Open Builds Folder")]
        public static void OpenBuildsFolder()
        {
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            EditorUtility.RevealInFinder(outputDir);
        }
    }
}
