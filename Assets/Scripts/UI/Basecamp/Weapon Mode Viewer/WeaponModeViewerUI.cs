using Player;
using System;
using UnityEngine;

public class WeaponModeViewerUI : BasecampUI
{
    [SerializeField] private WeaponOrder _weaponOrder;

    private PlayerCore _player;

    private bool _isInitialized = false;

    public void InitializeWeaponModeViewerUI(PlayerCore player)
    {
        _player = player;

        _isInitialized = true;
        
        if(_weaponOrder == null)
        {
            Debug.LogError("무기 정렬이 없음");
            return;
        }
        _weaponOrder.InitializeWeaponOrder(player);
    }

    private void ClearWeaponModeViewerUI()
    {
        _weaponOrder.ClearWeaponOrder();
    }
}
