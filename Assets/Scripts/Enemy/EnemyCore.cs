using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    private Rigidbody _rigidbody;

    private void Awake()
    {
        TryGetComponent(out _rigidbody);
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = Vector3.zero;
    }
}
