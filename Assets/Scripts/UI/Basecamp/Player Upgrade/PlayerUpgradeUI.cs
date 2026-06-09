using Player;
using UnityEngine;

public class PlayerUpgradeUI : BasecampUI
{
    private PlayerCore _player;

    private bool _isInitialized = false;

    public void InitializePlayerUpgradeUI(PlayerCore player)
    {
        _player = player;

        _isInitialized = true;
    }
}
