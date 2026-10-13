using System.Collections.Generic;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Bo dem Object Pool quan ly dan cua ke dich (0 GC Allocation).
    /// Tuan thu TDD v1.0.0 & v2.0.0:
    /// - Quan ly tai su dung 40 vien dan Plasma Orb da tang (Loi sang HDR + Quang Plasma + Ribbon Trail).
    /// - Tich hop quan ly hieu ung no va cham (Impact FX) qua VirtualRailVFXPool.
    /// - Ho tro nap Prefab tinh hoac tu dong kien tao template thu tuc chuan muc neu chay doc lap.
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
        [SerializeField] private int initialCapacity = 64;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject impactVFXPrefab;
        [SerializeField] private Material bulletMaterial;

        private readonly List<EnemyProjectile> _pool = new List<EnemyProjectile>();
        private bool _isInitialized = false;
        private int _recycleIndex = 0;

        public GameObject ProjectilePrefab => projectilePrefab;
        public GameObject ImpactVFXPrefab => impactVFXPrefab;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            InitializePool();
        }

        public void SetProjectilePrefab(GameObject prefab)
        {
            projectilePrefab = prefab;
        }

        public void SetImpactVFXPrefab(GameObject vfxPrefab)
        {
            impactVFXPrefab = vfxPrefab;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                {
                    _pool[i].SetImpactVFXPrefab(impactVFXPrefab);
                }
            }
        }

        public void InitializePool()
        {
            if (_isInitialized) return;

            // 1. Tự động nạp từ VirtualRailConfig nếu có
            var config = (VirtualRailAnchor.Instance != null) ? VirtualRailAnchor.Instance.config : null;
            if (config != null)
            {
                if (projectilePrefab == null && config.enemyProjectilePrefab != null)
                {
                    projectilePrefab = config.enemyProjectilePrefab;
                }
                if (impactVFXPrefab == null && config.enemyImpactVFXPrefab != null)
                {
                    impactVFXPrefab = config.enemyImpactVFXPrefab;
                }
            }

            // 2. Tự động nạp Impact FX nếu chưa được gán
            if (impactVFXPrefab == null)
            {
                impactVFXPrefab = Resources.Load<GameObject>("Prefabs/CFX4_Hit_B_Orange");
#if UNITY_EDITOR
                if (impactVFXPrefab == null)
                {
                    impactVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX4 Hit B (Orange).prefab");
                }
#endif
            }

            // 3. Tự động nạp Prefab đạn chuẩn từ Resources hoặc Prefabs nếu chưa được gán
            if (projectilePrefab == null)
            {
                projectilePrefab = Resources.Load<GameObject>("Prefabs/EnemyProjectile_PlasmaOrb");
                if (projectilePrefab == null)
                {
                    projectilePrefab = Resources.Load<GameObject>("Prefabs/EnemyProjectile_BrightRed");
                }
                if (projectilePrefab == null)
                {
                    projectilePrefab = Resources.Load<GameObject>("EnemyProjectile_PlasmaOrb");
                }
#if UNITY_EDITOR
                if (projectilePrefab == null)
                {
                    projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyProjectile_PlasmaOrb.prefab");
                }
                if (projectilePrefab == null)
                {
                    projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyProjectile_BrightRed.prefab");
                }
#endif
                if (projectilePrefab == null)
                {
                    projectilePrefab = CreateDefaultPlasmaOrbTemplate();
                }
            }

            for (int i = 0; i < initialCapacity; i++)
            {
                GameObject obj = Instantiate(projectilePrefab, transform);
                obj.name = "EnemyBullet_Pooled_" + i;
                obj.SetActive(false);

                var proj = obj.GetComponent<EnemyProjectile>();
                if (proj == null) proj = obj.AddComponent<EnemyProjectile>();
                if (impactVFXPrefab != null)
                {
                    proj.SetImpactVFXPrefab(impactVFXPrefab);
                }
                _pool.Add(proj);
            }

            _isInitialized = true;
        }

        public GameObject CreateDefaultPlasmaOrbTemplate()
        {
            GameObject template = new GameObject("EnemyPlasmaOrb_Template");

            // Collider Hitbox (35% kich thuoc thi giac)
            var sphereCol = template.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.45f;

            // Goc truc quan phuc vu Distance Scale Compensation
            GameObject visualRoot = new GameObject("[Visual_Root]");
            visualRoot.transform.SetParent(template.transform, false);

            // Tang 1: Quang Plasma ngoai (Outer Corona)
            GameObject corona = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            corona.name = "[Corona]";
            corona.transform.SetParent(visualRoot.transform, false);
            corona.transform.localScale = Vector3.one * 1.2f;
            var coronaCol = corona.GetComponent<Collider>();
            if (coronaCol != null)
            {
                if (Application.isPlaying) Destroy(coronaCol);
                else DestroyImmediate(coronaCol);
            }

            Renderer coronaRend = corona.GetComponent<Renderer>();
            if (coronaRend != null)
            {
                Material coronaMat = null;
#if UNITY_EDITOR
                coronaMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Hovl Studio/Procedural fire/Materials/FireSphere1.mat");
                if (coronaMat == null)
                {
                    coronaMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Materials/PlasmaFire.mat");
                }
#endif
                if (coronaMat == null)
                {
                    Shader coronaShader = Shader.Find("Mobile/Particles/Additive")
                        ?? Shader.Find("Particles/Additive")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Sprites/Default");
                    if (coronaShader != null)
                    {
                        coronaMat = new Material(coronaShader);
                        coronaMat.color = new Color(1f, 0.35f, 0.05f, 1f);
                    }
                }
                if (coronaMat != null) coronaRend.sharedMaterial = coronaMat;
            }

            // Tang 2: Loi trang phat sang sieu nhiet (White-Hot Inner Core)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "[Core]";
            core.transform.SetParent(visualRoot.transform, false);
            core.transform.localScale = Vector3.one * 0.55f;
            var coreCol = core.GetComponent<Collider>();
            if (coreCol != null)
            {
                if (Application.isPlaying) Destroy(coreCol);
                else DestroyImmediate(coreCol);
            }

            Renderer coreRend = core.GetComponent<Renderer>();
            if (coreRend != null)
            {
                Material coreMat = null;
#if UNITY_EDITOR
                coreMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/EnemyPlasma_Core.mat");
#endif
                if (coreMat == null)
                {
                    Shader coreShader = Shader.Find("VirtualRail/PlasmaOrbCore")
                        ?? Shader.Find("Mobile/Particles/Additive")
                        ?? Shader.Find("Particles/Additive")
                        ?? Shader.Find("Sprites/Default");
                    if (coreShader != null)
                    {
                        coreMat = new Material(coreShader);
                        coreMat.SetColor("_CoreColor", new Color(3.0f, 2.8f, 2.2f, 1.0f));
                        coreMat.SetColor("_RimColor", new Color(2.5f, 1.0f, 0.1f, 1.0f));
                    }
                }
                if (coreMat != null) coreRend.sharedMaterial = coreMat;
            }

            // Tang 3: Vet duoi Ribbon Trail nang luong
            TrailRenderer trail = template.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.85f;
            trail.endWidth = 0.05f;

            Material trailMat = null;
#if UNITY_EDITOR
            trailMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Hovl Studio/Procedural fire/Materials/Trail67.mat");
            if (trailMat == null)
            {
                trailMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset Store/JMO Assets/Cartoon FX/Materials/Fire/CFX3_FireBulk ADD.mat");
            }
#endif
            if (trailMat == null)
            {
                trailMat = coronaRend != null ? coronaRend.sharedMaterial : null;
            }
            if (trailMat == null)
            {
                Shader trailShader = Shader.Find("Mobile/Particles/Additive")
                    ?? Shader.Find("Particles/Additive")
                    ?? Shader.Find("Sprites/Default");
                if (trailShader != null) trailMat = new Material(trailShader);
            }
            trail.material = trailMat;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.98f, 0.75f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.5f),
                    new GradientColorKey(new Color(0.9f, 0.05f, 0.0f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.95f, 0.0f),
                    new GradientAlphaKey(0.7f, 0.6f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trail.colorGradient = gradient;

            // Tang 4: Hat tia lua tan nang luong bay ve phia sau
            GameObject sparksObj = new GameObject("[Sparks]");
            sparksObj.transform.SetParent(template.transform, false);
            ParticleSystem ps = sparksObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 1.5f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            if (VirtualRailAnchor.Instance != null)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Custom;
                main.customSimulationSpace = VirtualRailAnchor.Instance.transform;
            }
            else
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            main.maxParticles = 40;
            main.loop = true;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            // Gan EnemyProjectile script va cau hinh lien ket
            EnemyProjectile proj = template.AddComponent<EnemyProjectile>();
            proj.visualTransform = visualRoot.transform;
            proj.trailRenderer = trail;
            proj.sphereCollider = sphereCol;
            proj.sparkParticles = ps;
            proj.visualBaseRadius = 1.0f;
            proj.hitboxRadius = 0.45f;
            proj.damage = 1f;
            proj.maxLifetime = 5.0f;
            if (impactVFXPrefab != null)
            {
                proj.SetImpactVFXPrefab(impactVFXPrefab);
            }

            template.SetActive(false);
            return template;
        }

        /// <summary>
        /// Sinh vien dan Plasma don dau hoac bam duoi voi van toc tuong doi chuan muc.
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

            // Neu thieu dan thi tai su dung vien dan cu nhat theo ring buffer (0 GC Alloc, khong Instantiate)
            if (available == null)
            {
                if (_pool.Count >= initialCapacity)
                {
                    available = _pool[_recycleIndex];
                    _recycleIndex = (_recycleIndex + 1) % _pool.Count;
                }
                else
                {
                    if (projectilePrefab == null) projectilePrefab = CreateDefaultPlasmaOrbTemplate();
                    GameObject obj = Instantiate(projectilePrefab, transform);
                    available = obj.GetComponent<EnemyProjectile>();
                    if (available == null) available = obj.AddComponent<EnemyProjectile>();
                    if (impactVFXPrefab != null)
                    {
                        available.SetImpactVFXPrefab(impactVFXPrefab);
                    }
                    _pool.Add(available);
                }
            }

            available.Initialize(startPosition, direction, speed, isHeadOn, VirtualRailAnchor.Instance);
            return available;
        }

        /// <summary>
        /// Xoa sach toan bo dan dich dang bay (su dung boi Smart Bomb).
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

        /// <summary>
        /// Dịch chuyển toàn bộ đạn địch đang bay trong quá trình Tái định vị thế giới (Task 4.3).
        /// </summary>
        public void ShiftOrigin(Vector3 shift)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    _pool[i].ShiftOrigin(shift);
                }
            }
        }
    }
}
