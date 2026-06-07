using UnityEngine;

public struct WeaponSlot
{
    public static WeaponSlot Empty => new WeaponSlot(null, null, null);
    public WeaponData Data;
    public WeaponInstance Instance;
    public WeaponActor Actor;

    public WeaponSlot(WeaponInstance instance, WeaponActor actor, WeaponData data = null)
    {
        Data = data;
        Instance = instance;
        Actor = actor;
    }
}
