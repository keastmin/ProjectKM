using Player;
using UnityEngine;

public class DeathState : StateBase
{
    public DeathState(PlayerCore core) : base(core) {}

    public override bool CanReceiveDamage => false;

    public override void Enter()
    {
        _core.DamageFlag = false;
        _core.IsDead = true;
        _core.Animator.CrossFade(PlayerAnimationHash.Katana_Die, 0.03f, 0, 0f);
    }

    public override void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            _core.Heal(_core.MaxHealth);
            return;
        }
    }

    public override void Exit()
    {
        _core.IsDead = false;
    }
}
