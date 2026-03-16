using Player;
using UnityEngine;

public class JogState : StateBase
{
    public JogState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        Debug.Log("Jog State");
        _core.TargetSpeed = _core.JogSpeed;
        if (!_core.Animator.GetCurrentAnimatorStateInfo(0).IsName(PlayerAnimationNameContainer.NO_WEAPON_MOVE))
        {
            _core.Animator.CrossFade(PlayerAnimationNameContainer.NO_WEAPON_MOVE, 0.08f);
        }
    }

    public override void Tick()
    {
        if (_core.InputController.BasicComboAttackInput &&
            _core.KatanaComboDatas.Length > 0)
        {
            _core.FSM.Transition(_core.FSM.BasicComboAttackState);
            return;
        }

        if (_core.InputController.MoveInput.sqrMagnitude < 0.01f)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (_core.InputController.RunInput)
        {
            _core.FSM.Transition(_core.FSM.RunState);
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
        if(lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            _core.transform.rotation = Quaternion.Slerp(_core.transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
        }
    }

    private void Move()
    {
        _core.CharacterMover.Move(_core.transform.forward * _core.CurrentSpeed);
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
}
