using UnityEngine;

public class HellCatFSM
{
    public HellCatIdleState IdleState;
    public HellCatChaseState ChaseState;
    public HellCatStrafeState StrafeState;
    public HellCatDamagedState DamagedState;
    public HellCatBiteAttackState BiteAttackState;

    private IState _currentState;

    public HellCatFSM(HellCatCore core)
    {
        IdleState = new HellCatIdleState(core);
        ChaseState = new HellCatChaseState(core);
        StrafeState = new HellCatStrafeState(core);
        DamagedState = new HellCatDamagedState(core);
        BiteAttackState = new HellCatBiteAttackState(core);
    }

    public void Initialize(IState initState)
    {
        _currentState = initState;
        _currentState?.Enter();
    }

    public void Tick()
    {
        _currentState?.Tick();
    }

    public void FixedTick()
    {
        _currentState?.FixedTick();
    }

    public void LateTick()
    {
        _currentState?.LateTick();
    }

    public void AnimationTick()
    {
        _currentState?.AnimationTick();
    }

    public void Transition(IState nextstate)
    {
        _currentState.Exit();
        _currentState = nextstate;
        _currentState.Enter();
    }
}
