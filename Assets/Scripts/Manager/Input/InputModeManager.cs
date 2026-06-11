using System;
using System.Collections.Generic;
using UnityEngine;

public class InputModeManager : MonoBehaviour
{
    public static InputModeManager Instance;

    public InputState CurrentState => _stateStack.Peek();

    private Stack<InputState> _stateStack;

    public event Action<InputState> OnChangeInputState;

    private void Awake()
    {
        _stateStack = new Stack<InputState>();

        if(Instance != null)
        {
            Debug.Log("InputModeManager가 이미 있음");
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
        Instance = this;
    }

    public void PushInputState(InputState inputState)
    {
        if (_stateStack == null)
            _stateStack = new Stack<InputState>();

        _stateStack.Push(inputState);
        ApplyInputState(CurrentState);
    }

    public void PopInputState()
    {
        if (_stateStack == null || _stateStack.Count <= 1)
            return;

        _stateStack.Pop();
        ApplyInputState(CurrentState);
    }

    public void ClearInputState()
    {
        if (_stateStack == null)
            return;

        _stateStack.Clear();
        ApplyInputState(InputState.UI);
    }

    private void ApplyInputState(InputState inputState)
    {
        OnChangeInputState?.Invoke(inputState);

        switch (inputState)
        {
            case InputState.Combat:
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case InputState.UI:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case InputState.NodeMap:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }
}
