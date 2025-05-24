using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Tooltip("Reference to the player's inventory script.")]
    public Inventory playerInventory;

    [Header("UI Elements")]
    [Tooltip("Assign UI Image components for each weapon slot here. Order matters.")]
    public List<Image> weaponSlotImages;
    [Tooltip("Assign UI Button components for each weapon slot here. Order must match Images.")]
    public List<Button> weaponSlotButtons;

    [Header("Selection Visuals")]
    public Color selectedSlotColor = Color.yellow;
    public Color defaultSlotColor = Color.white;
    public Sprite emptySlotSprite; // Optional: Sprite for empty slots

    void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory not assigned to InventoryUI!");
            enabled = false;
            return;
        }

        if (weaponSlotImages.Count != playerInventory.capacity || weaponSlotButtons.Count != playerInventory.capacity)
        {
            Debug.LogWarning("InventoryUI slot counts do not match inventory capacity. Ensure lists are sized correctly in Inspector.");
        }

        // Subscribe to inventory events
        playerInventory.OnInventoryChanged += UpdateDisplay;
        playerInventory.OnWeaponSelected += HighlightSelectedWeapon;

        // Setup button listeners
        for (int i = 0; i < weaponSlotButtons.Count; i++)
        {
            int currentIndex = i; 
            if (i < playerInventory.capacity) 
            {
                 weaponSlotButtons[i].onClick.AddListener(() => HandleSlotClick(currentIndex));
            }
            else
            {
                weaponSlotButtons[i].gameObject.SetActive(false); // Disable excess UI slots
                weaponSlotImages[i].gameObject.SetActive(false);
            }
        }

        UpdateDisplay(); 
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= UpdateDisplay;
            playerInventory.OnWeaponSelected -= HighlightSelectedWeapon;
        }
    }

    void HandleSlotClick(int slotIndex)
    {
        List<WeaponData> currentWeapons = playerInventory.GetWeapons();
        if (slotIndex < currentWeapons.Count)
        {
            playerInventory.SelectWeaponByIndex(slotIndex);
        }
        else
        {
            Debug.Log($"Clicked empty slot {slotIndex}, no action taken.");
        }
    }

    void UpdateDisplay()
    {
        if (playerInventory == null) return;

        List<WeaponData> currentWeapons = playerInventory.GetWeapons();

        for (int i = 0; i < playerInventory.capacity; i++) 
        {
            if (i < weaponSlotImages.Count && weaponSlotImages[i] != null) 
            {
                if (i < currentWeapons.Count)
                {
                    // Slot has a weapon
                    weaponSlotImages[i].sprite = currentWeapons[i].icon;
                    weaponSlotImages[i].color = Color.white;
                    weaponSlotImages[i].enabled = true;
                    if (weaponSlotButtons.Count > i && weaponSlotButtons[i] != null)
                        weaponSlotButtons[i].interactable = true;
                }
                else
                {
                    // Slot is empty
                    weaponSlotImages[i].sprite = emptySlotSprite; 
                    weaponSlotImages[i].color = Color.clear; 
                    weaponSlotImages[i].enabled = (emptySlotSprite != null); 
                     if (weaponSlotButtons.Count > i && weaponSlotButtons[i] != null)
                        weaponSlotButtons[i].interactable = false;
                }
            }
        }
        HighlightSelectedWeapon(playerInventory.EquippedWeapon); // Ensure selection highlight is correct
    }

    void HighlightSelectedWeapon(WeaponData selectedWeapon)
    {
        if (playerInventory == null) return;
        List<WeaponData> currentWeapons = playerInventory.GetWeapons();

        for (int i = 0; i < playerInventory.capacity; i++)
        {
             if (i < weaponSlotImages.Count && weaponSlotImages[i] != null) // Check UI element exists
             {
                if (i < currentWeapons.Count && currentWeapons[i] == selectedWeapon && selectedWeapon != null)
                {
                    // This is the selected weapon's slot
                    weaponSlotImages[i].transform.parent.GetComponent<Image>().color = selectedSlotColor; // Example: highlight parent
                    // Or: weaponSlotImages[i].color = selectedSlotColor; if image itself should change dramatically
                }
                else
                {
                    // Not selected or empty
                     weaponSlotImages[i].transform.parent.GetComponent<Image>().color = defaultSlotColor;
                    // Or: weaponSlotImages[i].color = defaultSlotColor;
                }
             }
        }
    }
}