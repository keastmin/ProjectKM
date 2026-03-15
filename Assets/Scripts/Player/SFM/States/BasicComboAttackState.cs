using Player;
using UnityEngine;

public class BasicComboAttackState : StateBase
{
    public BasicComboAttackState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        Debug.Log("BasicComboAttackState");
    }
}
