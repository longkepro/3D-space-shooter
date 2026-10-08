using System;
using UnityEngine;
using UnityEngine.Audio;

namespace VirtualRail
{
    public enum AudioSFXType
    {
        LaserSingle = 0,
        LaserTwin = 1,
        LaserHyper = 2,
        ChargeHum = 3,
        ChargeLockOn = 4,
        ChargeSplash = 5,
        SmartBomb = 6,
        BarrelRoll = 7,
        EngineBoost = 8,
        EngineBrake = 9,
        ExplosionSmall = 10,
        ExplosionLarge = 11,
        ShieldDamage = 12,
        ShieldAlarm = 13,
        RingCollect = 14,
        RadioChirp = 15,
        GrazePass = 16
    }

    /// <summary>
    /// Hệ thống Quản lý Âm thanh Tập trung (Centralized Audio Manager - Chuẩn Star Fox 64 Arcade).
    /// Tuân thủ nghiêm ngặt Điều 4 Bộ Quy Tắc Dự Án (GEMINI.md / AGENTS.md):
    /// - 0 GC Allocation trong Game Loop qua AudioSource Object Pool (16 kênh 2D + 16 kênh 3D).
    /// - Quản lý nhạc nền BGM 2 kênh chạy song song hỗ trợ Cross-fade mượt mà không giật khung hình.
    /// - Tích hợp Bộ tổng hợp âm thanh thủ tục (Procedural Sound Synthesizer Fallback) sinh sóng âm chuẩn N64 lúc Awake,
    ///   bảo đảm trò chơi không bao giờ bị 'câm' ngay cả khi chưa import file SFX ngoài.
    /// - Hỗ trợ kéo thả AudioClip tùy biến ngoài trong Inspector để ghi đè âm thanh bất kỳ lúc nào.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PREFS_MASTER_VOL = "VR_Audio_MasterVolume";
        private const string PREFS_MUSIC_VOL = "VR_Audio_MusicVolume";
        private const string PREFS_SFX_VOL = "VR_Audio_SfxVolume";

        private const int POOL_SIZE_2D = 16;
        private const int POOL_SIZE_3D = 16;

        [Header("AudioMixer Integration (Optional)")]
        [Tooltip("AudioMixer chính của dự án (nếu có)")]
        public AudioMixer masterMixer;
        [Tooltip("Tên tham số Exposed cho Master Volume trong Mixer")]
        public string masterParamName = "MasterVolume";
        [Tooltip("Tên tham số Exposed cho Music Volume trong Mixer")]
        public string musicParamName = "MusicVolume";
        [Tooltip("Tên tham số Exposed cho SFX Volume trong Mixer")]
        public string sfxParamName = "SfxVolume";

        [Header("Volume Sliders (0.0 - 1.0)")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1.0f;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;

        [Header("Custom Sound Effects Overrides (Optional)")]
        [Tooltip("Kéo thả file âm thanh tùy chọn vào đây để ghi đè âm thanh mặc định")]
        public AudioClip customLaserSingle;
        public AudioClip customLaserTwin;
        public AudioClip customLaserHyper;
        public AudioClip customChargeHum;
        public AudioClip customChargeLockOn;
        public AudioClip customChargeSplash;
        public AudioClip customSmartBomb;
        public AudioClip customBarrelRoll;
        public AudioClip customEngineBoost;
        public AudioClip customEngineBrake;
        public AudioClip customExplosionSmall;
        public AudioClip customExplosionLarge;
        public AudioClip customShieldDamage;
        public AudioClip customShieldAlarm;
        public AudioClip customRingCollect;
        public AudioClip customRadioChirp;
        public AudioClip customGrazePass;

        [Header("Default Music Clips (Optional)")]
        public AudioClip defaultMenuBgm;
        public AudioClip defaultStageBgm;
        public AudioClip defaultBossBgm;

        // BGM Dual-Source Controller (Cross-Fade)
        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private bool _isUsingSourceA = true;
        private bool _isFadingMusic = false;
        private float _musicFadeTimer = 0f;
        private float _musicFadeDuration = 1.5f;
        private float _targetMusicVolA = 0f;
        private float _targetMusicVolB = 0f;
        private float _startMusicVolA = 0f;
        private float _startMusicVolB = 0f;

        // SFX Fixed Pools (0 GC Alloc)
        private AudioSource[] _sfxPool2D;
        private AudioSource[] _sfxPool3D;
        private int _next2DIndex = 0;
        private int _next3DIndex = 0;

        // Procedural Synthesized AudioClips (Generated once at Awake)
        private AudioClip[] _proceduralClips;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioEngine();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioEngine()
        {
            LoadVolumePreferences();

            // 1. Tạo 2 nguồn BGM cho Cross-Fade
            GameObject musicRoot = new GameObject("[BGM_Channels]");
            musicRoot.transform.SetParent(transform, false);

            _bgmSourceA = musicRoot.AddComponent<AudioSource>();
            _bgmSourceA.loop = true;
            _bgmSourceA.playOnAwake = false;
            _bgmSourceA.spatialBlend = 0f; // 2D Stereo
            _bgmSourceA.volume = 0f;

            _bgmSourceB = musicRoot.AddComponent<AudioSource>();
            _bgmSourceB.loop = true;
            _bgmSourceB.playOnAwake = false;
            _bgmSourceB.spatialBlend = 0f; // 2D Stereo
            _bgmSourceB.volume = 0f;

            // 2. Tạo Pool SFX 2D
            GameObject sfx2DRoot = new GameObject("[SFX_Pool_2D]");
            sfx2DRoot.transform.SetParent(transform, false);
            _sfxPool2D = new AudioSource[POOL_SIZE_2D];
            for (int i = 0; i < POOL_SIZE_2D; i++)
            {
                GameObject ch = new GameObject($"SFX_2D_{i:D2}");
                ch.transform.SetParent(sfx2DRoot.transform, false);
                var src = ch.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // Pure 2D
                _sfxPool2D[i] = src;
            }

            // 3. Tạo Pool SFX 3D
            GameObject sfx3DRoot = new GameObject("[SFX_Pool_3D]");
            sfx3DRoot.transform.SetParent(transform, false);
            _sfxPool3D = new AudioSource[POOL_SIZE_3D];
            for (int i = 0; i < POOL_SIZE_3D; i++)
            {
                GameObject ch = new GameObject($"SFX_3D_{i:D2}");
                ch.transform.SetParent(sfx3DRoot.transform, false);
                var src = ch.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1.0f; // Full 3D Spatial Audio
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 5f;
                src.maxDistance = 300f;
                src.dopplerLevel = 0.5f;
                _sfxPool3D[i] = src;
            }

            // 4. Khởi tạo sóng âm toán học dự phòng (Procedural SFX Synthesizer)
            SynthesizeProceduralArcadeClips();
        }

        private void LoadVolumePreferences()
        {
            _masterVolume = PlayerPrefs.GetFloat(PREFS_MASTER_VOL, 1.0f);
            _musicVolume = PlayerPrefs.GetFloat(PREFS_MUSIC_VOL, 0.8f);
            _sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOL, 1.0f);
            ApplyVolumesToHardware();
        }

        public void SetMasterVolume(float vol)
        {
            _masterVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(PREFS_MASTER_VOL, _masterVolume);
            ApplyVolumesToHardware();
        }

        public void SetMusicVolume(float vol)
        {
            _musicVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOL, _musicVolume);
            ApplyVolumesToHardware();
        }

        public void SetSfxVolume(float vol)
        {
            _sfxVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(PREFS_SFX_VOL, _sfxVolume);
            ApplyVolumesToHardware();
        }

        private void ApplyVolumesToHardware()
        {
            if (masterMixer != null)
            {
                // Quy đổi tỉ lệ tuyến tính sang Decibel: dB = 20 * log10(vol)
                masterMixer.SetFloat(masterParamName, LinearToDecibel(_masterVolume));
                masterMixer.SetFloat(musicParamName, LinearToDecibel(_musicVolume));
                masterMixer.SetFloat(sfxParamName, LinearToDecibel(_sfxVolume));
            }
            else
            {
                // Software Bus Fallback
                if (_bgmSourceA != null && !_isFadingMusic)
                {
                    if (_isUsingSourceA) _bgmSourceA.volume = _musicVolume * _masterVolume;
                    else _bgmSourceA.volume = 0f;
                }
                if (_bgmSourceB != null && !_isFadingMusic)
                {
                    if (!_isUsingSourceA) _bgmSourceB.volume = _musicVolume * _masterVolume;
                    else _bgmSourceB.volume = 0f;
                }
            }
        }

        private static float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f) return -80f;
            return Mathf.Log10(linear) * 20f;
        }

        private void Update()
        {
            // Cập nhật Cross-fade mượt mà giữa 2 kênh BGM (0 GC Alloc)
            if (_isFadingMusic)
            {
                _musicFadeTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_musicFadeTimer / Mathf.Max(0.01f, _musicFadeDuration));

                float volA = Mathf.Lerp(_startMusicVolA, _targetMusicVolA, t) * _musicVolume * _masterVolume;
                float volB = Mathf.Lerp(_startMusicVolB, _targetMusicVolB, t) * _musicVolume * _masterVolume;

                if (_bgmSourceA != null) _bgmSourceA.volume = volA;
                if (_bgmSourceB != null) _bgmSourceB.volume = volB;

                if (t >= 1.0f)
                {
                    _isFadingMusic = false;
                    if (_targetMusicVolA <= 0.001f && _bgmSourceA != null) _bgmSourceA.Stop();
                    if (_targetMusicVolB <= 0.001f && _bgmSourceB != null) _bgmSourceB.Stop();
                }
            }
        }

        #region BGM Playback & Cross-Fade

        public void PlayMusic(AudioClip clip, float volume = 1f, float fadeDuration = 1.2f, bool loop = true)
        {
            if (clip == null) return;

            AudioSource currentPlaying = _isUsingSourceA ? _bgmSourceA : _bgmSourceB;
            AudioSource nextToPlay = _isUsingSourceA ? _bgmSourceB : _bgmSourceA;

            if (currentPlaying != null && currentPlaying.isPlaying && currentPlaying.clip == clip)
            {
                return; // Đang phát đúng bài này, không ngắt
            }

            _isUsingSourceA = !_isUsingSourceA;

            nextToPlay.clip = clip;
            nextToPlay.loop = loop;
            nextToPlay.volume = 0f;
            nextToPlay.Play();

            _isFadingMusic = true;
            _musicFadeTimer = 0f;
            _musicFadeDuration = Mathf.Max(0.1f, fadeDuration);

            if (_isUsingSourceA)
            {
                _startMusicVolA = _bgmSourceA.volume / Mathf.Max(0.001f, _musicVolume * _masterVolume);
                _targetMusicVolA = volume;
                _startMusicVolB = _bgmSourceB.volume / Mathf.Max(0.001f, _musicVolume * _masterVolume);
                _targetMusicVolB = 0f;
            }
            else
            {
                _startMusicVolB = _bgmSourceB.volume / Mathf.Max(0.001f, _musicVolume * _masterVolume);
                _targetMusicVolB = volume;
                _startMusicVolA = _bgmSourceA.volume / Mathf.Max(0.001f, _musicVolume * _masterVolume);
                _targetMusicVolA = 0f;
            }
        }

        public void PlayStageMusic(AudioClip stageClip, float volume = 1f)
        {
            AudioClip clip = stageClip != null ? stageClip : defaultStageBgm;
            if (clip != null)
            {
                PlayMusic(clip, volume, 1.5f, true);
            }
        }

        public void PlayBossMusic(AudioClip bossClip, float volume = 1f)
        {
            AudioClip clip = bossClip != null ? bossClip : defaultBossBgm;
            if (clip != null)
            {
                PlayMusic(clip, volume, 1.0f, true);
            }
        }

        public void StopMusic(float fadeDuration = 1.0f)
        {
            _isFadingMusic = true;
            _musicFadeTimer = 0f;
            _musicFadeDuration = fadeDuration;
            _startMusicVolA = _bgmSourceA != null ? _bgmSourceA.volume / Mathf.Max(0.001f, _musicVolume * _masterVolume) : 0f;
            _startMusicVolB = _bgmSourceB != null ? _bgmSourceB.volume / Mathf.Max(0.001f, _musicVolume * _masterVolume) : 0f;
            _targetMusicVolA = 0f;
            _targetMusicVolB = 0f;
        }

        #endregion

        #region SFX Playback (0 GC Alloc Pools)

        public void PlaySFX2D(AudioSFXType type, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = GetClipForType(type);
            PlaySFX2D(clip, volume, pitch);
        }

        public void PlaySFX2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _sfxPool2D == null) return;

            AudioSource src = GetAvailable2DSource();
            if (src != null)
            {
                src.pitch = pitch;
                src.volume = volume * _sfxVolume * _masterVolume;
                src.clip = clip;
                src.Play();
            }
        }

        public void PlaySFX3D(AudioSFXType type, Vector3 worldPos, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = GetClipForType(type);
            PlaySFX3D(clip, worldPos, volume, pitch);
        }

        public void PlaySFX3D(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f, float minDistance = 5f, float maxDistance = 250f)
        {
            if (clip == null || _sfxPool3D == null) return;

            AudioSource src = GetAvailable3DSource();
            if (src != null)
            {
                src.transform.position = worldPos;
                src.minDistance = minDistance;
                src.maxDistance = maxDistance;
                src.pitch = pitch;
                src.volume = volume * _sfxVolume * _masterVolume;
                src.clip = clip;
                src.Play();
            }
        }

        private AudioSource GetAvailable2DSource()
        {
            // 1. Tìm kênh đang rảnh
            for (int i = 0; i < POOL_SIZE_2D; i++)
            {
                int idx = (_next2DIndex + i) % POOL_SIZE_2D;
                if (!_sfxPool2D[idx].isPlaying)
                {
                    _next2DIndex = (idx + 1) % POOL_SIZE_2D;
                    return _sfxPool2D[idx];
                }
            }

            // 2. Voice Stealing: mượn kênh vòng tròn Round-Robin
            int stealIdx = _next2DIndex;
            _next2DIndex = (_next2DIndex + 1) % POOL_SIZE_2D;
            return _sfxPool2D[stealIdx];
        }

        private AudioSource GetAvailable3DSource()
        {
            for (int i = 0; i < POOL_SIZE_3D; i++)
            {
                int idx = (_next3DIndex + i) % POOL_SIZE_3D;
                if (!_sfxPool3D[idx].isPlaying)
                {
                    _next3DIndex = (idx + 1) % POOL_SIZE_3D;
                    return _sfxPool3D[idx];
                }
            }

            int stealIdx = _next3DIndex;
            _next3DIndex = (_next3DIndex + 1) % POOL_SIZE_3D;
            return _sfxPool3D[stealIdx];
        }

        public AudioClip GetClipForType(AudioSFXType type)
        {
            switch (type)
            {
                case AudioSFXType.LaserSingle:
                    return customLaserSingle != null ? customLaserSingle : _proceduralClips[(int)type];
                case AudioSFXType.LaserTwin:
                    return customLaserTwin != null ? customLaserTwin : _proceduralClips[(int)type];
                case AudioSFXType.LaserHyper:
                    return customLaserHyper != null ? customLaserHyper : _proceduralClips[(int)type];
                case AudioSFXType.ChargeHum:
                    return customChargeHum != null ? customChargeHum : _proceduralClips[(int)type];
                case AudioSFXType.ChargeLockOn:
                    return customChargeLockOn != null ? customChargeLockOn : _proceduralClips[(int)type];
                case AudioSFXType.ChargeSplash:
                    return customChargeSplash != null ? customChargeSplash : _proceduralClips[(int)type];
                case AudioSFXType.SmartBomb:
                    return customSmartBomb != null ? customSmartBomb : _proceduralClips[(int)type];
                case AudioSFXType.BarrelRoll:
                    return customBarrelRoll != null ? customBarrelRoll : _proceduralClips[(int)type];
                case AudioSFXType.EngineBoost:
                    return customEngineBoost != null ? customEngineBoost : _proceduralClips[(int)type];
                case AudioSFXType.EngineBrake:
                    return customEngineBrake != null ? customEngineBrake : _proceduralClips[(int)type];
                case AudioSFXType.ExplosionSmall:
                    return customExplosionSmall != null ? customExplosionSmall : _proceduralClips[(int)type];
                case AudioSFXType.ExplosionLarge:
                    return customExplosionLarge != null ? customExplosionLarge : _proceduralClips[(int)type];
                case AudioSFXType.ShieldDamage:
                    return customShieldDamage != null ? customShieldDamage : _proceduralClips[(int)type];
                case AudioSFXType.ShieldAlarm:
                    return customShieldAlarm != null ? customShieldAlarm : _proceduralClips[(int)type];
                case AudioSFXType.RingCollect:
                    return customRingCollect != null ? customRingCollect : _proceduralClips[(int)type];
                case AudioSFXType.RadioChirp:
                    return customRadioChirp != null ? customRadioChirp : _proceduralClips[(int)type];
                case AudioSFXType.GrazePass:
                    return customGrazePass != null ? customGrazePass : _proceduralClips[(int)type];
                default:
                    return null;
            }
        }

        #endregion

        #region Procedural Arcade Synthesizer (Sóng Âm Toán Học N64 Fallback)

        private void SynthesizeProceduralArcadeClips()
        {
            int typeCount = Enum.GetValues(typeof(AudioSFXType)).Length;
            _proceduralClips = new AudioClip[typeCount];

            const int SAMPLE_RATE = 44100;

            // 1. Laser Single: Fast square-sine downsweep 920Hz -> 140Hz (0.09s)
            _proceduralClips[(int)AudioSFXType.LaserSingle] = SynthesizeLaser(SAMPLE_RATE, 0.085f, 920f, 140f, 0.45f);

            // 2. Laser Twin: Dual-tone downsweep 980Hz + 740Hz -> 160Hz (0.11s)
            _proceduralClips[(int)AudioSFXType.LaserTwin] = SynthesizeTwinLaser(SAMPLE_RATE, 0.105f, 980f, 740f, 160f, 0.55f);

            // 3. Laser Hyper: Resonant sweep with harmonic overtones 1250Hz -> 200Hz (0.14s)
            _proceduralClips[(int)AudioSFXType.LaserHyper] = SynthesizeHyperLaser(SAMPLE_RATE, 0.135f, 1250f, 200f, 0.65f);

            // 4. Charge Hum: Looping triangle FM wave (0.25s loopable)
            _proceduralClips[(int)AudioSFXType.ChargeHum] = SynthesizeChargeHum(SAMPLE_RATE, 0.25f);

            // 5. Charge Lock-On: Dual beep 1200Hz + 1450Hz ("BEEP-BEEP!" chuẩn Star Fox 64)
            _proceduralClips[(int)AudioSFXType.ChargeLockOn] = SynthesizeLockOn(SAMPLE_RATE, 0.14f);

            // 6. Charge Splash: Energetic resonant explosion burst (0.35s)
            _proceduralClips[(int)AudioSFXType.ChargeSplash] = SynthesizeNoiseBurst(SAMPLE_RATE, 0.35f, 280f, 45f, 0.8f);

            // 7. Smart Bomb: Deep sub-bass boom + expanding pink noise rumble (1.4s)
            _proceduralClips[(int)AudioSFXType.SmartBomb] = SynthesizeSmartBomb(SAMPLE_RATE, 1.4f);

            // 8. Barrel Roll: Aerodynamic bandpass noise whoosh (0.35s)
            _proceduralClips[(int)AudioSFXType.BarrelRoll] = SynthesizeWhoosh(SAMPLE_RATE, 0.36f);

            // 9. Engine Boost: Jet roar white noise (0.35s)
            _proceduralClips[(int)AudioSFXType.EngineBoost] = SynthesizeJetRoar(SAMPLE_RATE, 0.35f);

            // 10. Engine Brake: Air decompression release hiss (0.22s)
            _proceduralClips[(int)AudioSFXType.EngineBrake] = SynthesizeHiss(SAMPLE_RATE, 0.22f);

            // 11. Explosion Small: Low-pass noise punch (0.28s)
            _proceduralClips[(int)AudioSFXType.ExplosionSmall] = SynthesizeNoiseBurst(SAMPLE_RATE, 0.28f, 180f, 30f, 0.7f);

            // 12. Explosion Large: Heavy metal shockwave detonation (0.65s)
            _proceduralClips[(int)AudioSFXType.ExplosionLarge] = SynthesizeSmartBomb(SAMPLE_RATE, 0.65f);

            // 13. Shield Damage: Metallic clang + electrical buzz (0.16s)
            _proceduralClips[(int)AudioSFXType.ShieldDamage] = SynthesizeShieldHit(SAMPLE_RATE, 0.16f);

            // 14. Shield Alarm: Cockpit danger pulsing tone (0.20s)
            _proceduralClips[(int)AudioSFXType.ShieldAlarm] = SynthesizeAlarm(SAMPLE_RATE, 0.20f);

            // 15. Ring Collect: Rising arpeggio chime (C6-E6-G6-C7)
            _proceduralClips[(int)AudioSFXType.RingCollect] = SynthesizeChime(SAMPLE_RATE, 0.30f);

            // 16. Radio Chirp: High-frequency carrier blip (0.045s)
            _proceduralClips[(int)AudioSFXType.RadioChirp] = SynthesizeRadioBlip(SAMPLE_RATE, 0.045f);

            // 17. Graze Pass: Near-miss metallic shimmer (0.22s)
            _proceduralClips[(int)AudioSFXType.GrazePass] = SynthesizeGrazeShimmer(SAMPLE_RATE, 0.22f);
        }

        private static AudioClip SynthesizeLaser(int sampleRate, float duration, float startFreq, float endFreq, float volume)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.55f));
                phase += 2f * Mathf.PI * freq / sampleRate;

                float envelope = Mathf.Exp(-t * 12f);
                float wave = Mathf.Sin(phase);
                // Pha trộn 25% sóng vuông tạo tiếng laser 'tách' sắc nhọn của N64
                float sqr = wave >= 0f ? 0.8f : -0.8f;
                data[i] = Mathf.Lerp(wave, sqr, 0.25f) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_LaserSingle", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeTwinLaser(int sampleRate, float duration, float f1, float f2, float endFreq, float volume)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase1 = 0f;
            float phase2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq1 = Mathf.Lerp(f1, endFreq, Mathf.Pow(t, 0.6f));
                float freq2 = Mathf.Lerp(f2, endFreq * 0.9f, Mathf.Pow(t, 0.6f));

                phase1 += 2f * Mathf.PI * freq1 / sampleRate;
                phase2 += 2f * Mathf.PI * freq2 / sampleRate;

                float envelope = Mathf.Exp(-t * 10f);
                float wave = (Mathf.Sin(phase1) + Mathf.Sin(phase2) * 0.7f) * 0.6f;
                data[i] = wave * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_LaserTwin", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeHyperLaser(int sampleRate, float duration, float startFreq, float endFreq, float volume)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.5f));
                phase += 2f * Mathf.PI * freq / sampleRate;

                float envelope = Mathf.Exp(-t * 8f);
                float wave = (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.35f + Mathf.Sin(phase * 3f) * 0.2f) * 0.65f;
                data[i] = wave * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_LaserHyper", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeChargeHum(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float lfo = Mathf.Sin(2f * Mathf.PI * 8f * t) * 35f;
                float freq = 360f + lfo;
                phase += 2f * Mathf.PI * freq / sampleRate;

                // Triangle wave
                float tri = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
                data[i] = tri * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Proc_ChargeHum", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeLockOn(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            int beep1End = Mathf.FloorToInt(samples * 0.38f);
            int pauseEnd = Mathf.FloorToInt(samples * 0.55f);

            float phase1 = 0f;
            float phase2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                if (i < beep1End)
                {
                    phase1 += 2f * Mathf.PI * 1200f / sampleRate;
                    data[i] = Mathf.Sin(phase1) * 0.65f;
                }
                else if (i < pauseEnd)
                {
                    data[i] = 0f;
                }
                else
                {
                    phase2 += 2f * Mathf.PI * 1550f / sampleRate;
                    data[i] = Mathf.Sin(phase2) * 0.75f;
                }
            }

            AudioClip clip = AudioClip.Create("Proc_LockOn", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeSmartBomb(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float sinePhase = 0f;
            System.Random rng = new System.Random(1337);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Sub-bass sine sweep: 90Hz -> 28Hz
                float subFreq = Mathf.Lerp(90f, 28f, t);
                sinePhase += 2f * Mathf.PI * subFreq / sampleRate;
                float subBass = Mathf.Sin(sinePhase) * Mathf.Exp(-t * 2.8f);

                // Rumble noise
                float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 4.5f);

                data[i] = (subBass * 0.65f + noise * 0.35f) * 0.95f;
            }

            AudioClip clip = AudioClip.Create("Proc_SmartBomb", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeWhoosh(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(42);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI); // Parabolic bell curve
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                data[i] = noise * env * 0.55f;
            }

            AudioClip clip = AudioClip.Create("Proc_Whoosh", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeJetRoar(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(99);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                data[i] = noise * 0.4f * (1f - t * 0.3f);
            }

            AudioClip clip = AudioClip.Create("Proc_JetRoar", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeHiss(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(77);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                data[i] = noise * Mathf.Exp(-t * 8f) * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Proc_Hiss", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeNoiseBurst(int sampleRate, float duration, float punchFreq, float endFreq, float volume)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;
            System.Random rng = new System.Random(88);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(punchFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;

                float sinePunch = Mathf.Sin(phase) * Mathf.Exp(-t * 9f);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 7f);

                data[i] = (sinePunch * 0.5f + noise * 0.5f) * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_NoiseBurst", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeShieldHit(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase1 = 0f;
            float phase2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                phase1 += 2f * Mathf.PI * 680f / sampleRate;
                phase2 += 2f * Mathf.PI * 1150f / sampleRate;

                float metallic = (Mathf.Sin(phase1) + Mathf.Sin(phase2) * 0.8f) * 0.5f;
                data[i] = metallic * Mathf.Exp(-t * 15f) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Proc_ShieldHit", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeAlarm(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = (t < 0.5f) ? 880f : 660f;
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = Mathf.Sin(phase) * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Proc_Alarm", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeChime(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            // 4 nốt arpeggio: C6 (1046Hz), E6 (1318Hz), G6 (1568Hz), C7 (2093Hz)
            float[] freqs = new float[] { 1046.5f, 1318.5f, 1567.9f, 2093.0f };
            int stepSamples = samples / 4;

            float phase = 0f;
            int currentStep = 0;

            for (int i = 0; i < samples; i++)
            {
                int step = Mathf.Min(i / stepSamples, 3);
                if (step != currentStep)
                {
                    currentStep = step;
                }

                phase += 2f * Mathf.PI * freqs[currentStep] / sampleRate;
                float noteT = (float)(i % stepSamples) / stepSamples;
                float env = Mathf.Exp(-noteT * 5f);

                data[i] = Mathf.Sin(phase) * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Proc_Chime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeRadioBlip(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                phase += 2f * Mathf.PI * 2200f / sampleRate;
                data[i] = Mathf.Sin(phase) * Mathf.Sin(t * Mathf.PI) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Proc_RadioBlip", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeGrazeShimmer(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(1800f, 3200f, t);
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 9f) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Proc_GrazeShimmer", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        /// <summary>
        /// Khởi tạo an toàn tự động nếu Scene chưa có AudioManager (Plug & Play).
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<AudioManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[VirtualRail_AudioManager]");
                    Instance = go.AddComponent<AudioManager>();
                }
            }
        }
    }
}
