using Player;
using UnityEngine;

public class DashAttackState : StateBase
{
    private AttackData _attackData;
    private ComboAttackData _comboAttackData;
    private int _animationHash;
    private AnimatorStateInfo _stateInfo;
    private Vector3 _animDeltaPos;

    public DashAttackState(PlayerCore core) : base(core) 
    {
        _attackData = core.DashAttackData;
        _comboAttackData = _attackData.TimingProfile as ComboAttackData;
        _animationHash = PlayerAnimationHash.Katana_Dash_Attack;
    }

    public override void Enter()
    {
        Debug.Log("Dash Attack State");

        // 속도 초기화
        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        // 애니메이션 재생
        _core.Animator.CrossFade(_animationHash, 0.03f, 0, _comboAttackData.ComboInputStartNormalizedTime);
        _animDeltaPos = Vector3.zero;
    }

    public override void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animationHash, out _stateInfo);

        if(_stateInfo.normalizedTime >= 0.95f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (_stateInfo.normalizedTime >= _comboAttackData.ComboInputEndNormalizedTime &&
            _core.InputController.BasicComboAttackInput)
        {
            _core.FSM.Transition(_core.FSM.BasicComboAttackState);
            return;
        }
    }

    public override void FixedTick()
    {
        Vector3 vel = _animDeltaPos / Time.fixedDeltaTime;
        vel.y = 0f;
        _core.Mover.Move(vel);
        _animDeltaPos = Vector3.zero;
    }

    public override void AnimationTick()
    {
        _animDeltaPos += _core.Animator.deltaPosition;
    }
}
