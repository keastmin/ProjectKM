using Player;
using UnityEngine;

public class RunState : StateBase
{
    public RunState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        Debug.Log("Run State");
        _core.TargetSpeed = _core.RunSpeed;
        if (_core.Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != PlayerAnimationHash.Katana_Move)
        {
            _core.Animator.CrossFade(PlayerAnimationHash.Katana_Move, 0.08f);
        }
    }

    public override void Tick()
    {
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

        // 이전 상태가 달리기 였으며, 현재 속도가 조깅 속도보다 빠른 상태이고,
        // 지금 입력한 MoveInput을 통한 캐릭터가 회전하게 될 각도가 현재 각도에서 180도 +, -알파라면 턴 상태로 전환
        Vector2 moveInput = _core.InputController.MoveInput;
        if (moveInput.sqrMagnitude >= 0.01f &&
            IsOppositeTurnInput(moveInput))
        {
            _core.FSM.Transition(_core.FSM.RunTurnState);
            return;
        }

        if (_core.InputController.MoveInput.sqrMagnitude < 0.01f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }

    public override void FixedTick()
    {
        PlayerRotation();
        Move();
    }

    private void PlayerRotation()
    {
        Vector3 lookDir = GetLookDirectionFromCamera();
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            _core.transform.rotation = Quaternion.Slerp(_core.transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
        }
    }

    private void Move()
    {
        //_core.CharacterMover.Move(_core.transform.forward * _core.CurrentSpeed);
        _core.Mover.Move(_core.transform.forward * _core.CurrentSpeed);
    }

    // 카메라가 보고있는 정면을 구하는 함수
    private Vector3 GetLookDirectionFromCamera()
    {
        Transform camTransform = _core.PlayerCamera.transform;
        Vector3 camForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;

        Vector3 lookDir = camForward * _core.InputController.MoveInput.y + camRight * _core.InputController.MoveInput.x;

        return lookDir;
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
