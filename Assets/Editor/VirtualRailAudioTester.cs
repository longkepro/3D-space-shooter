using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Công cụ Kiểm thử & Nghe trước Âm thanh Thủ tục (Procedural SFX Tester & Previewer).
    /// Tuân thủ Điều 3 Bộ Quy Tắc Dự Án (Editor Tool Level):
    /// - Cho phép nghe thử tức thì toàn bộ 17 hiệu ứng âm thanh DSP đã nâng cấp ngay trong Editor mà không cần Play game.
    /// - Hỗ trợ xuất 1-click toàn bộ 17 âm thanh ra file WAV vật lý chuẩn PCM 16-bit 44.1kHz vào Assets/Audio/SFX/.
    /// </summary>
    public class VirtualRailAudioTester : EditorWindow
    {
        private Vector2 _scrollPos;
        private static AudioManager _tempManager;
        private static GameObject _tempAudioHost;

        [MenuItem("Tools/Virtual Rail/53. Audio: Open Procedural SFX Tester & Previewer")]
        public static void OpenWindow()
        {
            var window = GetWindow<VirtualRailAudioTester>("VR Audio Tester");
            window.minSize = new Vector2(460, 620);
            window.Show();
        }

        [MenuItem("Tools/Virtual Rail/54. Audio: Export Procedural SFX to WAV Files")]
        public static void ExportAllSfxToWav()
        {
            EnsureManagerInstance();
            string outputDir = Path.Combine(Application.dataPath, "Audio", "SFX");
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
            EditorGUILayout.LabelField("VIRTUAL RAIL - BỘ KIỂM THỬ ÂM THANH ARCADE N64", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bộ tổng hợp DSP Procedural Synthesizer được nâng cấp với Biquad Filter, Pink Noise và Tanh Saturation.\n" +
                "Click 'Nghe thử' để nghe trực tiếp chất âm đã cải tiến hoặc 'Xuất toàn bộ WAV' để lưu vào dự án.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Xuất toàn bộ 17 SFX ra tệp WAV (Assets/Audio/SFX)", GUILayout.Height(32)))
            {
                ExportAllSfxToWav();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("DANH SÁCH 17 HIỆU ỨNG ÂM THANH:", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawSfxCategory("VŨ KHÍ & CHIẾN ĐẤU (WEAPONS)", new[]
            {
                (AudioSFXType.LaserSingle, "Laser Đơn (Single Laser - 1150Hz -> 130Hz FM)"),
                (AudioSFXType.LaserTwin, "Laser Đôi (Twin Laser - Dual Detuned Tone)"),
                (AudioSFXType.LaserHyper, "Hyper Laser (Triple Harmonic Resonant Zap)"),
                (AudioSFXType.ChargeHum, "Tụ sạc Plasma (Charge Hum - 8.5Hz Vibrato LFO)"),
                (AudioSFXType.ChargeLockOn, "Khóa mục tiêu (Charge Lock-On - Star Fox 64 Beep-Beep)"),
                (AudioSFXType.ChargeSplash, "Nổ đạn sạc (Charge Splash - Sub-Bass Punch)"),
                (AudioSFXType.SmartBomb, "Bom thông minh (Smart Bomb - 85Hz Deep Boom)")
            });

            EditorGUILayout.Space(6);
            DrawSfxCategory("ĐIỀU KHIỂN & ĐỘNG CƠ (FLIGHT)", new[]
            {
                (AudioSFXType.BarrelRoll, "Lộn cánh phản đạn (Barrel Roll - Whoosh + Deflection Chime)"),
                (AudioSFXType.EngineBoost, "Tăng tốc phản lực (Engine Boost - Sub Ignition + Roar)"),
                (AudioSFXType.EngineBrake, "Hãm phanh xả khí (Engine Brake - Pneumatic Hiss)"),
                (AudioSFXType.GrazePass, "Bay sượt lướt gió (Graze Pass - Doppler Shimmer)")
            });

            EditorGUILayout.Space(6);
            DrawSfxCategory("CHẤN ĐỘNG & VA CHẠM (IMPACT)", new[]
            {
                (AudioSFXType.ExplosionSmall, "Nổ nhỏ (Explosion Small - 220Hz Punch + Pink Noise)"),
                (AudioSFXType.ExplosionLarge, "Nổ lớn (Explosion Large - 2-Stage Shockwave Detonation)"),
                (AudioSFXType.ShieldDamage, "Trúng đạn kim loại (Shield Damage - Clang + Spark)"),
                (AudioSFXType.ShieldAlarm, "Cảnh báo cạn khiên (Shield Alarm - Dual Square Siren)")
            });

            EditorGUILayout.Space(6);
            DrawSfxCategory("TƯƠNG TÁC & GIAO DIỆN (INTERACTION)", new[]
            {
                (AudioSFXType.RingCollect, "Nhặt vòng Ring (Ring Collect - C6-E6-G6-C7 Arpeggio)"),
                (AudioSFXType.RadioChirp, "Bộ đàm radio (Radio Chirp - Dual Squelch Carrier Blip)")
            });

            EditorGUILayout.EndScrollView();
        }

        private void DrawSfxCategory(string title, (AudioSFXType type, string label)[] items)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginVertical("box");
            foreach (var item in items)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.label, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Nghe thử", GUILayout.Width(75), GUILayout.Height(22)))
                {
                    PlayPreviewClip(item.type);
                }

                if (GUILayout.Button("Xuất WAV", GUILayout.Width(75), GUILayout.Height(22)))
                {
                    ExportSingleClip(item.type);
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void PlayPreviewClip(AudioSFXType type)
        {
            EnsureManagerInstance();
            AudioClip clip = _tempManager.GetClipForType(type);
            if (clip != null)
            {
                PlayAudioClipViaEditor(clip);
            }
        }

        private void ExportSingleClip(AudioSFXType type)
        {
            EnsureManagerInstance();
            AudioClip clip = _tempManager.GetClipForType(type);
            if (clip != null)
            {
                string outputDir = Path.Combine(Application.dataPath, "Audio", "SFX");
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                string filePath = Path.Combine(outputDir, $"SFX_{type}.wav");
                SaveAudioClipToWav(filePath, clip);
                AssetDatabase.Refresh();
                Debug.Log($"<color=green><b>[VirtualRail Audio] ĐÃ XUẤT TỆP:</b></color> {filePath}");
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

                // RIFF Header
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + sampleCount * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

                // fmt Subchunk
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1Size (16 for PCM)
                writer.Write((short)1); // AudioFormat (1 for PCM)
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2); // ByteRate
                writer.Write((short)(channels * 2)); // BlockAlign
                writer.Write((short)16); // BitsPerSample (16-bit)

                // data Subchunk
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
