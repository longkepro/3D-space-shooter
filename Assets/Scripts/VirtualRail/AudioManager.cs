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
        GrazePass = 16,
        ChargeFire = 17
    }

    public enum BgmContext
    {
        None = 0,
        Menu = 1,
        Stage = 2,
        Boss = 3
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
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[VirtualRail_AudioManager]");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                if (_instance != null)
                {
                    _instance.EnsureEngineInitialized();
                }
                return _instance;
            }
            private set => _instance = value;
        }

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

        [Header("Audio Mode Override (Layer 3 Undo Safety)")]
        [Tooltip("Nếu bật, cưỡng chế game luôn dùng âm thanh toán học thủ tục N64 (Procedural Fallback)")]
        public bool forceProceduralFallback = false;

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
        public AudioClip customChargeFire;

        [Header("Stage & Boss Music Playlists (Shuffle Supported)")]
        [Tooltip("Danh sách nhạc nền cho các màn chơi thông thường (hỗ trợ xáo trộn Shuffle)")]
        public AudioClip[] stageMusicPlaylist;

        [Tooltip("Danh sách nhạc nền riêng cho các trận chiến với Boss (hỗ trợ xáo trộn Shuffle)")]
        public AudioClip[] bossMusicPlaylist;

        [Header("Shuffle Settings")]
        [Tooltip("Tự động xáo trộn và chuyển bài khi hết nhạc trong màn chơi")]
        public bool shuffleStageMusic = true;

        [Tooltip("Tự động xáo trộn và chuyển bài khi đấu Boss (nếu có nhiều bài Boss)")]
        public bool shuffleBossMusic = true;

        [Range(0.5f, 5.0f)]
        [Tooltip("Thời gian trượt hòa âm (Cross-fade) trước khi bài hát hiện tại kết thúc (giây)")]
        public float crossfadeLeadTime = 2.0f;

        [Header("Default Music Clips (Optional Fallback)")]
        public AudioClip defaultMenuBgm;
        public AudioClip defaultStageBgm;
        public AudioClip defaultBossBgm;

        [Tooltip("Danh sách toàn bộ các bản nhạc trong game để phát tuần hoàn hoặc lựa chọn")]
        public AudioClip[] musicPlaylist;

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

        // Shuffle Context & Decks (0 GC Alloc)
        private BgmContext _currentContext = BgmContext.None;
        private float _currentContextVolume = 1.0f;
        private int[] _stageDeck;
        private int _stageDeckIndex = 0;
        private int _lastPlayedStageIndex = -1;
        private int[] _bossDeck;
        private int _bossDeckIndex = 0;
        private int _lastPlayedBossIndex = -1;

        // SFX Fixed Pools (0 GC Alloc)
        private AudioSource[] _sfxPool2D;
        private AudioSource[] _sfxPool3D;
        private int _next2DIndex = 0;
        private int _next3DIndex = 0;
        private bool _isAudioEngineInitialized = false;

        // Procedural Synthesized AudioClips (Generated once at Awake)
        private AudioClip[] _proceduralClips;

        public void EnsureEngineInitialized()
        {
            if (_isAudioEngineInitialized && _sfxPool2D != null && _sfxPool2D.Length >= POOL_SIZE_2D && _sfxPool3D != null && _sfxPool3D.Length >= POOL_SIZE_3D)
            {
                return;
            }
            InitializeAudioEngine();
        }

        private static void SafeDestroyNode(Transform t)
        {
            if (t == null) return;
            if (Application.isPlaying)
            {
                Destroy(t.gameObject);
            }
            else
            {
                DestroyImmediate(t.gameObject);
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioEngine();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioEngine()
        {
            _isAudioEngineInitialized = true;
            LoadVolumePreferences();

            // Dọn sạch root cũ nếu có (phòng ngừa trường hợp khởi tạo lại trong Editor)
            Transform oldMusic = transform.Find("[BGM_Channels]");
            if (oldMusic != null) SafeDestroyNode(oldMusic);
            Transform oldSfx2D = transform.Find("[SFX_Pool_2D]");
            if (oldSfx2D != null) SafeDestroyNode(oldSfx2D);
            Transform oldSfx3D = transform.Find("[SFX_Pool_3D]");
            if (oldSfx3D != null) SafeDestroyNode(oldSfx3D);

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

#if UNITY_EDITOR
            if (!forceProceduralFallback)
            {
                AutoLoadStudioClipsIfEmpty();
            }
#endif
        }

#if UNITY_EDITOR
        public void AutoLoadStudioClipsIfEmpty()
        {
            const string dir = "Assets/Audio/SFX_Studio";
            if (customLaserSingle == null) customLaserSingle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_LaserSingle.ogg");
            if (customLaserTwin == null) customLaserTwin = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_LaserTwin.ogg");
            if (customLaserHyper == null) customLaserHyper = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_LaserHyper.ogg");
            if (customChargeHum == null) customChargeHum = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ChargeHum.ogg");
            if (customChargeLockOn == null) customChargeLockOn = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ChargeLockOn.ogg");
            if (customChargeSplash == null) customChargeSplash = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ChargeSplash.ogg");
            if (customSmartBomb == null) customSmartBomb = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_SmartBomb.ogg");
            if (customBarrelRoll == null) customBarrelRoll = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_BarrelRoll.ogg");
            if (customEngineBoost == null) customEngineBoost = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_EngineBoost.ogg");
            if (customEngineBrake == null) customEngineBrake = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_EngineBrake.ogg");
            if (customExplosionSmall == null) customExplosionSmall = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ExplosionSmall.ogg");
            if (customExplosionLarge == null) customExplosionLarge = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ExplosionLarge.ogg");
            if (customShieldDamage == null) customShieldDamage = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ShieldDamage.ogg");
            if (customShieldAlarm == null) customShieldAlarm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ShieldAlarm.ogg");
            if (customRingCollect == null) customRingCollect = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_RingCollect.ogg");
            if (customRadioChirp == null) customRadioChirp = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_RadioChirp.ogg");
            if (customGrazePass == null) customGrazePass = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_GrazePass.ogg");
            if (customChargeFire == null) customChargeFire = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{dir}/Studio_ChargeFire.ogg");
        }
#endif

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
            // 1. Cập nhật Cross-fade mượt mà giữa 2 kênh BGM (0 GC Alloc)
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
            else
            {
                // 2. Tự động kiểm tra bài hát sắp kết thúc để kích hoạt Auto-Crossfade Shuffle
                CheckAutoAdvanceShuffle();
            }
        }

        #region Shuffle Decks & Context Playback (0 GC Alloc)

        private void CheckAutoAdvanceShuffle()
        {
            AudioSource activeSource = _isUsingSourceA ? _bgmSourceA : _bgmSourceB;
            if (activeSource == null || activeSource.clip == null) return;

            // Nếu đang loop thì không auto-advance
            if (activeSource.loop) return;

            bool shouldAdvance = false;
            if (activeSource.isPlaying)
            {
                float remainingTime = activeSource.clip.length - activeSource.time;
                if (remainingTime <= Mathf.Max(0.1f, crossfadeLeadTime))
                {
                    shouldAdvance = true;
                }
            }
            else
            {
                // Đã hết bài hoặc đã dừng
                shouldAdvance = true;
            }

            if (shouldAdvance)
            {
                if (_currentContext == BgmContext.Stage && shuffleStageMusic && stageMusicPlaylist != null && stageMusicPlaylist.Length > 1)
                {
                    AudioClip nextClip = GetNextShuffledStageClip();
                    if (nextClip != null)
                    {
                        PlayMusic(nextClip, _currentContextVolume, crossfadeLeadTime, false);
                    }
                }
                else if (_currentContext == BgmContext.Boss && shuffleBossMusic && bossMusicPlaylist != null && bossMusicPlaylist.Length > 1)
                {
                    AudioClip nextClip = GetNextShuffledBossClip();
                    if (nextClip != null)
                    {
                        PlayMusic(nextClip, _currentContextVolume, crossfadeLeadTime, false);
                    }
                }
            }
        }

        private void PrepareDeck(ref int[] deck, ref int deckIndex, int count, int lastPlayedIndex)
        {
            if (count <= 0)
            {
                deck = null;
                deckIndex = 0;
                return;
            }

            if (deck == null || deck.Length != count)
            {
                deck = new int[count];
            }

            for (int i = 0; i < count; i++)
            {
                deck[i] = i;
            }

            // Fisher-Yates Shuffle
            for (int i = count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                int tmp = deck[i];
                deck[i] = deck[r];
                deck[r] = tmp;
            }

            // Bảo đảm lá bài đầu tiên không trùng với bài vừa phát gần nhất (nếu count > 1)
            if (count > 1 && deck[0] == lastPlayedIndex)
            {
                int swapIdx = UnityEngine.Random.Range(1, count);
                int tmp = deck[0];
                deck[0] = deck[swapIdx];
                deck[swapIdx] = tmp;
            }

            deckIndex = 0;
        }

        public AudioClip GetNextShuffledStageClip()
        {
            if (stageMusicPlaylist == null || stageMusicPlaylist.Length == 0) return null;
            if (stageMusicPlaylist.Length == 1) return stageMusicPlaylist[0];

            int totalClips = stageMusicPlaylist.Length;
            if (_stageDeck == null || _stageDeck.Length != totalClips || _stageDeckIndex >= totalClips)
            {
                PrepareDeck(ref _stageDeck, ref _stageDeckIndex, totalClips, _lastPlayedStageIndex);
            }

            for (int attempts = 0; attempts < totalClips; attempts++)
            {
                int clipIndex = _stageDeck[_stageDeckIndex];
                _stageDeckIndex = (_stageDeckIndex + 1) % totalClips;
                AudioClip clip = stageMusicPlaylist[clipIndex];
                if (clip != null)
                {
                    _lastPlayedStageIndex = clipIndex;
                    return clip;
                }
            }

            return null;
        }

        public AudioClip GetNextShuffledBossClip()
        {
            if (bossMusicPlaylist == null || bossMusicPlaylist.Length == 0) return null;
            if (bossMusicPlaylist.Length == 1) return bossMusicPlaylist[0];

            int totalClips = bossMusicPlaylist.Length;
            if (_bossDeck == null || _bossDeck.Length != totalClips || _bossDeckIndex >= totalClips)
            {
                PrepareDeck(ref _bossDeck, ref _bossDeckIndex, totalClips, _lastPlayedBossIndex);
            }

            for (int attempts = 0; attempts < totalClips; attempts++)
            {
                int clipIndex = _bossDeck[_bossDeckIndex];
                _bossDeckIndex = (_bossDeckIndex + 1) % totalClips;
                AudioClip clip = bossMusicPlaylist[clipIndex];
                if (clip != null)
                {
                    _lastPlayedBossIndex = clipIndex;
                    return clip;
                }
            }

            return null;
        }

        public void SkipToNextStageTrack()
        {
            if (_currentContext == BgmContext.Stage && stageMusicPlaylist != null && stageMusicPlaylist.Length > 0)
            {
                AudioClip nextClip = GetNextShuffledStageClip();
                if (nextClip != null)
                {
                    bool shouldLoop = stageMusicPlaylist.Length == 1;
                    PlayMusic(nextClip, _currentContextVolume, 1.0f, shouldLoop);
                }
            }
        }

        public void SkipToNextBossTrack()
        {
            if (_currentContext == BgmContext.Boss && bossMusicPlaylist != null && bossMusicPlaylist.Length > 0)
            {
                AudioClip nextClip = GetNextShuffledBossClip();
                if (nextClip != null)
                {
                    bool shouldLoop = bossMusicPlaylist.Length == 1;
                    PlayMusic(nextClip, _currentContextVolume, 1.0f, shouldLoop);
                }
            }
        }

        #endregion

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
            _currentContext = BgmContext.Stage;
            _currentContextVolume = volume;

            if (shuffleStageMusic && stageMusicPlaylist != null && stageMusicPlaylist.Length > 0)
            {
                AudioClip clip = GetNextShuffledStageClip();
                if (clip != null)
                {
                    bool shouldLoop = stageMusicPlaylist.Length == 1;
                    PlayMusic(clip, volume, 1.5f, shouldLoop);
                    return;
                }
            }

            AudioClip fallback = stageClip != null ? stageClip : defaultStageBgm;
            if (fallback != null)
            {
                PlayMusic(fallback, volume, 1.5f, true);
            }
        }

        public void PlayBossMusic(AudioClip bossClip, float volume = 1f)
        {
            _currentContext = BgmContext.Boss;
            _currentContextVolume = volume;

            if (shuffleBossMusic && bossMusicPlaylist != null && bossMusicPlaylist.Length > 0)
            {
                AudioClip clip = GetNextShuffledBossClip();
                if (clip != null)
                {
                    bool shouldLoop = bossMusicPlaylist.Length == 1;
                    PlayMusic(clip, volume, 1.2f, shouldLoop);
                    return;
                }
            }

            AudioClip fallback = bossClip != null ? bossClip : defaultBossBgm;
            if (fallback != null)
            {
                PlayMusic(fallback, volume, 1.2f, true);
            }
            else
            {
                Debug.Log("<color=yellow>[VirtualRail Audio]</color> Chưa cấu hình danh sách nhạc Boss (bossMusicPlaylist). Tạm thời duy trì âm nhạc hiện tại.");
            }
        }

        public void PlayMenuMusic(AudioClip menuClip = null, float volume = 1f)
        {
            _currentContext = BgmContext.Menu;
            _currentContextVolume = volume;
            AudioClip clip = menuClip != null ? menuClip : defaultMenuBgm;
            if (clip != null)
            {
                PlayMusic(clip, volume, 1.2f, true);
            }
        }

        public void PlayPlaylistTrack(int index, float volume = 1f, float fadeDuration = 1.2f)
        {
            if (musicPlaylist != null && index >= 0 && index < musicPlaylist.Length)
            {
                if (musicPlaylist[index] != null)
                {
                    PlayMusic(musicPlaylist[index], volume, fadeDuration, true);
                }
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
            if (clip == null) return;
            EnsureEngineInitialized();
            if (_sfxPool2D == null || _sfxPool2D.Length == 0) return;

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
            if (clip == null) return;
            EnsureEngineInitialized();
            if (_sfxPool3D == null || _sfxPool3D.Length == 0) return;

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
            EnsureEngineInitialized();
            if (_sfxPool2D == null || _sfxPool2D.Length == 0) return null;

            int poolLen = _sfxPool2D.Length;
            // 1. Tìm kênh đang rảnh
            for (int i = 0; i < poolLen; i++)
            {
                int idx = (_next2DIndex + i) % poolLen;
                if (_sfxPool2D[idx] != null && !_sfxPool2D[idx].isPlaying)
                {
                    _next2DIndex = (idx + 1) % poolLen;
                    return _sfxPool2D[idx];
                }
            }

            // 2. Voice Stealing: mượn kênh vòng tròn Round-Robin
            int stealIdx = _next2DIndex % poolLen;
            _next2DIndex = (_next2DIndex + 1) % poolLen;
            return _sfxPool2D[stealIdx];
        }

        private AudioSource GetAvailable3DSource()
        {
            EnsureEngineInitialized();
            if (_sfxPool3D == null || _sfxPool3D.Length == 0) return null;

            int poolLen = _sfxPool3D.Length;
            for (int i = 0; i < poolLen; i++)
            {
                int idx = (_next3DIndex + i) % poolLen;
                if (_sfxPool3D[idx] != null && !_sfxPool3D[idx].isPlaying)
                {
                    _next3DIndex = (idx + 1) % poolLen;
                    return _sfxPool3D[idx];
                }
            }

            int stealIdx = _next3DIndex % poolLen;
            _next3DIndex = (_next3DIndex + 1) % poolLen;
            return _sfxPool3D[stealIdx];
        }

        public AudioClip GetClipForType(AudioSFXType type)
        {
            if (forceProceduralFallback)
            {
                return (_proceduralClips != null && (int)type >= 0 && (int)type < _proceduralClips.Length) ? _proceduralClips[(int)type] : null;
            }

#if UNITY_EDITOR
            if (customChargeHum == null || customChargeLockOn == null || customChargeFire == null || customChargeSplash == null)
            {
                AutoLoadStudioClipsIfEmpty();
            }
#endif

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
                case AudioSFXType.ChargeFire:
                    return customChargeFire != null ? customChargeFire : _proceduralClips[(int)type];
                default:
                    return null;
            }
        }

        #endregion

        #region Procedural Arcade Synthesizer (Sóng Âm Toán Học N64 Fallback)

        #region DSP Signal Processing Primitives (0 GC Allocation)

        /// <summary>
        /// Bộ lọc số IIR 2-Pole Biquad Filter (Lowpass, Bandpass, Highpass với Resonance Q).
        /// Cắt lọc tần số chói số và tạo độ rung chuyển cơ học tự nhiên cho arcade SFX.
        /// </summary>
        private struct BiquadFilter
        {
            public float b0, b1, b2, a1, a2;
            public float x1, x2, y1, y2;

            public static BiquadFilter CreateLowpass(float cutoff, float q, float sampleRate)
            {
                cutoff = Mathf.Clamp(cutoff, 20f, sampleRate * 0.48f);
                float omega = 2f * Mathf.PI * cutoff / sampleRate;
                float alpha = Mathf.Sin(omega) / (2f * Mathf.Max(0.1f, q));
                float cosw = Mathf.Cos(omega);
                float a0 = 1f + alpha;

                return new BiquadFilter
                {
                    b0 = ((1f - cosw) * 0.5f) / a0,
                    b1 = (1f - cosw) / a0,
                    b2 = ((1f - cosw) * 0.5f) / a0,
                    a1 = (-2f * cosw) / a0,
                    a2 = (1f - alpha) / a0,
                    x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f
                };
            }

            public static BiquadFilter CreateBandpass(float centerFreq, float q, float sampleRate)
            {
                centerFreq = Mathf.Clamp(centerFreq, 20f, sampleRate * 0.48f);
                float omega = 2f * Mathf.PI * centerFreq / sampleRate;
                float alpha = Mathf.Sin(omega) / (2f * Mathf.Max(0.1f, q));
                float cosw = Mathf.Cos(omega);
                float a0 = 1f + alpha;

                return new BiquadFilter
                {
                    b0 = alpha / a0,
                    b1 = 0f,
                    b2 = -alpha / a0,
                    a1 = (-2f * cosw) / a0,
                    a2 = (1f - alpha) / a0,
                    x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f
                };
            }

            public static BiquadFilter CreateHighpass(float cutoff, float q, float sampleRate)
            {
                cutoff = Mathf.Clamp(cutoff, 20f, sampleRate * 0.48f);
                float omega = 2f * Mathf.PI * cutoff / sampleRate;
                float alpha = Mathf.Sin(omega) / (2f * Mathf.Max(0.1f, q));
                float cosw = Mathf.Cos(omega);
                float a0 = 1f + alpha;

                return new BiquadFilter
                {
                    b0 = ((1f + cosw) * 0.5f) / a0,
                    b1 = (-(1f + cosw)) / a0,
                    b2 = ((1f + cosw) * 0.5f) / a0,
                    a1 = (-2f * cosw) / a0,
                    a2 = (1f - alpha) / a0,
                    x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f
                };
            }

            public float Process(float inSample)
            {
                float outSample = b0 * inSample + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1;
                x1 = inSample;
                y2 = y1;
                y1 = outSample;
                return outSample;
            }
        }

        /// <summary>
        /// Bộ tạo tiếng ồn hồng Pink Noise (Voss-McCartney / 3-pole IIR approximation).
        /// Giảm -3dB/octave cho tiếng nổ và động cơ trầm ấm, đầy uy lực.
        /// </summary>
        private struct PinkNoiseFilter
        {
            private float b0, b1, b2;

            public float Next(float white)
            {
                b0 = 0.99765f * b0 + white * 0.0990460f;
                b1 = 0.96300f * b1 + white * 0.2965164f;
                b2 = 0.57000f * b2 + white * 1.0526913f;
                return (b0 + b1 + b2 + white * 0.1848f) * 0.22f;
            }
        }

        /// <summary>
        /// Bão hòa phi tuyến Analog Saturation (Soft-Clipping Tanh).
        /// Ngăn chặn vỡ tiếng kỹ thuật số, làm ấm dải trung âm và tăng độ dày (warmth).
        /// </summary>
        private static float SoftClip(float x)
        {
            return (float)Math.Tanh(x);
        }

        #endregion

        private void SynthesizeProceduralArcadeClips()
        {
            int typeCount = Enum.GetValues(typeof(AudioSFXType)).Length;
            _proceduralClips = new AudioClip[typeCount];

            const int SAMPLE_RATE = 44100;

            // 1. Laser Single: FM Downsweep 1150Hz -> 130Hz + Transient Punch (0.085s)
            _proceduralClips[(int)AudioSFXType.LaserSingle] = SynthesizeLaser(SAMPLE_RATE, 0.085f, 1150f, 130f, 0.65f);

            // 2. Laser Twin: Detuned Dual-Tone FM Sweep 1250Hz + 950Hz -> 140Hz (0.105s)
            _proceduralClips[(int)AudioSFXType.LaserTwin] = SynthesizeTwinLaser(SAMPLE_RATE, 0.105f, 1250f, 950f, 140f, 0.75f);

            // 3. Laser Hyper: Multi-Harmonic Resonant FM Sweep 1400Hz + 1050Hz + 700Hz -> 180Hz (0.135s)
            _proceduralClips[(int)AudioSFXType.LaserHyper] = SynthesizeHyperLaser(SAMPLE_RATE, 0.135f, 1400f, 180f, 0.85f);

            // 4. Charge Hum: Looping Triangle + FM Vibrato Wave (0.28s Loopable)
            _proceduralClips[(int)AudioSFXType.ChargeHum] = SynthesizeChargeHum(SAMPLE_RATE, 0.28f);

            // 5. Charge Lock-On: Dual-Tone Beep 1180Hz/1770Hz & 1480Hz/2220Hz ("BEEP-BEEP!" chuẩn Star Fox 64)
            _proceduralClips[(int)AudioSFXType.ChargeLockOn] = SynthesizeLockOn(SAMPLE_RATE, 0.15f);

            // 6. Charge Splash: Sub-Bass Punch + Resonant Lowpass Noise Detonation (0.35s)
            _proceduralClips[(int)AudioSFXType.ChargeSplash] = SynthesizeNoiseBurst(SAMPLE_RATE, 0.35f, 320f, 45f, 0.85f);

            // 7. Smart Bomb: Deep 85Hz->24Hz Sub-Bass Boom + Expanding Pink Noise Rumble (1.4s)
            _proceduralClips[(int)AudioSFXType.SmartBomb] = SynthesizeSmartBomb(SAMPLE_RATE, 1.4f);

            // 8. Barrel Roll: Aerodynamic Bandpass Whoosh + Metallic Deflection Chime Ring (0.36s)
            _proceduralClips[(int)AudioSFXType.BarrelRoll] = SynthesizeWhoosh(SAMPLE_RATE, 0.36f);

            // 9. Engine Boost: Sub-Thump Ignition + Thick Afterburner Jet Roar (0.35s)
            _proceduralClips[(int)AudioSFXType.EngineBoost] = SynthesizeJetRoar(SAMPLE_RATE, 0.35f);

            // 10. Engine Brake: Highpass Pneumatic Gas Release Hiss (0.22s)
            _proceduralClips[(int)AudioSFXType.EngineBrake] = SynthesizeHiss(SAMPLE_RATE, 0.22f);

            // 11. Explosion Small: 220Hz Transient Punch + Resonant Filtered Pink Noise (0.28s)
            _proceduralClips[(int)AudioSFXType.ExplosionSmall] = SynthesizeNoiseBurst(SAMPLE_RATE, 0.28f, 220f, 35f, 0.8f);

            // 12. Explosion Large: Two-Stage Heavy Metal Shockwave Detonation (0.65s)
            _proceduralClips[(int)AudioSFXType.ExplosionLarge] = SynthesizeExplosionLarge(SAMPLE_RATE, 0.65f);

            // 13. Shield Damage: Dissonant Metallic Ring Modulation + Electric Spark Clang (0.16s)
            _proceduralClips[(int)AudioSFXType.ShieldDamage] = SynthesizeShieldHit(SAMPLE_RATE, 0.16f);

            // 14. Shield Alarm: Urgency Cockpit Siren Dual Square Wave (0.20s)
            _proceduralClips[(int)AudioSFXType.ShieldAlarm] = SynthesizeAlarm(SAMPLE_RATE, 0.20f);

            // 15. Ring Collect: Pure Harmonic Arpeggio Chime C6-E6-G6-C7 (0.30s)
            _proceduralClips[(int)AudioSFXType.RingCollect] = SynthesizeChime(SAMPLE_RATE, 0.30f);

            // 16. Radio Chirp: Dual-Carrier Squelch Military Blip (0.045s)
            _proceduralClips[(int)AudioSFXType.RadioChirp] = SynthesizeRadioBlip(SAMPLE_RATE, 0.045f);

            // 17. Graze Pass: Doppler Metallic Near-Miss Shimmer (0.22s)
            _proceduralClips[(int)AudioSFXType.GrazePass] = SynthesizeGrazeShimmer(SAMPLE_RATE, 0.22f);

            // 18. Charge Fire: Heavy Sub-Bass Down-Sweep FM Plasma Discharge (0.16s)
            _proceduralClips[(int)AudioSFXType.ChargeFire] = SynthesizeLaser(SAMPLE_RATE, 0.16f, 850f, 65f, 0.95f);
        }

        private static AudioClip SynthesizeLaser(int sampleRate, float duration, float startFreq, float endFreq, float volume)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;
            var filter = BiquadFilter.CreateLowpass(4500f, 1.8f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Exponential pitch dive
                float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.45f));
                phase += 2f * Mathf.PI * freq / sampleRate;

                // FM Modulation for laser metallic zap bite
                float fm = Mathf.Sin(phase * 1.8f) * 0.45f;
                float carrier = Mathf.Sin(phase + fm);

                // Transient click punch at t < 4ms
                float punch = (i < sampleRate * 0.004f) ? Mathf.Sin((float)i / (sampleRate * 0.004f) * Mathf.PI) * 0.7f : 0f;

                // Fast decay envelope
                float env = Mathf.Exp(-t * 14f);
                float raw = (carrier * 0.75f + punch) * env;

                // Filter and analog saturation
                float filtered = filter.Process(raw);
                data[i] = SoftClip(filtered * 1.25f) * volume;
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
            var filter = BiquadFilter.CreateLowpass(5200f, 1.6f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq1 = Mathf.Lerp(f1, endFreq, Mathf.Pow(t, 0.5f));
                float freq2 = Mathf.Lerp(f2, endFreq * 0.85f, Mathf.Pow(t, 0.5f));

                phase1 += 2f * Mathf.PI * freq1 / sampleRate;
                phase2 += 2f * Mathf.PI * freq2 / sampleRate;

                // FM on both tones + detuned chorus
                float fm1 = Mathf.Sin(phase1 * 1.5f) * 0.35f;
                float fm2 = Mathf.Sin(phase2 * 2.0f) * 0.35f;

                float wave1 = Mathf.Sin(phase1 + fm1);
                float wave2 = Mathf.Sin(phase2 + fm2);

                float punch = (i < sampleRate * 0.005f) ? Mathf.Sin((float)i / (sampleRate * 0.005f) * Mathf.PI) * 0.8f : 0f;
                float env = Mathf.Exp(-t * 11f);

                float raw = (wave1 * 0.5f + wave2 * 0.45f + punch * 0.4f) * env;
                data[i] = SoftClip(filter.Process(raw) * 1.3f) * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_LaserTwin", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeHyperLaser(int sampleRate, float duration, float startFreq, float endFreq, float volume)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase1 = 0f;
            float phase2 = 0f;
            float phase3 = 0f;
            var filter = BiquadFilter.CreateLowpass(6000f, 2.2f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 0.4f));

                phase1 += 2f * Mathf.PI * freq / sampleRate;
                phase2 += 2f * Mathf.PI * (freq * 1.5f) / sampleRate;
                phase3 += 2f * Mathf.PI * (freq * 0.5f) / sampleRate;

                // Heavy sci-fi FM modulation
                float fm = Mathf.Sin(phase1 * 2f) * 0.55f;
                float wave = (Mathf.Sin(phase1 + fm) * 0.6f + Mathf.Sin(phase2) * 0.3f + Mathf.Sin(phase3) * 0.35f);

                float punch = (i < sampleRate * 0.008f) ? Mathf.Sin((float)i / (sampleRate * 0.008f) * Mathf.PI) * 0.9f : 0f;
                float env = Mathf.Exp(-t * 8.5f);

                float raw = (wave * 0.8f + punch) * env;
                data[i] = SoftClip(filter.Process(raw) * 1.5f) * volume;
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
                // 8.5 Hz LFO Vibrato
                float lfo = Mathf.Sin(2f * Mathf.PI * 8.5f * t) * 32f;
                float freq = 340f + lfo;
                phase += 2f * Mathf.PI * freq / sampleRate;

                // Rich triangle wave with subtle harmonic
                float tri = Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
                float sinHarmonic = Mathf.Sin(phase * 2f) * 0.2f;

                // Smooth window to eliminate seam pops when looping
                float window = Mathf.Sin(t * Mathf.PI);
                data[i] = SoftClip((tri + sinHarmonic) * 0.5f) * (0.8f + window * 0.2f);
            }

            AudioClip clip = AudioClip.Create("Proc_ChargeHum", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeLockOn(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            int beep1Len = Mathf.FloorToInt(samples * 0.36f);
            int pauseLen = Mathf.FloorToInt(samples * 0.16f);
            int beep2Start = beep1Len + pauseLen;

            float phaseA1 = 0f, phaseA2 = 0f;
            float phaseB1 = 0f, phaseB2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                if (i < beep1Len)
                {
                    float t = (float)i / beep1Len;
                    // Beep 1: 1180Hz + 1770Hz (Perfect fifth harmony)
                    phaseA1 += 2f * Mathf.PI * 1180f / sampleRate;
                    phaseA2 += 2f * Mathf.PI * 1770f / sampleRate;
                    float env = Mathf.Sin(t * Mathf.PI);
                    float wave = Mathf.Sin(phaseA1) * 0.65f + Mathf.Sin(phaseA2) * 0.35f;
                    data[i] = SoftClip(wave * 1.2f) * env * 0.75f;
                }
                else if (i < beep2Start)
                {
                    data[i] = 0f;
                }
                else
                {
                    float t = (float)(i - beep2Start) / (samples - beep2Start);
                    // Beep 2: 1480Hz + 2220Hz (Higher pitch, louder confirm)
                    phaseB1 += 2f * Mathf.PI * 1480f / sampleRate;
                    phaseB2 += 2f * Mathf.PI * 2220f / sampleRate;
                    float env = Mathf.Sin(t * Mathf.PI);
                    float wave = Mathf.Sin(phaseB1) * 0.7f + Mathf.Sin(phaseB2) * 0.35f;
                    data[i] = SoftClip(wave * 1.3f) * env * 0.85f;
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
            float subPhase = 0f;
            var pink = new PinkNoiseFilter();
            var rng = new System.Random(1337);
            var filter = BiquadFilter.CreateLowpass(1600f, 1.2f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Sub-bass sine sweep: 85Hz -> 24Hz with heavy weight
                float subFreq = Mathf.Lerp(85f, 24f, Mathf.Pow(t, 0.6f));
                subPhase += 2f * Mathf.PI * subFreq / sampleRate;
                float subBass = Mathf.Sin(subPhase) * Mathf.Exp(-t * 2.2f);

                // Deep pink noise rumble
                float white = (float)rng.NextDouble() * 2f - 1f;
                float pinkVal = pink.Next(white) * Mathf.Exp(-t * 3.5f);

                // Impact crack at t < 20ms
                float crack = (i < sampleRate * 0.02f) ? ((float)rng.NextDouble() * 2f - 1f) * Mathf.Sin((float)i / (sampleRate * 0.02f) * Mathf.PI) * 0.8f : 0f;

                float raw = subBass * 0.7f + pinkVal * 0.55f + crack * 0.4f;
                float filtered = filter.Process(raw);
                data[i] = SoftClip(filtered * 1.6f) * 0.95f;
            }

            AudioClip clip = AudioClip.Create("Proc_SmartBomb", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeWhoosh(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            var pink = new PinkNoiseFilter();
            var rng = new System.Random(42);
            var bpFilter = BiquadFilter.CreateBandpass(1200f, 2.0f, sampleRate);

            float ringPhase1 = 0f;
            float ringPhase2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Parabolic bell envelope for whoosh
                float env = Mathf.Sin(t * Mathf.PI);

                // Pink noise through bandpass
                float white = (float)rng.NextDouble() * 2f - 1f;
                float pinkVal = pink.Next(white);
                float whoosh = bpFilter.Process(pinkVal) * env;

                // Metallic deflection chime ring (2100Hz & 3150Hz)
                ringPhase1 += 2f * Mathf.PI * 2100f / sampleRate;
                ringPhase2 += 2f * Mathf.PI * 3150f / sampleRate;
                float ring = (Mathf.Sin(ringPhase1) * 0.6f + Mathf.Sin(ringPhase2) * 0.4f) * Mathf.Exp(-t * 8f) * 0.35f;

                data[i] = SoftClip((whoosh * 1.8f + ring) * 1.2f) * 0.75f;
            }

            AudioClip clip = AudioClip.Create("Proc_Whoosh", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeJetRoar(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            var pink = new PinkNoiseFilter();
            var rng = new System.Random(99);
            var lpFilter = BiquadFilter.CreateLowpass(1400f, 1.5f, sampleRate);
            float subPhase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)rng.NextDouble() * 2f - 1f;
                float pinkVal = pink.Next(white);

                // Sub-thump ignition at start
                subPhase += 2f * Mathf.PI * 60f / sampleRate;
                float subIgnition = Mathf.Sin(subPhase) * Mathf.Exp(-t * 12f) * 0.5f;

                // Sustained thrust envelope
                float thrustEnv = Mathf.Clamp01(t * 15f) * (1f - t * 0.25f);
                float raw = lpFilter.Process(pinkVal) * thrustEnv + subIgnition;

                data[i] = SoftClip(raw * 1.6f) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Proc_JetRoar", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeHiss(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            var rng = new System.Random(77);
            var hpFilter = BiquadFilter.CreateHighpass(2400f, 1.0f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)rng.NextDouble() * 2f - 1f;
                float hiss = hpFilter.Process(white) * Mathf.Exp(-t * 9.5f);
                data[i] = SoftClip(hiss * 1.4f) * 0.55f;
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
            var pink = new PinkNoiseFilter();
            var rng = new System.Random(88);
            var filter = BiquadFilter.CreateLowpass(2200f, 1.4f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Downsweep punch
                float freq = Mathf.Lerp(punchFreq, endFreq, Mathf.Pow(t, 0.4f));
                phase += 2f * Mathf.PI * freq / sampleRate;

                float subPunch = Mathf.Sin(phase) * Mathf.Exp(-t * 9f);

                // Filtered pink noise
                float white = (float)rng.NextDouble() * 2f - 1f;
                float pinkVal = pink.Next(white) * Mathf.Exp(-t * 6f);

                float raw = (subPunch * 0.55f + pinkVal * 0.65f);
                float filtered = filter.Process(raw);
                data[i] = SoftClip(filtered * 1.5f) * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_NoiseBurst", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeExplosionLarge(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phaseSub = 0f;
            var pink = new PinkNoiseFilter();
            var rng = new System.Random(456);
            var filter = BiquadFilter.CreateLowpass(1800f, 1.6f, sampleRate);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Stage 1: Initial high-frequency fracture crack (0-30ms)
                float crack = (i < sampleRate * 0.03f) ? ((float)rng.NextDouble() * 2f - 1f) * Mathf.Sin((float)i / (sampleRate * 0.03f) * Mathf.PI) * 0.9f : 0f;

                // Stage 2: Heavy sub-bass detonation (55Hz -> 20Hz)
                float subFreq = Mathf.Lerp(65f, 22f, Mathf.Pow(t, 0.5f));
                phaseSub += 2f * Mathf.PI * subFreq / sampleRate;
                float subDetonation = Mathf.Sin(phaseSub) * Mathf.Exp(-t * 3.5f);

                // Expanding heavy pink rumble
                float white = (float)rng.NextDouble() * 2f - 1f;
                float rumble = pink.Next(white) * Mathf.Exp(-t * 4.2f);

                float raw = crack * 0.4f + subDetonation * 0.65f + rumble * 0.55f;
                float filtered = filter.Process(raw);
                data[i] = SoftClip(filtered * 1.6f) * 0.9f;
            }

            AudioClip clip = AudioClip.Create("Proc_ExplosionLarge", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeShieldHit(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase1 = 0f, phase2 = 0f, noisePhase = 0f;
            var rng = new System.Random(101);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Metallic dissonance (640Hz + 1180Hz)
                phase1 += 2f * Mathf.PI * 640f / sampleRate;
                phase2 += 2f * Mathf.PI * 1180f / sampleRate;
                float metal = (Mathf.Sin(phase1) + Mathf.Sin(phase2) * 0.8f) * 0.5f;

                // Electric buzz spark
                noisePhase += 2f * Mathf.PI * 180f / sampleRate;
                float spark = ((float)rng.NextDouble() * 2f - 1f) * (Mathf.Sin(noisePhase) > 0f ? 1f : -1f) * 0.4f;

                float env = Mathf.Exp(-t * 16f);
                data[i] = SoftClip((metal + spark) * env * 1.5f) * 0.75f;
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
                float freq = (t < 0.5f) ? 920f : 690f;
                phase += 2f * Mathf.PI * freq / sampleRate;

                // Square-tinted urgent alarm tone
                float sin = Mathf.Sin(phase);
                float sqr = sin >= 0f ? 0.7f : -0.7f;
                float wave = Mathf.Lerp(sin, sqr, 0.35f);

                // Trapezoidal envelope per half
                float halfT = (t < 0.5f) ? (t / 0.5f) : ((t - 0.5f) / 0.5f);
                float env = Mathf.Sin(halfT * Mathf.PI);

                data[i] = SoftClip(wave * 1.2f) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Proc_Alarm", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeChime(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            // 4 nốt arpeggio: C6 (1046.5Hz), E6 (1318.5Hz), G6 (1567.9Hz), C7 (2093.0Hz)
            float[] freqs = new float[] { 1046.5f, 1318.5f, 1567.9f, 2093.0f };
            int stepSamples = samples / 4;

            float phaseMain = 0f;
            float phaseHarmonic = 0f;
            int currentStep = 0;

            for (int i = 0; i < samples; i++)
            {
                int step = Mathf.Min(i / stepSamples, 3);
                if (step != currentStep)
                {
                    currentStep = step;
                }

                float baseFreq = freqs[currentStep];
                phaseMain += 2f * Mathf.PI * baseFreq / sampleRate;
                phaseHarmonic += 2f * Mathf.PI * (baseFreq * 2.0f) / sampleRate;

                float noteT = (float)(i % stepSamples) / stepSamples;
                float env = Mathf.Exp(-noteT * 4.8f);

                // Pure bell chime (Fundamental + 2nd overtone)
                float chime = (Mathf.Sin(phaseMain) + Mathf.Sin(phaseHarmonic) * 0.28f);
                data[i] = SoftClip(chime * 1.1f) * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create("Proc_Chime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthesizeRadioBlip(int sampleRate, float duration)
        {
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase1 = 0f;
            float phase2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                phase1 += 2f * Mathf.PI * 2400f / sampleRate;
                phase2 += 2f * Mathf.PI * 2900f / sampleRate;

                float wave = (Mathf.Sin(phase1) + Mathf.Sin(phase2) * 0.6f);
                float env = Mathf.Sin(t * Mathf.PI);

                data[i] = SoftClip(wave * 1.3f) * env * 0.65f;
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
            var pink = new PinkNoiseFilter();
            var rng = new System.Random(55);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Doppler bell pitch curve: 1400 -> 3200 -> 1600 Hz
                float freq = 1400f + Mathf.Sin(t * Mathf.PI) * 1800f;
                phase += 2f * Mathf.PI * freq / sampleRate;

                float shimmer = Mathf.Sin(phase);
                float white = (float)rng.NextDouble() * 2f - 1f;
                float air = pink.Next(white) * 0.35f;

                float env = Mathf.Sin(t * Mathf.PI);
                data[i] = SoftClip((shimmer * 0.7f + air) * 1.3f) * env * 0.6f;
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
