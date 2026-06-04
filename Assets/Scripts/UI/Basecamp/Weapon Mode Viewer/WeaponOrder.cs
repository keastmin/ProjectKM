using Player;
using System.Collections.Generic;
using UnityEngine;

public class WeaponOrder : MonoBehaviour
{
    [SerializeField] private WeaponOrderSlot _orderSlot;
    [SerializeField] private float _slotSpace = 20f;

    private List<WeaponOrderSlot> _slotList;

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

        WeaponSlot[] slots = weaponController.WeaponList.ToArray();

        if(slots == null)
        {
            Debug.LogError("무기 슬롯이 없습니다");
            return;
        }

        if(_slotList == null)
            _slotList = new List<WeaponOrderSlot>();

        float localYPos = 0f;
        for(int i = 0; i < slots.Length; i++)
        {
            WeaponOrderSlot orderSlot = Instantiate(_orderSlot, transform);
            _slotList.Add(orderSlot);
            orderSlot.InitializeSlot(slots[i], localYPos);
            localYPos -= (orderSlot.Height + _slotSpace);
        }
    }

    public void ClearWeaponOrder()
    {
        if (_slotList == null)
            return;

        WeaponOrderSlot[] orderSlots = _slotList.ToArray();
        foreach(var slot in orderSlots)
        {
            Destroy(slot.gameObject);
        }
        _slotList = null;
    }
}
