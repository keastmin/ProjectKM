using Player;
using System;
using UnityEngine;

public class WeaponModeViewerUI : BasecampUI
{
    [SerializeField] private WeaponOrder _weaponOrder;

    private void OnEnable()
    {
        InitializeWeaponModeViewerUI();
    }

    private void OnDisable()
    {
        ClearWeaponModeViewerUI();
    }

    private void InitializeWeaponModeViewerUI()
    {
        if(_player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        _weaponOrder.InitializeWeaponOrder(_player);
    }

    private void ClearWeaponModeViewerUI()
    {
        _weaponOrder.ClearWeaponOrder();
    }
}
