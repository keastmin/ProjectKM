using UnityEngine;

public class HellCatStrafeState : IState
{
    private HellCatCore _core;

    private int _leftStrafeAnimHash;
    private int _rightStrafeAnimHash;
    private int _forwardStrafeAnimHash;
    private int _backStrafeAnimHash;

    public HellCatStrafeState(HellCatCore core)
    {
        _core = core;
        _leftStrafeAnimHash = Animator.StringToHash("Base Layer." + core.LeftStrafeStateData.AnimationName);
        _rightStrafeAnimHash = Animator.StringToHash("Base Layer." + core.RightStrafeStateData.AnimationName);
        _forwardStrafeAnimHash = Animator.StringToHash("Base Layer." + core.ForwardStrafeStateData.AnimationName);
        _backStrafeAnimHash = Animator.StringToHash("Base Layer." + core.BackwardStrafeStateData.AnimationName);
    } 

    public void Enter()
    {
        _core.Animator.CrossFade(_rightStrafeAnimHash, 0.18f, 0, 0f);
    }

    public void Tick()
    {
        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if (_core.PlayerDistance > 7f)
        {
            _core.FSM.Transition(_core.FSM.ChaseState);
            return;
        }
    }

    public void FixedTick()
    {
        RotateTowardsPlayer();
        Strafe();
    }

    public void LateTick()
    {
        
    }

    public void AnimationTick()
    {

    }

    public void Exit()
    {
        _core.Rigidbody.linearVelocity = Vector3.zero;
    }

    private void RotateTowardsPlayer()
    {
        if(_core.PlayerCollider != null)
        {
            Vector3 lookDir = _core.PlayerCollider.transform.position - _core.transform.position;
            lookDir.y = 0f;
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            _core.Rigidbody.MoveRotation(Quaternion.RotateTowards(_core.Rigidbody.rotation, targetRot, 360f * Time.fixedDeltaTime));
        }
    }

    private void Strafe()
    {
        Debug.Log(_core.transform.right * _core.StrafeSpeed);
        _core.Rigidbody.linearVelocity = _core.transform.right * _core.StrafeSpeed;
    }
}
