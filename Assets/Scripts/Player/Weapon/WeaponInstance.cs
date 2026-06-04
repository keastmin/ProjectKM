using UnityEngine;

public class WeaponInstance
{
    public string WeaponName;
    public float OriginDamage;

    public WeaponInstance(string name, float originDamage)
    {
        this.WeaponName = name;
        this.OriginDamage = originDamage;
    }
}
