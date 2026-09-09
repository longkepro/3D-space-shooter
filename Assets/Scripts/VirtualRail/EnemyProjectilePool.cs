using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// B? d?m Object Pool qu?n lý d?n c?a k? d?ch (0 GC Allocation).
    /// Tuân th? TDD v1.0.0 Ph?n I: T? d?ng kh?i t?o và tái s? d?ng 40 viên d?n Plasma Orb / Dart.
    /// </summary>
    public class EnemyProjectilePool : MonoBehaviour
    {
        private static EnemyProjectilePool _instance;
        public static EnemyProjectilePool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<EnemyProjectilePool>();
                    if (_instance == null)
                    {
                        GameObject poolObj = new GameObject("[VirtualRail_EnemyProjectilePool]");
                        _instance = poolObj.AddComponent<EnemyProjectilePool>();
                    }
                }
                return _instance;
            }
        }

        [Header("Pool Configuration")]
        [SerializeField] private int initialCapacity = 40;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Material bulletMaterial;

        private readonly List<EnemyProjectile> _pool = new List<EnemyProjectile>();
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            InitializePool();
        }

        public void InitializePool()
        {
            if (_isInitialized) return;

            if (projectilePrefab == null)
            {
                projectilePrefab = CreateDefaultPlasmaOrbTemplate();
            }

            for (int i = 0; i < initialCapacity; i++)
            {
                GameObject obj = Instantiate(projectilePrefab, transform);
                obj.name = "EnemyBullet_Pooled_" + i;
                obj.SetActive(false);

                var proj = obj.GetComponent<EnemyProjectile>();
                if (proj == null) proj = obj.AddComponent<EnemyProjectile>();
                _pool.Add(proj);
            }

            _isInitialized = true;
        }

        private GameObject CreateDefaultPlasmaOrbTemplate()
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            template.name = "EnemyPlasmaOrb_Template";
            template.transform.localScale = Vector3.one * 1.2f;

            // Xóa collider v?t lý m?c d?nh d? thay b?ng trigger
            var col = template.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var sphereCol = template.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.45f; // Hitbox 35% kích thu?c th? giác

            // V?t li?u phát sáng d? cam
            Renderer rend = template.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = null;
#if UNITY_EDITOR
                mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Materials/PlasmaFire.mat");
                if (mat == null)
                {
                    mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Fire/CFX3_FireBulk ADD.mat");
                }
#endif
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = new Color(1f, 0.35f, 0.05f, 1f);
                }
                rend.sharedMaterial = mat;
            }

            // G?n TrailRenderer d?i ruy bang 5-8 mét
            TrailRenderer trail = template.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.8f;
            trail.endWidth = 0.05f;
            trail.material = rend != null ? rend.sharedMaterial : null;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0.0f), new GradientColorKey(new Color(1f, 0.2f, 0.0f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trail.colorGradient = gradient;

            template.AddComponent<EnemyProjectile>();
            template.SetActive(false);
            return template;
        }

        /// <summary>
        /// Sinh viên d?n Plasma dón d?u ho?c bám du?i v?i v?n t?c tuong d?i chu?n m?c.
        /// </summary>
        public EnemyProjectile SpawnProjectile(Vector3 startPosition, Vector3 direction, float speed, bool isHeadOn)
        {
            if (!_isInitialized) InitializePool();

            EnemyProjectile available = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    available = _pool[i];
                    break;
                }
            }

            // N?u thi?u d?n thì t? d?ng m? r?ng thêm 1 viên
            if (available == null)
            {
                if (projectilePrefab == null) projectilePrefab = CreateDefaultPlasmaOrbTemplate();
                GameObject obj = Instantiate(projectilePrefab, transform);
                available = obj.GetComponent<EnemyProjectile>();
                if (available == null) available = obj.AddComponent<EnemyProjectile>();
                _pool.Add(available);
            }

            available.Initialize(startPosition, direction, speed, isHeadOn, VirtualRailAnchor.Instance);
            return available;
        }

        /// <summary>
        /// Xóa s?ch toàn b? d?n d?ch dang bay (s? d?ng b?i Smart Bomb).
        /// </summary>
        public void ClearAllActiveProjectiles()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    _pool[i].DestroyByBomb();
                }
            }
        }
    }
}
