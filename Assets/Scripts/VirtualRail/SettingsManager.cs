using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Quản lý toàn bộ cấu hình Cài đặt (Settings Manager) chuẩn Sci-Fi Arcade.
    /// Lưu trữ bền vững qua PlayerPrefs, tự động đồng bộ phần cứng & hệ thống âm thanh.
    /// Tuân thủ 0 GC Allocation trong Game Loop.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public const string PREF_RES_INDEX = "Settings_ResolutionIndex";
        public const string PREF_FULLSCREEN = "Settings_Fullscreen";
        public const string PREF_LUMINOSITY = "Settings_Luminosity";
        public const string PREF_VSYNC = "Settings_VSync";
        public const string PREF_TARGET_FPS = "Settings_TargetFPS";
        public const string PREF_SHOW_FPS = "Settings_ShowFPS";
        public const string PREF_QUALITY = "Settings_QualityLevel";
        public const string PREF_VOL_MASTER = "Settings_VolumeMaster";
        public const string PREF_VOL_MUSIC = "Settings_VolumeMusic";
        public const string PREF_VOL_SFX = "Settings_VolumeSFX";
        public const string PREF_PILOT_CALLSIGN = "Settings_PilotCallsign";
        public const string PREF_MOBILE_AIM_SENSITIVITY = "Settings_MobileAimSensitivity";

        [Header("Runtime State")]
        public int resolutionIndex;
        public bool isFullscreen = true;
        public float luminosity = 1.0f;
        public bool isVSync = true;
        public int targetFramerate = 60;
        public bool showFPS = false;
        public int qualityLevel = 2;

        public float volumeMaster = 1.0f;
        public float volumeMusic = 0.8f;
        public float volumeSFX = 0.9f;
        public string pilotCallsign = "FOX McCLOUD";
        public float mobileAimSensitivity = 1.0f;

        public event Action OnSettingsChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
                ApplyAllSettings();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void LoadSettings()
        {
            resolutionIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, GetCurrentResolutionIndex());
            isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            luminosity = PlayerPrefs.GetFloat(PREF_LUMINOSITY, 1.0f);
            isVSync = PlayerPrefs.GetInt(PREF_VSYNC, 1) == 1;
            targetFramerate = PlayerPrefs.GetInt(PREF_TARGET_FPS, 60);
            showFPS = PlayerPrefs.GetInt(PREF_SHOW_FPS, 0) == 1;
            qualityLevel = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());

            volumeMaster = PlayerPrefs.GetFloat(PREF_VOL_MASTER, 1.0f);
            volumeMusic = PlayerPrefs.GetFloat(PREF_VOL_MUSIC, 0.8f);
            volumeSFX = PlayerPrefs.GetFloat(PREF_VOL_SFX, 0.9f);
            pilotCallsign = PlayerPrefs.GetString(PREF_PILOT_CALLSIGN, "FOX McCLOUD");
            mobileAimSensitivity = PlayerPrefs.GetFloat(PREF_MOBILE_AIM_SENSITIVITY, 1.0f);
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetInt(PREF_RES_INDEX, resolutionIndex);
            PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
            PlayerPrefs.SetFloat(PREF_LUMINOSITY, luminosity);
            PlayerPrefs.SetInt(PREF_VSYNC, isVSync ? 1 : 0);
            PlayerPrefs.SetInt(PREF_TARGET_FPS, targetFramerate);
            PlayerPrefs.SetInt(PREF_SHOW_FPS, showFPS ? 1 : 0);
            PlayerPrefs.SetInt(PREF_QUALITY, qualityLevel);

            PlayerPrefs.SetFloat(PREF_VOL_MASTER, volumeMaster);
            PlayerPrefs.SetFloat(PREF_VOL_MUSIC, volumeMusic);
            PlayerPrefs.SetFloat(PREF_VOL_SFX, volumeSFX);
            PlayerPrefs.SetString(PREF_PILOT_CALLSIGN, string.IsNullOrEmpty(pilotCallsign) ? "FOX McCLOUD" : pilotCallsign);
            PlayerPrefs.SetFloat(PREF_MOBILE_AIM_SENSITIVITY, mobileAimSensitivity);
            PlayerPrefs.Save();

            ApplyAllSettings();
            OnSettingsChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            resolutionIndex = GetCurrentResolutionIndex();
            isFullscreen = true;
            luminosity = 1.0f;
            isVSync = true;
            targetFramerate = 60;
            showFPS = false;
            qualityLevel = Mathf.Min(3, QualitySettings.names.Length - 1);

            volumeMaster = 1.0f;
            volumeMusic = 0.8f;
            volumeSFX = 0.9f;
            pilotCallsign = "FOX McCLOUD";
            mobileAimSensitivity = 1.0f;

            SaveSettings();
        }

        public void ApplyAllSettings()
        {
            // 1. Resolution & Fullscreen (Chỉ áp dụng trên Desktop PC/Mac, tuyệt đối không ép độ phân giải trên Mobile để chống kéo dãn màn hình)
            if (!Application.isMobilePlatform)
            {
                Resolution[] resList = Screen.resolutions;
                if (resList != null && resList.Length > 0)
                {
                    int idx = Mathf.Clamp(resolutionIndex, 0, resList.Length - 1);
                    Resolution r = resList[idx];
                    Screen.SetResolution(r.width, r.height, isFullscreen);
                }
            }

            // 2. VSync & Framerate
            QualitySettings.vSyncCount = isVSync ? 1 : 0;
            Application.targetFrameRate = isVSync ? -1 : targetFramerate;

            // 3. Quality Level
            if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
            }

            // 4. Audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(volumeMusic * volumeMaster);
                AudioManager.Instance.SetSfxVolume(volumeSFX * volumeMaster);
            }

            // 5. Mobile Aim Sensitivity
            var anchor = FindAnyObjectByType<VirtualRailAnchor>();
            if (anchor != null && anchor.config != null)
            {
                anchor.config.mobileAimSensitivity = mobileAimSensitivity;
            }
        }

        public void SetMobileAimSensitivity(float val)
        {
            mobileAimSensitivity = Mathf.Clamp(val, 0.3f, 2.5f);
            PlayerPrefs.SetFloat(PREF_MOBILE_AIM_SENSITIVITY, mobileAimSensitivity);
            PlayerPrefs.Save();

            var anchor = FindAnyObjectByType<VirtualRailAnchor>();
            if (anchor != null && anchor.config != null)
            {
                anchor.config.mobileAimSensitivity = mobileAimSensitivity;
            }
            OnSettingsChanged?.Invoke();
        }

        public int GetCurrentResolutionIndex()
        {
            Resolution[] resList = Screen.resolutions;
            if (resList == null || resList.Length == 0) return 0;

            for (int i = 0; i < resList.Length; i++)
            {
                if (resList[i].width == Screen.currentResolution.width &&
                    resList[i].height == Screen.currentResolution.height)
                {
                    return i;
                }
            }
            return resList.Length - 1;
        }

        public List<string> GetResolutionOptions()
        {
            List<string> options = new List<string>();
            Resolution[] resList = Screen.resolutions;
            if (resList == null || resList.Length == 0)
            {
                options.Add(Screen.width + " x " + Screen.height);
                return options;
            }

            for (int i = 0; i < resList.Length; i++)
            {
                options.Add(resList[i].width + " x " + resList[i].height + " @" + resList[i].refreshRateRatio.value.ToString("F0") + "Hz");
            }
            return options;
        }
    }
}
