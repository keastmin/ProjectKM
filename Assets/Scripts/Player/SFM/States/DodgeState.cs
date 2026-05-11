using Player;
using System;
using UnityEngine;

public class DodgeState : StateBase
{
    // 공통
    private AnimatorStateInfo _stateInfo;
    private bool _isFront = false;
    private int _animHash;
    private Vector3 _lookDir;

    // 정면 회피
    private float _dodgeCurrentTime;
    private float _dodgePlayTime;

    private StateVariableDodge _dodgeVariable;

    private float _currentStateTime = 0f;
    private bool _isPerfectDodge = false;

    private float _perfectDodgeEndNormalizedTime = 0.8f;
    public event Action<float> OnPerfectDodgeVolumeEffectStarted;
    public event Action OnPerfectDodgeVolumeEffectEnded;

    public DodgeState(PlayerCore core) : base(core) 
    {
        _dodgeVariable = core.StateVariables.DodgeVariable;
    }

    public override bool CanReceiveDamage => !_core.CanPerfectDodge;

    public override void Enter()
    {
        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        // 정면 회피, 후면 회피 결정
        _isFront = (_core.InputController.MoveInput.sqrMagnitude >= 0.01f);

        // 애니메이션 해쉬 결정
        _animHash = (_isFront) ? PlayerAnimationHash.Katana_Dodge_Front : PlayerAnimationHash.Katana_Dodge_Back;

        // 애니메이션 재생
        _core.Animator.CrossFade(_animHash, 0f, 0, 0f);
        _core.Animator.Update(0f);
        RefreshDodgeAnimatorStateInfo();
        _dodgePlayTime = GetAnimationPlayTime();

        Debug.Log("Dodge State");
        _core.ConsumeDodge(); // 회피 횟수 감소

        _isPerfectDodge = _core.CanPerfectDodge;

        // 완벽 회피 트리거
        if (_core.CanPerfectDodge)
        {
            PerfectDodgeStart();
        }
        _currentStateTime = 0f;

        // 바라볼 방향 결정
        _lookDir = PlayerStateUtil.GetCameraRelativeFacingDirection(_core.transform, _core.PlayerCamera, _core.InputController.MoveInput);
        PlayerStateUtil.RotateImmediatelyTowardsDirection(_core.transform, _lookDir);

        // 정면 회피 변수 초기화
        _dodgeCurrentTime = 0f;

        ApplyDodgeSpeed(0f);
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

        if (_currentStateTime >= _perfectDodgeEndNormalizedTime && _isPerfectDodge)
        {
            PerfectDodgeEnd();
            _isPerfectDodge = false;
        }

        DodgeTick();
    }

    public override void FixedTick()
    {
        DodgeFixedTick();
    }

    public override void Exit()
    {
        if (_isPerfectDodge)
            PerfectDodgeEnd();
        _isPerfectDodge = false;
    }

    private void DodgeTick()
    {
        _dodgeCurrentTime += Time.deltaTime;
        float normalizedTime = GetDodgeNormalizedTime();
        ApplyDodgeSpeed(normalizedTime);

        if(normalizedTime >= 0.98f)
        {
            _core.FSM.Transition(_isFront ? _core.FSM.RunState : _core.FSM.IdleState);
            return;
        }
    }

    private void DodgeFixedTick()
    {
        _core.Mover.Move(_lookDir.normalized * _core.CurrentSpeed);
    }

    private float GetDodgeNormalizedTime()
    {
        if (_dodgePlayTime <= 0f)
            return 1f;

        return Mathf.Clamp01(_dodgeCurrentTime / _dodgePlayTime);
    }

    private void ApplyDodgeSpeed(float normalizedTime)
    {
        float maxSpeed = _isFront ? _dodgeVariable.FrontDodgeMaxSpeed : _dodgeVariable.BackDodgeMaxSpeed;
        float recoverySpeed = _isFront ? _dodgeVariable.FrontDodgeRecoverySpeed : _dodgeVariable.BackDodgeRecoverySpeed;
        AnimationCurve speedCurve = _isFront ? _dodgeVariable.FrontDodgeSpeedCurve : _dodgeVariable.BackDodgeSpeedCurve;
        float curveValue = speedCurve != null && speedCurve.length > 0 ? Mathf.Clamp01(speedCurve.Evaluate(normalizedTime)) : 1f;
        float speed = Mathf.Lerp(recoverySpeed, maxSpeed, curveValue);
        float signedSpeed = _isFront ? speed : -speed;

        _core.TargetSpeed = signedSpeed;
        _core.CurrentSpeed = signedSpeed;
    }

    private void PerfectDodgeStart()
    {
        float animLength = GetAnimationPlayTime();
        OnPerfectDodgeVolumeEffectStarted?.Invoke(animLength);

        if (_core.DamageFlag)
        {
            _core.DamageFlag = false;
            // 잃은 HP 복원

        }
        _core.TriggerPerfectDodgeTimeScale();
        _core.TrailEffector.PerfactDodgeMeshTrailEffectOn(_core.DodgeCounterDuration);
        _core.VolumeEffect.PerfectDodgeEffectOn(_core.DodgeCounterDuration);
    }

    private void PerfectDodgeEnd()
    {
        OnPerfectDodgeVolumeEffectEnded?.Invoke();
    }

    private float GetAnimationPlayTime()
    {
        if (_stateInfo.length <= 0f)
            return 0f;

        float animatorSpeed = Mathf.Abs(_core.Animator.speed);
        if (animatorSpeed <= 0f)
            return _stateInfo.length;

        return _stateInfo.length / animatorSpeed;
    }

    private bool RefreshDodgeAnimatorStateInfo()
    {
        return AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _stateInfo);
    }
}
