using UnityEngine;

public class Shield : MonoBehaviour
{

    [SerializeField] private int _maxHealth = 10;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _regenerationAmount = 1;
    [Tooltip("Time in seconds")][SerializeField] private float _regenerationRate = 2f;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    private Explosion _cachedExplosion;
    private VirtualRail.VirtualRailShip _cachedShip;
    private float _nextRegenTime;

    private void Awake()
    {
        _cachedExplosion = GetComponent<Explosion>();
        _cachedShip = GetComponentInParent<VirtualRail.VirtualRailShip>();
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

    public void TakeDamage(int damageAmount = 1)
    {
        if (_currentHealth <= 0) return;

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
        GameEventManager.UpdateHealthBar(100);
    }

    public void Heal(int healAmount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + healAmount);
        GameEventManager.UpdateHealthBar(Mathf.RoundToInt(_currentHealth / (float)_maxHealth * 100));
    }
}
