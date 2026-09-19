using UnityEngine;

public class StarFoxCrosshair : MonoBehaviour
{
    public StarFoxMovement movement;
    public RectTransform crosshairUI;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (movement == null || crosshairUI == null || mainCam == null) return;
        
        // C?c k? don gi?n: Tâm ng?m UI ch? là bóng ma bám theo Ði?m ng?m 3D c?a Tàu
        Vector3 screenPos = mainCam.WorldToScreenPoint(movement.TargetPoint3D);
        
        if (screenPos.z > 0)
        {
            crosshairUI.position = screenPos; 
        }
    }
}
