using System;
using UnityEngine;

public class PlayerUpgradeOpener : MonoBehaviour, IInteraction
{
    public event Action OnInteractPlayerUpgradeAction;

    public void Interaction()
    {
        OnInteractPlayerUpgradeAction?.Invoke();
    }
}
