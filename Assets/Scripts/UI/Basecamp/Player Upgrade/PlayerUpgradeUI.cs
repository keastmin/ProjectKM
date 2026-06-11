using Player;
using UnityEngine;

public class PlayerUpgradeUI : BasecampUI
{
    private PlayerCore _player;

    private bool _isInitialized = false;

    private void OnEnable()
    {
        Open();
    }

    private void OnDisable()
    {
        Close();
    }

    public void InitializePlayerUpgradeUI(PlayerCore player)
    {
        _player = player;

        _isInitialized = true;
    }
}
