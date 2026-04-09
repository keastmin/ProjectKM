using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnemyMeleeAttackStruct
{
    public EnemyMeleeAttackData MeleeAttackData;
    public List<EnemyAttackColliderName> AttackObjects;
    public List<EnemyAttackColliderName> DodgeWindowObjects;
}
