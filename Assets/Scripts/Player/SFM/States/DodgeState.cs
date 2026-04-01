using Player;
using UnityEngine;

public class DodgeState : StateBase
{
    private bool _isFront = false;
    private float _dodgeTime;
    private int _animHash;
    private Vector3 _lookVec;

    private float _stateTime = 0f;

    public DodgeState(PlayerCore core) : base(core) { }

    public override void Enter()
    {
        _stateTime = 0f;

        // 이동 입력이 있으면 정면 회피 아니라면 후면 회피
        _isFront = (_core.InputController.MoveInput.sqrMagnitude >= 0.01f);

        _dodgeTime = _core.StateVariables.DodgeVariable.DodgeTime;
        _animHash = (_isFront) ? PlayerAnimationHash.Katana_Dodge_Front : PlayerAnimationHash.Katana_Dodge_Back;
        _core.Animator.CrossFade(_animHash, 0.03f, 0, 0f);
        _lookVec = PlayerStateUtil.GetCameraRelativeFacingDirection(_core);
    }

    public override void Tick()
    {
        _stateTime += Time.deltaTime;

        if(_stateTime >= _dodgeTime)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }
    }
}
