using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] private GameObject _enemyPrefab;
    [Tooltip("Time between spawns in seconds")][SerializeField] private float _spawnTime = 5f;

    private void OnEnable()
    {
        GameEventManager.OnStartGame += StartSpawning;
        GameEventManager.OnPlayerDestroyed += StopSpawning;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= StartSpawning;
        GameEventManager.OnPlayerDestroyed += StopSpawning;
    }

    private void SpawnEnemy()
    {
        Instantiate(_enemyPrefab, transform.position, Quaternion.identity, transform);
    }

    private void StartSpawning()
    {
        InvokeRepeating("SpawnEnemy", _spawnTime, _spawnTime);
    }

    private void StopSpawning()
    {
        CancelInvoke();
    }

}
