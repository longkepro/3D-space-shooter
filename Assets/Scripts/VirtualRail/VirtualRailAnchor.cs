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

        public float CurrentSpeed => currentSpeed;
        public Vector3 ForwardTangent => transform.forward;

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

            float targetSpeed = config.forwardSpeed * (isBoosting ? config.boostMultiplier : 1f);
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * 20f);
        }
    }
}
