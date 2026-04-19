using UnityEngine;

public class GameManager : MonoBehaviour
{
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
            MouseVisable(_state);
        }
    }

    private void Start()
    {
        State = GameState.Player;
    }

    private void MouseVisable(GameState state)
    {
        switch (state)
        {
            case GameState.Player:
                // 커서를 숨기고 락을 걸어둠
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case GameState.UI:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }
}
