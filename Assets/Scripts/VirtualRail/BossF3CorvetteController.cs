using System;
using UnityEngine;

namespace VirtualRail
{
    public enum BossBattlePhase
    {
        IntroBreach = 0,
        Phase1_DeckStrafe = 1,
        Phase2_ReactorTrench = 2,
        Phase3_MeltdownOverdrive = 3,
        Defeated = 4
    }

    /// <summary>
    /// Nhac truong Dieu phoi Chien ham Boss Federation Corvette F3 (Crimson Titan).
    /// Trien khai theo PHUONG AN A:
    /// - Boss bay duoi bung tau Arwing (Y = -25m, Z = 48m doc theo duong ray).
    /// - Khong can can thiep hay doi goc Camera (0 GC Alloc, Fixed 5 deg Horizon).
    /// - May trang thai tat dinh 5 pha, tich hop Hex-Shield, Flak Turrets, va Dorsal Reactor Trench.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossF3CorvetteController : MonoBehaviour
    {
        public static BossF3CorvetteController Instance { get; private set; }

        [Header("State & Health")]
        [SerializeField] private BossBattlePhase _currentPhase = BossBattlePhase.IntroBreach;
        [SerializeField] private int _maxHealth = 1000;
        [SerializeField] private int _currentHealth = 1000;
        [SerializeField] private bool _isDefeated = false;

        [Header("Spatial Positioning (Direction 1: 50% Vertical & 69.5% Horizontal)")]
        [SerializeField] private float _leadDistanceZ = 68f;
        [SerializeField] private float _baseElevationY = -22f;
        [SerializeField] private float _basePitchAngle = 7.5f;
        [SerializeField] private float _horizontalWaveAmp = 6.5f;
        [SerializeField] private float _horizontalWaveFreq = 0.45f;
        [SerializeField] private float _introStartElevationY = -65f;

        [Header("Hardpoints & Components")]
        [SerializeField] private BossHardpoint _turretLeft;
        [SerializeField] private BossHardpoint _turretRight;
        [SerializeField] private BossHardpoint _reactorTrench;
        [SerializeField] private BossHardpoint _mainBridge;
        [SerializeField] private VirtualRailHexShield _hexShield;

        [Header("Weapon Muzzles")]
        [SerializeField] private Transform[] _flakMuzzlesLeft;
        [SerializeField] private Transform[] _flakMuzzlesRight;
        [SerializeField] private Transform[] _vlsMissileSilos;
        [SerializeField] private Transform _reactorVentMuzzle;

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private GameObject _stunSparksPrefab;

        // Internal Timers & Caches (0 GC Alloc)
        private VirtualRailAnchor _anchor;
        private float _phaseTimer = 0f;
        private float _subventralY = -60f;
        private float _fireTimer = 0f;
        private float _vlsTimer = 0f;
        private float _stunTimer = 0f;
        private float _trenchChargeTimer = 0f;
        private bool _isChargingTrench = false;
        private float _hyperLaserTimer = 0f;
        private bool _isChargingHyperLaser = false;
        private float _cascadeExplosionTimer = 0f;
        private int _cascadeExplosionCount = 0;

        // Meltdown Emissive Visuals
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _emissionColorPropId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color _meltdownColor = new Color(2.5f, 0.4f, 0.05f, 1.0f);
        private static readonly Color _normalEmission = Color.black;

        [Header("Reactor Visual")]
        [SerializeField] private VirtualRailReactorTrenchVisual _reactorVisual;

        public BossBattlePhase CurrentPhase => _currentPhase;
        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;
        public float HealthNormalized => (float)_currentHealth / _maxHealth;
        public bool IsShieldOnline => (_hexShield != null && _hexShield.IsActive);
        public bool IsChargingTrench => _isChargingTrench;
        public bool IsStunned => _stunTimer > 0f;

        // Events for UI HUD (Unidirectional Data Flow)
        public static event Action<BossF3CorvetteController> OnBossSpawned;
        public static event Action<int, int, BossBattlePhase> OnBossHealthChanged;
        public static event Action<int, int> OnBossShieldChanged;
        public static event Action OnBossShieldBroken;
        public static event Action<bool> OnBossTrenchCharging;
        public static event Action<bool> OnBossHyperLaserCharging;
        public static event Action<bool> OnBossStunned;
        public static event Action OnBossDefeated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _currentHealth = _maxHealth;
            _subventralY = _introStartElevationY;

            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            AutoBindHardpoints();
        }

        private void Start()
        {
            _anchor = VirtualRailAnchor.Instance;
            if (_anchor == null)
            {
                _anchor = FindAnyObjectByType<VirtualRailAnchor>();
            }

            // Kich hoat khien chan ban dau va dang ky su kien do ben
            if (_hexShield != null)
            {
                _hexShield.OnShieldHealthChanged += HandleShieldHealthChanged;
                _hexShield.OnShieldBroken += HandleShieldBroken;
                _hexShield.SetShieldActive(true);
                OnBossShieldChanged?.Invoke(_hexShield.CurrentShieldHealth, _hexShield.MaxShieldHealth);
            }

            if (_reactorTrench != null)
            {
                _reactorTrench.SetWeakSpot(false);
                _reactorTrench.SetHardpointColliderActive(false);
            }

            OnBossSpawned?.Invoke(this);
            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth, _currentPhase);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_hexShield != null)
            {
                _hexShield.OnShieldHealthChanged -= HandleShieldHealthChanged;
                _hexShield.OnShieldBroken -= HandleShieldBroken;
            }
        }

        private void HandleShieldHealthChanged(int current, int max)
        {
            OnBossShieldChanged?.Invoke(current, max);
        }

        private void HandleShieldBroken()
        {
            OnBossShieldBroken?.Invoke();
            if (_currentPhase == BossBattlePhase.Phase1_DeckStrafe || _currentPhase == BossBattlePhase.IntroBreach)
            {
                // Đồng bộ lượng máu: Pha 1 hoàn tất -> Máu Boss tối đa còn 66% (660 HP)
                if (_currentHealth > 660)
                {
                    _currentHealth = 660;
                }
                TransitionToPhase(BossBattlePhase.Phase2_ReactorTrench);
            }
        }

        private void AutoBindHardpoints()
        {
            if (_turretLeft == null) _turretLeft = FindHardpoint(BossHardpointType.LeftFlakTurret);
            if (_turretRight == null) _turretRight = FindHardpoint(BossHardpointType.RightFlakTurret);
            if (_reactorTrench == null) _reactorTrench = FindHardpoint(BossHardpointType.DorsalReactorTrench);
            if (_mainBridge == null) _mainBridge = FindHardpoint(BossHardpointType.MainBridge);
            if (_hexShield == null) _hexShield = GetComponentInChildren<VirtualRailHexShield>(true);
            if (_reactorVisual == null)
            {
                _reactorVisual = GetComponentInChildren<VirtualRailReactorTrenchVisual>(true);
                if (_reactorVisual == null && _reactorTrench != null)
                {
                    _reactorVisual = _reactorTrench.gameObject.AddComponent<VirtualRailReactorTrenchVisual>();
                }
            }

            if (_turretLeft != null) _turretLeft.SetController(this);
            if (_turretRight != null) _turretRight.SetController(this);
            if (_reactorTrench != null) _reactorTrench.SetController(this);
            if (_mainBridge != null) _mainBridge.SetController(this);
        }

        private BossHardpoint FindHardpoint(BossHardpointType type)
        {
            var hardpoints = GetComponentsInChildren<BossHardpoint>(true);
            for (int i = 0; i < hardpoints.Length; i++)
            {
                if (hardpoints[i].HardpointType == type) return hardpoints[i];
            }
            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _phaseTimer += dt;

            // 1. Cap nhat Vi tri dong hoc theo Duong ray (Option A: Sub-Ventral)
            UpdateSpatialPosition(dt);

            // 2. May trang thai tat dinh 5 pha
            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                if (_stunTimer <= 0f)
                {
                    OnBossStunned?.Invoke(false);
                    if (_reactorVisual != null) _reactorVisual.SetStunned(false);
                }
                return; // Khi dang bi choang, tam dung moi don tan cong
            }

            switch (_currentPhase)
            {
                case BossBattlePhase.IntroBreach:
                    UpdateIntroPhase(dt);
                    break;
                case BossBattlePhase.Phase1_DeckStrafe:
                    UpdatePhase1(dt);
                    break;
                case BossBattlePhase.Phase2_ReactorTrench:
                    UpdatePhase2(dt);
                    break;
                case BossBattlePhase.Phase3_MeltdownOverdrive:
                    UpdatePhase3(dt);
                    break;
                case BossBattlePhase.Defeated:
                    UpdateDefeatedPhase(dt);
                    break;
            }
        }

        private void UpdateSpatialPosition(float dt)
        {
            if (_anchor == null)
            {
                _anchor = VirtualRailAnchor.Instance;
                if (_anchor == null) return;
            }

            Vector3 anchorPos = _anchor.transform.position;
            Vector3 anchorFwd = _anchor.ForwardTangent;
            Vector3 anchorUp = _anchor.transform.up;
            Vector3 anchorRight = _anchor.transform.right;

            // Tinh toan cao do Y tuy theo pha (Noi suy muot ma MoveTowards khong bi snap 1-frame)
            float targetBaseY = _baseElevationY;
            float waveFreq = 1.2f;

            if (_currentPhase == BossBattlePhase.IntroBreach)
            {
                targetBaseY = _baseElevationY;
                _subventralY = Mathf.MoveTowards(_subventralY, targetBaseY, dt * 14f);
                waveFreq = 0f;
            }
            else if (_currentPhase == BossBattlePhase.Phase3_MeltdownOverdrive)
            {
                // Pha 3 ap sat gan hon duoi bung (Y = -18m) - noi suy muot o toc do 3.5 m/s (~1.1s)
                targetBaseY = -18f;
                _subventralY = Mathf.MoveTowards(_subventralY, targetBaseY, dt * 3.5f);
                waveFreq = 2.5f;
            }
            else
            {
                targetBaseY = _baseElevationY;
                _subventralY = Mathf.MoveTowards(_subventralY, targetBaseY, dt * 3.5f);
                waveFreq = 1.2f;
            }

            float targetY = _subventralY + (waveFreq > 0.01f ? Mathf.Sin(_phaseTimer * waveFreq) * 2.0f : 0f);

            // Dao dong hinh sin sang 2 ben
            float waveX = Mathf.Sin(_phaseTimer * _horizontalWaveFreq) * _horizontalWaveAmp;
            float rollZ = -Mathf.Cos(_phaseTimer * _horizontalWaveFreq) * 6.0f;

            Vector3 targetWorldPos = anchorPos 
                + (anchorFwd * _leadDistanceZ) 
                + (anchorUp * targetY) 
                + (anchorRight * waveX);

            transform.position = targetWorldPos;

            // Xoay than tau: Ngoc mui nhe +7.5 deg de mui tau cham moc 50% chieu doc man hinh, them rollZ khi luon song
            Quaternion baseRot = Quaternion.LookRotation(anchorFwd, anchorUp);
            Quaternion tiltRot = Quaternion.Euler(-_basePitchAngle, 0f, rollZ);
            transform.rotation = baseRot * tiltRot;
        }

        private void UpdateIntroPhase(float dt)
        {
            if (_phaseTimer >= 3.2f)
            {
                TransitionToPhase(BossBattlePhase.Phase1_DeckStrafe);
            }
        }

        private void UpdatePhase1(float dt)
        {
            _fireTimer += dt;
            if (_fireTimer >= 1.2f)
            {
                _fireTimer = 0f;
                FireUpwardFlakBurst();
            }

            // Kiem tra ca 2 thap phao canh da bi ha chua
            bool leftDead = (_turretLeft == null || _turretLeft.IsDestroyed);
            bool rightDead = (_turretRight == null || _turretRight.IsDestroyed);

            if (leftDead && rightDead)
            {
                // Khi ca 2 thap phao canh bi ha, Khien Hex-Shield sup do vinh vien
                if (_hexShield != null && _hexShield.IsActive)
                {
                    _hexShield.BreakShield();
                }
                else
                {
                    TransitionToPhase(BossBattlePhase.Phase2_ReactorTrench);
                }
            }
        }

        private void UpdatePhase2(float dt)
        {
            // 1. Tháp pháo cánh còn sống tiếp tục bắn đạn phòng không (Flak) mỗi 1.8s
            _fireTimer += dt;
            if (_fireTimer >= 1.8f)
            {
                _fireTimer = 0f;
                FireUpwardFlakBurst();
            }

            // 2. Bắn tên lửa VLS thẳng đứng mỗi 3s
            _vlsTimer += dt;
            if (_vlsTimer >= 3.0f)
            {
                _vlsTimer = 0f;
                FireVlsMissileSalvo();
            }

            // 3. Chu kỳ sạc rãnh lò phản ứng mỗi 5s
            _trenchChargeTimer += dt;
            if (!_isChargingTrench && _trenchChargeTimer >= 5.0f)
            {
                _isChargingTrench = true;
                _trenchChargeTimer = 0f;
                OnBossTrenchCharging?.Invoke(true);
                if (_reactorTrench != null)
                {
                    _reactorTrench.SetWeakSpot(true);
                }
            }
            else if (_isChargingTrench)
            {
                float prog = Mathf.Clamp01(_trenchChargeTimer / 2.5f);
                if (_reactorVisual != null) _reactorVisual.UpdateCharging(prog);

                if (_trenchChargeTimer >= 2.5f)
                {
                    // Sạc thành công, phóng luồng nhiệt thẳng đứng
                    ExecuteTrenchThermalVent();
                    _isChargingTrench = false;
                    _trenchChargeTimer = 0f;
                    OnBossTrenchCharging?.Invoke(false);
                    if (_reactorTrench != null)
                    {
                        _reactorTrench.SetWeakSpot(false);
                    }
                }
            }

            if (HealthNormalized <= 0.35f || (_reactorTrench != null && _reactorTrench.IsDestroyed))
            {
                if (_currentHealth > 330)
                {
                    _currentHealth = 330;
                }
                TransitionToPhase(BossBattlePhase.Phase3_MeltdownOverdrive);
            }
        }

        private void UpdatePhase3(float dt)
        {
            // Hiệu ứng quá nhiệt: Nhịp thở emissive đỏ rực
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 6.0f);
            ApplyMeltdownVisual(pulse);

            // 1. Tháp pháo cánh dồn dập mỗi 0.8s
            _fireTimer += dt;
            if (_fireTimer >= 0.8f)
            {
                _fireTimer = 0f;
                FireUpwardFlakBurst();
            }

            // 2. Giàn tên lửa VLS dồn dập mỗi 2.5s
            _vlsTimer += dt;
            if (_vlsTimer >= 2.5f)
            {
                _vlsTimer = 0f;
                FireVlsMissileSalvo();
            }

            // 3. Chu kỳ phóng chùm Hyper Laser quét qua hành lang trung tâm mỗi 4.5s (báo trước 1.8s)
            _hyperLaserTimer += dt;
            if (!_isChargingHyperLaser && _hyperLaserTimer >= 4.5f)
            {
                _isChargingHyperLaser = true;
                _hyperLaserTimer = 0f;
                OnBossHyperLaserCharging?.Invoke(true);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX2D(AudioSFXType.ChargeHum, 1.0f);
                }
            }
            else if (_isChargingHyperLaser)
            {
                float prog = Mathf.Clamp01(_hyperLaserTimer / 1.8f);
                if (_reactorVisual != null) _reactorVisual.UpdateCharging(prog);

                if (_hyperLaserTimer >= 1.8f)
                {
                    ExecuteHyperLaserBarrage();
                    _isChargingHyperLaser = false;
                    _hyperLaserTimer = 0f;
                    OnBossHyperLaserCharging?.Invoke(false);
                }
            }
        }

        private void UpdateDefeatedPhase(float dt)
        {
            _cascadeExplosionTimer += dt;
            if (_cascadeExplosionTimer >= 0.18f && _cascadeExplosionCount < 14)
            {
                _cascadeExplosionTimer = 0f;
                _cascadeExplosionCount++;

                Vector3 randomOffset = transform.position + new Vector3(
                    UnityEngine.Random.Range(-30f, 30f),
                    UnityEngine.Random.Range(-5f, 8f),
                    UnityEngine.Random.Range(-35f, 35f)
                );

                if (_explosionPrefab != null)
                {
                    VirtualRailVFXPool.Instance.SpawnVFX(_explosionPrefab, randomOffset, Quaternion.identity, null, 1.5f);
                }

                VirtualRailCameraRig.TriggerShake(0.35f);
            }

            if (_phaseTimer >= 2.8f && !_isDefeated)
            {
                _isDefeated = true;
                ExecuteFinalDestruction();
            }
        }

        private void TransitionToPhase(BossBattlePhase newPhase)
        {
            _currentPhase = newPhase;
            _phaseTimer = 0f;
            _fireTimer = 0f;
            _vlsTimer = 0f;
            _hyperLaserTimer = 0f;
            _isChargingHyperLaser = false;
            _isChargingTrench = false;
            _trenchChargeTimer = 0f;

            OnBossTrenchCharging?.Invoke(false);
            OnBossHyperLaserCharging?.Invoke(false);

            if (newPhase == BossBattlePhase.Phase2_ReactorTrench)
            {
                if (_reactorTrench != null)
                {
                    _reactorTrench.SetHardpointColliderActive(true);
                }
            }
            else if (newPhase == BossBattlePhase.Phase3_MeltdownOverdrive)
            {
                if (_currentHealth > 330)
                {
                    _currentHealth = 330;
                }
                // Giữ collider rãnh lò phản ứng mở để người chơi bắn vào vùng lõi nóng chảy dứt điểm Boss
                if (_reactorTrench != null)
                {
                    _reactorTrench.SetHardpointColliderActive(true);
                }
            }

            if (_reactorVisual != null)
            {
                _reactorVisual.SetPhase(newPhase);
            }

            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth, _currentPhase);
        }

        private void FireUpwardFlakBurst()
        {
            if (EnemyProjectilePool.Instance == null) return;

            // Ban nguoc tu duoi len troi (Upward Flak) huong vao Arwing
            Vector3 upwardDir = (transform.up * 1.5f + -transform.forward * 0.8f).normalized;

            if (_turretLeft != null && !_turretLeft.IsDestroyed && _flakMuzzlesLeft != null)
            {
                for (int i = 0; i < _flakMuzzlesLeft.Length; i++)
                {
                    if (_flakMuzzlesLeft[i] != null)
                    {
                        EnemyProjectilePool.Instance.SpawnProjectile(_flakMuzzlesLeft[i].position, upwardDir, 36f, false);
                    }
                }
            }

            if (_turretRight != null && !_turretRight.IsDestroyed && _flakMuzzlesRight != null)
            {
                for (int i = 0; i < _flakMuzzlesRight.Length; i++)
                {
                    if (_flakMuzzlesRight[i] != null)
                    {
                        EnemyProjectilePool.Instance.SpawnProjectile(_flakMuzzlesRight[i].position, upwardDir, 36f, false);
                    }
                }
            }
        }

        private void FireVlsMissileSalvo()
        {
            if (_vlsMissileSilos == null || _vlsMissileSilos.Length == 0) return;
            if (EnemyProjectilePool.Instance == null) return;

            for (int i = 0; i < _vlsMissileSilos.Length; i++)
            {
                if (_vlsMissileSilos[i] != null)
                {
                    Vector3 launchDir = (transform.up * 2.2f + transform.forward * 0.5f).normalized;
                    EnemyProjectilePool.Instance.SpawnProjectile(_vlsMissileSilos[i].position, launchDir, 42f, false);
                }
            }
        }

        private void ExecuteTrenchThermalVent()
        {
            if (EnemyProjectilePool.Instance == null) return;
            Vector3 ventPos = _reactorVentMuzzle != null ? _reactorVentMuzzle.position : transform.position + transform.up * 3f;

            if (_reactorVisual != null)
            {
                _reactorVisual.TriggerVentEruption(_explosionPrefab);
            }

            // Xa chùm đạn nhiệt thẳng đứng
            for (int i = -2; i <= 2; i++)
            {
                Vector3 spreadDir = (transform.up * 2.0f + transform.right * (i * 0.35f)).normalized;
                EnemyProjectilePool.Instance.SpawnProjectile(ventPos, spreadDir, 45f, false);
            }
        }

        private void ExecuteHyperLaserBarrage()
        {
            if (EnemyProjectilePool.Instance == null) return;
            Vector3 muzzlePos = _reactorVentMuzzle != null ? _reactorVentMuzzle.position : transform.position + transform.up * 4f;

            if (_reactorVisual != null)
            {
                _reactorVisual.TriggerVentEruption(_explosionPrefab);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.LaserHyper, 1.0f);
            }

            VirtualRailCameraRig.TriggerShake(0.5f);

            // 11 tia đạn Hyper Laser quét ngang từ X = -18m đến +18m ở tốc độ 55 m/s
            for (int i = -5; i <= 5; i++)
            {
                float lateralOffset = i * 0.16f;
                Vector3 blastDir = (transform.up * 1.8f + -transform.forward * 0.6f + transform.right * lateralOffset).normalized;
                EnemyProjectilePool.Instance.SpawnProjectile(muzzlePos, blastDir, 55f, false);
            }
        }

        public void NotifyHardpointDamaged(BossHardpoint hardpoint, int damageDealt, Vector3 hitPoint)
        {
            if (_currentPhase == BossBattlePhase.Defeated) return;

            // 1. Kiểm tra nếu Khiên còn online: Mọi phát bắn vào ArmoredHull, Trench, Bridge đều được chuyển 100% vào Khiên
            if (IsShieldOnline && (hardpoint.HardpointType == BossHardpointType.ArmoredHull || 
                                   hardpoint.HardpointType == BossHardpointType.DorsalReactorTrench || 
                                   hardpoint.HardpointType == BossHardpointType.MainBridge))
            {
                if (_hexShield != null) _hexShield.TakeDamage(damageDealt);
                return;
            }

            // 2. Nếu bắn vào Rãnh Lò Phản Ứng trong lúc đang sạc ở Pha 2 -> STUN ngắt chiêu
            if (_currentPhase == BossBattlePhase.Phase2_ReactorTrench && 
                hardpoint.HardpointType == BossHardpointType.DorsalReactorTrench && 
                _isChargingTrench)
            {
                InterruptAndStun();
            }

            // 3. Khấu trừ sát thương theo từng pha (không để sát thương vượt pha khi chưa hoàn thành mục tiêu pha)
            if (_currentPhase == BossBattlePhase.Phase1_DeckStrafe || _currentPhase == BossBattlePhase.IntroBreach)
            {
                // Trong Pha 1, sát thương tháp pháo cánh không làm tụt qua mốc Pha 2 (660 HP)
                _currentHealth = Mathf.Max(660, _currentHealth - damageDealt);
            }
            else if (_currentPhase == BossBattlePhase.Phase2_ReactorTrench)
            {
                // Trong Pha 2, sát thương chỉ làm tụt tối đa đến mốc Pha 3 (330 HP)
                _currentHealth = Mathf.Max(330, _currentHealth - damageDealt);
            }
            else
            {
                // Trong Pha 3, sát thương dứt điểm Boss về 0
                _currentHealth = Mathf.Max(0, _currentHealth - damageDealt);
            }

            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth, _currentPhase);

            if (_currentHealth <= 0 && _currentPhase != BossBattlePhase.Defeated)
            {
                TransitionToPhase(BossBattlePhase.Defeated);
            }
        }

        public void NotifyHardpointDestroyed(BossHardpoint hardpoint)
        {
            VirtualRailCameraRig.TriggerShake(0.35f);

            Vector3 hpPos = (hardpoint != null) ? hardpoint.transform.position : transform.position;

            // 1. Âm thanh nổ lớn và hiệu ứng nổ tại vị trí hardpoint bị tiêu diệt
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX3D(AudioSFXType.ExplosionLarge, hpPos, 1.0f);
            }

            if (_explosionPrefab != null)
            {
                VirtualRailVFXPool.Instance.SpawnVFX(_explosionPrefab, hpPos, Quaternion.identity, null, 2.0f);
            }

            // 2. Điểm thưởng và cơ chế đặc thù cho từng loại hardpoint
            if (hardpoint != null)
            {
                switch (hardpoint.HardpointType)
                {
                    case BossHardpointType.LeftFlakTurret:
                    case BossHardpointType.RightFlakTurret:
                        GameEventManager.IncrementScore(2500);
                        break;

                    case BossHardpointType.MainBridge:
                        GameEventManager.IncrementScore(5000);
                        TriggerBridgeStun();
                        break;

                    case BossHardpointType.DorsalReactorTrench:
                        GameEventManager.IncrementScore(10000);
                        // Nếu Rãnh Lò Phản Ứng bị phá hủy ở Pha 2 -> Tự động chuyển tiếp sang Pha 3 Cuồng Nộ
                        if (_currentPhase == BossBattlePhase.Phase2_ReactorTrench)
                        {
                            if (_currentHealth > 330)
                            {
                                _currentHealth = 330;
                            }
                            TransitionToPhase(BossBattlePhase.Phase3_MeltdownOverdrive);
                        }
                        break;
                }
            }
        }

        private void TriggerBridgeStun()
        {
            _isChargingTrench = false;
            _trenchChargeTimer = 0f;
            _isChargingHyperLaser = false;
            _hyperLaserTimer = 0f;
            _stunTimer = 4.0f; // 4.0s Stun theo spec Task 2.5.2

            OnBossTrenchCharging?.Invoke(false);
            OnBossStunned?.Invoke(true);

            if (_reactorVisual != null)
            {
                _reactorVisual.SetStunned(true);
            }

            if (_reactorTrench != null)
            {
                _reactorTrench.SetWeakSpot(false);
            }

            if (_stunSparksPrefab != null)
            {
                Vector3 sparkPos = (_mainBridge != null) ? _mainBridge.transform.position : transform.position + transform.up * 4f;
                VirtualRailVFXPool.Instance.SpawnVFX(_stunSparksPrefab, sparkPos, Quaternion.identity, null, 4.0f);
            }

            VirtualRailCameraRig.TriggerShake(0.45f);
        }

        private void InterruptAndStun()
        {
            _isChargingTrench = false;
            _trenchChargeTimer = 0f;
            _stunTimer = 2.0f;
            OnBossTrenchCharging?.Invoke(false);
            OnBossStunned?.Invoke(true);

            if (_reactorVisual != null)
            {
                _reactorVisual.SetStunned(true);
            }

            if (_reactorTrench != null)
            {
                _reactorTrench.SetWeakSpot(false);
            }

            if (_stunSparksPrefab != null)
            {
                Vector3 sparkPos = transform.position + transform.up * 4f;
                VirtualRailVFXPool.Instance.SpawnVFX(_stunSparksPrefab, sparkPos, Quaternion.identity, null, 2.0f);
            }

            VirtualRailCameraRig.TriggerShake(0.4f);
        }

        private void ApplyMeltdownVisual(float intensity)
        {
            if (_renderers == null) return;
            Color emit = _meltdownColor * intensity;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(_emissionColorPropId, emit);
                    _renderers[i].SetPropertyBlock(_propBlock);
                }
            }
        }

        private void ExecuteFinalDestruction()
        {
            OnBossDefeated?.Invoke();
            GameEventManager.IncrementScore(50000);

            // Thông báo cho Endless Director để nâng bậc DDA Threat Level và chuyển sang Breathing Room
            if (VirtualRailEndlessDirector.Instance != null)
            {
                VirtualRailEndlessDirector.Instance.OnBossIncursionDefeated();
            }

            // Xoa sach toan bo dan dich dang bay
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.ClearAllActiveProjectiles();
            }

            VirtualRailCameraRig.TriggerShake(1.0f);
            Destroy(gameObject);
        }
    }
}
