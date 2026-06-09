using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private GameState _prevState;
    private GameState _currState;
    private PlayerInstance _playerInstance;

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

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.Log("GameManager가 이미 있음");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void SetGameState(GameState state)
    {
        CurrentState = state;
    }

}
