using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Component chuyên trách hiển thị HUD và hệ tâm ngắm 2 tầng (Dual Reticle Presentation).
    /// Tuân thủ nguyên tắc Unidirectional Data Flow (Điều 4 GEMINI.md) và 0 GC Allocation.
    /// Toàn bộ phép chiếu màn hình thực thi trong LateUpdate() để đồng bộ 100% sau CameraRig và Ship.
    /// </summary>
    [DisallowMultipleComponent]
    public class DualReticleHUD : MonoBehaviour
    {
        [Header("References")]
        public VirtualRailAnchor anchor;
        public VirtualRailReticle reticle;
        public VirtualRailShip ship;
        public RectTransform farCrosshairUI;
        public RectTransform nearCrosshairUI;
        public Image farCrosshairImage;
        public Image nearCrosshairImage;

        private Camera mainCamera;
        private Color originalFarColor = new Color(0.1f, 0.95f, 1f, 0.20f);
        private Color hoverFarColor = new Color(1f, 1f, 1f, 0.55f);
        private bool isMouseHovering = false;

        public bool IsMouseHovering => isMouseHovering;

        private void Start()
        {
            if (anchor == null) anchor = GetComponentInParent<VirtualRailAnchor>();
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
            if (ship == null) ship = FindAnyObjectByType<VirtualRailShip>();
            if (mainCamera == null) mainCamera = Camera.main;

            var cfg = anchor != null ? anchor.config : null;
            EnsureCrosshairStyling(cfg);
        }

        private void LateUpdate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            var cfg = anchor != null ? anchor.config : null;
            if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
            if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();

            // 1. Tâm ngắm xa (Outer Box Cyan 56x56) - Chiếu từ reticle.WorldPosition
            if (farCrosshairUI != null && reticle != null)
            {
                Vector3 farScreenPos = mainCamera.WorldToScreenPoint(reticle.WorldPosition);
                if (farScreenPos.z > 0)
                {
                    farCrosshairUI.position = farScreenPos;
                }
            }

            // 2. Tâm ngắm gần (Inner Crosshair Emerald Green 28x28) - Chiếu trục nòng súng (Boresight)
            if (cfg != null && cfg.enableDualReticle)
            {
                if (nearCrosshairUI != null)
                {
                    Vector3 boresightPoint;
                    float targetDist = (cfg.convergenceDistance > 10f) ? cfg.convergenceDistance : 150f;

                    if (ship != null)
                    {
                        Transform meshT = (ship.VisualMesh != null) ? ship.VisualMesh : ship.transform;
                        boresightPoint = ship.transform.position + meshT.forward * targetDist;
                    }
                    else if (anchor != null)
                    {
                        boresightPoint = anchor.transform.position + anchor.transform.forward * targetDist;
                    }
                    else
                    {
                        boresightPoint = transform.position + transform.forward * targetDist;
                    }

                    Vector3 nearScreenPos = mainCamera.WorldToScreenPoint(boresightPoint);
                    if (nearScreenPos.z > 0)
                    {
                        nearCrosshairUI.position = nearScreenPos;
                        if (!nearCrosshairUI.gameObject.activeSelf) nearCrosshairUI.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if (nearCrosshairUI != null && nearCrosshairUI.gameObject.activeSelf)
                {
                    nearCrosshairUI.gameObject.SetActive(false);
                }
            }

            // 3. Cập nhật trạng thái chuột chạm tâm ngắm (Hover-to-fire)
            UpdateMouseHoverState(cfg);
        }

        private void UpdateMouseHoverState(VirtualRailConfig cfg)
        {
            if (cfg == null || !cfg.enableMouseHoverFire || farCrosshairUI == null)
            {
                isMouseHovering = false;
                if (reticle != null) reticle.SetMouseTouching(false);
                if (farCrosshairImage != null) farCrosshairImage.color = originalFarColor;
                return;
            }

            Vector2 mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            {
                isMouseHovering = false;
                if (reticle != null) reticle.SetMouseTouching(false);
                if (farCrosshairImage != null) farCrosshairImage.color = originalFarColor;
                return;
            }

            Vector3 reticleScreenPos = farCrosshairUI.position;
            float dist = Vector2.Distance(mousePos, new Vector2(reticleScreenPos.x, reticleScreenPos.y));
            float effectiveRadius = cfg.reticleMouseTouchRadius > 5f ? cfg.reticleMouseTouchRadius : 45f;

            isMouseHovering = (dist <= effectiveRadius);
            if (reticle != null) reticle.SetMouseTouching(isMouseHovering);

            if (farCrosshairImage != null)
            {
                farCrosshairImage.color = isMouseHovering ? hoverFarColor : originalFarColor;
            }
        }

        private void EnsureCrosshairStyling(VirtualRailConfig cfg)
        {
            float farSize = cfg != null && cfg.farReticleSize > 10f ? cfg.farReticleSize : 56f;
            float nearSize = cfg != null && cfg.nearReticleSize > 10f ? cfg.nearReticleSize : 28f;

            if (farCrosshairUI != null)
            {
                farCrosshairUI.sizeDelta = new Vector2(farSize, farSize);
                if (farCrosshairImage == null) farCrosshairImage = farCrosshairUI.GetComponent<Image>();
                if (farCrosshairImage != null) farCrosshairImage.color = originalFarColor;

                if (farCrosshairUI.Find("BorderTop") == null)
                {
                    CreateBorderLine(farCrosshairUI, "BorderTop", new Vector2(0, farSize * 0.5f - 1f), new Vector2(farSize, 2f), new Color(0.1f, 0.95f, 1f, 0.90f));
                    CreateBorderLine(farCrosshairUI, "BorderBottom", new Vector2(0, -farSize * 0.5f + 1f), new Vector2(farSize, 2f), new Color(0.1f, 0.95f, 1f, 0.90f));
                    CreateBorderLine(farCrosshairUI, "BorderLeft", new Vector2(-farSize * 0.5f + 1f, 0), new Vector2(2f, farSize), new Color(0.1f, 0.95f, 1f, 0.90f));
                    CreateBorderLine(farCrosshairUI, "BorderRight", new Vector2(farSize * 0.5f - 1f, 0), new Vector2(2f, farSize), new Color(0.1f, 0.95f, 1f, 0.90f));
                }
            }

            if (nearCrosshairUI == null && cfg != null && cfg.enableDualReticle)
            {
                Transform existing = transform.Find("ReticleNearCrosshair");
                if (existing != null)
                {
                    nearCrosshairUI = existing.GetComponent<RectTransform>();
                }
                else
                {
                    nearCrosshairUI = CreateProceduralNearCrosshair(transform, nearSize);
                }
            }

            if (nearCrosshairUI != null)
            {
                nearCrosshairUI.sizeDelta = new Vector2(nearSize, nearSize);
                if (nearCrosshairImage == null) nearCrosshairImage = nearCrosshairUI.GetComponent<Image>();
            }
        }

        private RectTransform CreateProceduralNearCrosshair(Transform parent, float size)
        {
            GameObject nearObj = new GameObject("ReticleNearCrosshair");
            nearObj.transform.SetParent(parent, false);
            RectTransform rt = nearObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            Image img = nearObj.AddComponent<Image>();
            img.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            img.raycastTarget = false;

            // Center Dot 4x4
            GameObject dotObj = new GameObject("CenterDot");
            dotObj.transform.SetParent(nearObj.transform, false);
            RectTransform dotRt = dotObj.AddComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(4f, 4f);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(1f, 1f, 1f, 0.95f);
            dotImg.raycastTarget = false;

            // 4 Ticks
            CreateBorderLine(nearObj.transform, "NearTop", new Vector2(0, size * 0.35f), new Vector2(2f, size * 0.3f), new Color(0.2f, 1f, 0.4f, 0.95f));
            CreateBorderLine(nearObj.transform, "NearBottom", new Vector2(0, -size * 0.35f), new Vector2(2f, size * 0.3f), new Color(0.2f, 1f, 0.4f, 0.95f));
            CreateBorderLine(nearObj.transform, "NearLeft", new Vector2(-size * 0.35f, 0), new Vector2(size * 0.3f, 2f), new Color(0.2f, 1f, 0.4f, 0.95f));
            CreateBorderLine(nearObj.transform, "NearRight", new Vector2(size * 0.35f, 0), new Vector2(size * 0.3f, 2f), new Color(0.2f, 1f, 0.4f, 0.95f));

            return rt;
        }

        private static void CreateBorderLine(Transform parent, string name, Vector2 localPos, Vector2 size, Color color)
        {
            GameObject line = new GameObject(name);
            line.transform.SetParent(parent, false);
            RectTransform rt = line.AddComponent<RectTransform>();
            rt.anchoredPosition = localPos;
            rt.sizeDelta = size;
            Image img = line.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
