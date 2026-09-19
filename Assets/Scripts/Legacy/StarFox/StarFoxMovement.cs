using UnityEngine;

public class StarFoxMovement : MonoBehaviour
{
    public StarFoxInput input;
    
    [Header("Ship Physical Limits (Vùng Bay)")]
    public float shipMaxX = 25f;
    public float shipMaxY = 15f;
    public float xySpeed = 40f;
    public float forwardSpeed = 50f;
    public float movementSmoothing = 12f;
    
    [Header("Flight Direction (Góc B? Mui)")]
    public float maxYaw = 30f;
    public float maxPitch = 20f;
    public float maxRoll = 60f;
    public float rotationSmoothing = 12f;
    
    [Header("Reticle Projection")]
    public float projectionDistance = 100f;
    
    public Vector3 TargetPoint3D { get; private set; }
    
    private Thruster[] thrusters;
    private Vector2 currentVelocity;

    void Awake()
    {
        thrusters = GetComponentsInChildren<Thruster>();
    }

    void Update()
    {
        if (input == null) return;
        
        float boost = input.Boost;
        float speed = forwardSpeed * (1f + boost * 0.5f);
        
        Vector2 targetVelocity = new Vector2(input.Horizontal, input.Vertical) * xySpeed;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, movementSmoothing * Time.deltaTime);
        
        float newX = transform.position.x + currentVelocity.x * Time.deltaTime;
        float newY = transform.position.y + currentVelocity.y * Time.deltaTime;
        
        newX = Mathf.Clamp(newX, -shipMaxX, shipMaxX);
        newY = Mathf.Clamp(newY, -shipMaxY, shipMaxY);
        
        transform.position = new Vector3(newX, newY, transform.position.z + speed * Time.deltaTime);

        float targetYaw = input.Horizontal * maxYaw;
        float targetPitch = -input.Vertical * maxPitch;
        float targetRoll = -input.Horizontal * maxRoll;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, targetYaw, targetRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);

        TargetPoint3D = transform.position + transform.forward * projectionDistance;

        foreach (var t in thrusters) 
        {
            if (t != null) t.Intensity(0.5f + boost * 0.5f);
        }
    }
}
