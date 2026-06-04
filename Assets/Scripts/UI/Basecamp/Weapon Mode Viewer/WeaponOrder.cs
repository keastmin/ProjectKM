using Player;
using System.Collections.Generic;
using UnityEngine;

public class WeaponOrder : MonoBehaviour
{
    [SerializeField] private WeaponOrderSlot _orderSlotPrefab;
    [SerializeField] private float _slotSpace = 20f;

    private List<(WeaponSlot, WeaponOrderSlot)> _slotList = new();

    private PlayerCore _player;

    public void InitializeWeaponOrder(PlayerCore player)
    {
        if(player == null)
        {
            Debug.LogError("플레이어가 없습니다");
            return;
        }

        _player = player;
        PlayerWeaponController weaponController = _player.WeaponController;

        if(weaponController == null)
        {
            Debug.LogError("무기 컨트롤러가 없습니다");
            return;
        }

        List<WeaponSlot> weaponList = _player.WeaponController.WeaponList;

        if(weaponList == null)
        {
            Debug.LogError("무기 슬롯이 없습니다");
            return;
        }

        if (_slotList == null)
            _slotList = new List<(WeaponSlot, WeaponOrderSlot)>();

        float localYPos = 0f;
        foreach(var slot in weaponList)
        {
            WeaponOrderSlot orderSlot = Instantiate(_orderSlotPrefab, transform);
            orderSlot.InitializeSlot(slot, localYPos);
            orderSlot.OnDetectOtherSlot += ChangeSlot;
            localYPos -= (orderSlot.Height + _slotSpace);
            _slotList.Add((slot, orderSlot));
        }
    }

    public void ClearWeaponOrder()
    {
        if(_slotList != null)
        {
            foreach(var orderSlot in _slotList)
            {
                Destroy(orderSlot.Item2);
            }
            _slotList.Clear();
            _slotList = null;
        }
    }

    private void ChangeSlot(WeaponOrderSlot grabbingSlot, WeaponOrderSlot changedSlot)
    {
        if (_slotList == null)
            return;

        if (grabbingSlot == null || changedSlot == null)
            return;

        if (grabbingSlot == changedSlot)
            return;

        int grabbingIndex = _slotList.FindIndex(pair => pair.Item2 == grabbingSlot);
        int changedIndex = _slotList.FindIndex(pair => pair.Item2 == changedSlot);

        if (grabbingIndex < 0 || changedIndex < 0)
            return;

        (_slotList[grabbingIndex], _slotList[changedIndex]) =
            (_slotList[changedIndex], _slotList[grabbingIndex]);

        Vector3 originPos = grabbingSlot.OriginPos;
        grabbingSlot.SetOriginPosition(changedSlot.OriginPos);
        changedSlot.SetOriginPosition(originPos);

        List<WeaponSlot> newWeaponSlot = new List<WeaponSlot>();
        foreach(var slot in _slotList)
        {
            newWeaponSlot.Add(slot.Item1);
        }
        _player.ChangeWeaponSlotOrder(newWeaponSlot);
    }
}
