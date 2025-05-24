using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For health bar, if you keep it

public class BasicEnemyAI : BaseAI // Inherit from BaseAI
{
    [Header("Enemy Specifics")]
    [Tooltip("Radius to detect the player.")]
    public float detectionRadius = 10f;
    [Tooltip("Radius within which the enemy stops chasing and attempts to attack.")]
    public float attackRadius = 8f; // This is the range where AI decides to start shooting
    [Tooltip("Damage dealt to player on contact (if it also has contact damage).")]
    public int contactDamageToPlayer = 10; // If you want separate contact damage
    [Tooltip("XP awarded when defeated.")]
    public int xpValue = 50;
    [Tooltip("Layer mask for detecting the player (used by ScanForPlayer if OverlapSphere is preferred).")]
    public LayerMask playerLayer; // Currently not used by sqrMagnitude check, but good to have

    [Header("Health & Optional UI")]
    [Tooltip("Optional health bar fill image.")]
    public Image healthBarFill;
    public int totalHealth = 20;
    [SerializeField] private int currentHealth;

    [Header("Shooting Logic")]
    [Tooltip("Reference to the ShipShooting component on this enemy.")]
    public ShipShooting shipShooting;
    [Tooltip("The weapon this enemy will use.")]
    [SerializeField] WeaponData equippedWeapon; 
    [Tooltip("AI's own cooldown between attempts to fire or attack sequences.")]
    public float aiFireDecisionCooldown = 1f;
    private float _nextAIFireTime; 
    
    [Header("Resource Ejection Settings")]
    public GameObject resourceCubePrefab; // Assign your ResourceCube prefab here in the Inspector
    public int minCubesToEject = 2;
    public int maxCubesToEject = 5;
    public float minEjectionForce = 5f;
    public float maxEjectionForce = 10f;
    public float minSpinForce = 50f;  // Torque for spin
    public float maxSpinForce = 150f;
    public List<Color> resourceColors = new List<Color>(); // Add colors in the Inspector

    private Transform _playerTransform;

    private enum AiState { Wandering, ChasingPlayer, Attacking }
    [SerializeField] private AiState currentState = AiState.Wandering;

    protected override void Awake()
    {
        base.Awake();
        currentHealth = totalHealth;
        
        if (shipShooting == null)
        {
            shipShooting = GetComponent<ShipShooting>();
            if (shipShooting == null)
            {
                Debug.LogError($"BasicEnemyAI ({gameObject.name}): ShipShooting component not found or assigned!", this);
                enabled = false;
                return;
            }
        }
    }

    protected override void Start()
    {
        base.Start();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"BasicEnemyAI ({gameObject.name}): Player not found by tag. AI may not function correctly.", this);
        }

        if (equippedWeapon != null && shipShooting != null)
        {
            shipShooting.EquipWeapon(equippedWeapon);
        }
        else if (shipShooting != null && shipShooting.equippedWeapon == null) 
        {
            Debug.LogWarning($"BasicEnemyAI ({gameObject.name}): No specific weapon assigned to AI, ensure ShipShooting component has a default weapon or assign one here.", this);
        }
        if (resourceColors.Count == 0)
        {
            resourceColors.Add(Color.blue);
            resourceColors.Add(Color.green);
            resourceColors.Add(Color.yellow);
            resourceColors.Add(Color.red);
            resourceColors.Add(new Color(1f, 0.5f, 0f)); // Orange
        }

        InvokeRepeating(nameof(ScanForPlayer), 0f, 0.5f);
        UpdateHealthBarUI();
    }

    void ScanForPlayer()
    {
        if (currentKnockbackTimer > 0) return;

        if (!_playerTransform)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) _playerTransform = playerObj.transform;
        }

        if (_playerTransform)
        {
            float distanceToPlayerSqr = (_playerTransform.position - transform.position).sqrMagnitude;
            if (distanceToPlayerSqr <= detectionRadius * detectionRadius)
            {
                if (currentState == AiState.Wandering)
                {
                    currentState = AiState.ChasingPlayer;
                }
            }
            else
            {
                if (currentState == AiState.ChasingPlayer || currentState == AiState.Attacking)
                {
                    currentState = AiState.Wandering;
                }
            }
        }
        else
        {
            if (currentState != AiState.Wandering)
            {
                currentState = AiState.Wandering;
            }
        }
    }

    protected override void Update()
    {
        base.Update(); 
        if (_nextAIFireTime > 0)
        {
            _nextAIFireTime -= Time.deltaTime;
        }

        if (currentKnockbackTimer > 0) return; 
        
        if (currentState == AiState.Attacking || currentState == AiState.ChasingPlayer)
        {
            if (_playerTransform)
            {
                FaceDirection((_playerTransform.position - transform.position).normalized);
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (currentKnockbackTimer > 0 || !rb)
        {
            base.FixedUpdate(); 
            return;
        }

        if (_nextAIFireTime > 0 && currentState == AiState.Attacking) 
        {
            StopMovement();
            base.FixedUpdate();
            return;
        }

        switch (currentState)
        {
            case AiState.Wandering:
                PerformWanderBehavior();
                break;
            case AiState.ChasingPlayer:
                PerformChasePlayerBehavior();
                break;
            case AiState.Attacking:
                PerformAttackBehavior();
                break;
        }

        base.FixedUpdate(); 
    }

    void PerformChasePlayerBehavior()
    {
        if (!_playerTransform)
        {
            currentState = AiState.Wandering;
            return;
        }

        float distanceToPlayer = Vector2.Distance(rb.position, _playerTransform.position);

        if (distanceToPlayer > attackRadius) 
        {
            MoveTowardsTarget(_playerTransform.position, moveSpeed);
        }
        else 
        {
            StopMovement();
            if (_nextAIFireTime <= 0)
            {
                InitiateAttackSequence();
            }
         
        }
    }

    void InitiateAttackSequence()
    {
        currentState = AiState.Attacking;
    }

    private void PerformAttackBehavior()
    {
        if (!_playerTransform)
        {
            currentState = AiState.Wandering;
            return;
        }

        if (!shipShooting || !shipShooting.equippedWeapon)
        {
            currentState = AiState.ChasingPlayer; 
            return;
        }

        shipShooting.AttemptFire(aimAtTransform: _playerTransform);
        
        _nextAIFireTime = aiFireDecisionCooldown;
        
        currentState = AiState.ChasingPlayer;
    }

    void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.gameObject.CompareTag("PlayerProjectile")) 
        {
            Projectile projectile = other.gameObject.GetComponent<Projectile>();
            if (projectile != null)
            {
                TakeDamage(projectile.damage); 
            }
            else
            {
                TakeDamage(1); 
            }
            Destroy(other.gameObject);
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        UpdateHealthBarUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (currentKnockbackTimer <= 0 && _nextAIFireTime <= 0) 
            {
                ScanForPlayer();
            }
        }
    }

    void UpdateHealthBarUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / totalHealth;
        }
    }

    protected override void OnKnockbackStart()
    {
        base.OnKnockbackStart();
        currentState = AiState.Wandering;
    }

    protected override void OnKnockbackEnd()
    {
        base.OnKnockbackEnd();
        ScanForPlayer(); 
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died. Awarding {xpValue} XP.");
        EjectResources();
        Destroy(gameObject, 0.1f);
    }
    
     public void EjectResources()
    {
        if (resourceCubePrefab == null)
        {
            Debug.LogError("Resource Cube Prefab not assigned on " + gameObject.name);
            return;
        }

        int numberOfCubes = Random.Range(minCubesToEject, maxCubesToEject + 1); // +1 because Random.Range for int is exclusive for the max value

        for (int i = 0; i < numberOfCubes; i++)
        {
            // Instantiate the cube at the enemy's current position and rotation
            GameObject cubeInstance = Instantiate(resourceCubePrefab, transform.position, Quaternion.identity);

            // Get the Rigidbody2D and SpriteRenderer components from the instantiated cube
            Rigidbody2D rb2d = cubeInstance.GetComponent<Rigidbody2D>();
            SpriteRenderer sr = cubeInstance.GetComponent<SpriteRenderer>();

            if (rb2d != null)
            {
                // --- Random Ejection Direction & Force ---
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad; // Random angle in radians
                Vector2 ejectionDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                float ejectionForce = Random.Range(minEjectionForce, maxEjectionForce);

                rb2d.AddForce(ejectionDirection * ejectionForce, ForceMode2D.Impulse);

                // --- Random Spin ---
                float spinForce = Random.Range(minSpinForce, maxSpinForce);
                // Apply positive or negative spin randomly
                if (Random.value > 0.5f)
                {
                    spinForce = -spinForce;
                }
                rb2d.AddTorque(spinForce);
            }
            else
            {
                Debug.LogError("Instantiated Resource Cube is missing a Rigidbody2D component on " + cubeInstance.name);
            }

            // --- Random Color ---
            if (sr != null && resourceColors.Count > 0)
            {
                int randomColorIndex = Random.Range(0, resourceColors.Count);
                sr.color = resourceColors[randomColorIndex];
            }
            else if (sr == null)
            {
                Debug.LogError("Instantiated Resource Cube is missing a SpriteRenderer component on " + cubeInstance.name);
            }
            else
            {
                Debug.LogWarning("No resource colors defined in the EnemyBehavior script on " + gameObject.name);
            }

            // Optional: Add a script to the cube to handle its own lifetime, like destroying itself after a few seconds
            // cubeInstance.AddComponent<ResourceCubeSelfDestruct>();
        }
    }
}