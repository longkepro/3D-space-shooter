using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{

    [SerializeField] private float _timePassed;
    [SerializeField] private Text _timerText;
    private bool _keepTime;

    private void OnEnable()
    {
        GameEventManager.OnStartGame += StartGameTimer;
        GameEventManager.OnPlayerDestroyed += StopGameTimer;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= StartGameTimer;
        GameEventManager.OnPlayerDestroyed -= StopGameTimer;
    }

    private void Update()
    {
        if (_keepTime)
        {
            _timePassed += Time.deltaTime;
            UpdateGameTimerDisplay();
        }
    }

    private void StartGameTimer()
    {
        _timePassed = 0f;
        _keepTime = true;
    }

    private void StopGameTimer()
    {
        _keepTime = false;
    }

    private void UpdateGameTimerDisplay()
    {
        var minutes = Mathf.FloorToInt(_timePassed / 60);
        var seconds = _timePassed % 60;
        _timerText.text = string.Format("{0}:{1:00.00}", minutes, seconds);
    }

}