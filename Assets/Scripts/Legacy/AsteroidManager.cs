using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

public class AsteroidManager : MonoBehaviour
{
    [SerializeField] private Asteroid _asteroidPrefab;
    [SerializeField] private GameObject _pickupPrefab;
    [SerializeField] private int _asteroidsPerAxis = 10;
    [SerializeField] private int _gridSpacing = 10;

    private List<Asteroid> _asteroidList = new List<Asteroid>();
    private ObjectPool<Asteroid> _asteroidPool;

    private void Awake()
    {
        _asteroidPool = new ObjectPool<Asteroid>(
            createFunc: () => Instantiate(_asteroidPrefab, transform),
            actionOnGet: (asteroid) => asteroid.gameObject.SetActive(true),
            actionOnRelease: (asteroid) => asteroid.gameObject.SetActive(false),
            actionOnDestroy: (asteroid) => Destroy(asteroid.gameObject),
            collectionCheck: false,
            defaultCapacity: 1000,
            maxSize: 2000
        );
    }

    private void OnEnable()
    {
        GameEventManager.OnStartGame += PlaceAsteroids;
        GameEventManager.OnRespawnPickup += SpawnPickup;
        GameEventManager.OnPlayerDestroyed += DestroyAsteroids;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= PlaceAsteroids;
        GameEventManager.OnRespawnPickup -= SpawnPickup;
        GameEventManager.OnPlayerDestroyed -= DestroyAsteroids;
    }

    private void PlaceAsteroids()
    {
        return; // TAM THOI TAT THIEN THACH
        for (var x = 0; x < _asteroidsPerAxis; x++)
        {
            for (var y = 0; y < _asteroidsPerAxis; y++)
            {
                for (var z = 0; z < _asteroidsPerAxis; z++)
                {
                    var xPos = transform.position.x + (x * _gridSpacing) + RandomGridOffset();
                    var yPos = transform.position.y + (y * _gridSpacing) + RandomGridOffset();
                    var zPos = transform.position.z + (z * _gridSpacing) + RandomGridOffset();
                    var name = string.Format("Asteroid ({0},{1},{2})", x, y, z);
                    SpawnAsteroid(name, new Vector3(xPos, yPos, zPos));
                }
            }
        }
        SpawnPickup();
    }

    private float RandomGridOffset()
    {
        return Random.Range(-_gridSpacing / 2f, _gridSpacing / 2f);
    }

    private void SpawnAsteroid(string name, Vector3 asteroidPosition)
    {
        var spawnedAsteroid = _asteroidPool.Get();
        spawnedAsteroid.transform.position = asteroidPosition;
        spawnedAsteroid.transform.rotation = Quaternion.identity;
        spawnedAsteroid.name = name;
        _asteroidList.Add(spawnedAsteroid);
        
        // Reset scale explicitly because the old code relied on Start()
        spawnedAsteroid.ResetScale();
    }

    private void SpawnPickup()
    {
        if (_asteroidList.Count == 0) return;
        
        var randomAsteroid = _asteroidList[Random.Range(0, _asteroidList.Count)];
        var spawnedPickup = Instantiate(_pickupPrefab, randomAsteroid.transform.position, Quaternion.identity, transform);
        spawnedPickup.name = randomAsteroid.name.Replace("Asteroid", "Pickup");
        
        _asteroidList.Remove(randomAsteroid);
        _asteroidPool.Release(randomAsteroid);
    }

    private void DestroyAsteroids()
    {
        foreach (var asteroid in _asteroidList)
        {
            _asteroidPool.Release(asteroid);
        }
        _asteroidList.Clear();
    }
}
