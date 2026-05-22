using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField] private List<WeaponData> _testWeaponList;
    [SerializeField] private Transform _rightHandTransform;
    [SerializeField] private Transform _leftHandTransform;

    private List<WeaponInstance> _weaponList;
    private int _weaponIndex;

    public List<WeaponInstance> WeaponList => _weaponList;
    public bool IsExistWeapon
    {
        get
        {
            return (WeaponList != null && WeaponList.Count < _weaponIndex && WeaponList[_weaponIndex] != null);
        }
    }


    private void OnValidate()
    {
        
    }

    private void Awake()
    {
        _weaponIndex = 0;   
    }

    public void TryEquipWeapon()
    {
        if (!CheckWeaponList())
            return;


    }

    /// <summary>
    /// 다음 무기로 교체
    /// </summary>
    public void TryChangeNextWeapon()
    {
        if (!CheckWeaponList())
            return;

        int maxIndex = _weaponList.Count;
        int currentIndex = _weaponIndex;
        _weaponIndex = (currentIndex + 1) % maxIndex;
    }

    private bool CheckWeaponList()
    {
        if(_weaponList == null)
        {
            Debug.LogError("무기 리스트가 없습니다.");
            return false;
        }
        return true;
    }
}
