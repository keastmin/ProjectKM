using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameState _startGameState;

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

    public PlayerInstance CurrentPlayerInstance => _playerInstance;

    public PlayerInstance GetOrCreatePlayerInstance(PlayerStatData statData)
    {
        if (_playerInstance != null)
        {
            return _playerInstance;
        }

        if (statData == null)
        {
            Debug.LogError("PlayerStatData is null.");
            return null;
        }

        _playerInstance = new PlayerInstance(statData);
        return _playerInstance;
    }

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.Log("이미 있음");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void OnEnable()
    {
        OnChangeGameState += MouseVisable;
    }

    private void OnDisable()
    {
        OnChangeGameState -= MouseVisable;
    }

    private void Start()
    {
        CurrentState = _startGameState;
    }

    private void MouseVisable(GameState prev, GameState current)
    {
        switch (current)
        {
            case GameState.Game:
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case GameState.Main:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case GameState.Loading:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case GameState.UI:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case GameState.NodeMap:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            default:
                break;
        }
    }
}
