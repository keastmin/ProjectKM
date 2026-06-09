using System;
using UnityEngine;

public class WeaponModeViewerOpener : MonoBehaviour, IInteraction
{
    public event Action OnInteractWeaponModeViewerAction;

    public void Interaction()
    {
        OnInteractWeaponModeViewerAction?.Invoke();
    }
}