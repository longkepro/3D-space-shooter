using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Hiệu ứng xoay tròn, nhấp nháy phát quang và điều chỉnh kích thước cho Quả cầu Plasma tụ lực (Charge Shot Plasma Orb).
    /// Chuẩn Star Fox 64 Arcade, 0 GC Alloc.
    /// </summary>
    [ExecuteAlways]
    public class VirtualRailChargeShotVisual : MonoBehaviour
    {
        [Header("Size & Dimensions (Tùy Chỉnh Kích Thước)")]
        [Tooltip("Đường kính vỏ ngoài quả cầu plasma (mét)")]
        [Range(0.1f, 10.0f)]
        public float outerDiameter = 3.85f;

        [Tooltip("Tỉ lệ lõi năng lượng trong so với vỏ ngoài (0.1 - 1.0)")]
        [Range(0.1f, 1.0f)]
        public float innerCoreRatio = 0.54f;

        [Tooltip("Bật/tắt vệt đuôi phát sáng phía sau quả cầu")]
        public bool enableTrail = false;

        [Tooltip("Độ rộng vệt đuôi năng lượng phía sau quả cầu (mét)")]
        [Range(0.1f, 10.0f)]
        public float trailWidth = 1.2f;

        [Tooltip("Bán kính phát quang của đèn Point Light (mét)")]
        [Range(2.0f, 60.0f)]
        public float lightRange = 32.0f;

        [Header("Physics & Combat (Va Chạm & Sát Thương)")]
        [Tooltip("Tự động đồng bộ bán kính va chạm vật lý và bán kính nổ theo kích thước quả cầu")]
        public bool autoSyncHitbox = true;

        [Tooltip("Bán kính va chạm vật lý SphereCollider (mét)")]
        [Range(0.1f, 10.0f)]
        public float collisionRadius = 1.2f;

        [Tooltip("Bán kính nổ diện rộng AoE Splash (mét)")]
        [Range(1.0f, 50.0f)]
        public float splashRadius = 10.0f;

        [Header("Spin Settings")]
        [SerializeField] private float spinSpeedZ = 540f; // độ/giây
        [SerializeField] private float tumbleSpeed = 160f;

        [Header("Pulse Settings")]
        [SerializeField] private Transform coreTransform;
        [SerializeField] private Transform coronaTransform;
        [SerializeField] private Light pointLight;
        [SerializeField] private float pulseFrequency = 14f;
        [SerializeField] private float pulseAmplitude = 0.12f;

        private Vector3 _baseCoreScale = Vector3.one;
        private Vector3 _baseCoronaScale = Vector3.one * 0.35f;
        private float _baseLightIntensity = 3.5f;

        private void Awake()
        {
            AutoBindReferences();
            if (coreTransform != null) _baseCoreScale = coreTransform.localScale;
            if (coronaTransform != null) _baseCoronaScale = coronaTransform.localScale;
            if (pointLight != null) _baseLightIntensity = pointLight.intensity;
        }

        private void Start()
        {
            ApplyDimensions();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            float dt = Time.deltaTime;

            // 1. Xoay tròn lõi và quầng năng lượng quanh trục bay
            transform.Rotate(0f, 0f, spinSpeedZ * dt, Space.Self);

            if (coronaTransform != null)
            {
                coronaTransform.Rotate(tumbleSpeed * dt, -tumbleSpeed * 0.7f * dt, tumbleSpeed * 0.5f * dt, Space.Self);
            }

            // 2. Nhấp nháy xung điện từ trường (0 GC Alloc)
            float wave = Mathf.Sin(Time.time * pulseFrequency);
            float scaleMod = 1f + wave * pulseAmplitude;

            if (coreTransform != null)
            {
                coreTransform.localScale = _baseCoreScale * scaleMod;
            }

            if (pointLight != null)
            {
                pointLight.intensity = _baseLightIntensity * (1f + wave * 0.35f);
            }
        }

        public void AutoBindReferences()
        {
            if (coronaTransform == null)
            {
                Transform t = transform.Find("[Visual_PlasmaSphere3]");
                if (t == null) t = transform.Find("[Corona]");
                coronaTransform = t;
            }

            if (coreTransform == null)
            {
                if (coronaTransform != null)
                {
                    Transform t = coronaTransform.Find("Sphere");
                    if (t == null) t = coronaTransform.Find("[Core]");
                    coreTransform = t;
                }
                if (coreTransform == null)
                {
                    coreTransform = transform.Find("[Core]");
                }
            }

            if (pointLight == null)
            {
                pointLight = GetComponentInChildren<Light>(true);
            }
        }

        public void ApplyDimensions()
        {
            AutoBindReferences();

            // 1. Vỏ ngoài Plasma
            if (coronaTransform != null)
            {
                coronaTransform.localScale = Vector3.one * outerDiameter;
                _baseCoronaScale = coronaTransform.localScale;
            }

            // 2. Lõi trong (tỉ lệ innerCoreRatio)
            if (coreTransform != null)
            {
                coreTransform.localScale = Vector3.one * innerCoreRatio;
                _baseCoreScale = coreTransform.localScale;
            }

            // 3. Vệt đuôi TrailRenderer
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.enabled = enableTrail;
                trail.startWidth = trailWidth;
            }

            // 4. Đèn PointLight
            if (pointLight != null)
            {
                pointLight.range = lightRange;
            }

            // 5. Va chạm vật lý SphereCollider (Hitbox ôm lõi năng lượng 1.2m - 1.5m)
            SphereCollider col = GetComponent<SphereCollider>();
            if (col != null)
            {
                if (autoSyncHitbox)
                {
                    collisionRadius = Mathf.Clamp(outerDiameter * 0.38f, 0.5f, 1.5f);
                }
                col.radius = collisionRadius;
            }

            // 6. Sát thương nổ AoE (Chuẩn Star Fox 64: 10.0m)
            VirtualRailHomingMissile missile = GetComponent<VirtualRailHomingMissile>();
            if (missile != null)
            {
                if (autoSyncHitbox)
                {
                    splashRadius = 10.0f;
                }
                missile.splashRadius = splashRadius;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyDimensions();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position;

            // Vỏ ngoài Plasma (Xanh lá)
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.7f);
            Gizmos.DrawWireSphere(center, outerDiameter * 0.5f);

            // Lõi trong (Cyan)
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(center, outerDiameter * 0.5f * innerCoreRatio);

            // Va chạm Hitbox (Vàng)
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(center, collisionRadius);

            // Bán kính nổ AoE (Đỏ cam)
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(center, splashRadius);
        }
#endif
    }
}
