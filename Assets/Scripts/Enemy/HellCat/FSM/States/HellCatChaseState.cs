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
        _core.Agent.enabled = true;
        _core.Agent.isStopped = false;
        _core.Agent.updateRotation = false;
        _core.Agent.speed = _core.ChaseSpeed;
        _core.Agent.stoppingDistance = _core.ChaseEndDistance;

        _core.Animator.CrossFade(_animHash, 0.08f, 0, 0f);
    }

    public void Tick()
    {
        if (_core.DamagedFlag)
        {
            _core.FSM.Transition(_core.FSM.DamagedState);
            return;
        }

        if (_core.DetectedPlayer == null)
        {
            _core.FSM.Transition(_core.FSM.IdleState);
            return;
        }

        if (_core.PlayerDistance <= _core.ChaseEndDistance)
        {
            _core.FSM.Transition(_core.FSM.StrafeState);
            return;
        }

        _core.Agent.SetDestination(_core.DetectedPlayer.transform.position);
        _core.RequestModelRotationTowards(_core.DetectedPlayer.transform.position, 720f);
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
        _core.Agent.enabled = false;
    }
}
