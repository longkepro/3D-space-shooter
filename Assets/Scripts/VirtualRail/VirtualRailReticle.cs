using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Tâm ngắm 3D dẫn đường (Reticle-Driven Core).
    /// Nhận trực tiếp Input người chơi với 0 độ trễ trên mặt phẳng Z = ConvergenceDistance.
    /// Giới hạn bởi Khung biên ngoài (Outer Box ~85% Camera Viewport).
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailReticle : MonoBehaviour
    {
        public VirtualRailAnchor anchor;
        public RectTransform crosshairUI;
        private Camera mainCamera;

        [Header("Runtime Local Coordinates")]
        [SerializeField] private Vector2 localPos2D;
        [SerializeField] private VirtualRailConfig.FrustumBounds currentBounds;
        [SerializeField] private Vector2 currentNormalizedInput;

        public Vector2 LocalPos2D => localPos2D;
        public float LocalX => localPos2D.x;
        public float LocalY => localPos2D.y;
        public VirtualRailConfig.FrustumBounds CurrentBounds => currentBounds;
        public Vector2 NormalizedPos => currentNormalizedInput;
        public float NormalizedX => currentNormalizedInput.x;
        public float NormalizedY => currentNormalizedInput.y;
        public Vector2 CurrentLimit => new Vector2(currentBounds.HalfWidth, currentBounds.HalfHeight);

        public Vector3 LocalPosition3D => new Vector3(localPos2D.x, localPos2D.y, anchor != null ? anchor.config.convergenceDistance : 150f);
        public Vector3 WorldPosition => anchor != null ? anchor.ToWorldPoint(LocalPosition3D) : transform.position;

        private void Start()
        {
            mainCamera = Camera.main;
            if (anchor == null)
            {
                anchor = GetComponentInParent<VirtualRailAnchor>();
            }
        }

        private void Update()
        {
            if (anchor == null || anchor.config == null) return;

            var cfg = anchor.config;
            if (mainCamera == null) mainCamera = Camera.main;

            // 1. Tính toán biên ngoài Frustum chính xác (Outer Box: 80% - 85% Camera Frustum)
            currentBounds = cfg.CalculateFrustumBounds(mainCamera, anchor, cfg.convergenceDistance, cfg.reticleViewportRatio);

            float inputX = Input.GetAxis("Horizontal");
            float inputY = Input.GetAxis("Vertical");

            if (cfg.useDirectAnalogMapping)
            {
                // Ánh xạ trực tiếp từ analog cần gạt trong dải [-1, 1] (tự động hồi về tâm khi nhả phím)
                currentNormalizedInput.x = Mathf.Clamp(inputX, -1f, 1f);
                currentNormalizedInput.y = Mathf.Clamp(inputY, -1f, 1f);
            }
            else
            {
                // Hướng A: Tích lũy vị trí chuẩn hóa (Free Roam - Không bao giờ bị kéo về trung tâm)
                float speedNormX = (currentBounds.HalfWidth > 0.001f) ? (cfg.reticleSpeedX / currentBounds.HalfWidth) : 1.5f;
                float speedNormY = (currentBounds.HalfHeight > 0.001f) ? (cfg.reticleSpeedY / currentBounds.HalfHeight) : 1.5f;

                currentNormalizedInput.x = Mathf.Clamp(currentNormalizedInput.x + inputX * speedNormX * Time.deltaTime, -1f, 1f);
                currentNormalizedInput.y = Mathf.Clamp(currentNormalizedInput.y + inputY * speedNormY * Time.deltaTime, -1f, 1f);

                // Phím C: Giữ để chủ động đưa tàu & tâm ngắm lướt êm về lại trung tâm khi cần
                if (Input.GetKey(KeyCode.C))
                {
                    currentNormalizedInput = Vector2.MoveTowards(currentNormalizedInput, Vector2.zero, 2.5f * Time.deltaTime);
                }
            }

            // 2. Nội suy tọa độ 3D từ tâm quang học (opticalCenter) tới đúng các mép Frustum
            float targetX = currentNormalizedInput.x >= 0f
                ? Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.maxX, currentNormalizedInput.x)
                : Mathf.Lerp(currentBounds.opticalCenter.x, currentBounds.minX, -currentNormalizedInput.x);

            float targetY = currentNormalizedInput.y >= 0f
                ? Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.maxY, currentNormalizedInput.y)
                : Mathf.Lerp(currentBounds.opticalCenter.y, currentBounds.minY, -currentNormalizedInput.y);

            // Kẹp an toàn trong Khung biên ngoài (Outer Box: 85% Viewport)
            localPos2D = currentBounds.Clamp(new Vector2(targetX, targetY));

            // Cập nhật vị trí transform cục bộ
            transform.localPosition = LocalPosition3D;

            // Cập nhật Crosshair UI
            UpdateCrosshairUI();
        }

        private void UpdateCrosshairUI()
        {
            if (crosshairUI == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(WorldPosition);
            if (screenPos.z > 0)
            {
                crosshairUI.position = screenPos;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(WorldPosition, 1.5f);

            if (anchor != null && anchor.config != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
                Gizmos.matrix = Matrix4x4.TRS(anchor.transform.position, anchor.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(
                    new Vector3(currentBounds.Center.x, currentBounds.Center.y, anchor.config.convergenceDistance),
                    new Vector3(currentBounds.Width, currentBounds.Height, 0.1f)
                );
            }
        }
    }
}
