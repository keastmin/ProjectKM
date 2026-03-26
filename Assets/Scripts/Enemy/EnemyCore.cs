using System;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [SerializeField] private Animator _animator;
    [SerializeField] private DamageStatus _damageStatus;

    private const string DAMAGED_NAME = "Damaged";
    private int _damagedHash;

    protected virtual void Awake()
    {
        _damagedHash = Animator.StringToHash("Base Layer." + DAMAGED_NAME);
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
