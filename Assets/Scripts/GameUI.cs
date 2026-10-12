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
        // Nếu không có UIManager (chế độ Legacy hoàn nguyên), mới hiện menu cũ
        if (VirtualRail.UIManager.Instance == null && FindAnyObjectByType<VirtualRail.UIManager>() == null)
        {
            if (_mainMenu != null) _mainMenu.SetActive(true);
            if (_gameUI != null) _gameUI.SetActive(false);
        }
        else
        {
            // Có UIManager mới: Triệt tiêu hoàn toàn UI cũ
            if (_mainMenu != null) _mainMenu.SetActive(false);
            if (_gameUI != null) _gameUI.SetActive(false);
        }
    }

    private void ShowMainMenu()
    {
        // Ở chế độ Legacy mới bật lại menu cũ sau khi nổ
        if (VirtualRail.UIManager.Instance == null && FindAnyObjectByType<VirtualRail.UIManager>() == null)
        {
            Invoke("DelayShowMainMenu", Asteroid.destructionDelay * 3f);
        }
    }

    private void DelayShowMainMenu()
    {
        if (_mainMenu != null) _mainMenu.SetActive(true);
        if (_gameUI != null) _gameUI.SetActive(false);
    }

    private void ShowGameUI()
    {
        if (_mainMenu != null) _mainMenu.SetActive(false);
        if (_gameUI != null) _gameUI.SetActive(false);

        // Khởi tạo hoặc hồi sinh tàu người chơi
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
