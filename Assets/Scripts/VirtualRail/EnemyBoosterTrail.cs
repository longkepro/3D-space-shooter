using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Điều phối dải booster phản lực màu tím (Purple Booster Ribbon / Speed Trail - 0 GC Alloc).
    /// Chuẩn Star Fox 64:
    /// - Khi tàu chưa vào đội hình (Assembling / PanicScatter): Phụt dải booster tím rực rỡ kéo dài 18m - 25m.
    /// - Khi tàu tạt đầu / tạt sườn / lao bổ nhào (Flanker Strafing, Approach, Exit): Bùng nổ booster tốc độ cao.
    /// - Khi đã khóa vị trí trong đội hình (LockedInSlot) hoặc tuần hành ổn định: Thu ngắn dải booster, chỉ giữ lửa tĩnh.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBoosterTrail : MonoBehaviour
    {
        [Header("Trail Dynamics")]
        [Tooltip("Thời gian lưu vết của dải booster tím khi bứt tốc (giây)")]
        [SerializeField] private float _boostTrailTime = 0.55f;
        [Tooltip("Thời gian lưu vết khi bay tuần hành / đã vào đội hình (giây)")]
        [SerializeField] private float _cruiseTrailTime = 0.05f;
        [Tooltip("Độ rộng dải booster khi bứt tốc")]
        [SerializeField] private float _boostWidthMultiplier = 1.1f;
        [Tooltip("Độ rộng dải booster khi bay tuần hành")]
        [SerializeField] private float _cruiseWidthMultiplier = 0.4f;

        [Header("Light Dynamics")]
        [SerializeField] private float _boostLightIntensity = 3.5f;
        [SerializeField] private float _cruiseLightIntensity = 1.2f;

        [Header("Transition")]
        [SerializeField] private float _transitionSpeed = 4.0f;

        private TrailRenderer[] _trails;
        private Light[] _thrusterLights;
        private FormationWingman _wingman;
        private EnemyLifecycleFSM _lifecycleFSM;
        private EnemyMovement _movement;

        private float _currentTrailTime = 0.05f;
        private float _currentWidth = 0.4f;
        private float _currentLightIntensity = 1.2f;
        private bool _overrideBoost = false;

        public bool IsBoosting
        {
            get
            {
                if (_overrideBoost) return true;

                // 1. Thành viên phi đội: Bật khi chưa vào vị trí (Assembling) hoặc phân tán tháo chạy (PanicScatter)
                if (_wingman != null && _wingman.enabled)
                {
                    if (_wingman.State == FormationWingman.WingmanState.Assembling ||
                        _wingman.State == FormationWingman.WingmanState.PanicScatter)
                    {
                        return true;
                    }
                    if (_wingman.State == FormationWingman.WingmanState.LockedInSlot)
                    {
                        return false;
                    }
                }

                // 2. Máy trạng thái 4 pha (EnemyLifecycleFSM): Bật khi lao vào hành lang bay (Approach) hoặc thoát ly (Exit)
                if (_lifecycleFSM != null && _lifecycleFSM.enabled)
                {
                    if (_lifecycleFSM.CurrentPhase == EnemyLifecycleFSM.LifecyclePhase.Approach ||
                        _lifecycleFSM.CurrentPhase == EnemyLifecycleFSM.LifecyclePhase.Exit)
                    {
                        return true;
                    }
                }

                // 3. Tạt sườn Flanker Strafing (tạt đầu cắt mặt người chơi)
                if (_movement != null && _movement.enabled && _movement.IsFlankerStrafing)
                {
                    return true;
                }

                return false;
            }
        }

        private void Awake()
        {
            CacheComponents();
        }

        public void CacheComponents()
        {
            _trails = GetComponentsInChildren<TrailRenderer>(true);
            _thrusterLights = GetComponentsInChildren<Light>(true);
            _wingman = GetComponent<FormationWingman>();
            _lifecycleFSM = GetComponent<EnemyLifecycleFSM>();
            _movement = GetComponent<EnemyMovement>();

            // Cấu hình ban đầu cho TrailRenderer
            if (_trails != null)
            {
                for (int i = 0; i < _trails.Length; i++)
                {
                    if (_trails[i] == null) continue;
                    _trails[i].time = _cruiseTrailTime;
                    _trails[i].widthMultiplier = _cruiseWidthMultiplier;
                    _trails[i].alignment = LineAlignment.View;
                }
            }
        }

        public void SetOverrideBoost(bool active)
        {
            _overrideBoost = active;
        }

        private void Update()
        {
            bool boosting = IsBoosting;
            float dt = Time.deltaTime;

            float targetTrailTime = boosting ? _boostTrailTime : _cruiseTrailTime;
            float targetWidth = boosting ? _boostWidthMultiplier : _cruiseWidthMultiplier;
            float targetLight = boosting ? _boostLightIntensity : _cruiseLightIntensity;

            _currentTrailTime = Mathf.MoveTowards(_currentTrailTime, targetTrailTime, dt * _transitionSpeed);
            _currentWidth = Mathf.MoveTowards(_currentWidth, targetWidth, dt * _transitionSpeed);
            _currentLightIntensity = Mathf.MoveTowards(_currentLightIntensity, targetLight, dt * (_transitionSpeed * 2.5f));

            bool shouldEmit = boosting || (_currentTrailTime > 0.08f);

            if (_trails != null)
            {
                for (int i = 0; i < _trails.Length; i++)
                {
                    if (_trails[i] == null) continue;
                    _trails[i].time = _currentTrailTime;
                    _trails[i].widthMultiplier = _currentWidth;
                    _trails[i].emitting = shouldEmit;
                }
            }

            if (_thrusterLights != null)
            {
                for (int i = 0; i < _thrusterLights.Length; i++)
                {
                    if (_thrusterLights[i] == null) continue;
                    _thrusterLights[i].intensity = _currentLightIntensity;
                }
            }
        }
    }
}
