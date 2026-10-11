using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Module Bom Thông Minh (Smart Bomb Screen-Clear / Tactical Missile) chuẩn Star Fox 64 (N64 US Rev 1.1).
    /// - Phóng thực thể Tên lửa / Bom 3D từ mũi tàu ra phía trước với dải phản lực rực sáng.
    /// - Tự dẫn (Homing Missile) nếu đang khóa mục tiêu bởi AimAssist, hoặc bay thẳng theo Reticle nếu không khóa.
    /// - Kích nổ 2 nấc (Manual Air Detonation - Double-tap B) để phát nổ khẩn cấp giữa không trung.
    /// - Sóng xung kích 150m: Quét sạch 100% đạn địch trong màn chơi và hủy diệt quái vật / thiên thạch.
    /// - 0 GC Allocation trong Game Loop qua VirtualRailMissilePool.
    /// </summary>
    public class SmartBombModule : MonoBehaviour
    {
        public static SmartBombModule Instance { get; private set; }

        public static event Action<int> OnBombCountChanged;

        [Header("Settings")]
        [SerializeField] private int initialBombs = 3;
        [SerializeField] private float shockwaveRadius = 150f;
        [SerializeField] private KeyCode bombKey = KeyCode.B;
        [SerializeField] private GameObject bombPrefab;

        private int _currentBombs;
        private VirtualRailAnchor _anchor;
        private VirtualRailShip _ship;
        private VirtualRailReticle _cachedReticle;
        private VirtualRailHomingMissile _activeBombInstance;
        private static readonly Collider[] _bombColliders = new Collider[128];

        public int CurrentBombs => _currentBombs;
        public GameObject BombPrefab
        {
            get => bombPrefab;
            set => bombPrefab = value;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _anchor = GetComponentInParent<VirtualRailAnchor>();
            if (_anchor == null) _anchor = VirtualRailAnchor.Instance;

            if (_anchor != null && _anchor.config != null)
            {
                initialBombs = _anchor.config.initialSmartBombs;
                shockwaveRadius = (_anchor.config.smartBombShockwaveRadius > 10f) ? _anchor.config.smartBombShockwaveRadius : 150f;
            }
            _currentBombs = initialBombs;

            EnsureBombPrefab();
        }

        private void EnsureBombPrefab()
        {
            if (bombPrefab == null)
            {
#if UNITY_EDITOR
                bombPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerSmartBomb.prefab");
#endif
                if (bombPrefab == null)
                {
                    bombPrefab = Resources.Load<GameObject>("PlayerSmartBomb");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (_ship == null && _anchor != null) _ship = _anchor.GetComponentInChildren<VirtualRailShip>();
            if (_ship == null) _ship = FindAnyObjectByType<VirtualRailShip>();
            if (_cachedReticle == null && _anchor != null) _cachedReticle = _anchor.GetComponentInChildren<VirtualRailReticle>();
            if (_cachedReticle == null) _cachedReticle = FindAnyObjectByType<VirtualRailReticle>();

            EnsureBombPrefab();
            OnBombCountChanged?.Invoke(_currentBombs);
        }

        private void Update()
        {
            if (_anchor != null && _anchor.config != null && !_anchor.config.enableSmartBomb)
            {
                return;
            }

            if (Input.GetKeyDown(bombKey))
            {
                // Kiểm tra kích nổ thủ công giữa không trung (Manual Air Detonation)
                bool allowManualDetonate = (_anchor == null || _anchor.config == null || _anchor.config.enableSmartBombManualDetonation);
                if (allowManualDetonate && _activeBombInstance != null && _activeBombInstance.gameObject.activeSelf)
                {
                    _activeBombInstance.ManualDetonate();
                    _activeBombInstance = null;
                    Debug.Log("<color=yellow><b>[SMART BOMB]</b></color> Kích nổ thủ công giữa không trung (Double-tap B Air Detonation)!");
                    return;
                }

                TriggerBomb();
            }
        }

        public bool TriggerBomb()
        {
            if (_currentBombs <= 0)
            {
                Debug.LogWarning("[SmartBomb] Đã hết bom dự trữ!");
                return false;
            }

            _currentBombs--;
            OnBombCountChanged?.Invoke(_currentBombs);

            EnsureBombPrefab();

            // Tính vị trí mũi tàu
            Vector3 nosePos = (_ship != null && _ship.VisualMesh != null)
                ? (_ship.VisualMesh.position + _ship.VisualMesh.forward * 5.5f)
                : ((_ship != null)
                    ? (_ship.transform.position + _ship.ShipForward * 5.5f)
                    : (transform.position + transform.forward * 5.5f));

            // Xác định hướng phóng và mục tiêu tự dẫn (nếu có Lock từ AimAssist)
            Transform lockedTarget = null;
            Vector3 launchDir;
            if (AimAssistModule.Instance != null && AimAssistModule.Instance.LockedTarget != null)
            {
                lockedTarget = AimAssistModule.Instance.LockedTarget;
                launchDir = (lockedTarget.position - nosePos).normalized;
            }
            else
            {
                VirtualRailReticle reticle = _cachedReticle != null ? _cachedReticle : (_anchor != null ? _anchor.GetComponentInChildren<VirtualRailReticle>() : null);
                if (reticle != null)
                {
                    launchDir = (reticle.WorldPosition - nosePos).normalized;
                }
                else
                {
                    launchDir = (_ship != null) ? _ship.ShipForward : transform.forward;
                }
            }

            if (launchDir.sqrMagnitude < 0.001f)
            {
                launchDir = (_ship != null) ? _ship.ShipForward : transform.forward;
            }

            Quaternion spawnRot = Quaternion.LookRotation(launchDir);
            var cfg = (_anchor != null) ? _anchor.config : null;

            if (bombPrefab != null)
            {
                VirtualRailHomingMissile missile = (VirtualRailMissilePool.Instance != null)
                    ? VirtualRailMissilePool.Instance.SpawnMissile(bombPrefab, nosePos, spawnRot)
                    : null;

                if (missile != null)
                {
                    float shipSpeed = (_anchor != null) ? _anchor.CurrentSpeed : 120f;
                    float speed = (cfg != null && cfg.smartBombFlightSpeed > 10f) ? cfg.smartBombFlightSpeed : 160f;
                    float turnSpeed = (cfg != null && cfg.smartBombTurnRate > 10f) ? cfg.smartBombTurnRate : 320f;
                    float lifeTime = (cfg != null && cfg.smartBombLifetime > 0.5f) ? cfg.smartBombLifetime : 3.0f;
                    float radius = (cfg != null && cfg.smartBombShockwaveRadius > 10f) ? cfg.smartBombShockwaveRadius : shockwaveRadius;

                    missile.isSmartBomb = true;
                    missile.lifeTime = lifeTime;
                    missile.splashRadius = radius;
                    missile.InitializeKinematics(speed, shipSpeed, turnSpeed, cfg, lockedTarget);

                    _activeBombInstance = missile;
                }
                Debug.Log($"<color=cyan><b>[SMART BOMB]</b></color> Đã phóng Tên lửa/Bom {(lockedTarget != null ? "(Tự dẫn)" : "(Thẳng)")}! Còn lại {_currentBombs} quả.");
            }
            else
            {
                // Fallback nếu thiếu prefab: nổ tức thời không gây crash
                ExecuteInstantShockwave(transform.position);
                Debug.Log($"<color=cyan><b>[SMART BOMB]</b></color> Đã kích hoạt sóng xung kích 150m! Còn lại {_currentBombs} quả.");
            }

            return true;
        }

        private void ExecuteInstantShockwave(Vector3 centerPos)
        {
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.ClearAllActiveProjectiles();
            }

            int count = Physics.OverlapSphereNonAlloc(centerPos, shockwaveRadius, _bombColliders);
            for (int i = 0; i < count; i++)
            {
                var col = _bombColliders[i];
                if (col == null) continue;

                var enemyHealth = col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    enemyHealth.TakeDamage(999, enemyHealth.transform.position, (enemyHealth.transform.position - centerPos).normalized);
                    _bombColliders[i] = null;
                    continue;
                }

                var enemy = col.GetComponentInParent<EnemyMovement>();
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    enemy.BlowUp();
                    _bombColliders[i] = null;
                    continue;
                }

                var asteroid = col.GetComponentInParent<Asteroid>();
                if (asteroid != null && asteroid.gameObject.activeSelf)
                {
                    if (asteroid.HazardType == AsteroidHazardType.Destructible)
                    {
                        asteroid.TakeDamage(999, asteroid.transform.position);
                    }
                }
                _bombColliders[i] = null;
            }
        }

        public void AddBomb(int count = 1)
        {
            _currentBombs += count;
            OnBombCountChanged?.Invoke(_currentBombs);
        }

        /// <summary>
        /// Khôi phục số lượng bom thông minh về định mức ban đầu khi hồi sinh.
        /// </summary>
        public void ResetBombs()
        {
            _currentBombs = (_anchor != null && _anchor.config != null) ? _anchor.config.initialSmartBombs : initialBombs;
            OnBombCountChanged?.Invoke(_currentBombs);
        }
    }
}
