using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _movementSpeed = 20f;
    [SerializeField] private float _turnSpeed = 0.5f;
    [SerializeField] private float _rayCastOffset = 2.5f;
    [SerializeField] private float _rayCastRange = 20f;
    [SerializeField] private int _points = 50;

    private Transform _target;
    private bool _isBlowingUp = false;


    private void OnEnable()
    {
        GameEventManager.OnStartGame += SelfDestruct;
        GameEventManager.OnPlayerDestroyed += TargetMainCamera;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= SelfDestruct;
        GameEventManager.OnPlayerDestroyed -= TargetMainCamera;
    }

    private void Update()
    {
        if (!TargetPlayer()) return;

        Pathfinding();
        Move();
    }

    private void Turn()
    {
        if (!TargetPlayer()) return;

        Vector3 pos = _target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(pos);
        base.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _turnSpeed * Time.deltaTime);
    }

    private void Move()
    {
        transform.position += transform.forward * _movementSpeed * Time.deltaTime;
    }

    private void Pathfinding()
    {
        RaycastHit hit;
        Vector3 rayCastOffset = Vector3.zero;

        Vector3 left = base.transform.position - base.transform.right * _rayCastOffset;
        Vector3 right = base.transform.position + base.transform.right * _rayCastOffset;
        Vector3 up = base.transform.position + base.transform.up * _rayCastOffset;
        Vector3 down = base.transform.position - base.transform.up * _rayCastOffset;

        Debug.DrawRay(left, base.transform.forward * _rayCastRange, Color.cyan);
        Debug.DrawRay(right, base.transform.forward * _rayCastRange, Color.cyan);
        Debug.DrawRay(up, base.transform.forward * _rayCastRange, Color.cyan);
        Debug.DrawRay(down, base.transform.forward * _rayCastRange, Color.cyan);

        if (Physics.Raycast(left, base.transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset += Vector3.right;
        }
        else if (Physics.Raycast(right, base.transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset -= Vector3.right;
        }


        if (Physics.Raycast(up, base.transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset -= Vector3.up;
        }
        else if (Physics.Raycast(down, base.transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset = Vector3.up;
        }

        if (rayCastOffset != Vector3.zero)
        {
            base.transform.Rotate(rayCastOffset * 5f * Time.deltaTime);
        }
        else
        {
            Turn();
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
        }
        var foundTarget = (_target != null);
        return foundTarget;
    }

    private void TargetMainCamera()
    {
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera != null)
        {
            _target = mainCamera.transform;
        }
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }

    public void BlowUp()
    {
        if (!_isBlowingUp)
        {
            _isBlowingUp = true;
            GameEventManager.IncrementScore(_points);
            transform.GetComponent<Explosion>().BlowUp();
            SelfDestruct();
        }
    }

}
