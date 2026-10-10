using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ Kiểm thử & Quản lý Bộ Âm thanh Phòng thu Studio & Âm thanh Thủ tục.
    /// Tuân thủ nghiêm ngặt Điều 3 Bộ Quy Tắc Dự Án (3-Layer Safety):
    /// - Cặp MenuItem 1-click đối xứng:
    ///   [55] Bật bộ âm thanh phòng thu Studio (ON)
    ///   [56] Hoàn nguyên về âm thanh thủ tục Procedural (OFF - Legacy)
    /// - Cho phép nghe thử so sánh A/B trực tiếp giữa chất âm Studio và Procedural.
    /// </summary>
    public class VirtualRailAudioTester : EditorWindow
    {
        private Vector2 _scrollPos;
        private static AudioManager _tempManager;
        private static GameObject _tempAudioHost;

        private const string STUDIO_DIR = "Assets/Audio/SFX_Studio";

        [MenuItem("Tools/Virtual Rail/53. Audio: Open Audio Tester & Previewer (Nghe Thử & So Sánh)")]
        public static void OpenWindow()
        {
            var window = GetWindow<VirtualRailAudioTester>("VR Audio Tester");
            window.minSize = new Vector2(520, 680);
            window.Show();
        }

        [MenuItem("Tools/Virtual Rail/54. Audio: Export Procedural SFX to WAV Files")]
        public static void ExportAllSfxToWav()
        {
            EnsureManagerInstance();
            string outputDir = Path.Combine(Application.dataPath, "Audio", "SFX_Procedural");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            int count = 0;
            foreach (AudioSFXType type in Enum.GetValues(typeof(AudioSFXType)))
            {
                AudioClip clip = _tempManager.GetClipForType(type);
                if (clip != null)
                {
                    string filePath = Path.Combine(outputDir, $"SFX_{type}.wav");
                    SaveAudioClipToWav(filePath, clip);
                    count++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"<color=green><b>[VirtualRail Audio] ĐÃ XUẤT THÀNH CÔNG {count} FILE ÂM THANH (.WAV):</b></color> Thư mục: Assets/Audio/SFX/");
            EditorUtility.DisplayDialog("Xuất âm thanh hoàn tất", $"Đã xuất thành công {count} tệp âm thanh chuẩn 44.1kHz 16-bit vào Assets/Audio/SFX/!", "OK");
        }

        [MenuItem("Tools/Virtual Rail/55. Audio: Apply Studio SFX Pack (Bật Bộ Âm Thanh Phòng Thu - ON)")]
        public static void ApplyStudioSfxPack()
        {
            AudioManager audioMgr = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                GameObject go = new GameObject("[VirtualRail_AudioManager]");
                audioMgr = go.AddComponent<AudioManager>();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            audioMgr.forceProceduralFallback = false;
            audioMgr.customLaserSingle = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_LaserSingle.ogg");
            audioMgr.customLaserTwin = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_LaserTwin.ogg");
            audioMgr.customLaserHyper = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_LaserHyper.ogg");
            audioMgr.customChargeHum = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ChargeHum.ogg");
            audioMgr.customChargeLockOn = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ChargeLockOn.ogg");
            audioMgr.customChargeSplash = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ChargeSplash.ogg");
            audioMgr.customSmartBomb = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_SmartBomb.ogg");
            audioMgr.customBarrelRoll = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_BarrelRoll.ogg");
            audioMgr.customEngineBoost = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_EngineBoost.ogg");
            audioMgr.customEngineBrake = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_EngineBrake.ogg");
            audioMgr.customExplosionSmall = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ExplosionSmall.ogg");
            audioMgr.customExplosionLarge = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ExplosionLarge.ogg");
            audioMgr.customShieldDamage = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ShieldDamage.ogg");
            audioMgr.customShieldAlarm = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ShieldAlarm.ogg");
            audioMgr.customRingCollect = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_RingCollect.ogg");
            audioMgr.customRadioChirp = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_RadioChirp.ogg");
            audioMgr.customGrazePass = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_GrazePass.ogg");
            audioMgr.customChargeFire = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_ChargeFire.ogg");
            audioMgr.customBombSupply = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/Studio_BombSupply.ogg");

            EditorUtility.SetDirty(audioMgr);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=green><b>[VirtualRail Audio] ĐÃ KÍCH HOẠT BỘ ÂM THANH PHÒNG THU (STUDIO SFX PACK):</b></color> Toàn bộ 19 hiệu ứng âm thanh đã được gán file Studio Stereo Hi-Fi từ Assets/Audio/SFX_Studio/!");
        }

        [MenuItem("Tools/Virtual Rail/56. Audio: Revert to Procedural SFX (Hoàn Nguyên Về Âm Thanh Thủ Tục - OFF)")]
        public static void RevertToProceduralSfx()
        {
            AudioManager audioMgr = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.forceProceduralFallback = true;
                audioMgr.customLaserSingle = null;
                audioMgr.customLaserTwin = null;
                audioMgr.customLaserHyper = null;
                audioMgr.customChargeHum = null;
                audioMgr.customChargeLockOn = null;
                audioMgr.customChargeSplash = null;
                audioMgr.customSmartBomb = null;
                audioMgr.customBarrelRoll = null;
                audioMgr.customEngineBoost = null;
                audioMgr.customEngineBrake = null;
                audioMgr.customExplosionSmall = null;
                audioMgr.customExplosionLarge = null;
                audioMgr.customShieldDamage = null;
                audioMgr.customShieldAlarm = null;
                audioMgr.customRingCollect = null;
                audioMgr.customRadioChirp = null;
                audioMgr.customGrazePass = null;
                audioMgr.customChargeFire = null;
                audioMgr.customBombSupply = null;

                EditorUtility.SetDirty(audioMgr);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();

                Debug.Log("<color=yellow><b>[VirtualRail Audio] ĐÃ HOÀN NGUYÊN (UNDO) VỀ ÂM THANH THỦ TỤC N64 (PROCEDURAL SFX):</b></color> Đã gỡ bỏ toàn bộ override tùy biến.");
            }
        }

        private static void EnsureManagerInstance()
        {
            if (_tempManager == null)
            {
                _tempManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
                if (_tempManager == null)
                {
                    if (_tempAudioHost == null)
                    {
                        _tempAudioHost = new GameObject("[Editor_AudioManager_Preview]");
                        _tempAudioHost.hideFlags = HideFlags.DontSave;
                    }
                    _tempManager = _tempAudioHost.AddComponent<AudioManager>();
                }
            }
        }

        private void OnGUI()
        {
            EnsureManagerInstance();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("VIRTUAL RAIL - BỘ SO SÁNH & QUẢN LÝ ÂM THANH", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Nút 'BẬT STUDIO SFX': Gán trực tiếp toàn bộ 17 tệp âm thanh phòng thu Studio vào game.\n" +
                "• Nút 'HOÀN NGUYÊN PROCEDURAL': Trở về âm thanh toán học thủ tục N64 cổ điển.\n" +
                "• Bấm 'Nghe Studio' hoặc 'Nghe Proc' để nghe so sánh trực tiếp chất âm giữa 2 phiên bản.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Bật Bộ Âm Thanh Studio (ON)", GUILayout.Height(34)))
            {
                ApplyStudioSfxPack();
            }
            GUI.backgroundColor = new Color(1.0f, 0.8f, 0.4f);
            if (GUILayout.Button("Hoàn Nguyên Procedural (OFF)", GUILayout.Height(34)))
            {
                RevertToProceduralSfx();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("DANH SÁCH 17 HIỆU ỨNG ÂM THANH:", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawSfxCategory("VŨ KHÍ & CHIẾN ĐẤU (WEAPONS)", new[]
            {
                (AudioSFXType.LaserSingle, "Laser Đơn (Single Laser)", "Studio_LaserSingle.ogg"),
                (AudioSFXType.LaserTwin, "Laser Đôi (Twin Laser)", "Studio_LaserTwin.ogg"),
                (AudioSFXType.LaserHyper, "Hyper Laser (Chùm tia hạng nặng)", "Studio_LaserHyper.ogg"),
                (AudioSFXType.ChargeHum, "Tụ sạc Plasma (Charge Hum)", "Studio_ChargeHum.ogg"),
                (AudioSFXType.ChargeLockOn, "Khóa mục tiêu (Lock-On Chirp)", "Studio_ChargeLockOn.ogg"),
                (AudioSFXType.ChargeFire, "Bắn đạn sạc (Charge Shot Fire)", "Studio_ChargeFire.ogg"),
                (AudioSFXType.ChargeSplash, "Nổ đạn sạc (Charge Splash)", "Studio_ChargeSplash.ogg"),
                (AudioSFXType.SmartBomb, "Bom thông minh (Smart Bomb)", "Studio_SmartBomb.ogg")
            });

            EditorGUILayout.Space(6);
            DrawSfxCategory("ĐIỀU KHIỂN & ĐỘNG CƠ (FLIGHT)", new[]
            {
                (AudioSFXType.BarrelRoll, "Lộn cánh né đạn (Barrel Roll)", "Studio_BarrelRoll.ogg"),
                (AudioSFXType.EngineBoost, "Tăng tốc phản lực (Engine Boost)", "Studio_EngineBoost.ogg"),
                (AudioSFXType.EngineBrake, "Hãm phanh xả khí (Engine Brake)", "Studio_EngineBrake.ogg"),
                (AudioSFXType.GrazePass, "Bay sượt lướt gió (Graze Pass)", "Studio_GrazePass.ogg")
            });

            EditorGUILayout.Space(6);
            DrawSfxCategory("CHẤN ĐỘNG & VA CHẠM (IMPACT)", new[]
            {
                (AudioSFXType.ExplosionSmall, "Nổ nhỏ (Explosion Small)", "Studio_ExplosionSmall.ogg"),
                (AudioSFXType.ExplosionLarge, "Nổ lớn (Explosion Large)", "Studio_ExplosionLarge.ogg"),
                (AudioSFXType.ShieldDamage, "Trúng đạn kim loại (Shield Damage)", "Studio_ShieldDamage.ogg"),
                (AudioSFXType.ShieldAlarm, "Cảnh báo cạn khiên (Shield Alarm)", "Studio_ShieldAlarm.ogg")
            });

            EditorGUILayout.Space(6);
            DrawSfxCategory("TƯƠNG TÁC & GIAO DIỆN (INTERACTION)", new[]
            {
                (AudioSFXType.RingCollect, "Nhặt vòng Ring (Ring Collect)", "Studio_RingCollect.ogg"),
                (AudioSFXType.BombSupply, "Gài đạn nạp bom (Bomb Supply)", "Studio_BombSupply.ogg"),
                (AudioSFXType.RadioChirp, "Bộ đàm radio (Radio Chirp)", "Studio_RadioChirp.ogg")
            });

            EditorGUILayout.EndScrollView();
        }

        private void DrawSfxCategory(string title, (AudioSFXType type, string label, string studioFilename)[] items)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginVertical("box");
            foreach (var item in items)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.label, GUILayout.ExpandWidth(true));

                GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f);
                if (GUILayout.Button("Nghe Studio", GUILayout.Width(90), GUILayout.Height(22)))
                {
                    PlayStudioClip(item.studioFilename);
                }

                GUI.backgroundColor = new Color(0.9f, 0.9f, 1.0f);
                if (GUILayout.Button("Nghe Proc", GUILayout.Width(75), GUILayout.Height(22)))
                {
                    PlayProceduralClip(item.type);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void PlayStudioClip(string filename)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{STUDIO_DIR}/{filename}");
            if (clip != null)
            {
                PlayAudioClipViaEditor(clip);
            }
            else
            {
                Debug.LogWarning($"[VirtualRail Audio] Không tìm thấy file: {STUDIO_DIR}/{filename}");
            }
        }

        private void PlayProceduralClip(AudioSFXType type)
        {
            EnsureManagerInstance();
            AudioClip clip = _tempManager.GetClipForType(type);
            if (clip != null)
            {
                PlayAudioClipViaEditor(clip);
            }
        }

        public static void PlayAudioClipViaEditor(AudioClip clip)
        {
            if (clip == null) return;
            try
            {
                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                if (audioUtilClass != null)
                {
                    MethodInfo method = audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                    if (method != null)
                    {
                        method.Invoke(null, new object[] { clip, 0, false });
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VirtualRail Audio] Không thể gọi AudioUtil.PlayPreviewClip: {ex.Message}");
            }
        }

        public static void SaveAudioClipToWav(string filepath, AudioClip clip)
        {
            if (clip == null) return;
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int sampleRate = clip.frequency;
                int channels = clip.channels;
                int sampleCount = samples.Length;

                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + sampleCount * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);

                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(sampleCount * 2);

                for (int i = 0; i < sampleCount; i++)
                {
                    short intSample = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    writer.Write(intSample);
                }

                File.WriteAllBytes(filepath, stream.ToArray());
            }
        }

        private void OnDestroy()
        {
            if (_tempAudioHost != null)
            {
                DestroyImmediate(_tempAudioHost);
            }
        }
    }
}
