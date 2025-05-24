using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    public Image healthResourceImage; 
    public Image shieldResourceImage;
    public Image energyResourceImage;

    [Tooltip("Assign the PlayerResourceController instance here.")]
    public PlayerResourceController playerResourceController;

    private void Start()
    {
        if (playerResourceController == null)
        {
            Debug.LogError("PlayerResourceController not assigned to CharacterUI!");
            return;
        }

        // Subscribe to the events
        playerResourceController.onHealthChanged += HandleHealthChanged;
        playerResourceController.onResourcesChanged += HandleResourcesChanged;
        playerResourceController.onShieldChanged += HandleShieldChanged;


        if (playerResourceController.maxHealth > 0)
             HandleHealthChanged(playerResourceController.currentHealth, playerResourceController.maxHealth);
        else
             UpdateHealthFill(0);

        if (playerResourceController.maxResources > 0)
            HandleResourcesChanged(playerResourceController.currentResources, playerResourceController.maxResources);
        else
            UpdateResourceFill(0);

        if (playerResourceController.maxShield > 0)
            HandleShieldChanged(playerResourceController.currentShield, playerResourceController.maxShield);
        else
            UpdateShieldFill(0);
    }

    private void OnDestroy()
    {
        if (playerResourceController != null)
        {
            playerResourceController.onHealthChanged -= HandleHealthChanged;
            playerResourceController.onResourcesChanged -= HandleResourcesChanged;
            playerResourceController.onShieldChanged -= HandleShieldChanged;
        }
    }
    
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (maxHealth > 0)
        {
            UpdateHealthFill(currentHealth / maxHealth);
        }
        else
        {
            UpdateHealthFill(0); 
        }
    }

    private void HandleResourcesChanged(float currentResources, float maxResources)
    {
        if (maxResources > 0)
        {
            UpdateResourceFill(currentResources / maxResources);
        }
        else
        {
            UpdateResourceFill(0); 
        }
    }

    private void HandleShieldChanged(float currentShield, float maxShield)
    {
        if (maxShield > 0)
        {
            UpdateShieldFill(currentShield / maxShield);
        }
        else
        {
            UpdateShieldFill(0);
        }
    }

    public void UpdateHealthFill(float fillAmount)
    {
        if (healthResourceImage != null)
            healthResourceImage.fillAmount = fillAmount;
    }

    public void UpdateShieldFill(float fillAmount)
    {
        if (shieldResourceImage != null)
            shieldResourceImage.fillAmount = fillAmount;
    }

    public void UpdateResourceFill(float fillAmount) 
    {
        if (energyResourceImage != null)
            energyResourceImage.fillAmount = fillAmount;
    }
}