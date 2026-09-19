using System;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
[RequireComponent(typeof(Explosion))]
public class Asteroid : MonoBehaviour, VirtualRail.IPoolableEntity
{
    [SerializeField] private float _minScale = 0.8f;
    [SerializeField] private float _maxScale = 1.2f;

    public static float destructionDelay = 1f;
    public Action<Asteroid> OnRecycle;

    /// <summary>
    /// Cờ chống kích hoạt Graze lặp lại khi lướt qua tàu (TDD v2.0.0 Phần III.2).
    /// </summary>
    public bool HasGrazed { get; set; } = false;

    private void Start()
    {
        ResetScale();
    }

    public void ResetScale()
    {
        HasGrazed = false;
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

    public void Recycle()
    {
        HasGrazed = false;
        gameObject.SetActive(false);
        OnRecycle?.Invoke(this);
    }
}