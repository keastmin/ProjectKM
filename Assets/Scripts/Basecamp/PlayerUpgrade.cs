using UnityEngine;

public class PlayerUpgrade : MonoBehaviour, IInteraction
{
    [SerializeField] private PlayerUpgradeUI _playerUpgradeUI;

    public void Interaction()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.State = GameState.UI;
        _playerUpgradeUI.gameObject.SetActive(true);
    }
}
