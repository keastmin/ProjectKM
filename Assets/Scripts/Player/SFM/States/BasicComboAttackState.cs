using System.Collections.Generic;
using Player;
using UnityEngine;

public class BasicComboAttackState : StateBase
{
    private const int HitResultBufferSize = 32;

    public BasicComboAttackState(PlayerCore core) : base(core) { }

    private int _index = -1;
    private BasicComboAttackData[] _datas;
    private float _stateStartTime = 0f;

    private Vector3 _aniMoveDelta;
    private Quaternion _targetRot;
    
    // 공격 판정
    private bool[] _triggeredHitTimings = new bool[0];
    private readonly Collider[] _hitResults = new Collider[HitResultBufferSize];
    private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>();

    public override void Enter()
    {
        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        _core.Katana.SetActive(true);

        _datas = _core.KatanaComboDatas;

        _index++;
        _index %= _datas.Length;

        _aniMoveDelta = Vector3.zero;
        InitTriggeredHitTimings();

        if (_core.InputController.MoveInput.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = GetLookDirectionFromCamera();
            if (lookDir.sqrMagnitude >= 0.001f)
            {
                _targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }
        else
        {
            _targetRot = Quaternion.LookRotation(_core.transform.forward, Vector3.up);
        }

        Debug.Log("BasicComboAttackState" + ", Combo: " + (_index + 1));

        _core.Animator.CrossFade(_datas[_index].AnimationName, 0.08f, 0, 0f);
        _stateStartTime = Time.time;
    }

    public override void Tick()
    {
        AnimatorStateInfo info = _core.Animator.GetCurrentAnimatorStateInfo(0);
        bool isAniSame = info.IsName(_datas[_index].AnimationName);
        float aniDelta = info.normalizedTime;
        bool isSafeTransition = Time.time - _stateStartTime >= _core.SafeTransitionDuration;

        if (isAniSame)
        {
            ProcessHitTimings(aniDelta);
        }

        if (isSafeTransition && isAniSame && _datas[_index].Timing.ComboInputEndNormalizedTime <= aniDelta)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (_core.InputController.BasicComboAttackInput && isAniSame &&
            _datas[_index].Timing.ComboInputStartNormalizedTime <= aniDelta &&
            _datas[_index].Timing.ComboInputEndNormalizedTime > aniDelta)
        {
            Enter();
            return;
        }
    }

    public override void FixedTick()
    {
        PlayerRotation();
        RootMotionMove();
    }

    public override void AnimationTick()
    {
        _aniMoveDelta += _core.Animator.deltaPosition;
    }

    public override void Exit()
    {
        _index = -1;
        _core.Katana.SetActive(false);
        _aniMoveDelta = Vector3.zero;
        _triggeredHitTimings = new bool[0];
        _damagedTargets.Clear();
    }

    private void InitTriggeredHitTimings()
    {
        AttackTimingDefinition[] attackTimings = _datas[_index].Timing != null
            ? _datas[_index].Timing.AttackTimings
            : null;

        _triggeredHitTimings = attackTimings != null
            ? new bool[attackTimings.Length]
            : new bool[0];
    }

    private void ProcessHitTimings(float aniDelta)
    {
        ComboAttackData comboTiming = _datas[_index].Timing;
        if (comboTiming == null || comboTiming.AttackTimings == null)
        {
            return;
        }

        for (int i = 0; i < comboTiming.AttackTimings.Length; i++)
        {
            if (_triggeredHitTimings[i])
            {
                continue;
            }

            AttackTimingDefinition attackTiming = comboTiming.AttackTimings[i];
            if (attackTiming == null || attackTiming.NormalizedTime > aniDelta)
            {
                continue;
            }

            ApplyHitTiming(attackTiming);
            _triggeredHitTimings[i] = true;
        }
    }

    private void ApplyHitTiming(AttackTimingDefinition attackTiming)
    {
        if (!_core.HitController.TryGetHitboxes(attackTiming.Id, out BoxCollider[] hitboxes) || hitboxes == null)
        {
            return;
        }

        _damagedTargets.Clear();

        for (int i = 0; i < hitboxes.Length; i++)
        {
            BoxCollider hitbox = hitboxes[i];
            if (hitbox == null || !hitbox.enabled || !hitbox.gameObject.activeInHierarchy)
            {
                continue;
            }

            Transform hitboxTransform = hitbox.transform;
            Vector3 worldCenter = hitboxTransform.TransformPoint(hitbox.center);
            Vector3 scaledHalfExtents = Vector3.Scale(hitbox.size * 0.5f, hitboxTransform.lossyScale);
            Vector3 worldHalfExtents = new Vector3(
                Mathf.Abs(scaledHalfExtents.x),
                Mathf.Abs(scaledHalfExtents.y),
                Mathf.Abs(scaledHalfExtents.z));

            int hitCount = Physics.OverlapBoxNonAlloc(
                worldCenter,
                worldHalfExtents,
                _hitResults,
                hitboxTransform.rotation,
                _core.HitController.HitLayer,
                QueryTriggerInteraction.Collide);

            for (int j = 0; j < hitCount; j++)
            {
                Collider hitCollider = _hitResults[j];
                if (hitCollider == null || hitCollider.transform.IsChildOf(_core.transform))
                {
                    continue;
                }

                IDamageable damageable = hitCollider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
                if (damageable == null || !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                damageable.TakeDamage(_datas[_index].Damage);
            }
        }
    }

    private void RootMotionMove()
    {
        _aniMoveDelta.y = 0f;
        Vector3 vel = _aniMoveDelta / Time.fixedDeltaTime;
        _core.CharacterMover.Move(vel);
        _aniMoveDelta = Vector3.zero;
    }

    private Vector3 GetLookDirectionFromCamera()
    {
        Transform camTransform = _core.PlayerCamera.transform;
        Vector3 camForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;

        Vector3 lookDir = camForward * _core.InputController.MoveInput.y + camRight * _core.InputController.MoveInput.x;

        return lookDir;
    }

    private void PlayerRotation()
    {
        _core.transform.rotation = Quaternion.Slerp(_core.transform.rotation, _targetRot, 15f * Time.fixedDeltaTime);
    }
}
