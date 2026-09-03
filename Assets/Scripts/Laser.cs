using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
[RequireComponent(typeof(LineRenderer))]
public class Laser : MonoBehaviour
{

    [Tooltip("Time in seconds")][SerializeField] private float _laserDuration = 0.5f;
    [SerializeField] private float _laserDistance = 300f;
    [SerializeField] private float _fireDelay = 2f;
    private LineRenderer _laserBeam;
    private Light _laserLight;
    private bool _canFire = true;

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
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * _laserDistance, Color.yellow);
    }

    private Vector3 CastRay()
    {
        Vector3 laserDirection = transform.TransformDirection(Vector3.forward) * _laserDistance;
        if (Physics.Raycast(base.transform.position, laserDirection, out RaycastHit hit))
        {
            Debug.Log("Raycast Hit: " + hit.transform.name);
            SpawnExplostion(hit.point, hit.transform);

            if (hit.transform.CompareTag("Pickup"))
            {
                hit.transform.GetComponent<Pickup>().Collect();
            }
            else if (hit.transform.CompareTag("Enemy"))
            {
                hit.transform.GetComponent<EnemyMovement>().BlowUp();
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
            _laserBeam.SetPosition(0, transform.position);
            _laserBeam.SetPosition(1, targetPosition);
            _laserBeam.enabled = true;
            _laserLight.enabled = true;
            _canFire = false;
            Invoke("TurnOffLaser", _laserDuration);
            Invoke("CanFire", _fireDelay);
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
