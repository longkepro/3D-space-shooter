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
        [SerializeField] private Vector2 shipNormalizedPos;
        [SerializeField] private VirtualRailConfig.FrustumBounds shipBounds;
        private Vector2 currentVelocity;
        private Thruster[] thrusters;

        public Vector2 LocalPos2D => localPos2D;
        public Vector2 NormalizedPos => shipNormalizedPos;
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
            if (anchor == null || anchor.config == null) return;

            var cfg = anchor.config;
            if (mainCamera == null) mainCamera = Camera.main;

            // 1. Tính toán biên trong Frustum chính xác tại mặt phẳng Z = 0 của tàu
            shipBounds = cfg.CalculateFrustumBounds(mainCamera, anchor, 0f, cfg.shipViewportRatio);

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

            float rollNorm = 0f;
            float pitchNorm = 0f;

            if (cfg.enableIndependentControls)
            {
                // ==================== CHẾ ĐỘ ĐIỀU KHIỂN ĐỘC LẬP BẰNG PHÍM ====================
                float inputX = Input.GetAxis("Horizontal");
                float inputY = Input.GetAxis("Vertical");

                if (cfg.useDirectAnalogMapping)
                {
                    // Ánh xạ trực tiếp
                    shipNormalizedPos.x = Mathf.Clamp(inputX, -1f, 1f);
                    shipNormalizedPos.y = Mathf.Clamp(inputY, -1f, 1f);
                }
                else
                {
                    // Hướng A: Tích lũy vị trí tự do (Free Roam - nhả phím đứng yên tại chỗ)
                    float speedNormX = (halfW > 0.001f) ? (cfg.shipSpeedX / halfW) : 1.5f;
                    float speedNormY = (halfH > 0.001f) ? (cfg.shipSpeedY / halfH) : 1.5f;

                    shipNormalizedPos.x = Mathf.Clamp(shipNormalizedPos.x + inputX * speedNormX * Time.deltaTime, -1f, 1f);
                    shipNormalizedPos.y = Mathf.Clamp(shipNormalizedPos.y + inputY * speedNormY * Time.deltaTime, -1f, 1f);

                    // Phím C: Giữ để chủ động đưa tàu lướt êm về trung tâm khi cần
                    if (Input.GetKey(KeyCode.C))
                    {
                        shipNormalizedPos = Vector2.MoveTowards(shipNormalizedPos, Vector2.zero, 2.5f * Time.deltaTime);
                    }
                }

                // Nội suy vị trí mục tiêu trên mặt phẳng tàu
                float targetX = shipNormalizedPos.x >= 0f
                    ? Mathf.Lerp(shipBounds.opticalCenter.x, shipBounds.maxX, shipNormalizedPos.x)
                    : Mathf.Lerp(shipBounds.opticalCenter.x, shipBounds.minX, -shipNormalizedPos.x);

                float targetY = shipNormalizedPos.y >= 0f
                    ? Mathf.Lerp(shipBounds.opticalCenter.y, shipBounds.maxY, shipNormalizedPos.y)
                    : Mathf.Lerp(shipBounds.opticalCenter.y, shipBounds.minY, -shipNormalizedPos.y);

                Vector2 targetPos = new Vector2(targetX, targetY);

                // Tàu lướt êm ái tới vị trí đích
                localPos2D = Vector2.SmoothDamp(localPos2D, targetPos, ref currentVelocity, cfg.smoothDampLag);
                localPos2D = shipBounds.Clamp(localPos2D);

                // Lượn nghiêng khí động học trực tiếp từ tín hiệu phím
                rollNorm = inputX;
                pitchNorm = inputY;
            }
            else
            {
                // ==================== CHẾ ĐỘ BÁM THEO TÂM NGẮM CŨ (PHỤ THUỘC) ====================
                if (reticle != null)
                {
                    float normX = reticle.NormalizedX;
                    float normY = reticle.NormalizedY;

                    float targetX = normX >= 0f
                        ? Mathf.Lerp(shipBounds.opticalCenter.x, shipBounds.maxX, normX)
                        : Mathf.Lerp(shipBounds.opticalCenter.x, shipBounds.minX, -normX);

                    float targetY = normY >= 0f
                        ? Mathf.Lerp(shipBounds.opticalCenter.y, shipBounds.maxY, normY)
                        : Mathf.Lerp(shipBounds.opticalCenter.y, shipBounds.minY, -normY);

                    Vector2 targetPosOnShipPlane = new Vector2(targetX, targetY);
                    localPos2D = Vector2.SmoothDamp(localPos2D, targetPosOnShipPlane, ref currentVelocity, cfg.smoothDampLag);
                    localPos2D = shipBounds.Clamp(localPos2D);

                    float deltaX = targetPosOnShipPlane.x - localPos2D.x;
                    float deltaY = targetPosOnShipPlane.y - localPos2D.y;

                    rollNorm = (halfW > 0.001f) ? (deltaX / (halfW * 0.5f)) : 0f;
                    pitchNorm = (halfH > 0.001f) ? (deltaY / (halfH * 0.5f)) : 0f;
                }
            }

            // Cập nhật vị trí cục bộ của Ship Container
            transform.localPosition = LocalPosition3D;

            // Xoay khí động học (Roll, Pitch, Yaw)
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
