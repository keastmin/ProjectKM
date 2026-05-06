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

    private StateVariableDodge _dodgeVariable;

    private float _currentStateTime = 0f;
    private bool _isPerfectDodge = false;

    public DodgeState(PlayerCore core) : base(core) 
    {
        _dodgeVariable = core.StateVariables.DodgeVariable;
    }

    public override bool CanReceiveDamage => !_core.CanPerfectDodge;

    public override void Enter()
    {
        Debug.Log("Dodge State");
        _core.ConsumeDodge(); // 회피 횟수 감소

        _isPerfectDodge = _core.CanPerfectDodge;

        // 완벽 회피 트리거
        if (_core.CanPerfectDodge)
        {
            if (_core.DamageFlag)
            {
                _core.DamageFlag = false;
                // 잃은 HP 복원

            }
            _core.TriggerPerfectDodgeTimeScale();
            _core.SetNearDodgeCounterTarget();
            _core.TrailEffector.PerfactDodgeMeshTrailEffectOn(_core.DodgeCounterDuration);
            _core.VolumeEffect.PerfectDodgeEffectOn(_core.DodgeCounterDuration);
        }
        _currentStateTime = 0f;

        // 정면 회피, 후면 회피 결정
        _isFront = (_core.InputController.MoveInput.sqrMagnitude >= 0.01f);

        // 바라볼 방향 결정
        _lookDir = PlayerStateUtil.GetCameraRelativeFacingDirection(_core.transform, _core.PlayerCamera, _core.InputController.MoveInput);
        PlayerStateUtil.RotateImmediatelyTowardsDirection(_core.transform, _lookDir);

        // 애니메이션 해쉬 결정
        _animHash = (_isFront) ? PlayerAnimationHash.Katana_Dodge_Front : PlayerAnimationHash.Katana_Dodge_Back;
        
        // 애니메이션 재생
        _core.Animator.CrossFade(_animHash, 0f, 0, 0f);

        // 정면 회피 변수 초기화
        _frontDodgeCurrentTime = 0f;
        _frontDodgeEndTime = _core.StateVariables.DodgeVariable.FrontDodgeTime;
        _frontDodgeTargetEndSpeed = _core.RunSpeed; // 달리기 속도가 목표 회피 종료 속도

        _core.TargetSpeed = (_isFront) ? _core.RunSpeed : -_core.RunSpeed;
    }

    public override void Tick()
    {
        PlayerStateUtil.RotateImmediatelyTowardsDirection(_core.transform, _lookDir);

        // 데미지를 입으면 데미지 상태로 전환
        if (_core.DamageFlag && !_isPerfectDodge)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if (_isPerfectDodge && _core.InputController.BasicComboAttackInput)
        {
            _core.FSM.Transition(_core.FSM.DodgeCounterState);
            return;
        }

        if (_core.InputController.BasicComboAttackInput)
        {
            _core.FSM.Transition(_core.FSM.DashAttackState);
            return;
        }

        _currentStateTime += Time.deltaTime;

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

    public override void Exit()
    {
        _isPerfectDodge = false;
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
        _core.Mover.Move(_lookDir.normalized * _core.CurrentSpeed);
    }

    private void BackDodgeFixedTick()
    {
        _core.Mover.Move(_lookDir.normalized * _core.CurrentSpeed);
    }
}
