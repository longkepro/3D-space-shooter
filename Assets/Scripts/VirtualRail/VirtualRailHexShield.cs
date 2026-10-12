using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Component module hóa quản lý Tấm Khiên Năng Lượng Lưới Lục Giác (Procedural Hex Shield Barrier).
    /// Chuyên trách:
    /// 1. Vòng đời kích hoạt / tắt / quá tải vỡ khiên.
    /// 2. Quản lý độ bền/thanh máu riêng của Khiên (_maxShieldHealth, _currentShieldHealth).
    /// 3. Điều phối phản hồi thị giác chớp sáng trúng đạn (_HitFlash) bằng MaterialPropertyBlock.
    /// 4. Tuyệt đối 0 GC Allocation trong vòng lặp Update().
    /// 5. Đảm bảo hiển thị trường lực cự ly xa, không bị Small Mesh Culling ẩn mất.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailHexShield : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool _isActive = true;

        [Header("Shield Durability")]
        [SerializeField] private int _maxShieldHealth = 600;
        [SerializeField] private int _currentShieldHealth = 600;
        [SerializeField] private GameObject _overloadVfxPrefab;

        [Header("References")]
        [SerializeField] private Renderer _shieldRenderer;
        [SerializeField] private Collider _shieldCollider;

        [Header("Visual Feedback Settings")]
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private Color _normalHitColor = new Color(2.0f, 2.0f, 2.0f, 1.0f);

        private MaterialPropertyBlock _propBlock;
        private static readonly int _hitFlashPropId = Shader.PropertyToID("_HitFlash");
        private static readonly int _hitColorPropId = Shader.PropertyToID("_HitColor");

        private float _flashTimer = 0f;
        private bool _isFlashing = false;

        public bool IsActive => _isActive;
        public Collider ShieldCollider => _shieldCollider;
        public int MaxShieldHealth => _maxShieldHealth;
        public int CurrentShieldHealth => _currentShieldHealth;
        public float ShieldHealthRatio => _maxShieldHealth > 0 ? (float)_currentShieldHealth / _maxShieldHealth : 0f;

        public event Action<int, int> OnShieldHealthChanged;
        public event Action OnShieldBroken;

        private void Awake()
        {
            if (_shieldRenderer == null)
            {
                _shieldRenderer = GetComponent<Renderer>();
            }
            if (_shieldCollider == null)
            {
                _shieldCollider = GetComponent<Collider>();
            }

            // Tắt culling động để đảm bảo tấm khiên không bị engine ẩn đi khi ở xa
            if (_shieldRenderer != null)
            {
                _shieldRenderer.allowOcclusionWhenDynamic = false;
                _shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _shieldRenderer.receiveShadows = false;
            }

            _propBlock = new MaterialPropertyBlock();
            _currentShieldHealth = _maxShieldHealth;
            ApplyActiveState();
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
                ResetFlash();
            }
            else if (_shieldRenderer != null)
            {
                float ratio = _flashTimer / _flashDuration;
                _shieldRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(_hitFlashPropId, ratio);
                _shieldRenderer.SetPropertyBlock(_propBlock);
            }
        }

        public void SetShieldActive(bool active)
        {
            _isActive = active;
            if (active && _currentShieldHealth <= 0)
            {
                _currentShieldHealth = _maxShieldHealth;
                OnShieldHealthChanged?.Invoke(_currentShieldHealth, _maxShieldHealth);
            }
            ApplyActiveState();
        }

        /// <summary>
        /// Thiết lập lượng máu tối đa mới cho Khiên (phục vụ DDA leo thang độ khó).
        /// </summary>
        public void SetMaxHealth(int maxHealth)
        {
            _maxShieldHealth = Mathf.Max(1, maxHealth);
            _currentShieldHealth = _maxShieldHealth;
            _isActive = true;
            ApplyActiveState();
            OnShieldHealthChanged?.Invoke(_currentShieldHealth, _maxShieldHealth);
        }

        private void ApplyActiveState()
        {
            if (_shieldCollider != null)
            {
                _shieldCollider.enabled = _isActive;
            }
            if (_shieldRenderer != null)
            {
                _shieldRenderer.enabled = _isActive;
            }
            if (!_isActive)
            {
                _isFlashing = false;
            }
        }

        /// <summary>
        /// Nhận sát thương trực tiếp vào lá chắn năng lượng (0 GC Alloc).
        /// Trả về true nếu đòn đánh này làm khiên quá tải vỡ nát.
        /// </summary>
        public bool TakeDamage(int damage)
        {
            if (!_isActive) return false;

            TriggerHitFlash();

            _currentShieldHealth = Mathf.Max(0, _currentShieldHealth - damage);
            OnShieldHealthChanged?.Invoke(_currentShieldHealth, _maxShieldHealth);

            if (_currentShieldHealth <= 0)
            {
                BreakShield();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Quá tải sập khiên: Phát âm thanh báo động/vỡ khiên, rung chấn và chuyển pha Boss.
        /// </summary>
        public void BreakShield()
        {
            if (!_isActive) return;

            _currentShieldHealth = 0;
            SetShieldActive(false);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.ShieldAlarm, 1.0f);
                AudioManager.Instance.PlaySFX2D(AudioSFXType.ExplosionLarge, 0.9f);
            }

            VirtualRailCameraRig.TriggerShake(0.25f);

            if (_overloadVfxPrefab != null && VirtualRailVFXPool.Instance != null)
            {
                VirtualRailVFXPool.Instance.SpawnVFX(_overloadVfxPrefab, transform.position, Quaternion.identity, null, 2.5f);
            }

            OnShieldBroken?.Invoke();
        }

        public void TriggerHitFlash()
        {
            if (!_isActive || _shieldRenderer == null) return;

            _isFlashing = true;
            _flashTimer = _flashDuration;

            _shieldRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_hitFlashPropId, 1.0f);
            _propBlock.SetColor(_hitColorPropId, _normalHitColor);
            _shieldRenderer.SetPropertyBlock(_propBlock);
        }

        private void ResetFlash()
        {
            if (_shieldRenderer == null) return;

            _shieldRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_hitFlashPropId, 0.0f);
            _shieldRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
