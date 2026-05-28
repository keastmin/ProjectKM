using UnityEngine;

public class WeaponModeViewer : MonoBehaviour, IInteraction
{
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;

    public void Interaction()
    {
        _weaponModeViewerUI.gameObject.SetActive(true);
    }
}