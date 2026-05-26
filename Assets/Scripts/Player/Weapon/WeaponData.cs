using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Object/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public WeaponActor WeaponPrefab;
    public float WeaponDamage;
}