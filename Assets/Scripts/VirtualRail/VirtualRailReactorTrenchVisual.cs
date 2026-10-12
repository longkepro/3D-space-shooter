using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Component quản lý Hiệu ứng Thị giác Lõi Năng Lượng Rãnh Lò Phản Ứng (Dorsal Reactor Trench Visual).
    /// Đáp ứng tiêu chuẩn:
    /// 1. 0 GC Allocation trong Update() bằng cách sử dụng MaterialPropertyBlock và cached property IDs.
    /// 2. Điều phối các trạng thái thị giác: Dormant (Pha 1), Idle Hot Plasma (Pha 2), Overcharging Pulse (Đang sạc),
    ///    Short-Circuit Stun (Bị ngắt chiêu choáng), và Meltdown Runaway (Pha 3).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailReactorTrenchVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer _coreRenderer;
        [SerializeField] private Transform _ventMuzzle;
        [SerializeField] private GameObject _chargeSparksPrefab;

        [Header("State Values")]
        [SerializeField] private float _phaseActive = 0f;
        [SerializeField] private float _chargeProgress = 0f;
        [SerializeField] private float _stunIntensity = 0f;
        [SerializeField] private float _meltdownIntensity = 0f;

        private MaterialPropertyBlock _propBlock;
        private static readonly int _phaseActivePropId = Shader.PropertyToID("_PhaseActive");
        private static readonly int _chargeProgressPropId = Shader.PropertyToID("_ChargeProgress");
        private static readonly int _stunIntensityPropId = Shader.PropertyToID("_StunIntensity");
        private static readonly int _meltdownIntensityPropId = Shader.PropertyToID("_MeltdownIntensity");

        private bool _isDirty = true;
        private float _audioChargeTimer = 0f;

        private void Awake()
        {
            EnsureVisualCore();
            _propBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        private void EnsureVisualCore()
        {
            if (_coreRenderer != null) return;

            Transform visualTrans = transform.Find("[Reactor_Core_Visual]");
            if (visualTrans != null)
            {
                _coreRenderer = visualTrans.GetComponent<Renderer>();
                if (_coreRenderer != null) return;
            }

            // Tự động tạo khối Mesh hiển thị Lõi Năng Lượng Rãnh Lò Phản Ứng
            GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObj.name = "[Reactor_Core_Visual]";
            visualObj.transform.SetParent(transform, false);
            visualObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = new Vector3(4.5f, 1.2f, 48f);

            var primCol = visualObj.GetComponent<Collider>();
            if (primCol != null) Destroy(primCol);

            _coreRenderer = visualObj.GetComponent<Renderer>();
            Shader shader = Shader.Find("VirtualRail/ThermalReactorTrench");
            if (shader != null && _coreRenderer != null)
            {
                _coreRenderer.material = new Material(shader);
            }

            if (_ventMuzzle == null)
            {
                _ventMuzzle = transform.Find("Muzzle_Vent");
            }
        }

        public void SetPhase(BossBattlePhase phase)
        {
            switch (phase)
            {
                case BossBattlePhase.IntroBreach:
                case BossBattlePhase.Phase1_DeckStrafe:
                    _phaseActive = 0f;
                    _meltdownIntensity = 0f;
                    break;
                case BossBattlePhase.Phase2_ReactorTrench:
                    _phaseActive = 1f;
                    _meltdownIntensity = 0f;
                    _stunIntensity = 0f;
                    _chargeProgress = 0f;
                    break;
                case BossBattlePhase.Phase3_MeltdownOverdrive:
                case BossBattlePhase.Defeated:
                    _phaseActive = 1f;
                    _meltdownIntensity = 1f;
                    _chargeProgress = 0f;
                    _stunIntensity = 0f;
                    break;
            }
            _isDirty = true;
            ApplyProperties();
        }

        public void UpdateCharging(float progress)
        {
            _chargeProgress = Mathf.Clamp01(progress);
            _isDirty = true;

            // Phat am thanh tieng keu nap dien cao tan khi bat dau sac
            if (_chargeProgress > 0.05f)
            {
                _audioChargeTimer += Time.deltaTime;
                if (_audioChargeTimer >= 0.45f)
                {
                    _audioChargeTimer = 0f;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX2D(AudioSFXType.ShieldDamage, 0.7f);
                    }
                }
            }
            else
            {
                _audioChargeTimer = 0f;
            }

            ApplyProperties();
        }

        public void SetStunned(bool stunned)
        {
            _stunIntensity = stunned ? 1f : 0f;
            if (stunned)
            {
                _chargeProgress = 0f;
            }
            _isDirty = true;
            ApplyProperties();
        }

        public void TriggerVentEruption(GameObject explosionPrefab)
        {
            _chargeProgress = 0f;
            _isDirty = true;
            ApplyProperties();

            Vector3 spawnPos = (_ventMuzzle != null) ? _ventMuzzle.position : transform.position;
            if (explosionPrefab != null && VirtualRailVFXPool.Instance != null)
            {
                VirtualRailVFXPool.Instance.SpawnVFX(explosionPrefab, spawnPos, Quaternion.identity, null, 2.5f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(AudioSFXType.ExplosionLarge, 1.0f);
            }

            VirtualRailCameraRig.TriggerShake(0.35f);
        }

        private void Update()
        {
            // Trong luc sac hoac bi stun, cap nhat lien tuc de dong bo hieu ung xung nhip
            if (_chargeProgress > 0.001f || _stunIntensity > 0.001f || _meltdownIntensity > 0.001f || _isDirty)
            {
                ApplyProperties();
                _isDirty = false;
            }
        }

        private void ApplyProperties()
        {
            if (_coreRenderer == null) return;

            _coreRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_phaseActivePropId, _phaseActive);
            _propBlock.SetFloat(_chargeProgressPropId, _chargeProgress);
            _propBlock.SetFloat(_stunIntensityPropId, _stunIntensity);
            _propBlock.SetFloat(_meltdownIntensityPropId, _meltdownIntensity);
            _coreRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
