using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Camera Dynamic Framing.
    /// Nằm trong phân cấp của Rail Anchor.
    /// Giữ góc nhìn ổn định, cố định đường chân trời (Fixed Horizon) giống phong cách Star Fox gốc,
    /// tránh việc camera bị lắc lư hoặc xoay liếc theo tàu gây chóng mặt.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailCameraRig : MonoBehaviour
    {
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public VirtualRailShip ship;
        public Camera targetCamera;

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (targetCamera == null) targetCamera = Camera.main;

            // Tiếp quản Camera chính của Scene
            if (targetCamera != null)
            {
                var oldFollow = targetCamera.GetComponent<FollowCam>();
                if (oldFollow != null) oldFollow.enabled = false;

                targetCamera.transform.SetParent(transform, false);
                targetCamera.transform.localPosition = Vector3.zero;
                targetCamera.transform.localRotation = Quaternion.identity;

                if (anchor != null && anchor.config != null)
                {
                    targetCamera.fieldOfView = anchor.config.cameraFOV;
                }
            }
        }

        private void LateUpdate()
        {
            if (anchor == null || anchor.config == null) return;

            var cfg = anchor.config;

            // Đồng bộ góc FOV của ống kính theo cấu hình (Hỗ trợ đổi FOV thời gian thực)
            if (targetCamera != null && Mathf.Abs(targetCamera.fieldOfView - cfg.cameraFOV) > 0.01f)
            {
                targetCamera.fieldOfView = cfg.cameraFOV;
            }

            // 1. Vị trí Camera (Lùi xa phía sau và đặt cao để tầm nhìn thoáng đãng)
            float offsetX = (reticle != null) ? (reticle.LocalX - reticle.CurrentBounds.opticalCenter.x) : 0f;
            float offsetY = (reticle != null) ? (reticle.LocalY - reticle.CurrentBounds.opticalCenter.y) : 0f;

            float targetLocalX = offsetX * cfg.cameraPanFactor;
            float targetLocalY = (offsetY * cfg.cameraPanFactor) + cfg.cameraHeight;
            float targetLocalZ = -cfg.cameraDistance;

            Vector3 targetLocalPos = new Vector3(targetLocalX, targetLocalY, targetLocalZ);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, cfg.cameraSmoothSpeed * Time.deltaTime);

            // 2. Góc xoay Camera (Cố định đường chân trời)
            if (!cfg.enableDynamicLookAt)
            {
                // Giữ góc nhìn chúc nhẹ cố định dọc theo đường ray (5 độ) - Tuyệt đối không rung lắc
                transform.localRotation = Quaternion.Euler(cfg.fixedCameraPitch, 0f, 0f);
            }
            else
            {
                // Chỉ kích hoạt nếu người chơi chủ động bật Look-At trong Config
                Vector3 aimWorld = (reticle != null) ? reticle.WorldPosition : transform.position + transform.forward * 100f;
                Vector3 shipWorld = (ship != null) ? ship.WorldPosition : transform.position + transform.forward * 20f;
                Vector3 lookTargetWorld = Vector3.Lerp(shipWorld, aimWorld, cfg.cameraLookAtWeight);

                Vector3 direction = (lookTargetWorld - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction, anchor.transform.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, cfg.cameraSmoothSpeed * Time.deltaTime);
                }
            }
        }

        private void OnDestroy()
        {
            if (targetCamera != null)
            {
                targetCamera.transform.SetParent(null);
                var oldFollow = targetCamera.GetComponent<FollowCam>();
                if (oldFollow != null) oldFollow.enabled = true;
            }
        }
    }
}
