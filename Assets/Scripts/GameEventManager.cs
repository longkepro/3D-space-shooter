using UnityEngine;

public class GameEventManager : MonoBehaviour
{

    public delegate void PlayGameDelegate();
    public static PlayGameDelegate OnStartGame;
    public static PlayGameDelegate OnRespawnPickup;
    public static PlayGameDelegate OnPlayerDestroyed;

    public delegate void UpdateGameUIDelegate(int amount);
    public static UpdateGameUIDelegate OnUpdateHealthBar;
    public static UpdateGameUIDelegate OnIncrementScore;


    public static void StartGame()
    {
        if (OnStartGame != null) // Check for subscribers
        {
            OnStartGame();
        }
    }


    public static void RespawnPickup()
    {
        if (OnRespawnPickup != null) // Check for subscribers
        {
            OnRespawnPickup();
        }
    }

    public static void UpdateHealthBar(int percentHealthRemaining)
    {
        if (OnUpdateHealthBar != null) // Check for subscribers
        {
            OnUpdateHealthBar(percentHealthRemaining);
        }
    }

    public static void IncrementScore(int points)
    {
        if (OnIncrementScore != null) // Check for subscribers
        {
            OnIncrementScore(points);
        }
    }

    public static void PlayerDestroyed()
    {
        if (OnPlayerDestroyed != null) // Check for subscribers
        {
            OnPlayerDestroyed();
        }
    }


}
