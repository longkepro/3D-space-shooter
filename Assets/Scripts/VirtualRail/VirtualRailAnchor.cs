using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Gốc neo của đường ray (Rail Anchor).
    /// Tịnh tiến dọc theo trục Z thế giới, đóng vai trò là Hệ quy chiếu Cục bộ
    /// cho toàn bộ Camera, Khung thân tàu và Điểm neo tâm ngắm.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailAnchor : MonoBehaviour
    {
        public static VirtualRailAnchor Instance { get; private set; }
        public static Transform PlayerTransform => Instance != null ? Instance.transform : null;
        public static Transform PlayerShipTransform { get; set; }

        [Header("Configuration")]
        public VirtualRailConfig config;

        [Header("Runtime State")]
        [SerializeField] private float currentSpeed;
        private bool isBoosting = false;
        private float _overrideSpeed = -1f;

        public float CurrentSpeed
        {
            get => currentSpeed;
            set => currentSpeed = value;
        }
        public Vector3 ForwardTangent => transform.forward;

        public void SetSpeedOverride(float speed)
        {
            _overrideSpeed = speed;
        }

        public void ClearSpeedOverride()
        {
            _overrideSpeed = -1f;
        }

        private void Awake()
        {
            Instance = this;
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<VirtualRailConfig>();
            }
            currentSpeed = config.forwardSpeed;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (config != null && config.useRailWaveSpawner)
            {
                if (GetComponentInChildren<VirtualRailWaveSpawner>() == null)
                {
                    var spawner = gameObject.AddComponent<VirtualRailWaveSpawner>();
                    spawner.anchor = this;
                }
            }

            if (config != null && config.useRailAsteroidSpawner)
            {
                if (GetComponentInChildren<VirtualRailAsteroidSpawner>() == null)
                {
                    var asteroidSpawner = gameObject.AddComponent<VirtualRailAsteroidSpawner>();
                    asteroidSpawner.anchor = this;
                }
            }

            // Module Bom Thông Minh & Động cơ tính điểm Combo (TDD v1.0.0 Phần I)
            if (GetComponent<SmartBombModule>() == null)
            {
                gameObject.AddComponent<SmartBombModule>();
            }

            if (GetComponent<ComboScoringEngine>() == null)
            {
                gameObject.AddComponent<ComboScoringEngine>();
            }

            // Đảm bảo Pool đạn địch sẵn sàng
            if (EnemyProjectilePool.Instance == null)
            {
                var pool = EnemyProjectilePool.Instance;
            }
        }

        private void Update()
        {
            // Kiểm tra boost
            float boostInput = Input.GetAxis("Fire3") > 0 ? Input.GetAxis("Fire3") : (Input.GetKey(KeyCode.LeftShift) ? 1f : 0f);
            isBoosting = boostInput > 0.1f;

            float baseSpeed = _overrideSpeed > 0f ? _overrideSpeed : config.forwardSpeed;
            float targetSpeed = baseSpeed * (isBoosting ? config.boostMultiplier : 1f);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 8f * Time.deltaTime);

            // Tịnh tiến dọc theo hướng tiến của đường ray
            transform.position += transform.forward * (currentSpeed * Time.deltaTime);
        }

        public Vector3 ToWorldPoint(Vector3 localPoint)
        {
            return transform.TransformPoint(localPoint);
        }

        public Vector3 ToLocalPoint(Vector3 worldPoint)
        {
            return transform.InverseTransformPoint(worldPoint);
        }

        /// <summary>
        /// Hồi sinh tại chỗ tất định (Deterministic In-Place Respawn - 0 GC Alloc).
        /// Khôi phục toàn bộ Rig, Camera, Thân tàu, Khiên, Vũ khí, Tâm ngắm và Màn chơi mà không cần Destroy/Instantiate.
        /// </summary>
        public void Respawn(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // 1. Dịch chuyển vị trí Rig về tọa độ xuất phát
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            // 2. Phục hồi tốc độ bay ray
            ClearSpeedOverride();
            currentSpeed = (config != null) ? config.forwardSpeed : 28f;
            isBoosting = false;

            // 3. Phục hồi trạng thái thân tàu & hiển thị đồ họa
            var ship = GetComponentInChildren<VirtualRailShip>();
            if (ship != null)
            {
                ship.Respawn();
            }

            // 4. Phục hồi Máu & Thanh Khiên (Shield)
            var shield = GetComponentInChildren<Shield>();
            if (shield != null)
            {
                shield.ResetHealth();
            }

            // 5. Phục hồi Vũ khí
            var weapon = GetComponentInChildren<VirtualRailWeapon>();
            if (weapon != null)
            {
                weapon.ResetWeapon();
            }

            // 6. Phục hồi Tâm ngắm (Reticle) về chính giữa màn hình
            var reticle = GetComponentInChildren<VirtualRailReticle>();
            if (reticle != null)
            {
                reticle.ResetPosition();
            }

            // 7. Snap Camera về vị trí chuẩn tức thì (0 Lerp Jitter)
            var cameraRig = GetComponentInChildren<VirtualRailCameraRig>();
            if (cameraRig != null)
            {
                cameraRig.SnapToDefault();
            }

            // 8. Thu hồi toàn bộ quái vật, chướng ngại vật & đạn địch còn sót lại trong Scene (0 GC Alloc)
            if (VirtualRailWaveSpawner.Instance != null)
            {
                VirtualRailWaveSpawner.Instance.RecycleAll();
                VirtualRailWaveSpawner.Instance.StartSpawning();
            }

            if (VirtualRailAsteroidSpawner.Instance != null)
            {
                VirtualRailAsteroidSpawner.Instance.RecycleAll();
                VirtualRailAsteroidSpawner.Instance.StartSpawning();
            }

            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.ClearAllActiveProjectiles();
            }

            // 9. Đặt lại Động cơ tính điểm Combo & Bom thông minh
            if (ComboScoringEngine.Instance != null)
            {
                ComboScoringEngine.Instance.ResetSession();
            }

            var bombModule = GetComponentInChildren<SmartBombModule>();
            if (bombModule != null)
            {
                bombModule.ResetBombs();
            }

            // 10. Bắt đầu lại Màn 1 qua Nhạc trưởng Stage Director (ép buộc reset tiến trình màn chơi)
            if (VirtualRailStageDirector.Instance != null)
            {
                VirtualRailStageDirector.Instance.StartStage(0, true);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * 20f);
        }
    }
}
