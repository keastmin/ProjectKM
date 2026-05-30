using Player;
using System;
using UnityEngine;

public class WeaponModeViewerUI : MonoBehaviour
{
    [SerializeField] private WeaponOrder _weaponOrder;

    private PlayerCore _player;

    private void OnEnable()
    {
        InitializeWeaponOrder();
    }

    public void ExitWeaponModeViewerUI()
    {
        if (gameObject.activeSelf)
        {
            if (GameManager.Instance != null)
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

    private void InitializeWeaponOrder()
    {
        if(_player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        _weaponOrder.InitializeSlot(_player);
    }
}
