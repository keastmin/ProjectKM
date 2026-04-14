using Player;
using UnityEngine;

public class DodgeCounterState : StateBase
{
    private readonly AttackExecutionRuntime _attackRuntime;

    private AnimatorStateInfo _animInfo;
    private Vector3 _animDeltaPos;
    private AttackData _attackData;
    private int _animHash;

    public DodgeCounterState(PlayerCore core) : base(core)
    {
        _attackRuntime = new AttackExecutionRuntime(core);
    }

    public override bool CanReceiveDamage => false;

    public override void Enter()
    {
        _attackData = _core.DodgeCounterData;

        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;
        _animDeltaPos = Vector3.zero;
        _animHash = ResolveAnimationHash(_attackData);
        _attackRuntime.Reset(_attackData != null ? _attackData.TimingProfile : null);
        _core.Animator.CrossFade(_attackData != null ? _attackData.AnimationName : "Katana_Dodge_Counter", 0.03f, 0, 0f);
    }

    public override void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _animInfo);
        _attackRuntime.Process(_attackData, _animInfo.normalizedTime);

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

    public override void Exit()
    {
        _attackRuntime.Clear();
        _attackData = null;
    }

    private static int ResolveAnimationHash(AttackData attackData)
    {
        string animationName = attackData != null && !string.IsNullOrWhiteSpace(attackData.AnimationName)
            ? attackData.AnimationName
            : "Katana_Dodge_Counter";

        if (PlayerAnimationHash.TryGetHash(animationName, out int animationHash))
        {
            return animationHash;
        }

        return Animator.StringToHash($"Base Layer.{animationName}");
    }
}
