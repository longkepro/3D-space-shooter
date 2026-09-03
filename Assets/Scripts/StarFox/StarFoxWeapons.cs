using UnityEngine;

public class StarFoxWeapons : MonoBehaviour
{
    public StarFoxInput input;
    public Laser[] lasers;

    void Awake()
    {
        if (lasers == null || lasers.Length == 0)
            lasers = GetComponentsInChildren<Laser>();
    }

    void Update()
    {
        if (input != null && input.IsFiring)
        {
            foreach (var laser in lasers)
            {
                if (laser != null) laser.FireLaser();
            }
        }
    }
}
