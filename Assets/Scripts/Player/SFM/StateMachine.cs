using Player;
using UnityEngine;


public class StateMachine
{
    public IdleState IdleState;
    public JogState JogState;
    public RunState RunState;
    public RunTurnState RunTurnState;
    public BasicComboAttackState BasicComboAttackState;

    private StateBase _currentState;
    private StateBase _prevState;

    public StateBase PrevState => _prevState;

    public StateMachine(PlayerCore core)
    {
        IdleState = new IdleState(core);
        JogState = new JogState(core);
        RunState = new RunState(core);
        RunTurnState = new RunTurnState(core);
        BasicComboAttackState = new BasicComboAttackState(core);
    }

    public void InitStateMachine(StateBase initState)
    {
        _currentState = initState;
        _prevState = null;
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
        _prevState = _currentState;
        _currentState = nextState;
        _currentState?.Enter();
    }
}
