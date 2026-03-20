using System.Collections.Generic;
using Player;
using UnityEngine;

public class BasicComboAttackState : StateBase
{
    private const int HitResultBufferSize = 32;
    private const float EffectDestroyPadding = 0.5f;

    public BasicComboAttackState(PlayerCore core) : base(core) { }

    private int _index = -1;
    private BasicComboAttackData[] _datas;

    private Vector3 _aniMoveDelta;
    private Quaternion _targetRot;

    // 공격 판정
    private bool[] _triggeredHitTimings = new bool[0];
    private bool[] _triggeredEffectTimings = new bool[0];
    private readonly Collider[] _hitResults = new Collider[HitResultBufferSize];
    private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>();

    // 모션 워프
    private bool _isMotionWarp = false; // 모션 워프 여부
    private Vector3 _warpPos = Vector3.zero; // 모션 워프할 위치

    // 공격 애니메이션 상태 검증
    AnimatorStateInfo _stateInfo;
    private float _aniNormalizedTime = 0f;
    private int _aniHash;

    public override void Enter()
    {
        InitStateValue(); // 상태 검증값 초기화
        DeterminingMotionWarp(); // 모션 워프 여부 결정

        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        _core.Katana.SetActive(true);

        _datas = _core.KatanaComboDatas;

        _index++;
        _index %= _datas.Length;

        _aniMoveDelta = Vector3.zero;
        InitTriggeredHitTimings();

        SetTargetRotation(); // 회전할 방향을 구함

        Debug.Log("BasicComboAttackState" + ", Combo: " + (_index + 1));

        _core.Animator.CrossFade(_datas[_index].AnimationName, 0.08f, 0, 0f);
    }

    public override void Tick()
    {
        PlayerAnimationHash.TryGetHash(_datas[_index].AnimationName, out _aniHash);
        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _aniHash, out _stateInfo);
        _aniNormalizedTime = _stateInfo.normalizedTime;

        ProcessHitTimings(_aniNormalizedTime);
        ProcessEffectTimings(_aniNormalizedTime);
        MotionWarpTimeEndCheck(_aniNormalizedTime);

        if (_datas[_index].Timing.ComboInputEndNormalizedTime <= _aniNormalizedTime)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (_core.InputController.BasicComboAttackInput &&
            _datas[_index].Timing.ComboInputStartNormalizedTime <= _aniNormalizedTime &&
            _datas[_index].Timing.ComboInputEndNormalizedTime > _aniNormalizedTime)
        {
            Enter();
            return;
        }
    }

    public override void FixedTick()
    {
        PlayerRotation();
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
        _aniMoveDelta += _core.Animator.deltaPosition;
    }

    public override void Exit()
    {
        _index = -1;
        _core.Katana.SetActive(false);
        _aniMoveDelta = Vector3.zero;
        _triggeredHitTimings = new bool[0];
        _triggeredEffectTimings = new bool[0];
        _damagedTargets.Clear();
    }

    private void InitTriggeredHitTimings()
    {
        ComboAttackData comboTiming = _datas[_index].Timing;
        AttackTimingDefinition[] attackTimings = comboTiming != null ? comboTiming.AttackTimings : null;
        AttackEffectTimingDefinition[] attackEffectTimings = comboTiming != null ? comboTiming.AttackEffectTimings : null;

        _triggeredHitTimings = attackTimings != null
            ? new bool[attackTimings.Length]
            : new bool[0];

        _triggeredEffectTimings = attackEffectTimings != null
            ? new bool[attackEffectTimings.Length]
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

    private void ProcessEffectTimings(float aniDelta)
    {
        ComboAttackData comboTiming = _datas[_index].Timing;
        if (comboTiming == null || comboTiming.AttackEffectTimings == null)
        {
            return;
        }

        for (int i = 0; i < comboTiming.AttackEffectTimings.Length; i++)
        {
            if (_triggeredEffectTimings[i])
            {
                continue;
            }

            AttackEffectTimingDefinition effectTiming = comboTiming.AttackEffectTimings[i];
            if (effectTiming == null || effectTiming.NormalizedTime > aniDelta)
            {
                continue;
            }

            ApplyAttackEffect(effectTiming);
            _triggeredEffectTimings[i] = true;
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

    private void ApplyAttackEffect(AttackEffectTimingDefinition effectTiming)
    {
        if (!_core.AttackEffectController.TryGetAttackEffectData(effectTiming.Id, out AttackEffectController.AttackEffectData effectData))
        {
            return;
        }

        if (effectData.AttackEffect == null || effectData.EffectSpawnTransform == null)
        {
            return;
        }

        ParticleSystem effectInstance = Object.Instantiate(
            effectData.AttackEffect,
            effectData.EffectSpawnTransform);

        Object.Destroy(effectInstance.gameObject, GetEffectDestroyDelay(effectInstance));
    }

    private static float GetEffectDestroyDelay(ParticleSystem effectInstance)
    {
        if (effectInstance == null)
        {
            return EffectDestroyPadding;
        }

        ParticleSystem.MainModule main = effectInstance.main;
        float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
            ? main.startLifetime.constantMax
            : main.startLifetime.constant;

        return Mathf.Max(main.duration + startLifetime, EffectDestroyPadding);
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

    // 상태 검증값 초기화
    private void InitStateValue()
    {
        _isMotionWarp = false;
        _aniNormalizedTime = 0f;
    }

    // 타겟 위치로 정해진 속도로 모션 워프 수행
    private void MotionWarp()
    {
        float frameSpeed = Vector3.Distance(_core.transform.position, _warpPos) / Time.fixedDeltaTime;
        float warpSpeed = Mathf.Min(_core.BasicComboAttackMotionWarpSpeed, frameSpeed);
        Vector3 dir = _warpPos - _core.transform.position;
        _core.CharacterMover.Move(dir.normalized * warpSpeed);
    }

    // 모션 워프를 할지 방향키대로 움직일지 결정하고 모션 워프를 한다면 워프할 위치 저장
    private void DeterminingMotionWarp()
    {
        if (_core.TargetingController.Target != null)
        {
            _isMotionWarp = true;
            _warpPos = _core.TargetingController.GetWarpPos();
        }
    }

    // 회전할 타겟 방향을 세팅하는 함수
    private void SetTargetRotation()
    {
        if (_isMotionWarp)
        {
            Vector3 dir = _warpPos - _core.transform.position;
            _targetRot = Quaternion.LookRotation(dir, Vector3.up);
        }
        else
        {
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
        }
    }

    // 모션 워프 중에 시간 초과로 인한 종료 체크
    private void MotionWarpTimeEndCheck(float aniDelta)
    {
        ComboAttackData comboTiming = _datas[_index].Timing;
        bool isStartFirstAttack =
            comboTiming != null &&
            comboTiming.AttackTimings != null &&
            comboTiming.AttackTimings.Length > 0 &&
            comboTiming.AttackTimings[0] != null &&
            aniDelta >= comboTiming.AttackTimings[0].NormalizedTime;

        if (isStartFirstAttack)
            _isMotionWarp = false;
    }

    // 모션 워프 중에 거리 도달로 인한 종료 체크
    private void MotionWarpPositionEndCheck()
    { 
        bool isArriveTargetPos = (Vector3.Distance(_core.transform.position, _warpPos) <= 0.1f);
        if(isArriveTargetPos)
            _isMotionWarp = false;
    }
}
