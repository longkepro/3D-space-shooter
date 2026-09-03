using UnityEngine;

[DisallowMultipleComponent]
public class Rotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 50f;
    private Vector3 _currentRotation;

    private void Start()
    {
        _currentRotation = GetRandomVector3(_rotationSpeed);
    }

    private void Update()
    {
        transform.Rotate(_currentRotation * Time.deltaTime);
    }

    private Vector3 GetRandomVector3(float offset)
    {
        return GetRandomVector3(-offset, offset);
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
