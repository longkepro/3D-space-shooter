using System.Collections;
using UnityEngine;

public class GameUI : MonoBehaviour
{

    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _gameUI;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _playerSpawner;

    private void OnEnable()
    {
        GameEventManager.OnStartGame += ShowGameUI;
        GameEventManager.OnPlayerDestroyed += ShowMainMenu;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= ShowGameUI;
        GameEventManager.OnPlayerDestroyed -= ShowMainMenu;
    }

    private void Start()
    {
        DelayShowMainMenu();
    }

    private void ShowMainMenu()
    {
        Invoke("DelayShowMainMenu", Asteroid.destructionDelay * 3f);
    }

    private void DelayShowMainMenu()
    {
        _mainMenu.SetActive(true);
        _gameUI.SetActive(false);
    }

    private void ShowGameUI()
    {
        _mainMenu.SetActive(false);

        // Chặn hoàn toàn Game UI cũ khi ModernCockpitHUD đang hiện diện
        bool hasModernHUD = (VirtualRail.ModernCockpitHUD.Instance != null || FindAnyObjectByType<VirtualRail.ModernCockpitHUD>() != null);
        if (_gameUI != null)
        {
            _gameUI.SetActive(!hasModernHUD);
        }

        if (VirtualRail.VirtualRailAnchor.Instance != null)
        {
            Vector3 spawnPos = (_playerSpawner != null) ? _playerSpawner.transform.position : Vector3.zero;
            Quaternion spawnRot = (_playerSpawner != null) ? _playerSpawner.transform.rotation : Quaternion.identity;
            VirtualRail.VirtualRailAnchor.Instance.Respawn(spawnPos, spawnRot);
        }
        else if (_playerPrefab != null && _playerSpawner != null)
        {
            Instantiate(_playerPrefab, _playerSpawner.transform.position, _playerSpawner.transform.rotation);
        }
    }

}
