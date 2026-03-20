using Player;
using UnityEngine;

public class RunTurnState : StateBase
{
    private MotionWarpProfile _runTurnProfile;

    private Quaternion _turnStartRotation;
    private Quaternion _turnTargetRotation;

    // +1이면 오른쪽으로 180, -1이면 왼쪽으로 180
    private float _turnSign = 1f;

    // 회전 워핑은 애니메이션 상태와 분리된 내부 시간축으로 진행
    private float _warpStartTime;
    private float _warpDuration;

    public RunTurnState(PlayerCore core) : base(core)
    {
        _runTurnProfile = core.RunTurnMotionInfo;
    }

    public override void Enter()
    {
        Debug.Log("RunTurnState");

        _core.TargetSpeed = _core.RunSpeed;
        _core.CurrentSpeed = _core.RunSpeed;

        _turnStartRotation = _core.transform.rotation;

        // 턴 방향 결정
        _turnSign = ResolveTurnSign();

        // 최종 목표는 현재 회전에서 정확히 180도
        _turnTargetRotation = _turnStartRotation * Quaternion.Euler(0f, 180f * _turnSign, 0f);

        // 회전 워핑용 시간 초기화
        _warpStartTime = Time.time;
        _warpDuration = ResolveWarpDuration();

        _core.Animator.CrossFade(PlayerAnimationHash.No_Weapon_Run_Turn, 0.08f);
    }

    public override void Tick()
    {
        AnimatorStateInfo info;
        if (!AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, PlayerAnimationHash.No_Weapon_Run_Turn, out info))
            return;

        // 트랜지션 조건은 기존처럼 실제 애니메이션 상태를 기준으로 판단
        float animationNormalizedTime = info.normalizedTime;
        bool isFinishTurn = animationNormalizedTime >= 0.99f;

        if (isFinishTurn)
        {
            // 마지막 오차 보정
            _core.transform.rotation = _turnTargetRotation;
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
    }

    public override void AnimationTick()
    {
        // 회전은 애니메이션 상태가 아니라, 상태 진입 후 경과 시간으로 고정 진행
        float warpNormalizedTime = GetWarpNormalizedTime();

        float turnProgress = warpNormalizedTime;
        if (_runTurnProfile != null)
        {
            turnProgress = _runTurnProfile.EvaluateYawProgress(warpNormalizedTime);
        }

        turnProgress = Mathf.Clamp01(turnProgress);

        float warpedYaw = 180f * turnProgress * _turnSign;

        _core.transform.rotation =
            _turnStartRotation * Quaternion.Euler(0f, warpedYaw, 0f);

        // 끝점 보장
        if (warpNormalizedTime >= 1f)
        {
            _core.transform.rotation = _turnTargetRotation;
        }
    }

    private float GetWarpNormalizedTime()
    {
        if (_warpDuration <= 0.0001f)
            return 1f;

        return Mathf.Clamp01((Time.time - _warpStartTime) / _warpDuration);
    }

    private float ResolveWarpDuration()
    {
        if (_runTurnProfile != null && _runTurnProfile.clipLength > 0.0001f)
        {
            return _runTurnProfile.clipLength;
        }

        // 프로필이 없을 때의 fallback
        return 0.6f;
    }

    private float ResolveTurnSign()
    {
        if (_runTurnProfile != null && Mathf.Abs(_runTurnProfile.totalYawDegrees) > 0.001f)
        {
            return Mathf.Sign(_runTurnProfile.totalYawDegrees);
        }

        return 1f;
    }
}
