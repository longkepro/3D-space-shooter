using UnityEngine;

[DisallowMultipleComponent]
public class Explosion : MonoBehaviour
{

    [SerializeField] private GameObject _explosion;
    [SerializeField] private GameObject _blowUp;
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private Shield _shield;
    [SerializeField] private float _laserHitForce = 10f;
    [SerializeField] private float _explosionDuration = 6f;

    private void OnCollisionEnter(Collision collision)
    {
        foreach (var contactPoint in collision.contacts)
        {
            SpawnExplosion(contactPoint.point);
        }
    }

    private void SpawnExplosion(Vector3 explosionPosition)
    {
        if (_explosion != null)
        {
            var vfxPool = VirtualRail.VirtualRailVFXPool.Instance;
            if (vfxPool != null)
            {
                vfxPool.SpawnVFX(_explosion, explosionPosition, Quaternion.identity, transform, _explosionDuration);
            }
            else
            {
                GameObject spawnedExplosion = Instantiate(_explosion, explosionPosition, Quaternion.identity, transform);
                Destroy(spawnedExplosion, _explosionDuration);
            }
        }

        if (_shield != null)
        {
            _shield.TakeDamage();
        }
    }

    public void AddForce(Vector3 hitPosition, Transform hitSource)
    {
        SpawnExplosion(hitPosition);
        if (_rigidBody == null || hitSource == null) return;
        Vector3 forceDirection = (hitSource.position - transform.position).normalized;
        _rigidBody.AddForceAtPosition(forceDirection * _laserHitForce, hitPosition, ForceMode.Impulse);
    }

    public void BlowUp()
    {
        if (_blowUp != null)
        {
            var vfxPool = VirtualRail.VirtualRailVFXPool.Instance;
            if (vfxPool != null)
            {
                vfxPool.SpawnVFX(_blowUp, transform.position, Quaternion.identity, null, _explosionDuration);
            }
            else
            {
                var spawnedExplosion = Instantiate(_blowUp, transform.position, Quaternion.identity);
                Destroy(spawnedExplosion, _explosionDuration);
            }
        }

        // Vector 9: Cluster Fracture (TDD v1.0.0 Phần II.1)
        // Khi thiên thạch hoặc quái vật phát nổ, kích hoạt vỡ mảnh thứ cấp
        if (VirtualRail.VirtualRailWaveSpawner.Instance != null && Random.value < 0.25f)
        {
            VirtualRail.VirtualRailWaveSpawner.Instance.TriggerClusterFracture(transform.position);
        }

        if (TryGetComponent<VirtualRail.IPoolableEntity>(out var poolable))
        {
            poolable.Recycle();
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
