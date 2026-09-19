using UnityEngine;

public class StarFoxCameraRig : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 4, -15);
    public float smoothZ = 20f;
    private Camera cam;
    private MonoBehaviour oldFollowCam;

    void Start()
    {
        cam = Camera.main;
        if (cam != null)
        {
            oldFollowCam = cam.GetComponent("FollowCam") as MonoBehaviour;
            if (oldFollowCam != null) oldFollowCam.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;
        
        float targetZ = transform.position.z + offset.z;
        float newZ = Mathf.Lerp(cam.transform.position.z, targetZ, smoothZ * Time.deltaTime);
        
        float swayX = transform.position.x * 0.15f;
        float swayY = transform.position.y * 0.15f;
        
        cam.transform.position = new Vector3(swayX, offset.y + swayY, newZ);
        cam.transform.rotation = Quaternion.Euler(5f, 0, 0);
    }

    void OnDestroy()
    {
        if (oldFollowCam != null) oldFollowCam.enabled = true;
    }
}
