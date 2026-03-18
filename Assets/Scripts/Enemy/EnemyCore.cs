using System;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [SerializeField] private DamageStatus _damageStatus;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        TryGetComponent(out _rigidbody);
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = Vector3.zero;
    }

    public void TakeDamage(float damage)
    {
        if (_damageStatus != null)
        {
            _damageStatus.SetDamage(damage);
        }
    }
}
