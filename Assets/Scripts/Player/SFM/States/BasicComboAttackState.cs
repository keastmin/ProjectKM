using Player;
using UnityEngine;

public class BasicComboAttackState : StateBase
{
    private const float ATTACK_MOVE_STOP_BUFFER = 0.02f;

    private readonly AttackExecutionRuntime _attackRuntime;
    private readonly AdditionalRootmotionRuntime _additionalRootmotionRuntime;

    private int _index = -1;
    private AttackData[] _datas;
    private Vector3 _aniMoveDelta;
    private Quaternion _targetRot;
    private AnimatorStateInfo _stateInfo;
    private float _aniNormalizedTime;
    private int _aniHash;

    public BasicComboAttackState(PlayerCore core) : base(core)
    {
        _attackRuntime = new AttackExecutionRuntime(core);
        _additionalRootmotionRuntime = new AdditionalRootmotionRuntime();
    }

    public override void Enter()
    {
        InitStateValue();
        _datas = _core.KatanaComboDatas;
        _index++;
        _index %= _datas.Length;

        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        _aniMoveDelta = Vector3.zero;
        _attackRuntime.Reset(_datas[_index].TimingProfile);
        SetTargetRotation();
        ConfigureAdditionalRootmotion();

        Debug.Log("BasicComboAttackState" + ", Combo: " + (_index + 1));

        _core.Animator.CrossFade(_datas[_index].AnimationName, 0.08f, 0, 0f);
    }

    public override void Tick()
    {
        PlayerRotation();

        PlayerAnimationHash.TryGetHash(_datas[_index].AnimationName, out _aniHash);
        if (_aniHash == 0)
        {
            _aniHash = Animator.StringToHash($"Base Layer.{_datas[_index].AnimationName}");
        }

        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _aniHash, out _stateInfo);
        _aniNormalizedTime = _stateInfo.normalizedTime;

        _attackRuntime.Process(_datas[_index], _aniNormalizedTime, _core.CameraShake, _core.StartHitStop);

        if (_core.InputController.DodgeInput && _core.DodgeAvailableCount > 0)
        {
            _core.FSM.Transition(_core.FSM.DodgeState);
            return;
        }

        if (_core.DamageFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        ComboAttackData comboTiming = _datas[_index].TimingProfile as ComboAttackData;
        if (comboTiming == null)
        {
            if (_aniNormalizedTime >= 0.97f)
            {
                _core.FSM.Transition(_core.FSM.IdleState);
            }

            return;
        }

        if (comboTiming.ComboInputEndNormalizedTime <= _aniNormalizedTime)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (_core.InputController.BasicComboAttackInput &&
            comboTiming.ComboInputStartNormalizedTime <= _aniNormalizedTime &&
            comboTiming.ComboInputEndNormalizedTime > _aniNormalizedTime)
        {
            Enter();
            return;
        }
    }

    public override void FixedTick()
    {
        RootMotionMove();
    }

    public override void AnimationTick()
    {
        _aniMoveDelta += _core.Animator.deltaPosition;
    }

    public override void Exit()
    {
        _index = -1;
        _aniMoveDelta = Vector3.zero;
        _attackRuntime.Clear();
        _additionalRootmotionRuntime.Clear();
    }

    private void RootMotionMove()
    {
        _aniMoveDelta += _additionalRootmotionRuntime.ConsumeDelta(_aniNormalizedTime);
        _aniMoveDelta.y = 0f;
        Vector3 vel = GetBlockedAttackVelocity(_aniMoveDelta / Time.fixedDeltaTime);
        _core.Mover.Move(vel);
        _aniMoveDelta = Vector3.zero;
    }

    private Vector3 GetLookDirectionFromCamera()
    {
        Transform camTransform = _core.PlayerCamera.transform;
        Vector3 camForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;

        return camForward * _core.InputController.MoveInput.y + camRight * _core.InputController.MoveInput.x;
    }

    private void PlayerRotation()
    {
        _core.transform.rotation = Quaternion.Slerp(_core.transform.rotation, _targetRot, 15f * Time.fixedDeltaTime);
    }

    private void InitStateValue()
    {
        _aniNormalizedTime = 0f;
        _additionalRootmotionRuntime.Clear();
    }

    private void ConfigureAdditionalRootmotion()
    {
        AttackData currentAttackData = _datas != null && _index >= 0 && _index < _datas.Length ? _datas[_index] : null;
        _additionalRootmotionRuntime.Reset(currentAttackData != null ? currentAttackData.AdditionalRootmotion : null, _targetRot);
    }

    private void SetTargetRotation()
    {
        Vector3 lookDir = _core.transform.forward;

        if(_core.TargetingController.Target != null)
        {
            Vector3 targetPos = _core.TargetingController.Target.transform.position; targetPos.y = 0f;
            Vector3 playerPos = _core.transform.position; playerPos.y = 0f;
            lookDir = targetPos - playerPos;
            if(lookDir.sqrMagnitude >= 0.001f)
            {
                _targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                return;
            }
        }
        else if (_core.InputController.MoveInput.sqrMagnitude > 0.01f)
        {
            lookDir = GetLookDirectionFromCamera();
            if (lookDir.sqrMagnitude >= 0.001f)
            {
                _targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                return;
            }
        }

        _targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
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

        if (!TryGetPlayerCapsule(out CapsuleCollider capsule))
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

    private bool TryGetPlayerCapsule(out CapsuleCollider capsule)
    {
        return _core.TryGetComponent(out capsule);
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
