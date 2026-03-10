using Player;
using UnityEngine;


public class StateMachine
{
    public IdleState IdleState;
    public JogState JogState;
    public RunState RunState;

    private StateBase _currentState;

    public StateMachine(PlayerCore core)
    {
        IdleState = new IdleState(core);
        JogState = new JogState(core);
        RunState = new RunState(core);
    }

    public void InitStateMachine(StateBase initState)
    {
        _currentState = initState;
        _currentState.Enter();
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

    public void Transition(StateBase nextState)
    {
        _currentState?.Exit();
        _currentState = nextState;
        _currentState?.Enter();
    }
}
