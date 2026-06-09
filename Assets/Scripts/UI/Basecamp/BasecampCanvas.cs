using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public class BasecampCanvas : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private WeaponModeViewerUI _weaponModeViewerUI;
    [SerializeField] private PlayerUpgradeUI _playerUpgradeUI;

    private WeaponModeViewerOpener _weaponModeViewerOpener;
    private PlayerUpgradeOpener _playerUpgradeOpener;
    private InputModeManager _inputModeManager;

    private bool _isInitialized = false;

    private BasecampUI _currentFocusedUI;

    private void OnEnable()
    {
        if(_weaponModeViewerOpener != null)
        {
            _weaponModeViewerOpener.OnInteractWeaponModeViewerAction -= OpenWeaponModeViewerUIHandle;
            _weaponModeViewerOpener.OnInteractWeaponModeViewerAction += OpenWeaponModeViewerUIHandle;
        }

        if(_playerUpgradeOpener != null)
        {
            _playerUpgradeOpener.OnInteractPlayerUpgradeAction -= OpenWeaponModeViewerUIHandle;
            _playerUpgradeOpener.OnInteractPlayerUpgradeAction += OpenWeaponModeViewerUIHandle;
        }

        if(_weaponModeViewerUI != null)
        {
            _weaponModeViewerUI.OnOpenThisUIAction -= OpenUI;
            _weaponModeViewerUI.OnOpenThisUIAction += OpenUI;
        }

        if(_playerUpgradeUI != null)
        {
            _playerUpgradeUI.OnOpenThisUIAction -= OpenUI;
            _playerUpgradeUI.OnOpenThisUIAction += OpenUI;
        }
    }

    private void OnDisable()
    {
        if (_weaponModeViewerOpener != null)
        {
            _weaponModeViewerOpener.OnInteractWeaponModeViewerAction -= OpenWeaponModeViewerUIHandle;
        }

        if (_playerUpgradeOpener != null)
        {
            _playerUpgradeOpener.OnInteractPlayerUpgradeAction -= OpenWeaponModeViewerUIHandle;
        }

        if (_weaponModeViewerUI != null)
        {
            _weaponModeViewerUI.OnOpenThisUIAction -= OpenUI;
        }

        if (_playerUpgradeUI != null)
        {
            _playerUpgradeUI.OnOpenThisUIAction -= OpenUI;
        }
    }

    private void Update()
    {
        if (!_isInitialized || _currentFocusedUI == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            InputEscapeKey();
        }
    }

    public void InitBasecampCanvas(PlayerCore player,
                                   InputModeManager inputModeManager, 
                                   WeaponModeViewerOpener weaponModeViewerOpener, 
                                   PlayerUpgradeOpener playerUpgradeOpener)
    {
        _weaponModeViewerOpener = weaponModeViewerOpener;
        _playerUpgradeOpener = playerUpgradeOpener;
        _inputModeManager = inputModeManager;

        _weaponModeViewerOpener.OnInteractWeaponModeViewerAction -= OpenWeaponModeViewerUIHandle;
        _weaponModeViewerOpener.OnInteractWeaponModeViewerAction += OpenWeaponModeViewerUIHandle;
        _playerUpgradeOpener.OnInteractPlayerUpgradeAction -= OpenPlayerUpgradeUIHandle;
        _playerUpgradeOpener.OnInteractPlayerUpgradeAction += OpenPlayerUpgradeUIHandle;

        if(_playerUpgradeUI == null)
        {
            Debug.LogError("플레이어 업그레이드 UI가 없음");
            return;
        }
        if(_weaponModeViewerUI == null)
        {
            Debug.LogError("무기 모드 뷰어 UI가 없음");
            return;
        }

        _weaponModeViewerUI.InitializeWeaponModeViewerUI(player);
        _playerUpgradeUI.InitializePlayerUpgradeUI(player);
        _weaponModeViewerUI.gameObject.SetActive(false);
        _playerUpgradeUI.gameObject.SetActive(false);

        _playerUpgradeUI.OnOpenThisUIAction -= OpenUI;
        _playerUpgradeUI.OnOpenThisUIAction += OpenUI;
        _playerUpgradeUI.OnCloseThisUIAction -= CloseUI;
        _playerUpgradeUI.OnCloseThisUIAction += CloseUI;
        _weaponModeViewerUI.OnOpenThisUIAction -= OpenUI;
        _weaponModeViewerUI.OnOpenThisUIAction += OpenUI;
        _weaponModeViewerUI.OnCloseThisUIAction -= CloseUI;
        _weaponModeViewerUI.OnCloseThisUIAction += CloseUI;

        _isInitialized = true;
    }

    private void InputEscapeKey()
    {
        CloseCurrentUI();
    }

    private void OpenWeaponModeViewerUIHandle()
    {
        _weaponModeViewerUI.gameObject.SetActive(true);
    }

    private void OpenPlayerUpgradeUIHandle()
    {
        _playerUpgradeUI.gameObject.SetActive(true);
    }

    private void CloseCurrentUI()
    {
        _currentFocusedUI.gameObject.SetActive(false);
    }

    private void OpenUI(BasecampUI ui)
    {
        _currentFocusedUI = ui;
        _inputModeManager.PushInputState(InputState.UI);
    }

    private void CloseUI(BasecampUI ui)
    {
        if (_currentFocusedUI == ui)
            _currentFocusedUI = null;

        _inputModeManager.PopInputState();
    }
}