using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponHandPosition _handPositioning;

    public WeaponHandPosition HandPositioning => _handPositioning;
}