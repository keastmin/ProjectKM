using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField] private WeaponData[] _testWeaponArray;
    [SerializeField] private Transform _rightHandTransform;
    [SerializeField] private Transform _leftHandTransform;

    private List<WeaponSlot> _weaponList;
    private int _weaponIndex;
    private WeaponSlot _currentWeaponSlot;
    private bool _isEquipped;

    public List<WeaponSlot> WeaponList => _weaponList;
    public int WeaponIndex => _weaponIndex;
    public WeaponSlot CurrentWeaponSlot => _currentWeaponSlot;
    public bool IsEquipped => _isEquipped;

    private void Awake()
    {
        InitializeWeaponDatas(_testWeaponArray);
    }

    public void InitializeWeaponDatas(IEnumerable<WeaponData> weaponDatas)
    {
        ClearWeaponActors();
        _weaponList = new List<WeaponSlot>();
        _weaponIndex = 0;
        _currentWeaponSlot = WeaponSlot.Empty;
        _isEquipped = true;

        if(weaponDatas != null)
        {
            foreach(var data in weaponDatas)
            {
                AcquisitionWeaponData(data);
            }
        }
    }

    public void AcquisitionWeaponData(WeaponData data)
    {
        WeaponSlot newSlot;
        if (!TryCreateWeaponSlot(data, null, out newSlot))
        {
            return;
        }

        if(_weaponList == null)
        {
            _weaponList = new List<WeaponSlot>();
        }

        _weaponList.Add(newSlot);

        if (IsEquipped && ((WeaponList.Count - 1) == WeaponIndex))
        {
            EquipWeapon();
        }
    }

    public void InitializeWeaponSlots(IEnumerable<WeaponSlot> weaponSlots, int weaponIndex)
    {
        ClearWeaponActors();
        _weaponList = new List<WeaponSlot>();
        _weaponIndex = 0;
        _currentWeaponSlot = WeaponSlot.Empty;
        _isEquipped = true;

        if (weaponSlots != null)
        {
            foreach (var savedSlot in weaponSlots)
            {
                WeaponSlot restoredSlot;
                if (TryCreateWeaponSlot(savedSlot.Data, savedSlot.Instance, out restoredSlot))
                {
                    _weaponList.Add(restoredSlot);
                }
            }
        }

        if (_weaponList.Count <= 0)
        {
            _isEquipped = false;
            return;
        }

        _weaponIndex = Mathf.Clamp(weaponIndex, 0, _weaponList.Count - 1);
        EquipWeapon();
    }

    public void ChangeNextWeapon()
    {
        UnequipWeapon();
        ChangeNextIndex();
        EquipWeapon();
    }

    public void UnequipWeapon()
    {
        if (CurrentWeaponSlot.Actor == null)
            return;

        WeaponActor actor = CurrentWeaponSlot.Actor;
        actor.gameObject.SetActive(false);
        _currentWeaponSlot = WeaponSlot.Empty;
        _isEquipped = false;
    }

    public void EquipWeapon()
    {
        WeaponSlot slot;
        if (TryGetWeaponSlot(WeaponIndex, out slot))
        {
            _currentWeaponSlot = slot;
            EquipWeaponOnHand();
            _isEquipped = true;
        }
    }

    /// <summary>
    /// 다음 인덱스로 이동
    /// </summary>
    public void ChangeNextIndex()
    {
        if (!CheckWeaponList(WeaponIndex))
            return;

        int maxIndex = WeaponList.Count;
        int currentIndex = WeaponIndex;
        _weaponIndex = (currentIndex + 1) % maxIndex;
    }

    public bool TryGetWeaponSlot(int index, out WeaponSlot slot)
    {
        slot = WeaponSlot.Empty;

        if (!CheckWeaponList(index))
        {
            return false;
        }
        if (WeaponList[index].Instance == null)
        {
            Debug.LogError("무기 인스턴스가 없습니다");
            return false;
        }

        slot = WeaponList[index];
        return true;
    }

    public bool CheckWeaponList(int index)
    {
        if (WeaponList == null)
        {
            Debug.LogError("무기 리스트가 없습니다");
            return false;
        }
        if (index < 0 || index >= WeaponList.Count)
        {
            Debug.LogError("인덱스가 리스트 카운트를 벗어납니다");
            return false;
        }
        return true;
    }

    public void EquipWeaponOnHand()
    {
        if(CurrentWeaponSlot.Actor == null)
        {
            Debug.LogError("무기가 없습니다");
            return;
        }

        WeaponActor actor = CurrentWeaponSlot.Actor;
        WeaponHandType type = actor.HandType;

        actor.gameObject.SetActive(true);
        if(type == WeaponHandType.Left)
            actor.transform.parent = _leftHandTransform;
        else if(type == WeaponHandType.Right)
            actor.transform.parent = _rightHandTransform;
        actor.transform.localPosition = Vector3.zero;
        actor.transform.localRotation = Quaternion.identity;
        actor.transform.localScale = Vector3.one;
    }

    public void ChangeWeaponSlotOrder(List<WeaponSlot> slotList)
    {
        if (slotList == null)
            return;
        _weaponList = slotList;
        if (_weaponIndex >= _weaponList.Count)
        {
            _weaponIndex = 0;
        }
    }

    public List<WeaponSlot> GetWeaponSlotOrder()
    {
        List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

        if (_weaponList == null)
        {
            return weaponSlots;
        }

        foreach (var slot in _weaponList)
        {
            if (slot.Instance != null && slot.Data != null)
            {
                weaponSlots.Add(new WeaponSlot(slot.Instance, null, slot.Data));
            }
        }

        return weaponSlots;
    }

    private void ClearWeaponActors()
    {
        if (_weaponList == null)
        {
            return;
        }

        foreach (var slot in _weaponList)
        {
            if (slot.Actor != null)
            {
                Destroy(slot.Actor.gameObject);
            }
        }
    }

    private bool TryCreateWeaponSlot(WeaponData data, WeaponInstance instance, out WeaponSlot slot)
    {
        slot = WeaponSlot.Empty;

        if (data == null)
        {
            Debug.LogError("WeaponData is null.");
            return false;
        }

        if (data.Actor == null)
        {
            Debug.LogError("WeaponData Actor is null.");
            return false;
        }

        WeaponInstance weaponInstance = instance ?? new WeaponInstance(data.WeaponName, data.OriginDamage);
        WeaponActor actor = Instantiate(data.Actor);
        actor.gameObject.SetActive(false);
        slot = new WeaponSlot(weaponInstance, actor, data);
        return true;
    }
}
