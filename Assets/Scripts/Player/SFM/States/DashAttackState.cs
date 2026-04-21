using Player;
using UnityEngine;

public class DashAttackState : StateBase
{
    private const float ATTACK_MOVE_STOP_BUFFER = 0.02f;

    private readonly AttackExecutionRuntime _attackRuntime;
    private AttackData _attackData;
    private ComboAttackData _comboAttackData;
    private int _animationHash;
    private AnimatorStateInfo _stateInfo;
    private Vector3 _animDeltaPos;
    private Quaternion _targetRot;
    private bool _hasNearTarget;
    private bool _isMotionWarp;
    private Vector3 _targetPos;

    public DashAttackState(PlayerCore core) : base(core) 
    {
        _attackData = core.DashAttackData;
        _comboAttackData = _attackData.TimingProfile as ComboAttackData;
        _animationHash = PlayerAnimationHash.Katana_Dash_Attack;
        _attackRuntime = new AttackExecutionRuntime(core);
    }

    public override void Enter()
    {
        Debug.Log("Dash Attack State");

        // 속도 초기화
        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;
        _isMotionWarp = false;

        // 타겟팅
        _hasNearTarget = (_core.TargetingController.Target != null);
        if (_hasNearTarget)
        {
            _targetPos = _core.TargetingController.GetWarpPos();
            _isMotionWarp = true;
        }

        _attackRuntime.Reset(_attackData != null ? _attackData.TimingProfile : null);
        SetTargetRotation();

        // 애니메이션 재생
        _core.Animator.CrossFade(_animationHash, 0.03f, 0, _comboAttackData.ComboInputStartNormalizedTime);
        _animDeltaPos = Vector3.zero;
    }

    public override void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animationHash, out _stateInfo);
        PlayerRotation();

        _attackRuntime.Process(_attackData, _stateInfo.normalizedTime, _core.CameraShake, _core.StartHitStop);
        MotionWarpTimeEndCheck(_stateInfo.normalizedTime);

        // 데미지를 입으면 데미지 상태로 전환
        if (_core.DamageFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        // 회피 입력이 있으면 회피 상태로 전환
        if (_core.InputController.DodgeInput && _core.DodgeAvailableCount > 0)
        {
            _core.FSM.Transition(_core.FSM.DodgeState);
            return;
        }

        if (_stateInfo.normalizedTime >= 0.95f)
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
        MotionWarpPositionEndCheck();
        if (_isMotionWarp)
        {
            MotionWarp();
        }
        else
        {
            RootMotionMove();
        }
    }

    public override void AnimationTick()
    {
        _animDeltaPos += _core.Animator.deltaPosition;
    }

    public override void Exit()
    {
        _attackRuntime.Clear();
    }

    private void PlayerRotation()
    {
        _core.transform.rotation = Quaternion.Slerp(_core.transform.rotation, _targetRot, 15f * Time.fixedDeltaTime);
    }

    private void SetTargetRotation()
    {
        if (_hasNearTarget)
        {
            Vector3 dir = _targetPos - _core.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude >= 0.0001f)
            {
                _targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                return;
            }
        }

        _targetRot = Quaternion.LookRotation(_core.transform.forward, Vector3.up);
    }

    private void RootMotionMove()
    {
        _animDeltaPos.y = 0f;
        Vector3 vel = GetBlockedAttackVelocity(_animDeltaPos / Time.fixedDeltaTime);
        _core.Mover.Move(vel);
        _animDeltaPos = Vector3.zero;
    }

    private void MotionWarp()
    {
        Vector3 dir = _targetPos - _core.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            _isMotionWarp = false;
            RootMotionMove();
            return;
        }

        float frameSpeed = dir.magnitude / Time.fixedDeltaTime;
        float warpSpeed = Mathf.Min(_core.BasicComboAttackMotionWarpSpeed, frameSpeed);
        Vector3 vel = GetBlockedAttackVelocity(dir.normalized * warpSpeed);
        _core.Mover.Move(vel);
        _animDeltaPos = Vector3.zero;
    }

    private void MotionWarpTimeEndCheck(float aniDelta)
    {
        float firstHitTiming = AttackExecutionRuntime.GetFirstHitNormalizedTime(_attackData != null ? _attackData.TimingProfile : null);
        if (firstHitTiming >= 0f && aniDelta >= firstHitTiming)
        {
            _isMotionWarp = false;
        }
    }

    private void MotionWarpPositionEndCheck()
    {
        if (!_isMotionWarp)
        {
            return;
        }

        Vector3 planarDelta = _targetPos - _core.transform.position;
        planarDelta.y = 0f;
        if (planarDelta.sqrMagnitude <= 0.01f)
        {
            _isMotionWarp = false;
        }
    }

    private Vector3 GetBlockedAttackVelocity(Vector3 velocity)
    {
        velocity.y = 0f;

        Vector3 displacement = velocity * Time.fixedDeltaTime;
        displacement.y = 0f;

        if (displacement.sqrMagnitude <= 0.000001f)
        {
            return Vector3.zero;
        }

        if (!_core.TryGetComponent(out CapsuleCollider capsule))
        {
            return velocity;
        }

        Vector3 moveDir = displacement.normalized;
        float moveDistance = displacement.magnitude;

        GetCapsuleWorldPoints(capsule, _core.transform.position, _core.transform.rotation, out Vector3 point1, out Vector3 point2, out float radius);

        if (Physics.CapsuleCast(point1, point2, radius, moveDir, out RaycastHit hit, moveDistance + ATTACK_MOVE_STOP_BUFFER, _core.TargetingController.TargetingLayer, QueryTriggerInteraction.Ignore))
        {
            float allowedDistance = Mathf.Max(0f, hit.distance - ATTACK_MOVE_STOP_BUFFER);
            return moveDir * (allowedDistance / Time.fixedDeltaTime);
        }

        Vector3 nextPosition = _core.transform.position + displacement;
        GetCapsuleWorldPoints(capsule, nextPosition, _core.transform.rotation, out point1, out point2, out radius);

        if (Physics.CheckCapsule(point1, point2, radius, _core.TargetingController.TargetingLayer, QueryTriggerInteraction.Ignore))
        {
            return Vector3.zero;
        }

        return velocity;
    }

    private void GetCapsuleWorldPoints(CapsuleCollider capsule, Vector3 position, Quaternion rotation, out Vector3 point1, out Vector3 point2, out float radius)
    {
        Vector3 lossyScale = _core.transform.lossyScale;
        Vector3 scaledCenter = Vector3.Scale(capsule.center, lossyScale);
        Vector3 center = position + rotation * scaledCenter;

        radius = capsule.radius * Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
        float height = Mathf.Max(capsule.height * Mathf.Abs(lossyScale.y), radius * 2f);
        float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 up = rotation * Vector3.up;

        point1 = center + up * halfSegment;
        point2 = center - up * halfSegment;
    }
}
