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
        // 1. Sinh hiệu ứng nổ / tia lửa tại các điểm tiếp xúc (giới hạn tối đa 2 điểm để tối ưu hiệu năng)
        int contactCount = collision.contacts != null ? collision.contacts.Length : 0;
        int spawnLimit = Mathf.Min(contactCount, 2);
        for (int i = 0; i < spawnLimit; i++)
        {
            SpawnExplosionVFX(collision.contacts[i].point);
        }

        // 2. Xử lý sát thương khiên (CHỈ GỌI ĐÚNG 1 LẦN DUY NHẤT CHO CẢ VA CHẠM)
        if (_shield != null)
        {
            HandleCollisionDamage(collision);
        }
    }

    private void SpawnExplosionVFX(Vector3 explosionPosition)
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
    }

    private void HandleCollisionDamage(Collision collision)
    {
        if (_shield == null || _shield.CurrentHealth <= 0) return;

        // Kiểm tra xem vật va chạm có phải là Thiên thạch (Asteroid) không
        var asteroid = collision.collider != null ? collision.collider.GetComponentInParent<Asteroid>() : null;
        if (asteroid == null && collision.gameObject != null)
        {
            asteroid = collision.gameObject.GetComponent<Asteroid>();
        }

        if (asteroid != null)
        {
            if (asteroid.HazardType == AsteroidHazardType.Indestructible)
            {
                // Thiên thạch kim loại bất hoại: Sát thương nặng (50% khiên = 5 HP)
                _shield.TakeDamage(5);
                VirtualRail.VirtualRailCameraRig.TriggerShake(0.65f);
                VirtualRail.AudioManager.Instance?.PlaySFX2D(VirtualRail.AudioSFXType.ShieldDamage, 1.0f);
            }
            else
            {
                // Thiên thạch thường có thể phá hủy: Sát thương vừa (30% khiên = 3 HP)
                _shield.TakeDamage(3);
                VirtualRail.VirtualRailCameraRig.TriggerShake(0.4f);

                // Cú húc trực diện của tàu làm vỡ nát thiên thạch thường để giải phóng đường bay
                asteroid.ShatterByPlayerCollision();
            }
            return;
        }

        // Kiểm tra va chạm với các vật cản vật lý thông thường khác (1 HP)
        _shield.TakeDamage(1);
    }

    public void AddForce(Vector3 hitPosition, Transform hitSource)
    {
        SpawnExplosionVFX(hitPosition);
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
