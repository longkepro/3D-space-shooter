using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GameScore : MonoBehaviour
{

    [SerializeField] private Text _highScoreText;
    [SerializeField] private Text _gameScoreText;
    [SerializeField] private int _gameScore;
    [SerializeField] private int _highScore;


    private void OnEnable()
    {
        GameEventManager.OnStartGame += ResetGameScore;
        GameEventManager.OnStartGame += LoadHighScore;
        GameEventManager.OnIncrementScore += IncrementScore;
        GameEventManager.OnPlayerDestroyed += CheckNewHighScore;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= ResetGameScore;
        GameEventManager.OnStartGame -= LoadHighScore;
        GameEventManager.OnIncrementScore -= IncrementScore;
        GameEventManager.OnPlayerDestroyed -= CheckNewHighScore;
    }

    private void LoadHighScore()
    {
        _highScore = PlayerPrefs.GetInt("highScore", 0);
        DisplayHighScore();
    }

    private void CheckNewHighScore()
    {
        if (_gameScore > _highScore)
        {
            _highScore = _gameScore;
            PlayerPrefs.SetInt("highScore", _highScore);
            DisplayHighScore();
        }
    }

    private void IncrementScore(int points)
    {
        _gameScore += points;
        DisplayGameScore();
    }

    private void ResetGameScore()
    {
        _gameScore = 0;
        DisplayGameScore();
    }

    private void DisplayGameScore()
    {
        _gameScoreText.text = _gameScore.ToString();
    }

    private void DisplayHighScore()
    {
        _highScoreText.text = _highScore.ToString();
    }


}
