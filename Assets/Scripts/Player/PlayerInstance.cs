using System.Collections.Generic;
using UnityEngine;

public class PlayerInstance
{
    public float MaxHealth;
    public float CurrentHealth;
    public float Strength;
    public float Defence;
    public float Finesse;
    public float DodgeCooldown;
    public float DodgeCount;
    public float JogSpeed;
    public float RunSpeed;
    public List<WeaponSlot> WeaponSlots;
    public int WeaponIndex;

    public PlayerInstance(PlayerStatData data)
    {
        MaxHealth = data.Health;
        CurrentHealth = MaxHealth;
        Strength = data.Strength;
        Defence = data.Defence;
        Finesse = data.Finesse;
        DodgeCooldown = data.DodgeCooldown;
        DodgeCount = data.DodgeCount;
        JogSpeed = data.JogSpeed;
        RunSpeed = data.RunSpeed;
        WeaponSlots = new List<WeaponSlot>();
        WeaponIndex = 0;
    }

    public void SetWeaponSlots(IEnumerable<WeaponSlot> weaponSlots, int weaponIndex)
    {
        WeaponSlots.Clear();
        WeaponIndex = Mathf.Max(0, weaponIndex);

        if (weaponSlots == null)
        {
            return;
        }

        foreach (var weaponSlot in weaponSlots)
        {
            if (weaponSlot.Instance != null && weaponSlot.Data != null)
            {
                WeaponSlots.Add(new WeaponSlot(weaponSlot.Instance, null, weaponSlot.Data));
            }
        }
    }
}
