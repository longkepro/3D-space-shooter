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

        [Header("Formation Shield Mechanics")]
        [SerializeField] private bool _isShieldActive = false;
        [SerializeField] private VirtualRailHexShield _hexShield;
        private GameObject _shieldVisualObj;
        private Renderer _shieldRenderer;
        private MaterialPropertyBlock _shieldPropBlock;
        private float _shieldFlashTimer = 0f;
        private bool _isShieldFlashing = false;
        private static readonly Color _shieldNormalColor = new Color(0.1f, 0.75f, 1.0f, 0.65f); // Xanh cyan neon
        private static readonly Color _shieldHitColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);    // Trắng chớp sáng khi dội đạn

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _colorPropId = Shader.PropertyToID("_Color");
        private static readonly int _hitFlashPropId = Shader.PropertyToID("_HitFlash");
        private float _flashTimer = 0f;
        private const float FLASH_DURATION = 0.08f;
        private Color _originalColor = Color.white;
        private bool _isFlashing = false;
        private float _invulnerableTimer = 0f;

        private EnemyMovement _movement;
        private FormationWingman _wingman;

        public EnemyData Data => _data;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _data != null ? _data.maxHealth : 20;
        public bool IsDead => _isDead;
        public bool IsShieldActive => _isShieldActive;

        public event Action<EnemyHealth> OnDied;
        public event Action<int, Vector3, bool> OnDamaged; // (damageDealt, hitPoint, isWeakSpot)

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _shieldPropBlock = new MaterialPropertyBlock();
            _movement = GetComponent<EnemyMovement>();
            _wingman = GetComponent<FormationWingman>();
            if (_hexShield == null) _hexShield = GetComponentInChildren<VirtualRailHexShield>(true);
            if (_hexShield != null)
            {
                _hexShield.SetShieldActive(_isShieldActive);
            }

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

        public void SetShieldActive(bool active)
        {
            _isShieldActive = active;
            if (_hexShield == null) _hexShield = GetComponentInChildren<VirtualRailHexShield>(true);

            if (_hexShield != null)
            {
                _hexShield.SetShieldActive(active);
            }
            else if (active)
            {
                EnsureShieldVisual();
                if (_shieldVisualObj != null)
                {
                    _shieldVisualObj.SetActive(true);
                    ResetShieldColor();
                }
            }
            else
            {
                if (_shieldVisualObj != null)
                {
                    _shieldVisualObj.SetActive(false);
                }
                _isShieldFlashing = false;
            }

            if (active)
            {
                Debug.Log($"<color=cyan><b>[SHIELD ONLINE]</b></color> Lá chắn của {name} đã KÍCH HOẠT (Liên kết Tàu Chỉ Huy)!");
            }
        }

        private static Mesh _sharedQuadMesh;
        private static Material _sharedShieldMat;

        private static Mesh GetOrCreateQuadMesh()
        {
            if (_sharedQuadMesh != null) return _sharedQuadMesh;
            _sharedQuadMesh = new Mesh
            {
                name = "Procedural_ShieldQuad",
                vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new int[] { 0, 2, 1, 2, 3, 1 }
            };
            _sharedQuadMesh.RecalculateNormals();
            return _sharedQuadMesh;
        }

        private static Material GetOrCreateShieldMaterial()
        {
            if (_sharedShieldMat != null) return _sharedShieldMat;
#if UNITY_EDITOR
            _sharedShieldMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexShield_ElectricCyan.mat");
#endif
            if (_sharedShieldMat == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) _sharedShieldMat = new Material(shader);
            }
            return _sharedShieldMat;
        }

        private void EnsureShieldVisual()
        {
            if (_hexShield != null) return;
            if (_shieldVisualObj != null) return;

            Transform existing = transform.Find("[Shield_Barrier]");
            if (existing != null)
            {
                _shieldVisualObj = existing.gameObject;
                _shieldRenderer = _shieldVisualObj.GetComponent<Renderer>();
                _hexShield = _shieldVisualObj.GetComponent<VirtualRailHexShield>();
                if (_hexShield == null) _hexShield = _shieldVisualObj.AddComponent<VirtualRailHexShield>();
                return;
            }

            _shieldVisualObj = new GameObject("[Shield_Barrier]");
            _shieldVisualObj.transform.SetParent(transform, false);
            _shieldVisualObj.transform.localPosition = new Vector3(0f, 0.25f, 3.10f);
            _shieldVisualObj.transform.localRotation = Quaternion.identity;
            _shieldVisualObj.transform.localScale = new Vector3(5.6f, 3.2f, 1.0f);

            var mf = _shieldVisualObj.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrCreateQuadMesh();

            var mr = _shieldVisualObj.AddComponent<MeshRenderer>();
            _shieldRenderer = mr;
            _shieldRenderer.sharedMaterial = GetOrCreateShieldMaterial();
            _shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _shieldRenderer.receiveShadows = false;

            var box = _shieldVisualObj.AddComponent<BoxCollider>();
            box.size = new Vector3(1.0f, 1.0f, 0.2f);
            box.center = Vector3.zero;
            _shieldVisualObj.tag = "Enemy";

            _hexShield = _shieldVisualObj.AddComponent<VirtualRailHexShield>();
            _shieldVisualObj.SetActive(false);
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

            if (_isShieldFlashing)
            {
                _shieldFlashTimer -= Time.deltaTime;
                if (_shieldFlashTimer <= 0f)
                {
                    _isShieldFlashing = false;
                    ResetShieldColor();
                }
                else if (_shieldRenderer != null)
                {
                    float flashRatio = _shieldFlashTimer / FLASH_DURATION;
                    _shieldRenderer.GetPropertyBlock(_shieldPropBlock);
                    _shieldPropBlock.SetFloat(_hitFlashPropId, flashRatio);
                    _shieldRenderer.SetPropertyBlock(_shieldPropBlock);
                }
            }

            if (_invulnerableTimer > 0f)
            {
                _invulnerableTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Kích hoạt cửa sổ bất hoại ngắn (Grace Period) để tránh bị nổ lan chết oan khi Tàu Chỉ Huy nổ cạnh bên.
        /// </summary>
        public void TriggerInvulnerability(float duration)
        {
            _invulnerableTimer = Mathf.Max(_invulnerableTimer, duration);
        }

        private void TriggerShieldFlash()
        {
            if (_hexShield != null)
            {
                _hexShield.TriggerHitFlash();
                return;
            }

            if (_shieldRenderer == null) return;
            _isShieldFlashing = true;
            _shieldFlashTimer = FLASH_DURATION;
            _shieldRenderer.GetPropertyBlock(_shieldPropBlock);
            _shieldPropBlock.SetColor(_colorPropId, _shieldHitColor);
            _shieldPropBlock.SetFloat(_hitFlashPropId, 1.0f);
            _shieldRenderer.SetPropertyBlock(_shieldPropBlock);
        }

        private void ResetShieldColor()
        {
            if (_shieldRenderer == null) return;
            _shieldRenderer.GetPropertyBlock(_shieldPropBlock);
            _shieldPropBlock.SetColor(_colorPropId, _shieldNormalColor);
            _shieldPropBlock.SetFloat(_hitFlashPropId, 0.0f);
            _shieldRenderer.SetPropertyBlock(_shieldPropBlock);
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
            if (_invulnerableTimer > 0f) return false;

            // 1. Kiểm tra lá chắn năng lượng liên kết Chỉ Huy
            if (_isShieldActive)
            {
                TriggerShieldFlash();
                Debug.Log($"<color=cyan><b>[SHIELD DEFLECT]</b></color> Lá chắn của {name} đã cản phá 100% đạn laser! Hãy tiêu diệt Tàu Chỉ Huy!");
                return false;
            }

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

            SetShieldActive(false);
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
            _invulnerableTimer = 0f;
            SetShieldActive(false);
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
