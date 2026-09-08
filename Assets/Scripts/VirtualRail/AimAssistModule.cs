using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Module Trợ ngắm & Khóa mục tiêu tụ lực (Charge Shot Lock-on).
    /// </summary>
    [DisallowMultipleComponent]
    public class AimAssistModule : MonoBehaviour
    {
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public GameObject missilePrefab;
        public RectTransform lockMarkerUI;
        private Camera mainCamera;

        [Header("Runtime State")]
        [SerializeField] private float currentCharge = 0f;
        [SerializeField] private bool isCharging = false;
        [SerializeField] private Transform lockedTarget;

        public bool IsFullyCharged => anchor != null && anchor.config != null && currentCharge >= anchor.config.chargeTime;
        public Transform LockedTarget => lockedTarget;

        private void Start()
        {
            mainCamera = Camera.main;
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();

            if (lockMarkerUI != null) lockMarkerUI.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (anchor == null || anchor.config == null || reticle == null) return;

            var cfg = anchor.config;

            // Kiểm tra nút tụ lực (Chuột phải / Phím E / Giữ Fire1)
            bool chargeHeld = Input.GetButton("Fire2") || Input.GetKey(KeyCode.E);

            if (chargeHeld)
            {
                isCharging = true;
                currentCharge += Time.deltaTime;

                if (currentCharge >= cfg.chargeTime)
                {
                    // Đã tụ đủ lực -> Quét tìm mục tiêu trong hình nón Lock Cone
                    ScanForLockTarget(cfg);
                }
            }
            else if (isCharging)
            {
                // Nhả nút -> Bắn đạn truy đuổi nếu đã lock được mục tiêu
                if (IsFullyCharged && lockedTarget != null)
                {
                    FireHomingMissile();
                }

                // Reset trạng thái tụ lực
                currentCharge = 0f;
                isCharging = false;
                lockedTarget = null;
                if (lockMarkerUI != null) lockMarkerUI.gameObject.SetActive(false);
            }

            // Cập nhật vị trí UI Lock Marker
            UpdateLockMarkerUI();
        }

        private void ScanForLockTarget(VirtualRailConfig cfg)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Vector3 reticleWorld = reticle.WorldPosition;
            Vector3 railForward = anchor.ForwardTangent;

            float minAngle = cfg.lockConeAngle;
            Transform bestTarget = null;

            float cosLimit = Mathf.Cos(cfg.lockConeAngle * Mathf.Deg2Rad);

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.activeInHierarchy) continue;

                Vector3 toEnemy = enemy.transform.position - reticleWorld;
                float dist = toEnemy.magnitude;

                if (dist > cfg.lockMaxDistance || dist < 1f) continue;

                float cosTheta = Vector3.Dot(toEnemy.normalized, railForward);

                if (cosTheta >= cosLimit)
                {
                    float angle = Mathf.Acos(cosTheta) * Mathf.Rad2Deg;
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        bestTarget = enemy.transform;
                    }
                }
            }

            lockedTarget = bestTarget;
        }

        private void FireHomingMissile()
        {
            if (missilePrefab == null)
            {
                // Nếu chưa có prefab riêng, dùng tia nổ trực tiếp
                var enemy = lockedTarget.GetComponent<EnemyMovement>();
                if (enemy != null) enemy.BlowUp();
                Debug.Log($"[VirtualRail] Charge Shot Hit Locked Target: {lockedTarget.name}");
                return;
            }

            GameObject missileObj = Instantiate(missilePrefab, transform.position, transform.rotation);
            var missile = missileObj.GetComponent<VirtualRailHomingMissile>();
            if (missile == null) missile = missileObj.AddComponent<VirtualRailHomingMissile>();

            missile.target = lockedTarget;
            Debug.Log($"[VirtualRail] Fired Homing Missile at {lockedTarget.name}");
        }

        private void UpdateLockMarkerUI()
        {
            if (lockMarkerUI == null) return;
            if (mainCamera == null) mainCamera = Camera.main;

            if (lockedTarget != null && mainCamera != null)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(lockedTarget.position);
                if (screenPos.z > 0)
                {
                    lockMarkerUI.gameObject.SetActive(true);
                    lockMarkerUI.position = screenPos;
                    return;
                }
            }

            lockMarkerUI.gameObject.SetActive(false);
        }
    }
}
