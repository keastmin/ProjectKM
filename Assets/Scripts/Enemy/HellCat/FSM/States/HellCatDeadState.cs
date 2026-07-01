using UnityEngine;

public class HellCatDeadState : IState
{
    private HellCatCore _core;

    private int _deadAnimationHash;

    public HellCatDeadState(HellCatCore core)
    {
        _core = core;
        _deadAnimationHash = Animator.StringToHash("Base Layer." + _core.DeadStateData.AnimationName);
    }

    public void Enter()
    {
        _core.Rigidbody.isKinematic = true;
        _core.Animator.CrossFade(_deadAnimationHash, 0.03f, 0, 0f);
    }

    public void AnimationTick()
    {
        
    }

    public void Exit()
    {
        
    }

    public void FixedTick()
    {
        
    }

    public void LateTick()
    {
        
    }

    public void Tick()
    {
        
    }
}
