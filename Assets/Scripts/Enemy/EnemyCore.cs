using System;
using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    [SerializeField] private Animator _animator;
    [SerializeField] private DamageStatus _damageStatus;

    public Animator Animator => _animator;

    public bool DamagedFlag { get; set; }

    protected virtual void Awake()
    {
        
    }

    public virtual void TakeDamage(float damage)
    {
        _damageStatus.SetDamage(damage);
        DamagedFlag = true;
    }
}
