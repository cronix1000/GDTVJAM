using Mono.Cecil;
using UnityEngine;

public class PlayerResourceController : MonoBehaviour
{
    public float maxHealth = 100f;
    private float _currentHealth;
    public float currentHealth
    {
        get { return _currentHealth; }
        private set
        {
            _currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            // Invoke the event when health changes
            onHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
    }

    public float maxResources = 100f; 
    private float _currentResources;
    public float currentResources
    {
        get { return _currentResources; }
        private set
        {
            _currentResources = Mathf.Clamp(value, 0f, maxResources);
            onResourcesChanged?.Invoke(_currentResources, maxResources);
        }
    }

    public float maxShield = 100f;
    private float _currentShield;
    public float currentShield
    {
        get { return _currentShield; }
        private set
        {
            _currentShield = Mathf.Clamp(value, 0f, maxShield);
            onShieldChanged?.Invoke(_currentShield, maxShield);
        }
    }

    public delegate void OnHealthChanged(float currentHealth, float maxHealth);
    public event OnHealthChanged onHealthChanged;

    public delegate void OnResourcesChanged(float currentResources, float maxResources);
    public event OnResourcesChanged onResourcesChanged;

    public delegate void OnShieldChanged(float currentShield, float maxShield);
    public event OnShieldChanged onShieldChanged;

    private void Start()
    {
        // Set initial values using the properties to trigger events for UI initialization
        currentHealth = maxHealth;
        currentResources = maxResources; // Assuming it starts full, adjust if 0 is correct
        currentShield = maxShield;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("EnemyProjectile"))
        {
            Projectile projectile = other.gameObject.GetComponent<Projectile>();
            float damageToTake = projectile != null ? projectile.damage : 1;

            // Apply damage to shield first, then health
            if (currentShield > 0)
            {
                if (currentShield >= damageToTake)
                {
                    currentShield -= damageToTake;
                    damageToTake = 0;
                }
                else
                {
                    damageToTake -= currentShield;
                    currentShield = 0;
                }
            }

            if (damageToTake > 0)
            {
                TakeDamage(damageToTake);
            }

            Destroy(other.gameObject);
        }
        else if (other.gameObject.CompareTag("Resource"))
        {
            
        }
    }

    private void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount; 
        if (currentHealth <= 0)
        {
            Debug.Log("Player is dead!");
            // Handle player death
        }
    }
    
    public void SpendResources(float amount)
    {
        currentResources -= amount;
    }

    public void GainResources(float amount)
    {
        currentResources += amount;
    }
    
    public void RechargeShield(float amount)
    {
        currentShield += amount;
    }
}