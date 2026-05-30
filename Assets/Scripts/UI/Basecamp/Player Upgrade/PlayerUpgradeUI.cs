using Player;
using UnityEngine;

public class PlayerUpgradeUI : MonoBehaviour
{

    private PlayerCore _player;

    public void ExitPlayerUpgradeUI()
    {
        if (gameObject.activeSelf)
        {
            if(GameManager.Instance != null)
            {
                GameManager.Instance.State = GameState.Game;
            }

            gameObject.SetActive(false);
        }
    }

    public void GetPlayerReference(PlayerCore player)
    {
        _player = player;
    }
}
