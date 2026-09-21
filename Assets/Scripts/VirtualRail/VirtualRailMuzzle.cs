using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Nòng súng chuẩn hóa (Spatial Weapon Muzzle Hardpoint - 0 GC Alloc).
    /// Đóng vai trò là điểm neo không gian (Transform, hướng bắn, hiệu ứng lóe nòng).
    /// Tách biệt hoàn toàn khỏi logic vẽ tia laser hitscan, không chứa LineRenderer hay Light cố định.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailMuzzle : MonoBehaviour
    {
        [Header("Muzzle Configuration")]
        [Tooltip("Góc lệch hướng bắn nếu cần (mặc định thẳng phía trước transform.forward)")]
        [SerializeField] private Vector3 _localFireDirection = Vector3.forward;

        [Header("Optional FX")]
        [Tooltip("Particle hệ thống lóe sáng đầu nòng (Muzzle Flash)")]
        [SerializeField] private ParticleSystem _muzzleFlash;
        [Tooltip("Âm thanh bắn nếu gắn tại nòng")]
        [SerializeField] private AudioSource _audioSource;

        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.TransformDirection(_localFireDirection.normalized);

        private void Awake()
        {
            if (_muzzleFlash == null)
            {
                _muzzleFlash = GetComponentInChildren<ParticleSystem>();
            }
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Kích hoạt hiệu ứng lóe nòng và âm thanh nếu có (0 GC Alloc).
        /// </summary>
        public void PlayFireEffects()
        {
            if (_muzzleFlash != null)
            {
                _muzzleFlash.Play();
            }
            if (_audioSource != null && _audioSource.clip != null)
            {
                _audioSource.Play();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
            Gizmos.DrawRay(transform.position, Forward * 1.5f);
        }
#endif
    }
}
