using System;
using UnityEngine;

public class WeaponModeViewer : MonoBehaviour, IInteraction
{
    public event Action OnInteractWeaponModeViewerAction;

    public void Interaction()
    {
        OnInteractWeaponModeViewerAction?.Invoke();
    }
}