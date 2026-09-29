using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Hệ thống Bụi Sao Không Gian (Space Dust / Star Specks Motion Parallax).
    /// Gắn dưới [Camera Rig] hoặc [Rail Anchor].
    /// Giải quyết triệt để cảm giác phi thuyền đứng im trên nền Skybox tĩnh ở vô cực (Z = infinity):
    /// 1. Liên tục sinh các hạt bụi sao li ti trôi ngược chiều dọc trục -Z với vận tốc bằng đúng vận tốc bay thực tế (anchor.CurrentSpeed).
    /// 2. Tạo dòng chảy quang học (Optical Flow) và thị sai chuyển động (Motion Parallax) tự nhiên 100%.
    /// 3. Prewarm ngay từ frame đầu tiên để không gian luôn tràn ngập bụi sao lướt qua cánh tàu.
    /// 4. 0 GC Allocation trong toàn bộ vòng lặp Update.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public class VirtualRailSpaceDust : MonoBehaviour
    {
        [Header("References")]
        public VirtualRailAnchor anchor;
        public VirtualRailConfig config;

        [Header("Dust Field Geometry")]
        [Tooltip("Độ rộng trường bụi sao theo trục X (m)")]
        [SerializeField] private float fieldWidth = 50f;
        [Tooltip("Độ cao trường bụi sao theo trục Y (m)")]
        [SerializeField] private float fieldHeight = 30f;
        [Tooltip("Độ sâu trường bụi sao theo trục Z (m)")]
        [SerializeField] private float fieldDepth = 90f;
        [Tooltip("Độ lệch tâm Z phía trước camera (m)")]
        [SerializeField] private float fieldZOffset = 45f;
        [Tooltip("Độ lệch tâm Y để căn chỉnh theo góc chúc của Camera (m)")]
        [SerializeField] private float fieldYOffset = -3.5f;

        [Header("Particle Appearance")]
        [Tooltip("Số lượng hạt bụi sao tối đa trong khung nhìn")]
        [SerializeField] private int targetParticleCount = 220;
        [Tooltip("Kích thước tối thiểu của hạt (m)")]
        [SerializeField] private float minParticleSize = 0.10f;
        [Tooltip("Kích thước tối đa của hạt (m)")]
        [SerializeField] private float maxParticleSize = 0.28f;
        [Tooltip("Màu sắc bụi sao ánh sao Neon trắng/cyan")]
        [SerializeField] private Color dustColor = new Color(0.85f, 0.95f, 1.0f, 0.85f);
        [Tooltip("Vật liệu hạt (nếu để trống sẽ tự tìm CFX3_CircleSoft ADDSOFT)")]
        [SerializeField] private Material particleMaterial;

        private ParticleSystem _ps;
        private ParticleSystemRenderer _psr;
        private float _lastSpeed = -1f;
        private bool _isInitialized = false;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _psr = GetComponent<ParticleSystemRenderer>();
            InitializeSystem();
        }

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (config == null && anchor != null) config = anchor.config;

            ApplyConfig();
            UpdateVelocity(anchor != null ? anchor.CurrentSpeed : 28f);
        }

        /// <summary>
        /// Cập nhật màu sắc và số lượng hạt bụi sao khi chuyển màn (0 GC Alloc).
        /// </summary>
        public void SetDustSettings(Color color, int particleCount)
        {
            dustColor = color;
            if (particleCount > 10) targetParticleCount = particleCount;
            if (_ps != null)
            {
                var main = _ps.main;
                main.startColor = color;
                main.maxParticles = targetParticleCount;
            }
        }

        public void InitializeSystem()
        {
            if (_isInitialized) return;
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            if (_psr == null) _psr = GetComponent<ParticleSystemRenderer>();

            // 1. Tải vật liệu hạt phát quang mềm mại
            if (particleMaterial == null)
            {
#if UNITY_EDITOR
                particleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Light/CFX3_CircleSoft ADDSOFT.mat");
                if (particleMaterial == null)
                {
                    particleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LaserBolt_BrightNeon.mat");
                }
#endif
            }

            if (_psr != null && particleMaterial != null)
            {
                _psr.sharedMaterial = particleMaterial;
                _psr.renderMode = ParticleSystemRenderMode.Billboard;
                _psr.sortMode = ParticleSystemSortMode.Distance;
            }

            // 2. Cấu hình Main Module
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.playOnAwake = true;
            main.loop = true;
            main.prewarm = true; // Khởi tạo đầy ắp hạt ngay frame đầu tiên
            main.maxParticles = targetParticleCount + 80;
            main.startSpeed = 0f; // Vận tốc được điều khiển bởi velocityOverLifetime
            main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
            main.startColor = dustColor;

            float baseSpeed = 28f;
            float lifetime = fieldDepth / Mathf.Max(baseSpeed, 10f);
            main.startLifetime = lifetime;

            // 3. Cấu hình Emission Module
            var emission = _ps.emission;
            emission.enabled = true;
            emission.rateOverTime = targetParticleCount / lifetime;

            // 4. Cấu hình Shape Module (Hộp không gian bao trùm từ Camera xuyên qua Tàu)
            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, fieldYOffset, fieldZOffset);
            shape.scale = new Vector3(fieldWidth, fieldHeight, fieldDepth);

            // 5. Cấu hình Velocity Over Lifetime (Trôi dạt lùi ngược trục Z)
            var vel = _ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(-baseSpeed);

            // 6. Cấu hình Color Over Lifetime (Mờ dần 2 đầu để hạt không bị xuất hiện/biến mất đột ngột)
            var col = _ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(new Color(0.7f, 0.9f, 1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),       // Mới sinh ở xa: mờ 0
                    new GradientAlphaKey(0.85f, 0.15f), // Rực sáng ở trung cảnh
                    new GradientAlphaKey(0.85f, 0.85f), // Duy trì độ sáng khi lướt qua tàu
                    new GradientAlphaKey(0f, 1f)        // Biến mất êm ái sau lưng camera
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            _isInitialized = true;
            if (!_ps.isPlaying) _ps.Play();
        }

        private void ApplyConfig()
        {
            if (config == null) return;

            // Nếu người dùng tắt Space Dust trong config thì tắt hệ thống hạt
            if (!config.enableSpaceDust)
            {
                if (_ps != null && _ps.isPlaying) _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            targetParticleCount = config.dustParticleCount > 10 ? config.dustParticleCount : 220;
            minParticleSize = config.minDustSize > 0.01f ? config.minDustSize : 0.10f;
            maxParticleSize = config.maxDustSize > 0.01f ? config.maxDustSize : 0.28f;
            fieldWidth = config.dustFieldWidth > 10f ? config.dustFieldWidth : 50f;
            fieldHeight = config.dustFieldHeight > 10f ? config.dustFieldHeight : 30f;
            fieldDepth = config.dustFieldDepth > 20f ? config.dustFieldDepth : 90f;

            if (_ps != null)
            {
                var main = _ps.main;
                main.maxParticles = targetParticleCount + 80;
                main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);

                var shape = _ps.shape;
                shape.scale = new Vector3(fieldWidth, fieldHeight, fieldDepth);
            }
        }

        private void Update()
        {
            if (anchor == null)
            {
                anchor = GetComponentInParent<VirtualRailAnchor>();
                if (anchor == null) return;
            }

            float currentSpeed = anchor.CurrentSpeed;
            // Chỉ cập nhật khi vận tốc thay đổi đáng kể (> 0.05 m/s) để tiết kiệm CPU (0 GC Alloc)
            if (Mathf.Abs(_lastSpeed - currentSpeed) > 0.05f)
            {
                UpdateVelocity(currentSpeed);
            }
        }

        private void UpdateVelocity(float speed)
        {
            _lastSpeed = speed;
            if (_ps == null) return;

            float effSpeed = Mathf.Max(speed, 5f);
            float lifetime = fieldDepth / effSpeed;

            var main = _ps.main;
            main.startLifetime = lifetime;

            var emission = _ps.emission;
            emission.rateOverTime = targetParticleCount / lifetime;

            var vel = _ps.velocityOverLifetime;
            vel.z = new ParticleSystem.MinMaxCurve(-effSpeed);
        }

        public void SetDustActive(bool active)
        {
            if (_ps == null) return;
            if (active)
            {
                gameObject.SetActive(true);
                if (!_ps.isPlaying) _ps.Play();
            }
            else
            {
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                gameObject.SetActive(false);
            }
        }
    }
}
