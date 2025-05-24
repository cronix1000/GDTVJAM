using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponData weaponData;
    public bool destroyOnPickup = true;

    // Using 2D for this example, change to Collider for 3D
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // Ensure your player GameObject has the "Player" tag
        {
            Inventory playerInventory = other.GetComponent<Inventory>();
            if (playerInventory != null)
            {
                if (weaponData != null)
                {
                    playerInventory.AddWeapon(weaponData);
                    if (destroyOnPickup)
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Debug.LogWarning("WeaponData not set on this pickup!", gameObject);
                }
            }
        }
    }
}