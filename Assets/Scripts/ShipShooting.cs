// ShipShooting.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // If managing multiple fire points or weapon instances

public enum ShipType
{
    Player,
    Enemy,
    Neutral
}
public class ShipShooting : MonoBehaviour
{
    [Header("Weapon Setup")]
    public WeaponData equippedWeapon; // Assign current weapon ScriptableObject
    public Transform firePoint;       // The transform where projectiles will spawn
    // public List<Transform> firePoints; // For multiple muzzles

    // public ShipEnergy shipEnergy; // Optional: Reference to ship's energy system

    private float _lastFireTime = -Mathf.Infinity; 
    private float _currentSpiralAngle = 0f;      
    [SerializeField] private ShipType shipType = ShipType.Enemy; 

    void Update()
    {
        // Example: Player Input (This would typically be in a PlayerController script)
        // if (Input.GetMouseButton(0) && gameObject.CompareTag("Player")) // Or your fire button
        // {
        //     Vector3 mousePos = Input.mousePosition;
        //     AttemptFire(aimAtScreenPoint: mousePos);
        // }

        // Example: Enemy AI (This would typically be in an EnemyAI script)
        // if (targetPlayer != null && gameObject.CompareTag("Enemy"))
        // {
        //    AttemptFire(aimAtTransform: targetPlayer.transform);
        // }
    }

    public void Initilize(WeaponData weaponData)
    {
        equippedWeapon = weaponData;
        _lastFireTime = -Mathf.Infinity; // Reset fire timer
        _currentSpiralAngle = 0f; // Reset spiral angle
    }
    
    /// <summary>
    /// Attempts to fire the equipped weapon.
    /// </summary>
    /// <param name="aimAtScreenPoint">Optional: Screen point to aim at (e.g., mouse position). Used by player.</param>
    /// <param name="aimAtTransform">Optional: Transform to aim at (e.g., player ship). Used by AI.</param>
    public void AttemptFire(Vector2? aimAtScreenPoint = null, Transform aimAtTransform = null)
    {
        if (equippedWeapon == null)
        {
            Debug.LogWarning("No weapon equipped!", this);
            return;
        }

        // Check fire rate
        if (Time.time < _lastFireTime + (1f / equippedWeapon.fireRate))
        {
            return;
        }
        
        _lastFireTime = Time.time;
        
        if (equippedWeapon.muzzleFlashPrefab && firePoint)
        {
            Instantiate(equippedWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint); // Parent to firePoint briefly
        }
        if (equippedWeapon.fireSound)
        {
            AudioSource.PlayClipAtPoint(equippedWeapon.fireSound, firePoint.position, equippedWeapon.fireSoundVolume);
        }

        Vector2 baseAimDirection = firePoint.up; // Default to ship's forward (for 2D top-down)

        if (aimAtScreenPoint.HasValue)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(aimAtScreenPoint.Value.x, aimAtScreenPoint.Value.y, Camera.main.nearClipPlane));
            worldPoint.z = firePoint.position.z; // Ensure Z is consistent for 2D
            baseAimDirection = (worldPoint - firePoint.position).normalized;
        }
        else if (aimAtTransform)
        {
            baseAimDirection = (aimAtTransform.position - firePoint.position).normalized;
        }

        // Execute firing based on weapon's mode
        ExecuteFiringMode(baseAimDirection);
    }

    private void ExecuteFiringMode(Vector2 baseAimDirection)
    {
        switch (equippedWeapon.firingMode)
        {
            case FiringMode.SingleTarget:
                SpawnSingleProjectile(baseAimDirection);
                break;

            case FiringMode.Spread:
                FireSpreadPattern(baseAimDirection);
                break;

            case FiringMode.ForwardVolley:
                StartCoroutine(FireVolleyPattern(baseAimDirection));
                break;

            case FiringMode.Spiral:
                FireSpiralPattern(baseAimDirection);
                break;

            default:
                Debug.LogWarning($"FiringMode {equippedWeapon.firingMode} not implemented.", this);
                SpawnSingleProjectile(baseAimDirection); // Fallback to single shot
                break;
        }
    }

    private void SpawnSingleProjectile(Vector2 direction)
    {
        if (equippedWeapon.projectilePrefab == null || firePoint == null) return;

        GameObject projGO = Instantiate(equippedWeapon.projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projectileScript = projGO.GetComponent<Projectile>();
        if (projectileScript)
        {
            projectileScript.Initialize(direction, equippedWeapon.projectileSpeed);
            projectileScript.tag = shipType == ShipType.Player ? "PlayerProjectile" : "EnemyProjectile"; // Set tag based on ship type
            projectileScript.damage = equippedWeapon.projectileDamage; // Set damage
        }
    }

    private void FireSpreadPattern(Vector2 centerDirection)
    {
        int numProjectiles = equippedWeapon.projectilesInSpread;
        float totalSpreadAngle = equippedWeapon.spreadAngle;
        float angleStep = (numProjectiles > 1) ? totalSpreadAngle / (numProjectiles - 1) : 0f;
        float startAngleOffset = (numProjectiles > 1) ? -totalSpreadAngle / 2f : 0f; // Start from the leftmost part of the spread

        for (int i = 0; i < numProjectiles; i++)
        {
            float currentAngle = startAngleOffset + (i * angleStep);
            // Rotate the centerDirection by currentAngle
            // Vector2.SignedAngle can get angle of centerDirection, add currentAngle, then convert back
            float centerAngleRad = Mathf.Atan2(centerDirection.y, centerDirection.x);
            float finalAngleRad = centerAngleRad + (currentAngle * Mathf.Deg2Rad);

            Vector2 spreadDirection = new Vector2(Mathf.Cos(finalAngleRad), Mathf.Sin(finalAngleRad));
            SpawnSingleProjectile(spreadDirection);
        }
    }

    private IEnumerator FireVolleyPattern(Vector2 direction)
    {
        for (int i = 0; i < equippedWeapon.volleyCount; i++)
        {
            SpawnSingleProjectile(direction);
            if (i < equippedWeapon.volleyCount - 1) // Don't wait after the last shot
            {
                yield return new WaitForSeconds(equippedWeapon.volleyDelay);
            }
        }
    }

    private void FireSpiralPattern(Vector2 baseAimDirection)
    {
        Vector2 spiralDirection;
        if (equippedWeapon.spiralOrientsToTarget) // Spiral's 'forward' aims at target, then spirals around that forward
        {
             float baseAngleRad = Mathf.Atan2(baseAimDirection.y, baseAimDirection.x);
             float finalAngleRad = baseAngleRad + (_currentSpiralAngle * Mathf.Deg2Rad);
             spiralDirection = new Vector2(Mathf.Cos(finalAngleRad), Mathf.Sin(finalAngleRad));
        }
        else // Spiral fires relative to ship's fixed orientation (e.g. firePoint.up)
        {
            Quaternion rotation = Quaternion.AngleAxis(_currentSpiralAngle, Vector3.forward);
            spiralDirection = rotation * firePoint.up; // Or Vector2.up if you want a fixed world-space spiral start
        }

        SpawnSingleProjectile(spiralDirection);
        _currentSpiralAngle += equippedWeapon.spiralAngleStep;
        if (_currentSpiralAngle >= 360f)
        {
            _currentSpiralAngle -= 360f; // Keep angle within 0-360
        }
    }

    // Public method to allow changing weapons at runtime
    public void EquipWeapon(WeaponData newWeapon)
    {
        equippedWeapon = newWeapon;
        _lastFireTime = -Mathf.Infinity; // Reset fire timer
        _currentSpiralAngle = 0f; // Reset pattern state
        Debug.Log($"Weapon equipped: {newWeapon.weaponName}", this);
    }
}