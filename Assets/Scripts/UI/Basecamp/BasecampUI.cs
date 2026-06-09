using Player;
using System;
using UnityEngine;

public class BasecampUI : MonoBehaviour
{
    public event Action<BasecampUI> OnOpenThisUIAction;
    public event Action<BasecampUI> OnCloseThisUIAction;

    protected void Open()
    {
        OnOpenThisUIAction?.Invoke(this);
    }

    protected void Close()
    {
        OnCloseThisUIAction?.Invoke(this);
    }
}
