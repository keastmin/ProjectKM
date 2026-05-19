using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameState _startGameState;

    private GameState _state;

    public GameState State
    {
        get 
        { 
            return _state; 
        }
        set
        {
            _state = value;
            OnChangeGameState?.Invoke(_state);
        }
    }

    public event Action<GameState> OnChangeGameState;

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
        State = _startGameState;
    }

    private void MouseVisable(GameState state)
    {
        switch (state)
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
            default:
                break;
        }
    }
}
