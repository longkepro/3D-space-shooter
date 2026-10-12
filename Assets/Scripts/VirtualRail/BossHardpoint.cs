using System;
using UnityEngine;

namespace VirtualRail
{
    public enum BossHardpointType
    {
        LeftFlakTurret = 0,
        RightFlakTurret = 1,
        DorsalReactorTrench = 2,
        MainBridge = 3,
        ArmoredHull = 4
    }

    /// <summary>
    /// Module quan ly tung linh kien/hardpoint tren chien ham Boss Federation Corvette F3.
    /// Tich hop cung EnemyHealth de lap tuc tuong thich voi AimAssistModule, Dynamic Depth Convergence,
    /// va DualReticle HUD Lock-on (0 GC Allocation).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class BossHardpoint : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BossHardpointType _hardpointType;
        [SerializeField] private int _maxHealth = 150;
        [SerializeField] private bool _isWeakSpot = false;
        [SerializeField] private float _weakSpotDamageMultiplier = 2.0f;

        [Header("References")]
        [SerializeField] private BossF3CorvetteController _controller;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private Transform[] _muzzlePoints;
        [SerializeField] private GameObject _destroyedFireVFX;

        private Collider _collider;
        private bool _isDestroyed = false;

        public BossHardpointType HardpointType => _hardpointType;
        public bool IsDestroyed => _isDestroyed;
        public bool IsWeakSpot => _isWeakSpot;
        public float WeakSpotMultiplier => _weakSpotDamageMultiplier;
        public Transform[] MuzzlePoints => _muzzlePoints;
        public EnemyHealth HealthComponent => _health;

        public event Action<BossHardpoint> OnHardpointDestroyed;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_controller == null)
            {
                _controller = GetComponentInParent<BossF3CorvetteController>();
            }

            if (_health == null)
            {
                _health = GetComponent<EnemyHealth>();
                if (_health == null)
                {
                    _health = gameObject.AddComponent<EnemyHealth>();
                }
            }

            // Gan tag Enemy de dam bao AimAssistModule va VirtualRailWeapon nhan dien lap tuc
            gameObject.tag = "Enemy";
        }

        private void Start()
        {
            if (_health != null)
            {
                var preset = EnemyData.CreateDefaultPreset(EnemyArchetype.SquadLeader);
                preset.maxHealth = _maxHealth;
                _health.ApplyData(preset);
                _health.OnDamaged += HandleHealthDamaged;
                _health.OnDied += HandleHealthDied;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleHealthDamaged;
                _health.OnDied -= HandleHealthDied;
            }
        }

        public void SetController(BossF3CorvetteController controller)
        {
            _controller = controller;
        }

        public void SetHardpointColliderActive(bool active)
        {
            if (_collider != null)
            {
                _collider.enabled = active;
            }
        }

        private void HandleHealthDamaged(int damageDealt, Vector3 hitPoint, bool wasWeakSpot)
        {
            if (_controller != null && !_isDestroyed)
            {
                int effectiveDmg = _isWeakSpot ? Mathf.RoundToInt(damageDealt * _weakSpotDamageMultiplier) : damageDealt;
                _controller.NotifyHardpointDamaged(this, effectiveDmg, hitPoint);
            }
        }

        private void HandleHealthDied(EnemyHealth deadHealth)
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            // Với Rãnh Lò Phản Ứng (DorsalReactorTrench), không tắt collider hoàn toàn mà giữ mở để ở Pha 3 người chơi tiếp tục bắn vào vùng lõi nóng chảy
            if (_hardpointType != BossHardpointType.DorsalReactorTrench)
            {
                SetHardpointColliderActive(false);
            }
            else
            {
                _isWeakSpot = false; // Chuyển về sát thương chuẩn x1
            }

            if (_destroyedFireVFX != null)
            {
                _destroyedFireVFX.SetActive(true);
            }

            if (_controller != null)
            {
                _controller.NotifyHardpointDestroyed(this);
            }

            OnHardpointDestroyed?.Invoke(this);
        }

        /// <summary>
        /// Kich hoat hoac vo hieu hoa trang thai diem yeu.
        /// </summary>
        public void SetWeakSpot(bool weakSpot)
        {
            _isWeakSpot = weakSpot;
        }
    }
}
