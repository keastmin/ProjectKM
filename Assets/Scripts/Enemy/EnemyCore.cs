using System;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [SerializeField] private Animator _animator;
    [SerializeField] private DamageStatus _damageStatus;

    private const string DAMAGED_NAME = "Damaged";
    private int _damagedHash;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        TryGetComponent(out _rigidbody);
        _damagedHash = Animator.StringToHash("Base Layer." + DAMAGED_NAME);
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

        if(_animator != null)
        {
            _animator.CrossFade(_damagedHash, 0.08f, 0, 0f, 0f);
        }
    }
}
