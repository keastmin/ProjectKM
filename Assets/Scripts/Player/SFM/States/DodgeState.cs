using Player;
using UnityEngine;

public class DodgeState : StateBase
{
    // 공통
    private AnimatorStateInfo _stateInfo;
    private bool _isFront = false;
    private int _animHash;
    private Vector3 _lookDir;

    // 정면 회피
    private float _frontDodgeCurrentTime;
    private float _frontDodgeEndTime;
    private float _frontDodgeTargetEndSpeed; // 정면 회피 목표 종료 속도

    public DodgeState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        // 정면 회피, 후면 회피 결정
        _isFront = (_core.InputController.MoveInput.sqrMagnitude >= 0.01f);

        // 바라볼 방향 결정
        _lookDir = PlayerStateUtil.GetCameraRelativeFacingDirection(_core);

        // 애니메이션 해쉬 결정
        _animHash = (_isFront) ? PlayerAnimationHash.Katana_Dodge_Front : PlayerAnimationHash.Katana_Dodge_Back;
        
        // 애니메이션 재생
        _core.Animator.CrossFade(_animHash, 0.03f, 0, 0f);

        // 정면 회피 변수 초기화
        _frontDodgeCurrentTime = 0f;
        _frontDodgeEndTime = _core.StateVariables.DodgeVariable.FrontDodgeTime;
        _frontDodgeTargetEndSpeed = _core.RunSpeed; // 달리기 속도가 목표 회피 종료 속도

        _core.TargetSpeed = (_isFront) ? _core.RunSpeed : -_core.RunSpeed;
    }

    public override void Tick()
    {
        if (_isFront)
            FrontDodgeTick();
        else
            BackDodgeTick();
    }

    public override void FixedTick()
    {
        if (_isFront)
            FrontDodgeFixedTick();
        else
            BackDodgeFixedTick();
    }

    private void FrontDodgeTick()
    {
        _frontDodgeCurrentTime += Time.deltaTime;

        if (_frontDodgeCurrentTime >= _frontDodgeEndTime)
        {
            _core.FSM.Transition(_core.FSM.RunState);
            return;
        }
    }

    private void BackDodgeTick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _stateInfo);

        if(_stateInfo.normalizedTime >= 0.98f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    private void FrontDodgeFixedTick()
    {
        PlayerStateUtil.RotateTowardsDirection(_core, _lookDir, _core.StateVariables.DodgeVariable.FrontDodgeRotateSpeed);
        _core.Mover.Move(_lookDir.normalized * _core.CurrentSpeed);
    }

    private void BackDodgeFixedTick()
    {
        _core.Mover.Move(_lookDir.normalized * _core.CurrentSpeed);
    }
}
