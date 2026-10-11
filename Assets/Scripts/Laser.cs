using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
[RequireComponent(typeof(LineRenderer))]
public class Laser : MonoBehaviour
{

    [Tooltip("Time in seconds")][SerializeField] private float _laserDuration = 0.08f;
    [SerializeField] private float _laserDistance = 300f;
    [SerializeField] private float _fireDelay = 0.12f;
    private LineRenderer _laserBeam;
    private Light _laserLight;
    private bool _canFire = true;
    private float _nextFireTime;

    private void Awake()
    {
        _laserBeam = GetComponent<LineRenderer>();
        _laserLight = GetComponent<Light>();
    }

    private void Start()
    {
        _laserBeam.enabled = false;
        _laserLight.enabled = false;
        _canFire = true;
    }

    private void Update()
    {
        if (!_canFire && Time.time >= _nextFireTime)
        {
            _canFire = true;
        }
    }

    private Vector3 CastRay()
    {
        Vector3 laserDirection = transform.TransformDirection(Vector3.forward) * _laserDistance;
        if (Physics.Raycast(base.transform.position, laserDirection, out RaycastHit hit))
        {
            Debug.Log("Raycast Hit: " + hit.transform.name);
            SpawnExplostion(hit.point, hit.transform);

            var ring = hit.transform.GetComponentInParent<VirtualRail.CollectibleRing>();
            if (ring != null)
            {
                ring.Collect();
            }
            else
            {
                var pickup = hit.transform.GetComponentInParent<Pickup>();
                if (pickup != null)
                {
                    pickup.Collect();
                }
                else
                {
                    var enemyHealth = hit.transform.GetComponentInParent<VirtualRail.EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(20, hit.point, laserDirection);
                    }
                    else
                    {
                        var enemy = hit.transform.GetComponentInParent<EnemyMovement>();
                        if (enemy != null)
                        {
                            enemy.BlowUp();
                        }
                    }

                    var asteroid = hit.transform.GetComponentInParent<Asteroid>();
                    if (asteroid != null)
                    {
                        asteroid.TakeDamage(20, hit.point);
                    }
                }
            }

            return hit.point;
        }
        else
        {
            Debug.Log("Raycast Miss");
            return transform.position + (transform.forward * _laserDistance);
        }
    }

    private void SpawnExplostion(Vector3 hitPosition, Transform target)
    {
        Explosion hitExplosion = target.GetComponent<Explosion>();
        if (hitExplosion != null)
        {
            hitExplosion.AddForce(hitPosition, base.transform);
        }

    }

    public void FireLaser()
    {
        FireLaser(CastRay(), null);
    }

    public void FireLaser(Vector3 targetPosition, Transform target = null)
    {
        if (_canFire)
        {
            if (target != null)
            {
                SpawnExplostion(targetPosition, target);
            }
            // Đã vô hiệu hóa LineRenderer và Light: Hệ thống chuyển sang thuần đạn vật lý (Star Fox 64 spec)
            if (_laserBeam != null) _laserBeam.enabled = false;
            if (_laserLight != null) _laserLight.enabled = false;
            _canFire = false;
            _nextFireTime = Time.time + _fireDelay;
        }
    }

    private void TurnOffLaser()
    {
        _laserBeam.enabled = false;
        _laserLight.enabled = false;
    }

    public float Distance
    {
        get { return _laserDistance; }
    }

    private void CanFire()
    {
        _canFire = true;
    }

}
