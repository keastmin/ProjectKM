using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Object/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string WeaponName;
    public WeaponActor Actor;
    public float OriginDamage;
}