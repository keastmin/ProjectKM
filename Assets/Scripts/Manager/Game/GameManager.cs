using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameState _prevState;
    private GameState _currState;

    public GameState CurrentState
    {
        get 
        { 
            return _currState; 
        }
        set
        {
            _prevState = _currState;
            _currState = value;
            OnChangeGameState?.Invoke(_prevState, _currState);
        }
    }

    public event Action<GameState, GameState> OnChangeGameState; // 이전 상태, 현재 상태

    public void SetGameState(GameState state)
    {
        CurrentState = state;
    }
}