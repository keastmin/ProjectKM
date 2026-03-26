using UnityEngine;

public class HellCatIdleState : IState
{
    private HellCatCore _core;

    private const string IDLE_NAME = "Idle";
    private int _idleHash;

    public HellCatIdleState(HellCatCore core)
    {
        _idleHash = Animator.StringToHash("Base Layer." + IDLE_NAME);
        _core = core;
    }

    public void Enter()
    {
        _core.Animator.CrossFade(_idleHash, 0.08f, 0, 0f);
    }

    public void Tick()
    {
        if (_core.DamagedFlag)
        {
            _core.DamagedFlag = false;
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }
    }

    public void FixedTick()
    {
        
    }

    public void LateTick()
    {
        
    }

    public void AnimationTick()
    {
        
    }

    public void Exit()
    {
        
    }
}
