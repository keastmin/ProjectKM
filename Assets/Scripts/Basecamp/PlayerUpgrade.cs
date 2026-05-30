using System;
using UnityEngine;

public class PlayerUpgrade : MonoBehaviour, IInteraction
{
    public event Action OnInteractPlayerUpgradeAction;

    public void Interaction()
    {
        OnInteractPlayerUpgradeAction?.Invoke();
    }
}
