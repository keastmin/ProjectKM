using Player;
using UnityEngine;

public class BasicComboAttackState : StateBase
{
    private readonly AttackExecutionRuntime _attackRuntime;

    private int _index = -1;
    private AttackData[] _datas;
    private Vector3 _aniMoveDelta;
    private Quaternion _targetRot;
    private bool _isMotionWarp;
    private Vector3 _warpPos = Vector3.zero;
    private AnimatorStateInfo _stateInfo;
    private float _aniNormalizedTime;
    private int _aniHash;

    public BasicComboAttackState(PlayerCore core) : base(core)
    {
        _attackRuntime = new AttackExecutionRuntime(core);
    }

    public override void Enter()
    {
        InitStateValue();
        DeterminingMotionWarp();

        _core.TargetSpeed = 0f;
        _core.CurrentSpeed = 0f;

        _datas = _core.KatanaComboDatas;

        _index++;
        _index %= _datas.Length;

        _aniMoveDelta = Vector3.zero;
        _attackRuntime.Reset(_datas[_index].TimingProfile);
        SetTargetRotation();

        Debug.Log("BasicComboAttackState" + ", Combo: " + (_index + 1));

        _core.Animator.CrossFade(_datas[_index].AnimationName, 0.08f, 0, 0f);
    }

    public override void Tick()
    {
        PlayerAnimationHash.TryGetHash(_datas[_index].AnimationName, out _aniHash);
        if (_aniHash == 0)
        {
            _aniHash = Animator.StringToHash($"Base Layer.{_datas[_index].AnimationName}");
        }

        AnimatorChecker.TryGetActiveAnimatorStateInfo(_core.Animator, 0, _aniHash, out _stateInfo);
        _aniNormalizedTime = _stateInfo.normalizedTime;

        _attackRuntime.Process(_datas[_index], _aniNormalizedTime);
        MotionWarpTimeEndCheck(_aniNormalizedTime);

        if (_core.InputController.DodgeInput)
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
        _aniMoveDelta = Vector3.zero;
        _attackRuntime.Clear();
    }

    private void RootMotionMove()
    {
        _aniMoveDelta.y = 0f;
        Vector3 vel = _aniMoveDelta / Time.fixedDeltaTime;
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
        _isMotionWarp = false;
        _aniNormalizedTime = 0f;
    }

    private void MotionWarp()
    {
        float frameSpeed = Vector3.Distance(_core.transform.position, _warpPos) / Time.fixedDeltaTime;
        float warpSpeed = Mathf.Min(_core.BasicComboAttackMotionWarpSpeed, frameSpeed);
        Vector3 dir = _warpPos - _core.transform.position;
        _core.Mover.Move(dir.normalized * warpSpeed);
    }

    private void DeterminingMotionWarp()
    {
        if (_core.TargetingController.Target != null)
        {
            _isMotionWarp = true;
            _warpPos = _core.TargetingController.GetWarpPos();
        }
    }

    private void SetTargetRotation()
    {
        if (_isMotionWarp)
        {
            Vector3 dir = _warpPos - _core.transform.position;
            _targetRot = Quaternion.LookRotation(dir, Vector3.up);
            return;
        }

        if (_core.InputController.MoveInput.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = GetLookDirectionFromCamera();
            if (lookDir.sqrMagnitude >= 0.001f)
            {
                _targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                return;
            }
        }

        _targetRot = Quaternion.LookRotation(_core.transform.forward, Vector3.up);
    }

    private void MotionWarpTimeEndCheck(float aniDelta)
    {
        float firstHitTiming = AttackExecutionRuntime.GetFirstHitNormalizedTime(_datas[_index].TimingProfile);
        if (firstHitTiming >= 0f && aniDelta >= firstHitTiming)
        {
            _isMotionWarp = false;
        }
    }

    private void MotionWarpPositionEndCheck()
    {
        if (Vector3.Distance(_core.transform.position, _warpPos) <= 0.1f)
        {
            _isMotionWarp = false;
        }
    }
}
