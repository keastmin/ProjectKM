using Player;
using UnityEngine;

public class DodgeCounterState : StateBase
{
    private const float ATTACK_MOVE_STOP_BUFFER = 0.02f;

    private readonly AttackExecutionRuntime _attackRuntime;
    private readonly AdditionalRootmotionRuntime _additionalRootmotionRuntime;

    private AnimatorStateInfo _animInfo;
    private Vector3 _animDeltaPos;
    private AttackData _attackData;
    private int _animHash;

    public DodgeCounterState(PlayerCore core) : base(core)
    {
        _attackRuntime = new AttackExecutionRuntime(core);
        _additionalRootmotionRuntime = new AdditionalRootmotionRuntime();
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

        if(_core.DodgeCounterTarget != null)
        {
            Vector3 enemyHurtColPos = _core.DodgeCounterTarget.transform.position;
            enemyHurtColPos.y = _core.transform.position.y;
            PlayerStateUtil.RotateImmediatelyTowardsDirection(_core.transform, (enemyHurtColPos - _core.transform.position).normalized);
        }

        _additionalRootmotionRuntime.Reset(_attackData != null ? _attackData.AdditionalRootmotion : null, _core.transform.rotation);

        _core.StopPerfectDodgeTimeScaleImmediate();
    }

    public override void Tick()
    {
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _animHash, out _animInfo);
        _attackRuntime.Process(_attackData, _animInfo.normalizedTime, _core.CameraShake, _core.StartHitStop);

        if (_animInfo.normalizedTime >= 0.97f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    public override void FixedTick()
    {
        _animDeltaPos += _additionalRootmotionRuntime.ConsumeDelta(_animInfo.normalizedTime);
        _animDeltaPos.y = 0;
        Vector3 vel = GetBlockedAttackVelocity(_animDeltaPos / Time.fixedDeltaTime);
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
        _additionalRootmotionRuntime.Clear();
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
