using UnityEngine;

public class WeaponModeViewer : MonoBehaviour, IInteraction
{
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;

    public void Interaction()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.State = GameState.UI;
        _weaponModeViewerUI.gameObject.SetActive(true);
    }
}