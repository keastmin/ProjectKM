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
        _weaponList = new List<WeaponSlot>();
        _weaponIndex = 0;
        _currentWeaponSlot = WeaponSlot.Empty;
        _isEquipped = true;

        if(_testWeaponArray != null)
        {
            foreach(var data in _testWeaponArray)
            {
                AcquisitionWeaponData(data);
            }
        }
    }

    public void AcquisitionWeaponData(WeaponData data)
    {
        if(_weaponList == null)
        {
            _weaponList = new List<WeaponSlot>();
        }

        WeaponInstance instance = new WeaponInstance(data.WeaponName, data.OriginDamage);
        WeaponActor actor = Instantiate(data.Actor);
        actor.gameObject.SetActive(false);
        WeaponSlot newSlot = new WeaponSlot(instance, actor);
        _weaponList.Add(newSlot);

        if (IsEquipped && ((WeaponList.Count - 1) == WeaponIndex))
        {
            EquipWeapon();
        }
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
        if (index < 0 && index >= WeaponList.Count)
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
}
