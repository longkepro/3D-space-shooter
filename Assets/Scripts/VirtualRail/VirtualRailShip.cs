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
        public VirtualRailInputReader inputReader;
        public Transform shipVisualMesh;
        private Camera mainCamera;

        [Header("Runtime State")]
        [SerializeField] private Vector2 localPos2D;
        [SerializeField] private Vector2 shipNormalizedPos;
        [SerializeField] private VirtualRailConfig.FrustumBounds shipBounds;
        private Vector2 currentVelocity;
        private Thruster[] thrusters;

        [Header("Barrel Roll Deflection (TDD v1.0.0 Phần I.5)")]
        [SerializeField] private bool isDeflecting = false;
        [SerializeField] private float barrelRollDuration = 0.40f;
        private bool isRolling = false;
        private float rollTimer = 0f;
        private float rollDirection = 0f;
        private float currentBarrelRollAngle = 0f;
        private float lastTapTimeA = -1f;
        private float lastTapTimeD = -1f;
        private const float DOUBLE_TAP_THRESHOLD = 0.28f;

        public bool IsDeflecting => isDeflecting;

        public Vector2 LocalPos2D => localPos2D;
        public Vector2 NormalizedPos => shipNormalizedPos;
        public VirtualRailConfig.FrustumBounds ShipBounds => shipBounds;
        public Vector2 CurrentLimit => new Vector2(shipBounds.HalfWidth, shipBounds.HalfHeight);
        public Vector3 LocalPosition3D => new Vector3(localPos2D.x, localPos2D.y, 0f);
        public Vector3 WorldPosition => anchor != null ? anchor.ToWorldPoint(LocalPosition3D) : transform.position;

        private void Awake()
        {
            VirtualRailAnchor.PlayerShipTransform = transform;
            thrusters = GetComponentsInChildren<Thruster>();
            if (shipVisualMesh == null && transform.childCount > 0)
            {
                shipVisualMesh = transform.GetChild(0);
            }
        }

        private void OnDestroy()
        {
            if (VirtualRailAnchor.PlayerShipTransform == transform)
            {
                VirtualRailAnchor.PlayerShipTransform = null;
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (inputReader == null && anchor != null) inputReader = anchor.GetComponentInChildren<VirtualRailInputReader>();

            // Nếu anchor/rig chưa có InputReader (do dùng prefab cũ), tự động thêm vào!
            if (inputReader == null)
            {
                Transform rootTransform = (anchor != null) ? anchor.transform : transform;
                inputReader = rootTransform.gameObject.AddComponent<VirtualRailInputReader>();
                if (anchor != null) inputReader.config = anchor.config;
            }
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
                // ==================== CHẾ ĐỘ ĐIỀU KHIỂN ĐỘC LẬP (PHÍM / JOYSTICK TRÁI) ====================
                Vector2 moveInput = (inputReader != null)
                    ? inputReader.GetShipMovement()
                    : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

                float inputX = moveInput.x;
                float inputY = moveInput.y;

                if (cfg.useDirectAnalogMapping)
                {
                    // Ánh xạ trực tiếp
                    shipNormalizedPos.x = Mathf.Clamp(inputX, -1f, 1f);
                    shipNormalizedPos.y = Mathf.Clamp(inputY, -1f, 1f);
                }
                else
                {
                    // Hướng A: Tích lũy vị trí tự do (Free Roam - nhả phím đứng yên tại chỗ)
                    float effectiveSpeedX = cfg.useGoldenRatioMotion
                        ? (anchor != null ? anchor.CurrentSpeed * cfg.goldenRatioX : cfg.shipSpeedX)
                        : cfg.shipSpeedX;

                    float effectiveSpeedY = cfg.useGoldenRatioMotion
                        ? (anchor != null ? anchor.CurrentSpeed * cfg.goldenRatioY : cfg.shipSpeedY)
                        : cfg.shipSpeedY;

                    float speedNormX = (halfW > 0.001f) ? (effectiveSpeedX / halfW) : 1.5f;
                    float speedNormY = (halfH > 0.001f) ? (effectiveSpeedY / halfH) : 1.5f;

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

                // Độ trễ lò xo thích ứng động học (khống chế S_drift <= 2.5m)
                float adaptiveLag = (anchor != null) ? cfg.CalculateAdaptiveLag(anchor.CurrentSpeed) : cfg.smoothDampLag;

                // Tàu lướt êm ái tới vị trí đích
                localPos2D = Vector2.SmoothDamp(localPos2D, targetPos, ref currentVelocity, adaptiveLag);
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
                    float adaptiveLag = (anchor != null) ? cfg.CalculateAdaptiveLag(anchor.CurrentSpeed) : cfg.smoothDampLag;
                    localPos2D = Vector2.SmoothDamp(localPos2D, targetPosOnShipPlane, ref currentVelocity, adaptiveLag);
                    localPos2D = shipBounds.Clamp(localPos2D);

                    float deltaX = targetPosOnShipPlane.x - localPos2D.x;
                    float deltaY = targetPosOnShipPlane.y - localPos2D.y;

                    rollNorm = (halfW > 0.001f) ? (deltaX / (halfW * 0.5f)) : 0f;
                    pitchNorm = (halfH > 0.001f) ? (deltaY / (halfH * 0.5f)) : 0f;
                }
            }

            // ==================== DOUBLE TAP DETECT: BARREL ROLL DEFLECTION ====================
            if (cfg.enableBarrelRollDeflection)
            {
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Q))
                {
                    if (Time.time - lastTapTimeA <= DOUBLE_TAP_THRESHOLD || Input.GetKeyDown(KeyCode.Q))
                    {
                        TriggerBarrelRoll(-1f);
                    }
                    lastTapTimeA = Time.time;
                }
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.E))
                {
                    if (Time.time - lastTapTimeD <= DOUBLE_TAP_THRESHOLD || Input.GetKeyDown(KeyCode.E))
                    {
                        TriggerBarrelRoll(1f);
                    }
                    lastTapTimeD = Time.time;
                }
            }

            // Cập nhật trạng thái Barrel Roll
            if (isRolling)
            {
                rollTimer += Time.deltaTime;
                float t = Mathf.Clamp01(rollTimer / barrelRollDuration);
                // Xoay 360 độ quanh trục Z bằng hàm làm mượt Cosine
                currentBarrelRollAngle = -rollDirection * 360f * (0.5f - 0.5f * Mathf.Cos(t * Mathf.PI));

                if (t >= 1.0f)
                {
                    isRolling = false;
                    isDeflecting = false;
                    currentBarrelRollAngle = 0f;
                }
            }

            // Cập nhật vị trí cục bộ của Ship Container
            transform.localPosition = LocalPosition3D;

            // Xoay khí động học (Roll, Pitch, Yaw)
            float targetRoll = Mathf.Clamp(-rollNorm * cfg.maxRollAngle, -cfg.maxRollAngle, cfg.maxRollAngle) * kDamp;
            float targetPitch = Mathf.Clamp(-pitchNorm * cfg.maxPitchAngle, -cfg.maxPitchAngle, cfg.maxPitchAngle);
            float targetYaw = Mathf.Clamp(rollNorm * cfg.maxYawAngle, -cfg.maxYawAngle, cfg.maxYawAngle);

            float totalRoll = targetRoll + currentBarrelRollAngle;
            Quaternion targetRotation = Quaternion.Euler(targetPitch, targetYaw, totalRoll);

            // Xoay visual mesh (tốc độ quay nhanh hơn khi đang lộn vòng)
            Transform meshTransform = (shipVisualMesh != null) ? shipVisualMesh : transform;
            float slerpSpeed = isRolling ? 35f : cfg.rotationSlerpSpeed;
            meshTransform.localRotation = Quaternion.Slerp(meshTransform.localRotation, targetRotation, slerpSpeed * Time.deltaTime);

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

        public void TriggerBarrelRoll(float direction)
        {
            if (isRolling) return;
            var cfg = (anchor != null) ? anchor.config : null;
            if (cfg != null && !cfg.enableBarrelRollDeflection) return;

            barrelRollDuration = (cfg != null) ? cfg.barrelRollDuration : 0.40f;
            isRolling = true;
            isDeflecting = true;
            rollTimer = 0f;
            rollDirection = Mathf.Sign(direction);

            // Xung lực lách ngang nhẹ khi lộn vòng né đạn (Emergency Evasion)
            shipNormalizedPos.x = Mathf.Clamp(shipNormalizedPos.x + rollDirection * 0.18f, -1f, 1f);

            Debug.Log($"<color=cyan><b>[BARREL ROLL DEFLECTION]</b></color> Kích hoạt lộn cánh {(rollDirection > 0 ? "Phải" : "Trái")}! Phản xạ đạn 100% trong {barrelRollDuration:F2}s.");
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
