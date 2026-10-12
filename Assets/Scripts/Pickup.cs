using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshCollider))]
public class Pickup : MonoBehaviour
{

    [SerializeField] private int _points = 100;
    private bool _isCollected = false; // make sure the pickup is only collected once (protect from multiple hits)

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            if (!_isCollected)
            {
                var shield = other.GetComponentInParent<Shield>() ?? other.GetComponentInChildren<Shield>();
                if (shield != null)
                {
                    int healAmount = (VirtualRail.VirtualRailAnchor.Instance != null && VirtualRail.VirtualRailAnchor.Instance.config != null)
                        ? VirtualRail.VirtualRailAnchor.Instance.config.whiteOrbHealAmount
                        : 3;
                    shield.Heal(healAmount);
                }
                Collect();
            }
        }
    }

    public void Collect()
    {
        if (!_isCollected)
        {
            _isCollected = true;
            GameEventManager.IncrementScore(_points);
            GameEventManager.RespawnPickup();
            Destroy(gameObject);
        }
    }

}
