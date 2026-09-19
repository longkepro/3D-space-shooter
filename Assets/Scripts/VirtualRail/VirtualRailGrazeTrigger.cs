using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Vỏ bọc lướt cận kề (Outer Graze Box - R = 1.8m) theo chuẩn TDD v2.0.0 Phần III.2.
    /// Kích hoạt cơ chế kỹ năng cao: Đạn hoặc thiên thạch sạt qua sườn tàu mà không chạm Hurtbox trong (R = 0.5m)
    /// sẽ thưởng +500 điểm x Multiplier, hồi 30% thanh thời gian Multiplier Decay và phát tia lửa quang học.
    /// Tuân thủ nghiêm ngặt 0 GC Allocation trong vòng lặp game loop.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public class VirtualRailGrazeTrigger : MonoBehaviour
    {
        [Header("Graze Geometry (TDD v2.0.0)")]
        [Tooltip("Bán kính vùng lướt sát (mặc định 1.8m chuẩn TDD)")]
        [SerializeField] private float grazeRadius = 1.8f;

        [Header("References")]
        public VirtualRailShip parentShip;
        [SerializeField] private ParticleSystem grazeSparkParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip grazeSound;

        private SphereCollider _triggerCollider;
        private Rigidbody _rigidbody;

        public float GrazeRadius => grazeRadius;

        private void Awake()
        {
            SetupPhysicsComponents();
            SetupGrazeVFX();
        }

        private void Start()
        {
            if (parentShip == null)
            {
                parentShip = GetComponentInParent<VirtualRailShip>();
            }
        }

        public void Initialize(VirtualRailShip ship)
        {
            parentShip = ship;
            SetupPhysicsComponents();
        }

        private void SetupPhysicsComponents()
        {
            if (_triggerCollider == null) _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider == null) _triggerCollider = gameObject.AddComponent<SphereCollider>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = grazeRadius;

            // Rigidbody Kinematic bắt buộc để Unity Physics kích hoạt OnTriggerEnter 100% tin cậy
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null) _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        private void SetupGrazeVFX()
        {
            // Tạo một hệ thống hạt tia lửa điện nhẹ (Zero GC khi Emit) nếu chưa gắn sẵn
            if (grazeSparkParticles == null)
            {
                Transform existing = transform.Find("GrazeSparks");
                if (existing != null)
                {
                    grazeSparkParticles = existing.GetComponent<ParticleSystem>();
                }
                else
                {
                    GameObject sparkObj = new GameObject("GrazeSparks");
                    sparkObj.transform.SetParent(transform, false);
                    grazeSparkParticles = sparkObj.AddComponent<ParticleSystem>();

                    var main = grazeSparkParticles.main;
                    main.playOnAwake = false;
                    main.loop = false;
                    main.startLifetime = 0.25f;
                    main.startSpeed = 12f;
                    main.startSize = 0.18f;
                    main.startColor = new Color(0.2f, 0.95f, 1f, 1.0f); // Xanh neon quang học
                    main.maxParticles = 50;

                    var emission = grazeSparkParticles.emission;
                    emission.rateOverTime = 0f;

                    var shape = grazeSparkParticles.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.5f;

                    var pRenderer = sparkObj.GetComponent<ParticleSystemRenderer>();
                    if (pRenderer != null)
                    {
                        Material mat = null;
#if UNITY_EDITOR
                        mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Electric/CFX_Sparkles_Add.mat");
                        if (mat == null)
                        {
                            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Misc Effects/Materials/ElectricalSparks.mat");
                        }
#endif
                        if (mat == null)
                        {
                            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                        }
                        pRenderer.sharedMaterial = mat;
                    }
                }
            }

            // Thiết lập âm thanh xé gió (Graze Sizzle)
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D Stereo âm thanh rõ nét
            }

            if (grazeSound == null)
            {
#if UNITY_EDITOR
                grazeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Asset Store/SpaceshipCollectionPack/Audio/Button_Pressed_Sound.wav");
#endif
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (parentShip != null && parentShip.IsDeflecting)
            {
                // Khi đang lộn cánh Barrel Roll Deflection, đạn đã bị hất văng nên không tính graze
                return;
            }

            // 1. Kiểm tra Đạn của kẻ địch (EnemyProjectile)
            var projectile = other.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                if (!projectile.HasGrazed)
                {
                    projectile.HasGrazed = true;
                    TriggerGrazeSuccess(projectile.transform.position);
                }
                return;
            }

            // 2. Kiểm tra Thiên thạch (Asteroid)
            var asteroid = other.GetComponentInParent<Asteroid>();
            if (asteroid != null)
            {
                if (!asteroid.HasGrazed)
                {
                    asteroid.HasGrazed = true;
                    TriggerGrazeSuccess(asteroid.transform.position);
                }
                return;
            }
        }

        /// <summary>
        /// Kích hoạt thành công cơ chế Graze: Thưởng điểm, cộng thanh thời gian, phát VFX/SFX (0 GC Alloc).
        /// </summary>
        private void TriggerGrazeSuccess(Vector3 hazardPosition)
        {
            // Điểm tiếp xúc trực quan giữa mối nguy và thân tàu
            Vector3 sparkPos = (hazardPosition + transform.position) * 0.5f;

            // 1. Báo cáo động cơ tính điểm Combo (TDD: +500 điểm x Multiplier, hồi 30% decay gauge)
            if (ComboScoringEngine.Instance != null)
            {
                ComboScoringEngine.Instance.RegisterGraze(sparkPos, 500);
            }

            // 2. Phát hiệu ứng tia lửa quang học dọc mạn tàu (0 GC: Emit từ ParticleSystem sẵn có)
            if (grazeSparkParticles != null)
            {
                grazeSparkParticles.transform.position = sparkPos;
                grazeSparkParticles.Emit(16);
            }

            // 3. Âm thanh xé gió tần số cao (High-pitch Laser Sizzle)
            if (audioSource != null && grazeSound != null)
            {
                audioSource.pitch = 1.75f;
                audioSource.PlayOneShot(grazeSound, 0.45f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1.0f, 0.8f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, grazeRadius);
        }
    }
}
