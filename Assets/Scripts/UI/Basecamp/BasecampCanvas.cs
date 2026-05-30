using Player;
using System;
using UnityEngine;

public class BasecampCanvas : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private GameStarter _gameStarter;
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;
    [SerializeField] private PlayerUpgradeUI _playerUpgradeUI;

    private void Awake()
    {
        _gameStarter.OnPlayerSpawnedAction += PlayerReferenceInject;
        _weaponModeViewerUI.gameObject.SetActive(false);
        _playerUpgradeUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _weaponModeViewerUI.ExitWeaponModeViewerUI();
            _playerUpgradeUI.ExitPlayerUpgradeUI();
        }
    }

    private void PlayerReferenceInject(PlayerCore player)
    {
        _player = player;
        _weaponModeViewerUI.GetPlayerReference(player);
        _playerUpgradeUI.GetPlayerReference(player);
    }
}
