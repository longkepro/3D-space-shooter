using UnityEngine;

public class Shield : MonoBehaviour
{

    [SerializeField] private int _maxHealth = 10;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _regenerationAmount = 1;
    [Tooltip("Time in seconds")][SerializeField] private float _regenerationRate = 2f;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    private void Start()
    {
        _currentHealth = _maxHealth;
        InvokeRepeating("Regenerate", _regenerationRate, _regenerationRate);
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
        if (_currentHealth < 1)
        {
            TriggerPlayerDeath();
        }
    }

    private void TriggerPlayerDeath()
    {
        var exp = GetComponent<Explosion>();
        if (exp != null)
        {
            exp.SpawnBlowUpVFX();
        }

        var ship = GetComponentInParent<VirtualRail.VirtualRailShip>();
        if (ship != null)
        {
            ship.OnPlayerDefeated();
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
        CancelInvoke("Regenerate");
        _currentHealth = _maxHealth;
        GameEventManager.UpdateHealthBar(100);
        InvokeRepeating("Regenerate", _regenerationRate, _regenerationRate);
    }

    public void Heal(int healAmount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + healAmount);
        GameEventManager.UpdateHealthBar(Mathf.RoundToInt(_currentHealth / (float)_maxHealth * 100));
    }
}
