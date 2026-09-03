using UnityEngine;

public class PlayGameButton : MonoBehaviour
{
    public void Click()
    {
        GameEventManager.StartGame();
    }
}
