using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [SerializeField] private Vector3 _followPosition = new Vector3(0f, 2f, -10f);
    [SerializeField] private float _followDelay = 0.15f;

    private Transform _target;
    private Vector3 _velocity = Vector3.one;
    private float _nextFindTime = 0f;

    private void LateUpdate()
    {
        SmoothFollow();
    }

    private void SmoothFollow()
    {
        if (!TargetPlayer()) return;

        Vector3 toPosition = _target.position + (_target.rotation * _followPosition);
        transform.position = Vector3.SmoothDamp(transform.position, toPosition, ref _velocity, _followDelay);
        transform.LookAt(_target, _target.up);
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            _target = VirtualRail.VirtualRailAnchor.PlayerShipTransform != null
                ? VirtualRail.VirtualRailAnchor.PlayerShipTransform
                : VirtualRail.VirtualRailAnchor.PlayerTransform;

            if (_target == null && Time.time >= _nextFindTime)
            {
                _nextFindTime = Time.time + 1.0f;
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }
        return _target != null;
    }

}