using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum AsteroidHazardType
{
    Destructible = 0,   // Có máu (40 HP = 2 hits), bắn nổ được, rơi vật phẩm
    Indestructible = 1  // Bất hoại (Monolith), không thể phá hủy bằng đạn thường, phải né
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Explosion))]
public class Asteroid : MonoBehaviour, VirtualRail.IPoolableEntity
{
    [Header("Hazard Configuration")]
    [SerializeField] private AsteroidHazardType _hazardType = AsteroidHazardType.Destructible;
    [SerializeField] private int _maxHealth = 40;
    [SerializeField] private int _currentHealth = 40;
    [SerializeField] private int _scoreValue = 150;
    [SerializeField] private float _ringDropChance = 0.35f;

    [Header("Scaling")]
    [SerializeField] private float _minScale = 0.8f;
    [SerializeField] private float _maxScale = 1.2f;

    public static float destructionDelay = 1f;
    public Action<Asteroid> OnRecycle;

    /// <summary>
    /// Cờ chống kích hoạt Graze lặp lại khi lướt qua tàu (TDD v2.0.0 Phần III.2).
    /// </summary>
    public bool HasGrazed { get; set; } = false;
    public AsteroidHazardType HazardType => _hazardType;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private static readonly int _colorPropId = Shader.PropertyToID("_Color");
    private float _flashTimer = 0f;
    private const float FLASH_DURATION = 0.08f;
    private Color _originalColor = Color.white;
    private bool _isFlashing = false;
    private bool _isDestroyed = false;
    private Explosion _cachedExplosion;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _cachedExplosion = GetComponent<Explosion>();

        if (_renderers != null && _renderers.Length > 0 && _renderers[0].sharedMaterial != null)
        {
            if (_renderers[0].sharedMaterial.HasProperty(_colorPropId))
            {
                _originalColor = _renderers[0].sharedMaterial.GetColor(_colorPropId);
            }
        }
    }

    private void Start()
    {
        ResetScale();
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

    public void SetHazardType(AsteroidHazardType type)
    {
        _hazardType = type;
        if (_hazardType == AsteroidHazardType.Indestructible)
        {
            // Thiên thạch bất hoại: Đổi màu sẫm ánh thép kim loại
            ApplyTint(new Color(0.25f, 0.28f, 0.35f));
        }
        else
        {
            ApplyTint(_originalColor);
        }
    }

    private void ApplyTint(Color color)
    {
        if (_renderers == null || _renderers.Length == 0) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].GetPropertyBlock(_propBlock);
            _propBlock.SetColor(_colorPropId, color);
            _renderers[i].SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Nhận sát thương từ đạn laser của người chơi.
    /// </summary>
    /// <returns>True nếu thiên thạch bị phá hủy hoàn toàn</returns>
    public bool TakeDamage(int damage, Vector3 hitPoint)
    {
        if (_isDestroyed) return true;

        if (_hazardType == AsteroidHazardType.Indestructible)
        {
            // Thiên thạch bất hoại: Nảy lửa (Ricochet Spark), không trừ máu
            TriggerHitFlash(new Color(0.8f, 0.8f, 1f));
            return false;
        }

        _currentHealth -= damage;
        TriggerHitFlash(Color.red);

        if (_currentHealth <= 0)
        {
            DestroyAsteroid();
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
        Color targetColor = (_hazardType == AsteroidHazardType.Indestructible)
            ? new Color(0.25f, 0.28f, 0.35f)
            : _originalColor;
        ApplyTint(targetColor);
    }

    private void DestroyAsteroid()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        // 1. Cộng điểm
        GameEventManager.IncrementScore(_scoreValue);
        VirtualRail.ComboScoringEngine.Instance?.RegisterSingleKill(_scoreValue);

        // 2. Xác suất rơi Vòng thu thập năng lượng (Silver, Gold, Bomb)
        if (VirtualRail.VirtualRailCollectiblePool.Instance != null && UnityEngine.Random.value <= _ringDropChance)
        {
            float roll = UnityEngine.Random.value;
            VirtualRail.RingType dropType = roll < 0.65f
                ? VirtualRail.RingType.SilverRing
                : (roll < 0.90f ? VirtualRail.RingType.GoldRing : VirtualRail.RingType.BombSupply);

            VirtualRail.VirtualRailCollectiblePool.Instance.SpawnRing(dropType, transform.position);
        }

        // 3. Kích nổ mảnh vỡ
        if (_cachedExplosion != null)
        {
            _cachedExplosion.BlowUp();
        }
        else
        {
            Recycle();
        }
    }

    public void ResetScale()
    {
        HasGrazed = false;
        _isDestroyed = false;
        _isFlashing = false;
        _currentHealth = _maxHealth;
        ResetColor();

        float min = _hazardType == AsteroidHazardType.Indestructible ? _minScale * 1.3f : _minScale;
        float max = _hazardType == AsteroidHazardType.Indestructible ? _maxScale * 1.5f : _maxScale;
        transform.localScale = GetRandomVector3(min, max);
    }

    private Vector3 GetRandomVector3(float minRange, float maxRange)
    {
        var returnValue = Vector3.zero;
        returnValue.x = Random.Range(minRange, maxRange);
        returnValue.y = Random.Range(minRange, maxRange);
        returnValue.z = Random.Range(minRange, maxRange);
        return returnValue;
    }

    public void Recycle()
    {
        HasGrazed = false;
        _isDestroyed = false;
        _isFlashing = false;
        ResetColor();
        gameObject.SetActive(false);
        OnRecycle?.Invoke(this);
    }
}