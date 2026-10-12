using UnityEngine;

public class Shield : MonoBehaviour
{

    [SerializeField] private int _maxHealth = 10;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _regenerationAmount = 1;
    [Tooltip("Time in seconds")][SerializeField] private float _regenerationRate = 2f;
    [Tooltip("Thời gian bất tử sau khi nhận sát thương (giây)")][SerializeField] private float _invulnerabilityDuration = 0.8f;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsInvulnerable => Time.time < _invulnerableUntil;

    private Explosion _cachedExplosion;
    private VirtualRail.VirtualRailShip _cachedShip;
    private float _nextRegenTime;
    private float _invulnerableUntil = 0f;
    private bool _wasInvulnerable = false;
    private MeshRenderer[] _shipMeshRenderers;

    private void Awake()
    {
        _cachedExplosion = GetComponent<Explosion>();
        _cachedShip = GetComponentInParent<VirtualRail.VirtualRailShip>();

        var allRenderers = GetComponentsInChildren<MeshRenderer>(true);
        var activeList = new System.Collections.Generic.List<MeshRenderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null && allRenderers[i].enabled)
            {
                activeList.Add(allRenderers[i]);
            }
        }
        _shipMeshRenderers = activeList.ToArray();
    }

    private void Start()
    {
        _currentHealth = _maxHealth;
        _nextRegenTime = Time.time + _regenerationRate;
    }

    private void Update()
    {
        if (Time.time >= _nextRegenTime)
        {
            _nextRegenTime = Time.time + _regenerationRate;
            Regenerate();
        }

        UpdateInvulnerabilityVisual();
    }

    private void UpdateInvulnerabilityVisual()
    {
        if (IsInvulnerable)
        {
            // Nhấp nháy hiển thị thân tàu (tần số 20Hz) trong thời gian bất tử
            bool visible = (Mathf.FloorToInt(Time.time * 20f) % 2) == 0;
            SetMeshRenderersVisible(visible);
            _wasInvulnerable = true;
        }
        else if (_wasInvulnerable)
        {
            SetMeshRenderersVisible(true);
            _wasInvulnerable = false;
        }
    }

    private void SetMeshRenderersVisible(bool visible)
    {
        if (_shipMeshRenderers == null) return;
        for (int i = 0; i < _shipMeshRenderers.Length; i++)
        {
            if (_shipMeshRenderers[i] != null)
            {
                _shipMeshRenderers[i].enabled = visible;
            }
        }
    }

    private void Regenerate()
    {
        if (_currentHealth < _maxHealth)
        {
            _currentHealth += _regenerationAmount;
        }
        else if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
        GameEventManager.UpdateHealthBar(Mathf.RoundToInt(_currentHealth / (float)_maxHealth * 100));
    }

    public void TriggerInvulnerability(float duration)
    {
        _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + duration);
    }

    public void TakeDamage(int damageAmount = 1, bool ignoreInvulnerability = false)
    {
        if (_currentHealth <= 0) return;
        if (IsInvulnerable && !ignoreInvulnerability) return;

        _invulnerableUntil = Time.time + _invulnerabilityDuration;
        _currentHealth -= damageAmount;
        if (_currentHealth < 0)
        {
            _currentHealth = 0;
        }
        GameEventManager.UpdateHealthBar(Mathf.RoundToInt(_currentHealth / (float)_maxHealth * 100));
        VirtualRail.ComboScoringEngine.Instance?.ResetMultiplierOnDamage();

        VirtualRail.AudioManager.Instance?.PlaySFX2D(VirtualRail.AudioSFXType.ShieldDamage, 0.85f);
        VirtualRail.VirtualRailCameraRig.TriggerShake(0.35f);

        if (_currentHealth > 0 && (_currentHealth / (float)_maxHealth) <= 0.3f)
        {
            VirtualRail.AudioManager.Instance?.PlaySFX2D(VirtualRail.AudioSFXType.ShieldAlarm, 0.75f);
        }

        if (_currentHealth < 1)
        {
            TriggerPlayerDeath();
        }
    }

    private void TriggerPlayerDeath()
    {
        _invulnerableUntil = 0f;
        SetMeshRenderersVisible(true);
        _wasInvulnerable = false;

        VirtualRail.AudioManager.Instance?.PlaySFX2D(VirtualRail.AudioSFXType.ExplosionLarge, 1.0f);
        VirtualRail.VirtualRailCameraRig.TriggerShake(0.85f);

        if (_cachedExplosion != null)
        {
            _cachedExplosion.SpawnBlowUpVFX();
        }

        if (_cachedShip != null)
        {
            _cachedShip.OnPlayerDefeated();
        }
        else
        {
            gameObject.SetActive(false);
        }

        var anchor = VirtualRail.VirtualRailAnchor.Instance;
        if (anchor != null)
        {
            anchor.CurrentSpeed = 0f;
            anchor.ClearSpeedOverride();
        }

        GameEventManager.PlayerDestroyed();
    }

    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        _nextRegenTime = Time.time + _regenerationRate;
        _invulnerableUntil = 0f;
        SetMeshRenderersVisible(true);
        _wasInvulnerable = false;
        GameEventManager.UpdateHealthBar(100);
    }

    public void Heal(int healAmount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + healAmount);
        GameEventManager.UpdateHealthBar(Mathf.RoundToInt(_currentHealth / (float)_maxHealth * 100));
    }
}
