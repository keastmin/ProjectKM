using UnityEngine;

public class HellCatChaseState : IState
{
    private HellCatCore _core;

    private EnemyStateData _stateData;
    private int _animHash;

    public HellCatChaseState(HellCatCore core)
    {
        _core = core;
        _stateData = core.ChaseStateData;
        _animHash = Animator.StringToHash("Base Layer." + _stateData.AnimationName);
    }

    public void Enter()
    {
        _core.Agent.isStopped = false;

        _core.Animator.CrossFade(_animHash, 0.08f, 0, 0f);
    }

    public void Tick()
    {
        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        _core.Agent.SetDestination(_core.PlayerCollider.transform.position);
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
        _core.Agent.isStopped = true;
    }
}
