
using UnityEngine;

[RequireComponent(typeof(Light))]
public class Thruster : MonoBehaviour
{
    private Light _thrusterLight;

    private void Awake()
    {
        _thrusterLight = GetComponent<Light>();
    }

    private void Start()
    {
        _thrusterLight.intensity = 0f;
    }

    public void Intensity(float value)
    {
        _thrusterLight.intensity = value * 2f;
    }

}
