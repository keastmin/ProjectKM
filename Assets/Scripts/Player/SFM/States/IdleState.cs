using Player;
using UnityEngine;

public class IdleState : StateBase
{
    public IdleState(PlayerCore core) : base(core) { }

    private bool _isWaitingComboAttackAni = false;

    public override void Enter()
    {
        _isWaitingComboAttackAni = false;

        Debug.Log("Idle State");
        _core.TargetSpeed = 0f;

        // 이전 상태 애니메이션을 기다려야 하는 경우 구분
        if(_core.FSM.PrevState == _core.FSM.BasicComboAttackState)
        {
            _isWaitingComboAttackAni = true;
        }
        else if (_core.Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != PlayerAnimationHash.Katana_Move)
        {
            _core.Animator.CrossFade(PlayerAnimationHash.Katana_Move, 0.08f);
        }
    }

    public override void Tick()
    {
        // 이전 상태 애니메이션을 기다려야 될 때
        if (_isWaitingComboAttackAni)
        {
            AnimatorStateInfo stateInfo = _core.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime > 0.98f)
            {
                _isWaitingComboAttackAni = false;
                _core.Animator.CrossFade(PlayerAnimationHash.Katana_Move, 0.08f);
            }
        }

        if (_core.InputController.DodgeInput)
        {
            _core.FSM.Transition(_core.FSM.DodgeState);
            return;
        }

        // 데미지를 입으면 데미지 상태로 전환
        if (_core.DamageFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if (_core.InputController.BasicComboAttackInput &&
            _core.KatanaComboDatas.Length > 0)
        {
            _core.FSM.Transition(_core.FSM.BasicComboAttackState);
            return;
        }

        // 이전 상태가 달리기 였으며, 현재 속도가 조깅 속도보다 빠른 상태이고,
        // 지금 입력한 MoveInput을 통한 캐릭터가 회전하게 될 각도가 현재 각도에서 180도 +, -알파라면 턴 상태로 전환
        Vector2 moveInput = _core.InputController.MoveInput;
        if (_core.FSM.PrevState == _core.FSM.RunState &&
            _core.CurrentSpeed >= (_core.RunSpeed / 2f) &&
            moveInput.sqrMagnitude >= 0.01f &&
            IsOppositeTurnInput(moveInput))
        {
            _core.FSM.Transition(_core.FSM.RunTurnState);
            return;
        }

        // 움직임 입력이 있을 경우 Jog로 이동
        if (_core.InputController.MoveInput.sqrMagnitude >= 0.01f)
        {
            _core.FSM.Transition(_core.FSM.JogState);
            return;
        }
    }

    public override void FixedTick()
    {
        Move();
    }

    public override void Exit()
    {
        _isWaitingComboAttackAni = false;
    }

    private void Move()
    {
        //_core.CharacterMover.Move(_core.transform.forward * _core.CurrentSpeed);
        _core.Mover.Move(_core.transform.forward * _core.CurrentSpeed);
    }

    private bool IsOppositeTurnInput(Vector2 moveInput)
    {
        Transform cam = _core.PlayerCamera.transform;

        Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

        // 입력 기준 목표 방향
        Vector3 desiredDir = camForward * moveInput.y + camRight * moveInput.x;

        if (desiredDir.sqrMagnitude < 0.0001f)
            return false;

        desiredDir.Normalize();

        // 현재 바라보는 방향과 목표 방향의 수평 각도
        float angle = Vector3.Angle(_core.transform.forward, desiredDir);

        // 예: 150도 이상이면 반대 방향 턴으로 간주
        return angle >= 160f;
    }
}
