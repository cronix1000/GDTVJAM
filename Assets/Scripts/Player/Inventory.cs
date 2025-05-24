using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for ElementAt
using System;

public class Inventory : MonoBehaviour
{
    [Tooltip("Maximum number of weapons the player can carry.")]
    public int capacity = 3;

    // Using LinkedList for efficient AddLast, RemoveFirst, and Remove operations
    private LinkedList<WeaponData> weapons = new LinkedList<WeaponData>();
    public WeaponData EquippedWeapon { get; private set; }

    // Event triggered when the inventory content changes (item added/removed, order changed)
    public event Action OnInventoryChanged;
    // Event triggered when a new weapon is selected/equipped
    public event Action<WeaponData> OnWeaponSelected;

    /// <summary>
    /// Adds a weapon to the inventory.
    /// If inventory is full, the oldest weapon (least recently selected/added) is removed.
    /// If the weapon is already present, it's moved to the "newest" slot (marked as recently used).
    /// </summary>
    public void AddWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null)
        {
            Debug.LogWarning("Tried to add a null weapon.");
            return;
        }

        bool weaponExisted = weapons.Contains(newWeapon);

        if (weaponExisted)
        {
            // Weapon is already in inventory, refresh its position (move to end)
            weapons.Remove(newWeapon);
            weapons.AddLast(newWeapon);
        }
        else
        {
            // New weapon, add it. Check for capacity.
            if (weapons.Count >= capacity)
            {
                WeaponData removedWeapon = weapons.First.Value; // Oldest weapon
                weapons.RemoveFirst();
                Debug.Log($"Inventory full. Removed: {removedWeapon.weaponName}");

                if (EquippedWeapon == removedWeapon)
                {
                    EquippedWeapon = null; // Equipped weapon was removed
                }
            }
            weapons.AddLast(newWeapon);
            Debug.Log($"Added new weapon: {newWeapon.weaponName}");
        }

        // Auto-equip logic:
        // 1. If nothing was equipped, equip the newly added/refreshed weapon.
        // 2. If the equipped weapon was removed, equip the most recently added weapon.
        if (EquippedWeapon == null && weapons.Count > 0)
        {
            SelectWeapon(weapons.Last.Value); // Select the newest one
        }
        // If it existed and was picked up again, and it's the currently equipped one,
        // its position update is good. If you want to re-trigger selection animation/logic,
        // you could call SelectWeapon here too. For now, just re-ordering is enough.

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Selects a weapon to be the currently equipped weapon.
    /// Moves the selected weapon to the end of the list to mark it as recently used.
    /// </summary>
    public void SelectWeapon(WeaponData weaponToSelect)
    {
        if (weaponToSelect == null || !weapons.Contains(weaponToSelect))
        {
            // Debug.LogWarning($"Cannot select weapon: {weaponToSelect?.weaponName ?? "NULL"}. Not found in inventory.");
            if (weapons.Count > 0 && EquippedWeapon == null) {
                 // If nothing is selected and we have weapons, select the last one by default.
                weaponToSelect = weapons.Last.Value;
            } else if (EquippedWeapon != null && weapons.Contains(EquippedWeapon)) {
                // If current weapon is still valid, keep it.
                weaponToSelect = EquippedWeapon;
            }
            else {
                EquippedWeapon = null; // Truly nothing to select or current is invalid
                OnWeaponSelected?.Invoke(null);
                return;
            }
        }

        EquippedWeapon = weaponToSelect;

        // Move to end of list to mark as "recently used", protecting it from FIFO removal
        weapons.Remove(weaponToSelect);
        weapons.AddLast(weaponToSelect);

        Debug.Log($"Weapon selected: {EquippedWeapon.weaponName}");
        OnWeaponSelected?.Invoke(EquippedWeapon);
        OnInventoryChanged?.Invoke(); // Order changed, UI needs full refresh
    }

    /// <summary>
    /// Selects a weapon by its current display index in the inventory.
    /// </summary>
    public void SelectWeaponByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count)
        {
            Debug.LogWarning($"Invalid weapon index for selection: {index}. Inventory count: {weapons.Count}");
            return;
        }
        WeaponData weaponToSelect = weapons.ElementAt(index); // LINQ's ElementAt for LinkedList
        SelectWeapon(weaponToSelect);
    }

    /// <summary>
    /// Returns a list of weapons currently in the inventory, in their current order.
    /// </summary>
    public List<WeaponData> GetWeapons()
    {
        return new List<WeaponData>(weapons);
    }

    private void Start()
    {
        // Ensure initial state for equipped weapon if inventory has items (e.g., from save data later)
        if (EquippedWeapon == null && weapons.Count > 0)
        {
            SelectWeapon(weapons.Last.Value);
        }
        OnInventoryChanged?.Invoke(); // Initial UI update
    }
}