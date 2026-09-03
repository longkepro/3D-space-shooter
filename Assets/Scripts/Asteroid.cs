using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Explosion))]
public class Asteroid : MonoBehaviour
{
    [SerializeField] private float _minScale = 0.8f;
    [SerializeField] private float _maxScale = 1.2f;

    public static float destructionDelay = 1f;

    private void Start()
    {
        ResetScale();
    }

    public void ResetScale()
    {
        transform.localScale = GetRandomVector3(_minScale, _maxScale);
    }

    private Vector3 GetRandomVector3(float minRange, float maxRange)
    {
        var returnValue = Vector3.zero;
        returnValue.x = Random.Range(minRange, maxRange);
        returnValue.y = Random.Range(minRange, maxRange);
        returnValue.z = Random.Range(minRange, maxRange);
        return returnValue;
    }
}