using Player;
using UnityEngine;

public class DodgeCounterState : StateBase
{
    private AnimatorStateInfo _animInfo;
    private Vector3 _animDeltaPos;

    public DodgeCounterState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        _animDeltaPos = Vector3.zero;
        _core.Animator.CrossFade(PlayerAnimationHash.Katana_Dodge_Counter, 0.03f, 0, 0f);
    }

    public override void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, PlayerAnimationHash.Katana_Dodge_Counter, out _animInfo);

        if (_animInfo.normalizedTime >= 0.97f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    public override void FixedTick()
    {
        _animDeltaPos.y = 0;
        Vector3 vel = _animDeltaPos / Time.fixedDeltaTime;
        _core.Mover.Move(vel);
        _animDeltaPos = Vector3.zero;
    }

    public override void AnimationTick()
    {
        _animDeltaPos += _core.Animator.deltaPosition;
    }
}
