using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Thân tàu bám theo Tâm ngắm (Position Tracking with Elastic Lag & Aerodynamic Banking).
    /// Giới hạn trong Khung biên trong (Inner Box ~65% Camera Viewport) để bảo vệ sải cánh.
    /// </summary>
    [DisallowMultipleComponent]
    public class VirtualRailShip : MonoBehaviour
    {
        [Header("References")]
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public Transform shipVisualMesh;
        private Camera mainCamera;

        [Header("Runtime State")]
        [SerializeField] private Vector2 localPos2D;
        [SerializeField] private VirtualRailConfig.FrustumBounds shipBounds;
        private Vector2 currentVelocity;
        private Thruster[] thrusters;

        public Vector2 LocalPos2D => localPos2D;
        public VirtualRailConfig.FrustumBounds ShipBounds => shipBounds;
        public Vector2 CurrentLimit => new Vector2(shipBounds.HalfWidth, shipBounds.HalfHeight);
        public Vector3 LocalPosition3D => new Vector3(localPos2D.x, localPos2D.y, 0f);
        public Vector3 WorldPosition => anchor != null ? anchor.ToWorldPoint(LocalPosition3D) : transform.position;

        private void Awake()
        {
            thrusters = GetComponentsInChildren<Thruster>();
            if (shipVisualMesh == null && transform.childCount > 0)
            {
                shipVisualMesh = transform.GetChild(0);
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
        }

        private void Update()
        {
            if (anchor == null || anchor.config == null || reticle == null) return;

            var cfg = anchor.config;
            if (mainCamera == null) mainCamera = Camera.main;

            // 1. Tính toán biên trong Frustum chính xác tại mặt phẳng Z = 0 của tàu
            shipBounds = cfg.CalculateFrustumBounds(mainCamera, anchor, 0f, cfg.shipViewportRatio);

            // 2. Lấy tọa độ chuẩn hóa không thiên lệch [-1, 1] từ tâm ngắm
            float normX = reticle.NormalizedX;
            float normY = reticle.NormalizedY;

            // Nội suy vị trí mục tiêu từ tâm quang học (opticalCenter) tới đúng các mép Frustum của tàu
            float targetX = normX >= 0f
                ? Mathf.Lerp(shipBounds.opticalCenter.x, shipBounds.maxX, normX)
                : Mathf.Lerp(shipBounds.opticalCenter.x, shipBounds.minX, -normX);

            float targetY = normY >= 0f
                ? Mathf.Lerp(shipBounds.opticalCenter.y, shipBounds.maxY, normY)
                : Mathf.Lerp(shipBounds.opticalCenter.y, shipBounds.minY, -normY);

            Vector2 targetPosOnShipPlane = new Vector2(targetX, targetY);

            // 3. Tàu bám theo mục tiêu bằng hàm suy giảm chấn SmoothDamp mượt mà
            localPos2D = Vector2.SmoothDamp(localPos2D, targetPosOnShipPlane, ref currentVelocity, cfg.smoothDampLag);

            // Kẹp an toàn trong Khung biên trong (Inner Box: 75% Viewport)
            localPos2D = shipBounds.Clamp(localPos2D);

            // Cập nhật vị trí cục bộ của Ship Container
            transform.localPosition = LocalPosition3D;

            // 4. Tính toán góc xoay khí động học dựa trên độ trễ thực tế trên mặt phẳng tàu
            float deltaX = targetPosOnShipPlane.x - localPos2D.x;
            float deltaY = targetPosOnShipPlane.y - localPos2D.y;

            float halfW = shipBounds.HalfWidth;
            float halfH = shipBounds.HalfHeight;

            // Hệ số hãm biên (Edge Angle Damping) tính theo tâm quang học
            float kDamp = 1.0f;
            if (cfg.enableEdgeDamping && halfW > 0.001f)
            {
                float edgeRatio = Mathf.Clamp01(Mathf.Abs(localPos2D.x - shipBounds.opticalCenter.x) / halfW);
                kDamp = 1.0f - (edgeRatio * edgeRatio);
                float minRatio = cfg.minEdgeRollAngle / Mathf.Max(cfg.maxRollAngle, 1f);
                kDamp = Mathf.Max(kDamp, minRatio);
            }

            // Chuẩn hóa độ lệch theo biên của tàu để góc lượn tự nhiên và tỉ lệ
            float rollNorm = (halfW > 0.001f) ? (deltaX / (halfW * 0.5f)) : 0f;
            float pitchNorm = (halfH > 0.001f) ? (deltaY / (halfH * 0.5f)) : 0f;

            float targetRoll = Mathf.Clamp(-rollNorm * cfg.maxRollAngle, -cfg.maxRollAngle, cfg.maxRollAngle) * kDamp;
            float targetPitch = Mathf.Clamp(-pitchNorm * cfg.maxPitchAngle, -cfg.maxPitchAngle, cfg.maxPitchAngle);
            float targetYaw = Mathf.Clamp(rollNorm * cfg.maxYawAngle, -cfg.maxYawAngle, cfg.maxYawAngle);

            Quaternion targetRotation = Quaternion.Euler(targetPitch, targetYaw, targetRoll);

            // Xoay visual mesh
            Transform meshTransform = (shipVisualMesh != null) ? shipVisualMesh : transform;
            meshTransform.localRotation = Quaternion.Slerp(meshTransform.localRotation, targetRotation, cfg.rotationSlerpSpeed * Time.deltaTime);

            // Cập nhật hiệu ứng động cơ
            float boostIntensity = anchor.CurrentSpeed > cfg.forwardSpeed ? 1.0f : 0.5f;
            if (thrusters != null)
            {
                foreach (var t in thrusters)
                {
                    if (t != null) t.Intensity(boostIntensity);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (anchor != null && anchor.config != null)
            {
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
                Gizmos.matrix = Matrix4x4.TRS(anchor.transform.position, anchor.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(
                    new Vector3(shipBounds.Center.x, shipBounds.Center.y, 0f),
                    new Vector3(shipBounds.Width, shipBounds.Height, 0.1f)
                );
            }
        }
    }
}
