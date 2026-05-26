using UnityEngine;

public struct WeaponSlot
{
    public static WeaponSlot Empty => new WeaponSlot(null, null);
    public WeaponInstance Instance;
    public WeaponActor Actor;

    public WeaponSlot(WeaponInstance instance, WeaponActor actor)
    {
        Instance = instance;
        Actor = actor;
    }
}
