using Player;
using UnityEngine;

public class IdleState : StateBase
{
    public IdleState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        Debug.Log("IdleState");
        _core.Animator.CrossFade(PlayerAnimationNameContainer.NO_WEAPON_IDLE, 0.08f);
    }

    public override void Tick()
    {
        // 움직임 입력이 있을 경우 Jog로 이동
        if(_core.InputController.MoveInput.sqrMagnitude >= 0.01f)
        {
            _core.FSM.Transition(_core.FSM.JogState);
            return;
        }
    }
}
