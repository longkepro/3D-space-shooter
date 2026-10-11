using System;
using UnityEngine;

namespace VirtualRail
{
    public enum RingType
    {
        SilverRing = 0, // Hồi phục 25% Khiên (Shield)
        GoldRing = 1,   // Thu thập đủ 3 vòng sẽ nâng cấp Laser (Twin Laser)
        BombSupply = 2  // Tặng thêm 1 quả Smart Bomb
    }

    /// <summary>
    /// Vòng năng lượng / Vật phẩm thu thập trên đường bay theo chuẩn Star Fox 64 & TDD v1.0.0.
    /// Hoạt hóa xoay liên tục, tự động hồi phục khiên hoặc nâng cấp vũ khí khi người chơi bay xuyên qua.
    /// Tuân thủ nguyên tắc 0 GC Alloc.
    /// </summary>
    [DisallowMultipleComponent]
    public class CollectibleRing : MonoBehaviour, IPoolableEntity
    {
        [Header("Ring Type")]
        [SerializeField] private RingType _type = RingType.SilverRing;
        [SerializeField] private int _scoreValue = 500;

        [Header("Animation")]
        [SerializeField] private float _rotationSpeed = 90f;
        [SerializeField] private float _bobAmplitude = 0.5f;
        [SerializeField] private float _bobFrequency = 2f;

        private Vector3 _basePosition;
        private float _animTimer = 0f;
        private bool _isCollected = false;
        private Collider _collider;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _colorPropId = Shader.PropertyToID("_Color");
        private static readonly int _emissionColorPropId = Shader.PropertyToID("_EmissionColor");

        public Action<CollectibleRing> OnRecycle;
        public RingType Type => _type;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider == null)
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 2.5f; // Bán kính vòng thu thập rộng rãi
                _collider = col;
            }
            else
            {
                _collider.isTrigger = true;
            }

            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public void Initialize(RingType type, Vector3 position)
        {
            _type = type;
            _basePosition = position;
            transform.position = position;
            _animTimer = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _isCollected = false;

            if (_collider != null) _collider.enabled = true;

            ApplyVisualColor();
            gameObject.SetActive(true);
        }

        private void ApplyVisualColor()
        {
            Color baseColor;
            switch (_type)
            {
                case RingType.SilverRing:
                    baseColor = new Color(0.4f, 0.9f, 1.0f); // Bạc ánh lam (Hồi phục)
                    _scoreValue = 500;
                    break;
                case RingType.GoldRing:
                    baseColor = new Color(1.0f, 0.85f, 0.1f); // Vàng kim (Nâng cấp)
                    _scoreValue = 1000;
                    break;
                case RingType.BombSupply:
                    baseColor = new Color(1.0f, 0.25f, 0.2f); // Đỏ thẫm (Bom)
                    _scoreValue = 800;
                    break;
                default:
                    baseColor = Color.white;
                    break;
            }

            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] == null) continue;
                    _renderers[i].GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(_colorPropId, baseColor);
                    _propBlock.SetColor(_emissionColorPropId, baseColor * 2.0f);
                    _renderers[i].SetPropertyBlock(_propBlock);
                }
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            _animTimer += Time.deltaTime;

            // Xoay quanh trục Z và Y tạo hiệu ứng lơ lửng 3D
            transform.Rotate(0f, _rotationSpeed * Time.deltaTime, _rotationSpeed * 0.5f * Time.deltaTime, Space.Self);

            // Nhấp nhô hình sin
            float bobOffset = Mathf.Sin(_animTimer * _bobFrequency) * _bobAmplitude;
            transform.position = _basePosition + Vector3.up * bobOffset;

            // Thu hồi khi tụt lại phía sau người chơi quá xa
            if (VirtualRailAnchor.Instance != null)
            {
                if (transform.position.z < VirtualRailAnchor.Instance.transform.position.z - 30f)
                {
                    Recycle();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<VirtualRailShip>() != null)
            {
                Collect(other);
            }
        }

        public void Collect(Collider collector = null)
        {
            if (_isCollected) return;
            _isCollected = true;

            // 1. Cộng điểm
            GameEventManager.IncrementScore(_scoreValue);
            ComboScoringEngine.Instance?.RegisterSingleKill(_scoreValue);

            // 2. Kích hoạt hiệu ứng và âm thanh tương ứng loại vòng
            switch (_type)
            {
                case RingType.SilverRing:
                    AudioManager.Instance?.PlaySFX2D(AudioSFXType.RingCollect, 0.95f);
                    // Hồi phục 25% Khiên
                    Shield shield = null;
                    if (collector != null)
                    {
                        shield = collector.GetComponentInParent<Shield>();
                        if (shield == null) shield = collector.GetComponentInChildren<Shield>();
                    }
                    if (shield == null)
                    {
                        var playerObj = VirtualRailAnchor.PlayerShipTransform != null ? VirtualRailAnchor.PlayerShipTransform.gameObject : GameObject.FindGameObjectWithTag("Player");
                        if (playerObj != null)
                        {
                            shield = playerObj.GetComponentInChildren<Shield>();
                        }
                    }
                    if (shield != null)
                    {
                        shield.Heal(3); // 3/10 ~ 30% máu
                    }
                    Debug.Log("<color=cyan><b>[SILVER RING]</b></color> Thu thập Vòng Bạc! Hồi phục khiên +500 PTS");
                    break;

                case RingType.GoldRing:
                    // Phát âm thanh hấp thụ năng lượng viễn tưởng (Gold_Capture.mp3)
                    AudioManager.Instance?.PlaySFX2D(AudioSFXType.RingGold, 1.0f);
                    // Nạp tiến trình nâng cấp vũ khí
                    if (VirtualRailWeapon.Instance != null)
                    {
                        VirtualRailWeapon.Instance.CollectGoldRing();
                    }
                    Debug.Log("<color=yellow><b>[GOLD RING]</b></color> Thu thập Vòng Vàng! Hấp thụ năng lượng nâng cấp laser +1000 PTS");
                    break;

                case RingType.BombSupply:
                    // Phát âm thanh nạp đạn vũ khí viễn tưởng (Reload.mp3)
                    AudioManager.Instance?.PlaySFX2D(AudioSFXType.BombSupply, 1.0f);
                    // Bổ sung 1 quả bom thông minh
                    if (SmartBombModule.Instance != null)
                    {
                        SmartBombModule.Instance.AddBomb(1);
                    }
                    Debug.Log("<color=red><b>[BOMB SUPPLY]</b></color> Thu thập Bom Tiếp Tế (Vòng Đỏ Thẫm)! Gài đạn nén +1 Smart Bomb");
                    break;
            }

            Recycle();
        }

        public void Recycle()
        {
            _isCollected = true;
            gameObject.SetActive(false);
            OnRecycle?.Invoke(this);
        }

        public void ResetState()
        {
            _isCollected = false;
            if (_collider != null) _collider.enabled = true;
        }
    }
}
