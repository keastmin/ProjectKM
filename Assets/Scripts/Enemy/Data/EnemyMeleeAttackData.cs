using UnityEngine;

[CreateAssetMenu(fileName = "EnemyMeleeAttackData", menuName = "Enemy/MeleeAttack")]
public class EnemyMeleeAttackData : ScriptableObject
{
    public string ID;
    public string AnimName;
    public AttackDodgeTimingWindow[] TimingWindows;
    public AttackColliderTimingWindow[] AttackColliderTimingWindows;
}
