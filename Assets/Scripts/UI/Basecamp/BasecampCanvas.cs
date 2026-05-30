using Player;
using System;
using UnityEngine;

public class BasecampCanvas : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private PlayerCore _player;
    [SerializeField] private GameStarter _gameStarter;
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;
    [SerializeField] private PlayerUpgradeUI _playerUpgradeUI;

    [Header("World")]
    [SerializeField] private WeaponModeViewer _weaponModeViewerWorld;
    [SerializeField] private PlayerUpgrade _playerUpgradeWorld;

    private BasecampUI _currentFocusUI;

    private void Awake()
    {
        _currentFocusUI = null;
        _gameStarter.OnPlayerSpawnedAction += PlayerReferenceInject;
        _weaponModeViewerUI.gameObject.SetActive(false);
        _playerUpgradeUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _weaponModeViewerWorld.OnInteractWeaponModeViewerAction += ActiveWeaponModeViewerUIHandle;
        _playerUpgradeWorld.OnInteractPlayerUpgradeAction += ActivePlayerUpgradeUIHandle;

        _weaponModeViewerUI.OnEscapeThisUIAction += DisactiveUI;
        _playerUpgradeUI.OnEscapeThisUIAction += DisactiveUI;
    }

    private void OnDisable()
    {
        _weaponModeViewerWorld.OnInteractWeaponModeViewerAction -= ActiveWeaponModeViewerUIHandle;
        _playerUpgradeWorld.OnInteractPlayerUpgradeAction -= ActivePlayerUpgradeUIHandle;

        _weaponModeViewerUI.OnEscapeThisUIAction -= DisactiveUI;
        _playerUpgradeUI.OnEscapeThisUIAction -= DisactiveUI;
    }

    private void Update()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }
        if(GameManager.Instance.State == GameState.Game)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _currentFocusUI.InputEscapeKey();
        }
    }

    private void PlayerReferenceInject(PlayerCore player)
    {
        _player = player;
        _weaponModeViewerUI.GetPlayerReference(player);
        _playerUpgradeUI.GetPlayerReference(player);
    }

    private void ActiveWeaponModeViewerUIHandle()
    {
        ActiveUI(_weaponModeViewerUI);
    }

    private void ActivePlayerUpgradeUIHandle()
    {
        ActiveUI(_playerUpgradeUI);
    }

    private void ActiveUI(BasecampUI ui)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.State = GameState.UI;
        ui.gameObject.SetActive(true);
        _currentFocusUI = ui;
    }

    private void DisactiveUI(BasecampUI ui)
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("게임 매니저가 없습니다");
            return;
        }

        GameManager.Instance.State = GameState.Game;
        ui.gameObject.SetActive(false);
        _currentFocusUI = null;
    }
}
