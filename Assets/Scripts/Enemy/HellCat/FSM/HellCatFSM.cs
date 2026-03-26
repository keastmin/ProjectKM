using UnityEngine;

public class HellCatFSM
{
    public HellCatIdleState IdleState;
    public HellCatDamagedState DamagedState;

    private IState _currentState;

    public HellCatFSM(HellCatCore core)
    {
        IdleState = new HellCatIdleState(core);
        DamagedState = new HellCatDamagedState(core);
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
