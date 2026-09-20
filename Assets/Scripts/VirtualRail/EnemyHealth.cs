using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bộ quản lý Máu, Giáp, Điểm yếu và Sát thương cho Kẻ địch theo từng Archetype.
    /// Tích hợp 0 GC Alloc, phản hồi thị giác trúng đòn (Hit Flash) và cơ chế Điểm yếu sau đuôi.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour, IPoolableEntity
    {
        [Header("Configuration")]
        [SerializeField] private EnemyData _data;
        [SerializeField] private int _currentHealth;
        [SerializeField] private bool _isDead = false;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _colorPropId = Shader.PropertyToID("_Color");
        private float _flashTimer = 0f;
        private const float FLASH_DURATION = 0.08f;
        private Color _originalColor = Color.white;
        private bool _isFlashing = false;

        private EnemyMovement _movement;
        private FormationWingman _wingman;

        public EnemyData Data => _data;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _data != null ? _data.maxHealth : 20;
        public bool IsDead => _isDead;

        public event Action<EnemyHealth> OnDied;
        public event Action<int, Vector3, bool> OnDamaged; // (damageDealt, hitPoint, isWeakSpot)

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _movement = GetComponent<EnemyMovement>();
            _wingman = GetComponent<FormationWingman>();

            if (_renderers != null && _renderers.Length > 0 && _renderers[0].sharedMaterial != null)
            {
                if (_renderers[0].sharedMaterial.HasProperty(_colorPropId))
                {
                    _originalColor = _renderers[0].sharedMaterial.GetColor(_colorPropId);
                }
            }

            if (_data == null)
            {
                // Mặc định là Scout nếu chưa được gán Data
                ApplyData(EnemyData.CreateDefaultPreset(EnemyArchetype.Scout));
            }
        }

        public void ApplyData(EnemyData data)
        {
            _data = data;
            if (_data != null)
            {
                _currentHealth = _data.maxHealth;
            }
            _isDead = false;
        }

        private void Update()
        {
            if (_isFlashing)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    _isFlashing = false;
                    ResetColor();
                }
            }
        }

        /// <summary>
        /// Nhận sát thương từ đạn của người chơi (Laser, Charge Shot, Smart Bomb).
        /// </summary>
        /// <param name="rawDamage">Sát thương cơ bản</param>
        /// <param name="hitPoint">Vị trí trúng đạn</param>
        /// <param name="hitDirection">Hướng bay của đạn</param>
        /// <returns>True nếu kẻ địch bị tiêu diệt</returns>
        public bool TakeDamage(int rawDamage, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (_isDead) return true;

            int finalDamage = rawDamage;
            bool isWeakSpot = false;

            // Kiểm tra điểm yếu phía sau (TDD Phần II.1: Rear Weakspot 200% Damage)
            if (_data != null && _data.hasRearWeakSpot)
            {
                // Nếu đạn bay cùng hướng với mặt trước tàu địch (tức bắn từ sau tới)
                float alignment = Vector3.Dot(hitDirection.normalized, transform.forward);
                if (alignment > 0.25f)
                {
                    isWeakSpot = true;
                    finalDamage = Mathf.RoundToInt(rawDamage * _data.weakSpotMultiplier);
                    Debug.Log($"<color=yellow><b>[CRITICAL HIT!]</b></color> Trúng điểm yếu sau đuôi {name}! Sát thương: {finalDamage}");
                }
            }

            _currentHealth -= finalDamage;
            OnDamaged?.Invoke(finalDamage, hitPoint, isWeakSpot);

            // Hiệu ứng chớp sáng khi trúng đạn (Hit Flash - 0 GC)
            TriggerHitFlash(isWeakSpot ? Color.yellow : Color.red);

            if (_currentHealth <= 0)
            {
                Die();
                return true;
            }

            return false;
        }

        private void TriggerHitFlash(Color flashColor)
        {
            if (_renderers == null || _renderers.Length == 0) return;

            _isFlashing = true;
            _flashTimer = FLASH_DURATION;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor(_colorPropId, flashColor);
                _renderers[i].SetPropertyBlock(_propBlock);
            }
        }

        private void ResetColor()
        {
            if (_renderers == null || _renderers.Length == 0) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor(_colorPropId, _originalColor);
                _renderers[i].SetPropertyBlock(_propBlock);
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            ResetColor();

            OnDied?.Invoke(this);

            if (_movement != null)
            {
                _movement.BlowUp();
            }
        }

        public void Recycle()
        {
            ResetState();
        }

        public void ResetState()
        {
            _isDead = false;
            _isFlashing = false;
            ResetColor();
            if (_data != null)
            {
                _currentHealth = _data.maxHealth;
            }
            else
            {
                _currentHealth = 20;
            }
        }
    }
}
