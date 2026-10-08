using UnityEngine;
using UnityEngine.UI;

public class ShieldUI : MonoBehaviour
{
    [SerializeField] private RectTransform _healthBar;
    [SerializeField] private Image _healthBarImage;

    private float _maxWidth;
    private int _currentHealthPercent = 100;
    private Color _originalColor = Color.green;

    private void OnEnable()
    {
        GameEventManager.OnUpdateHealthBar += UpdateHealthBar;
    }

    private void OnDisable()
    {
        GameEventManager.OnUpdateHealthBar -= UpdateHealthBar;
    }

    private void Awake()
    {
        if (_healthBar != null)
        {
            _maxWidth = _healthBar.rect.width;
            if (_healthBarImage == null) _healthBarImage = _healthBar.GetComponent<Image>();
            if (_healthBarImage != null) _originalColor = _healthBarImage.color;
        }
    }

    private void Update()
    {
        if (_healthBarImage != null && _currentHealthPercent <= 30 && _currentHealthPercent > 0)
        {
            float pulse = Mathf.PingPong(Time.time * 5f, 1f);
            _healthBarImage.color = Color.Lerp(Color.red, Color.yellow, pulse);
        }
    }

    private void UpdateHealthBar(int percentHealthRemaining)
    {
        _currentHealthPercent = percentHealthRemaining;
        if (_healthBar != null)
        {
            _healthBar.sizeDelta = new Vector2(Mathf.RoundToInt(_maxWidth * (percentHealthRemaining / 100f)), _healthBar.rect.height);
        }

        if (_healthBarImage != null && _currentHealthPercent > 30)
        {
            _healthBarImage.color = _originalColor;
        }
    }
}
