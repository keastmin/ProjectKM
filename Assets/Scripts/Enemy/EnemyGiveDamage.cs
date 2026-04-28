using System.Collections.Generic;
using UnityEngine;

public class EnemyGiveDamage : MonoBehaviour
{
    private HashSet<IDamageable> _damageTarget = new();

    private void OnEnable()
    {
        _damageTarget.Clear();
    }

    private void OnDisable()
    {
        _damageTarget.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        GiveDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        GiveDamage(other);
    }

    private void GiveDamage(Collider other)
    {
        IDamageable damageTarget = other.GetComponentInParent<IDamageable>();

        if(damageTarget != null && !_damageTarget.Contains(damageTarget))
        {
            Debug.Log("데미지 줌: " + other.name);
            damageTarget.TakeDamage(30f);
            _damageTarget.Add(damageTarget);
        }
    }
}
