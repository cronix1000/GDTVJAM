using UnityEngine;
using UnityEngine.UI; // For health bar, if you keep it

public class BasicEnemyAI : BaseAI // Inherit from BaseAI
{
    [Header("Enemy Specifics")]
    [Tooltip("Radius to detect the player.")]
    public float detectionRadius = 10f;
    [Tooltip("Radius within which the enemy attempts to attack or make contact.")]
    public float attackRadius = 1.5f; // Or stopping distance
    [Tooltip("Damage dealt to player on contact or by attack.")]
    public int damageToPlayer = 10;
    [Tooltip("XP awarded when defeated.")]
    public int xpValue = 50;
    [Tooltip("Layer mask for detecting the player.")]
    public LayerMask playerLayer;

    [Header("Health & Optional UI")]
    [Tooltip("Optional health bar fill image.")]
    public Image healthBarFill;
    public int totalHealth = 20;
    [SerializeField] private int currentHealth;

    // --- REMOVED Conversion Behavior ---
    // public float conversionTimeToConvert = 1.5f;
    // private float currentConversionProgress = 0f;
    // private PeacefulGoat _goatBeingConverted = null; // Goat-specific

    // --- REMOVED Goat Specific Target Variables ---
    // private Transform _targetGoatTransform;
    // private PeacefulGoat _targetGoatScript;

    private Transform _playerTransform; // Store reference to player

    private enum AiState { Wandering, ChasingPlayer, Attacking } // Simplified states
    [SerializeField] private AiState currentState = AiState.Wandering; //SerializeField to see in inspector

    // Optional: For timed attacks
    [Header("Attacking Behavior (Optional)")]
    [Tooltip("Time between attacks if the enemy has a ranged or cooldown-based attack.")]
    public float attackCooldown = 2f;
    private float currentAttackCooldownTimer = 0f;
    [Tooltip("Duration of an attack animation or action where the enemy might be locked.")]
    public float attackDuration = 0.5f; // e.g., for a lunge or spell cast
    private float currentAttackActionTimer = 0f;


    protected override void Awake()
    {
        base.Awake(); // Call base Awake
        currentHealth = totalHealth;
    }

    protected override void Start()
    {
        base.Start(); // Call base Start

        // Find player using tag. Ensure your player GameObject has the "Player" tag.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"BasicEnemyAI ({gameObject.name}): Player not found by tag. AI may not function correctly.", this);
        }

        // More frequent check for player presence if not initially found or if player can respawn.
        InvokeRepeating(nameof(ScanForPlayer), 0f, 0.5f); // Periodically scan for player
        UpdateHealthBarUI();
    }

    void ScanForPlayer()
    {
        if (currentKnockbackTimer > 0) return; // Don't change targets during knockback

        if (_playerTransform == null) // Try to find player if not already set
        {
             GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
             if (playerObj != null) _playerTransform = playerObj.transform;
        }

        if (_playerTransform != null)
        {
            float distanceToPlayerSqr = (_playerTransform.position - transform.position).sqrMagnitude;
            if (distanceToPlayerSqr <= detectionRadius * detectionRadius)
            {
                if (currentState == AiState.Wandering) // Switch to chasing if player detected
                {
                    currentState = AiState.ChasingPlayer;
                    // Debug.Log($"{gameObject.name} detected player and started chasing.");
                }
            }
            else
            {
                if (currentState == AiState.ChasingPlayer || currentState == AiState.Attacking) // If player out of range, go back to wandering
                {
                    currentState = AiState.Wandering;
                    _playerTransform = null; // Lose target if they get too far. ScanForPlayer will try to reacquire.
                    // Debug.Log($"{gameObject.name} lost player, returning to wander.");
                }
            }
        }
        else // No player found
        {
            if (currentState != AiState.Wandering)
            {
                currentState = AiState.Wandering; // Default to wandering if no player reference
            }
        }
    }

    protected override void Update()
    {
        base.Update(); // Handles knockback timer

        if (currentKnockbackTimer > 0) return;

        // Handle cooldowns if applicable
        if (currentAttackCooldownTimer > 0)
        {
            currentAttackCooldownTimer -= Time.deltaTime;
        }
        if (currentAttackActionTimer > 0)
        {
            currentAttackActionTimer -= Time.deltaTime;
            if (currentAttackActionTimer <= 0)
            {
                // Attack action finished, decide next state (e.g., back to chasing or cooldown)
                if (currentState == AiState.Attacking) // Ensure still in attacking state
                {
                    currentAttackCooldownTimer = attackCooldown; // Start cooldown
                    currentState = AiState.ChasingPlayer; // Default back to chasing after attack action
                }
            }
        }
    }


    protected override void FixedUpdate()
    {
        if (currentKnockbackTimer > 0 || rb == null)
        {
            // During knockback, base.FixedUpdate might still apply boundary constraints if needed
            // but typically velocity is controlled by knockback.
            base.FixedUpdate();
            return;
        }

        if (currentAttackActionTimer > 0) // If in an attack animation/action, likely no movement
        {
            StopMovement(); // Or specific attack movement
            // FaceDirection towards player might still be relevant if attack is directional
            if(_playerTransform != null) FaceDirection((_playerTransform.position - transform.position).normalized);
            base.FixedUpdate(); // Apply boundary constraints
            return;
        }

        switch (currentState)
        {
            case AiState.Wandering:
                PerformWanderBehavior(); // Uses base class wander
                break;
            case AiState.ChasingPlayer:
                PerformChasePlayerBehavior();
                break;
            case AiState.Attacking:
                // This state might be brief, for an attack animation.
                // Movement during attack is handled above (or could be specific logic here).
                // If it's an instant attack on contact, this state might not be used,
                // and attack logic happens in OnCollisionEnter.
                // For this example, we assume Attacking state means "currently performing an attack action".
                break;
        }

        base.FixedUpdate(); // IMPORTANT: Apply boundary constraints from base class AFTER movement logic
    }

    void PerformChasePlayerBehavior()
    {
        if (_playerTransform == null)
        {
            currentState = AiState.Wandering; // Player lost or destroyed
            return;
        }

        float distanceToPlayer = Vector2.Distance(rb.position, _playerTransform.position);

        if (distanceToPlayer > attackRadius)
        {
            MoveTowardsTarget(_playerTransform.position, moveSpeed);
        }
        else // Within attack radius
        {
            StopMovement(); // Stop to prepare/initiate attack
            if (_playerTransform != null) FaceDirection((_playerTransform.position - transform.position).normalized);

            // Attempt to attack if cooldown is ready and not already in an attack action
            if (currentAttackCooldownTimer <= 0 && currentAttackActionTimer <= 0)
            {
                InitiateAttack();
            }
        }
    }

    void InitiateAttack()
    {
        // This is where you'd trigger an attack.
        // It could be instant damage on contact (handled by OnCollision),
        // or start an attack animation/action.
        currentState = AiState.Attacking;
        currentAttackActionTimer = attackDuration; // Lock into attack animation/action
        currentAttackCooldownTimer = attackCooldown + attackDuration; // Set full cooldown after action completes

        // Example: Play an attack animation
        // if (animator != null) animator.SetTrigger("Attack");

        // Example: For a simple melee enemy, damage might be applied on collision during the "Attacking" state
        // or this function could spawn a projectile for a ranged enemy.
        // For this example, let's assume damage is primarily via contact (OnCollisionEnter2D).
        Debug.Log($"{gameObject.name} is initiating an attack on player.");

        // If it's a very simple "touch attack" and no animation lock:
        // TryDamagePlayer(_playerTransform.gameObject); // Could call this directly
        // currentAttackCooldownTimer = attackCooldown;
        // currentState = AiState.ChasingPlayer; // Go back to chasing immediately if no attack animation
    }


    // Handle direct contact with player (for melee/touch damage)
    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentKnockbackTimer > 0) return;

        if ((playerLayer.value & (1 << collision.gameObject.layer)) > 0) // Check if collided object is on playerLayer
        {
            // More robust check:
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                 // Apply damage if in Chasing or Attack state (or always on contact if that's the design)
                if (currentState == AiState.ChasingPlayer || currentState == AiState.Attacking || attackRadius <= 1.0f /* close enough for touch */)
                {
                    TryDamagePlayer(collision.gameObject);

                    // Optionally, trigger a short cooldown even for contact damage to prevent rapid multi-hits
                    // if (currentAttackCooldownTimer <= 0) currentAttackCooldownTimer = 0.5f; // Short touch cooldown
                }
            }
        }
    }

    // --- REMOVED GOAT CONVERSION COLLISION LOGIC ---
    // public virtual void OnCollisionStay2D(Collision2D collision) { ... }
    // public virtual void OnCollisionExit2D(Collision2D collision) { ... }
    // void HandleContactStart(GameObject contactObject) { ... }
    // void ProcessContactForConversion(GameObject contactObject) { ... }
    // void HandleContactEnd(GameObject contactObject) { ... }
    // void TryConvertGoat(PeacefulGoat goatToConvert) { ... }

    void TryDamagePlayer(GameObject playerObject)
    {
        // GetComponent is fine if PlayerController is expected on the root of the player.
        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Assuming PlayerController script (or a dedicated health script) has TakeDamage
            // This needs to be implemented on your PlayerController
            // playerController.TakeDamage(damageToPlayer);
            Debug.Log($"{gameObject.name} damaged player for {damageToPlayer} health.");

            // Example: Apply a small knockback to self or player after attacking
            // if (rb != null) ApplyKnockback((transform.position - playerObject.transform.position).normalized, 1f, 0.2f);
        }
    }

    public void TakeDamage(int amount) // Public for other scripts (e.g., player attacks) to call
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage, {currentHealth}/{totalHealth} health remaining.");
        UpdateHealthBarUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Optional: Flinch animation or brief state change on taking damage
            // if (animator != null) animator.SetTrigger("Hit");
            // If not in knockback, maybe briefly interrupt action or re-evaluate target
             if (currentKnockbackTimer <= 0 && currentState != AiState.Attacking)
             {
                // If player is still a valid target, ensure we are chasing
                ScanForPlayer(); // Re-evaluate, might make it more reactive
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
        // Enemy-specific reactions to knockback
        if (currentState == AiState.Attacking)
        {
            currentAttackActionTimer = 0f; // Cancel current attack action
             // Don't reset cooldown here, let it run its course or reset it if desired
        }
        currentState = AiState.Wandering; // Temporarily go to wander, OnKnockbackEnd will re-evaluate
        // Debug.Log($"{gameObject.name} (Enemy) knockback started.");
    }

    protected override void OnKnockbackEnd()
    {
        base.OnKnockbackEnd();
        // After knockback, actively find player or resume wandering.
        ScanForPlayer();
        // Debug.Log($"{gameObject.name} (Enemy) knockback ended.");
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died. Awarding {xpValue} XP.");
        // Grant XP to player (you'll need a system for this)
        // Example: if (PlayerExperienceSystem.Instance != null) PlayerExperienceSystem.Instance.GrantXP(xpValue);

        // Spawn death effects, loot, etc. here

        Destroy(gameObject);
    }
}