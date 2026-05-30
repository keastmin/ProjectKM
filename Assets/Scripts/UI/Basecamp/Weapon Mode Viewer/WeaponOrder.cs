using Player;
using System.Collections.Generic;
using UnityEngine;

public class WeaponOrder : MonoBehaviour
{
    [SerializeField] private WeaponOrderSlot _orderSlot;

    public void InitializeWeaponOrder(PlayerCore player)
    {
        if(player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        PlayerWeaponController weaponController = player.WeaponController;

        if(weaponController == null)
        {
            Debug.LogError("무기 컨트롤러가 없습니다");
            return;
        }

        List<WeaponSlot> slots = weaponController.WeaponList;

        if(slots == null)
        {
            Debug.LogError("무기 슬롯이 없습니다");
            return;
        }

        foreach(var slot in slots)
        {
            WeaponOrderSlot orderSlot = Instantiate(_orderSlot, transform);
            orderSlot.InitializeSlot();
        }
    }

    public void ClearWeaponOrder()
    {
        WeaponOrderSlot[] orderSlots = this.GetComponentsInChildren<WeaponOrderSlot>();
        foreach(var slot in orderSlots)
        {
            Destroy(slot.gameObject);
        }
    }
}
