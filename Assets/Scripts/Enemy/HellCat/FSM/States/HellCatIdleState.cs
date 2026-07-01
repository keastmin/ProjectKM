using UnityEngine;

public class HellCatIdleState : IState
{
    private HellCatCore _core;

    private int _animHash;

    public HellCatIdleState(HellCatCore core)
    {
        _core = core;
        _animHash = Animator.StringToHash("Base Layer." + core.IdleStateData.AnimationName);
    }

    public void Enter()
    {
        _core.Animator.CrossFade(_animHash, 0.08f, 0, 0f);
    }

    public void Tick()
    {
        if (_core.IsDead)
        {
            _core.FSM.Transition(_core.FSM.DeadState);
            return;
        }

        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if(_core.DetectedPlayer != null)
        {
            _core.FSM.Transition(_core.FSM.ChaseState);
            return;
        }

        //if (_core.IsBasicAttackEnable)
        //{
        //    _core.FSM.Transition(_core.FSM.BasicAttackState);
        //    return;
        //}
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
