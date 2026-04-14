using Player;
using UnityEngine;

public class RunTurnState : StateBase
{
    private AnimatorStateInfo _animInfo;
    private Vector3 _animDeltaPos;

    public RunTurnState(PlayerCore core) : base(core)
    {
        
    }

    public override void Enter()
    {
        Debug.Log("RunTurnState");

        _core.TargetSpeed = _core.RunSpeed;
        _core.CurrentSpeed = _core.RunSpeed;

        _core.Animator.CrossFade(PlayerAnimationHash.Katana_Run_Turn, 0f, 0, 0f);
    }

    public override void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, PlayerAnimationHash.Katana_Run_Turn, out _animInfo);

        // 트랜지션 조건은 기존처럼 실제 애니메이션 상태를 기준으로 판단
        float animationNormalizedTime = _animInfo.normalizedTime;
        bool isFinishTurn = animationNormalizedTime >= 0.99f;

        // 데미지를 입으면 데미지 상태로 전환
        if (_core.DamageFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if (isFinishTurn && _core.InputController.MoveInput.sqrMagnitude < 0.01f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (isFinishTurn && _core.InputController.MoveInput.sqrMagnitude >= 0.01f)
        {
            _core.FSM.Transition(_core.FSM.RunState);
            return;
        }
    }

    public override void FixedTick()
    {
        var vel = _animDeltaPos / Time.fixedDeltaTime;
        vel.y = 0f;
        _core.Mover.Move(vel);
        _animDeltaPos = Vector3.zero;
    }

    public override void AnimationTick()
    {
        _animDeltaPos += _core.Animator.deltaPosition;
        _core.transform.rotation *= _core.Animator.deltaRotation;
    }
}
