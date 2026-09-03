using UnityEngine;

public class ShieldUI : MonoBehaviour
{

    [SerializeField] private RectTransform _healthBar;

    private float _maxWidth;

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
        _maxWidth = _healthBar.rect.width;
    }

    private void UpdateHealthBar(int percentHealthRemaining)
    {
        _healthBar.sizeDelta = new Vector2(Mathf.RoundToInt(_maxWidth * (percentHealthRemaining / 100f)), _healthBar.rect.height);
    }
}
