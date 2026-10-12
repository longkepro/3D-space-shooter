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

            // Tinh toan cao do Y tuy theo pha
            float targetY = _baseElevationY;
            if (_currentPhase == BossBattlePhase.IntroBreach)
            {
                _subventralY = Mathf.MoveTowards(_subventralY, _baseElevationY, dt * 14f);
                targetY = _subventralY;
            }
            else if (_currentPhase == BossBattlePhase.Phase3_MeltdownOverdrive)
            {
                // Pha 3 ap sat gan hon duoi bung (Y = -18m cho kích thước 1.55x)
                targetY = -18f + Mathf.Sin(_phaseTimer * 2.5f) * 2.0f;
            }
            else
            {
                targetY = _baseElevationY + Mathf.Sin(_phaseTimer * 1.2f) * 2.0f;
            }

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
            // Ban ten lua VLS thang dung moi 3s
            _vlsTimer += dt;
            if (_vlsTimer >= 3.0f)
            {
                _vlsTimer = 0f;
                FireVlsMissileSalvo();
            }

            // Chu ky sac ranh lo phan ung moi 5s
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
                    // Sac thanh cong, phong luong nhiet thang dung
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

            if (HealthNormalized <= 0.35f)
            {
                TransitionToPhase(BossBattlePhase.Phase3_MeltdownOverdrive);
            }
        }

        private void UpdatePhase3(float dt)
        {
            // Hieu ung qua nhiet: Nhip tho emissive do ruc
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 6.0f);
            ApplyMeltdownVisual(pulse);

            _fireTimer += dt;
            if (_fireTimer >= 0.8f)
            {
                _fireTimer = 0f;
                FireUpwardFlakBurst();
            }

            _vlsTimer += dt;
            if (_vlsTimer >= 2.5f)
            {
                _vlsTimer = 0f;
                FireVlsMissileSalvo();
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

            if (newPhase == BossBattlePhase.Phase2_ReactorTrench)
            {
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

        public void NotifyHardpointDamaged(BossHardpoint hardpoint, int damageDealt, Vector3 hitPoint)
        {
            if (_currentPhase == BossBattlePhase.Defeated) return;

            // Kiem tra neu Khien con online va ban vao than chinh thi chuyen sat thuong vao khien
            if (hardpoint.HardpointType == BossHardpointType.ArmoredHull && IsShieldOnline)
            {
                if (_hexShield != null) _hexShield.TakeDamage(damageDealt);
                return;
            }

            // Neu ban vao Ranh Lo Phan Ung trong luc dang sac -> STUN ngat chieu
            if (hardpoint.HardpointType == BossHardpointType.DorsalReactorTrench && _isChargingTrench)
            {
                InterruptAndStun();
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damageDealt);
            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth, _currentPhase);

            if (_currentHealth <= 0 && _currentPhase != BossBattlePhase.Defeated)
            {
                TransitionToPhase(BossBattlePhase.Defeated);
            }
        }

        public void NotifyHardpointDestroyed(BossHardpoint hardpoint)
        {
            VirtualRailCameraRig.TriggerShake(0.25f);
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
