using UnityEngine;

public class StarFoxInput : MonoBehaviour
{
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool IsFiring { get; private set; }
    public float Boost { get; private set; }

    void Update()
    {
        // 1 C?n g?t duy nh?t di?u khi?n toàn b?
        Horizontal = Input.GetAxis("Horizontal");
        Vertical = Input.GetAxis("Vertical");
        IsFiring = Input.GetButton("Fire1");
        Boost = Input.GetAxis("Fire3") > 0 ? Input.GetAxis("Fire3") : 0f;
    }
}
