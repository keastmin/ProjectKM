using Player;
using System;
using UnityEngine;

public class BasecampUI : MonoBehaviour
{
    protected PlayerCore _player;

    public event Action<BasecampUI> OnEscapeThisUIAction;

    public void GetPlayerReference(PlayerCore player)
    {
        _player = player;
    }

    public virtual void InputEscapeKey()
    {
        OnEscapeThisUIAction?.Invoke(this);
    }
}
