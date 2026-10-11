using System.Collections.Generic;
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

    private static readonly List<VirtualRail.IPoolableEntity> _poolableBuffer = new List<VirtualRail.IPoolableEntity>(8);

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
        SpawnBlowUpVFX();

        _poolableBuffer.Clear();
        GetComponents(_poolableBuffer);
        if (_poolableBuffer.Count > 0)
        {
            for (int i = 0; i < _poolableBuffer.Count; i++)
            {
                _poolableBuffer[i].Recycle();
            }
            _poolableBuffer.Clear();
        }
        else if (CompareTag("Player"))
        {
            // Với Player Rig / ShipVisual: Bảo tồn GameObject để tránh rò rỉ phân cấp và Camera Rig
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnBlowUpVFX()
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
    }

}
