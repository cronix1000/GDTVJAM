using UnityEngine;

public abstract class BaseAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed for the AI.")]
    public float moveSpeed = 3f;
    [Tooltip("Multiplier for speed when the AI is wandering.")]
    public float wanderSpeedMultiplier = 0.6f; // Applied during PerformWanderBehavior

    [Header("Wandering Behavior")]
    [Tooltip("Radius within which the AI will pick new wander destinations.")]
    public float wanderRadius = 5f;
    [Tooltip("Minimum time the AI will move towards a wander destination.")]
    public float minWanderTime = 2f;
    [Tooltip("Maximum time the AI will move towards a wander destination.")]
    public float maxWanderTime = 5f;
    [Tooltip("How close the AI needs to be to its wander target to consider it reached.")]
    public float wanderPointReachedThreshold = 0.5f;
    [Tooltip("If true, AI wanders around its initial spawn position. If false, wanders around its current position when a new point is chosen.")]
    public bool wanderAroundInitialPosition = true;

    [Header("Visuals")]
    [Tooltip("Reference to the child Transform that holds the AI's main visuals (for flipping). If null, attempts to find 'Visuals' child or defaults to this transform.")]
    [SerializeField] protected Transform visualsTransform;

    [Header("Map Boundaries")]
    [Tooltip("Should the AI be restricted by map boundaries?")]
    public bool useMapBoundaries = true;
    [Tooltip("Minimum X and Y coordinates the AI can reach.")]
    public Vector2 mapMinBounds = new Vector2(0, 0);
    [Tooltip("Maximum X and Y coordinates the AI can reach.")]
    public Vector2 mapMaxBounds = new Vector2(50f, 50f);

    protected Rigidbody2D rb;
    protected Animator animator; // Optional: Assign in Inspector or will try to find

    protected Vector2 currentWanderDestination;
    protected float currentWanderTimer;
    protected Vector3 _initialPosition;

    protected float knockbackActiveDuration = 0f;
    protected float currentKnockbackTimer = 0f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponent<Animator>();
        if (visualsTransform != null && animator == null) animator = visualsTransform.GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (visualsTransform == null)
        {
            Transform foundVisuals = transform.Find("Visuals");
            visualsTransform = foundVisuals != null ? foundVisuals : transform;
            if (visualsTransform == transform && transform.childCount > 0)
            {
                // Debug.LogWarning($"VisualsTransform not set on {gameObject.name} and no 'Visuals' child found. Defaulting to root. Sprite flipping might affect children.", this);
            }
        }
    }

    protected virtual void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            Debug.LogError($"Rigidbody2D not found on {gameObject.name}. AI movement will not work.", this);
            enabled = false; // Disable script if no Rigidbody
            return;
        }
        _initialPosition = transform.position;
        SetNewWanderDestination();
    }

    protected virtual void Update()
    {
        if (currentKnockbackTimer > 0)
        {
            currentKnockbackTimer -= Time.deltaTime;
            if (currentKnockbackTimer <= 0)
            {
                OnKnockbackEnd();
            }
            return; // No other updates during knockback
        }
        // Derived classes can add non-physics updates here if needed.
    }

    protected virtual void FixedUpdate()
    {
        if (currentKnockbackTimer > 0 || rb == null)
        {
            return; // No movement or physics updates during knockback or if no Rigidbody
        }

        // Movement logic should be handled by derived classes (e.g., in their PerformWanderBehavior or ChasePlayer)
        // This base FixedUpdate primarily handles applying boundary constraints after movement has been set.

        if (useMapBoundaries)
        {
            ApplyBoundaryConstraints();
        }
    }

    protected virtual void SetNewWanderDestination()
    {
        Vector2 wanderOrigin = wanderAroundInitialPosition ? (Vector2)_initialPosition : rb.position;
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        float randomDistance = Random.Range(Mathf.Min(wanderRadius * 0.25f, wanderRadius), wanderRadius);
        Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;
        currentWanderDestination = wanderOrigin + offset;

        if (useMapBoundaries)
        {
            currentWanderDestination = ClampToBoundaries(currentWanderDestination);
        }
        currentWanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    // Called by derived classes when in a wandering state
    protected virtual void PerformWanderBehavior()
    {
        if (rb == null || currentKnockbackTimer > 0) return;

        currentWanderTimer -= Time.fixedDeltaTime;

        Vector2 directionToTarget = (currentWanderDestination - rb.position).normalized;
        float distanceToTarget = Vector2.Distance(rb.position, currentWanderDestination);

        if (distanceToTarget > wanderPointReachedThreshold)
        {
            rb.linearVelocity = directionToTarget * (moveSpeed * wanderSpeedMultiplier);
            FaceDirection(directionToTarget); // Pass direction instead of absolute target
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f); // Smooth stop
        }

        if (currentWanderTimer <= 0f || distanceToTarget <= wanderPointReachedThreshold)
        {
            SetNewWanderDestination();
        }
    }

    // Called by derived classes to move towards a specific target
    protected virtual void MoveTowardsTarget(Vector2 targetPosition, float speed)
    {
        if (rb == null || currentKnockbackTimer > 0) return;

        Vector2 direction = (targetPosition - rb.position).normalized;
        rb.linearVelocity = direction * speed;
        FaceDirection(direction); // Pass direction
    }

    protected virtual void StopMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
    }

    protected virtual void ApplyBoundaryConstraints()
    {
        if (rb == null || !useMapBoundaries) return;

        Vector2 currentPosition = rb.position;
        Vector2 clampedPosition = ClampToBoundaries(currentPosition);

        if (currentPosition != clampedPosition)
        {
            rb.position = clampedPosition;
            if ((clampedPosition - currentPosition).sqrMagnitude > 0.0001f)
            {
                 rb.linearVelocity = Vector2.zero; // Stop if clamped
            }
        }
    }

    protected Vector2 ClampToBoundaries(Vector2 position)
    {
        float clampedX = Mathf.Clamp(position.x, mapMinBounds.x, mapMaxBounds.x);
        float clampedY = Mathf.Clamp(position.y, mapMinBounds.y, mapMaxBounds.y);
        return new Vector2(clampedX, clampedY);
    }

    // Modified FaceDirection to accept a direction vector
    protected virtual void FaceDirection(Vector2 moveDirection)
    {
        if (visualsTransform == null || moveDirection.sqrMagnitude < 0.01f) return; // Don't flip if not moving or no visuals

        Vector3 currentScale = visualsTransform.localScale;
        if (moveDirection.x < -0.01f && currentScale.x > 0) // Moving left, visuals currently right
        {
            currentScale.x *= -1;
        }
        else if (moveDirection.x > 0.01f && currentScale.x < 0) // Moving right, visuals currently left
        {
            currentScale.x *= -1;
        }
        visualsTransform.localScale = currentScale;
    }


    public virtual void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = Vector2.zero; // Stop current movement
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
            currentKnockbackTimer = duration;
            knockbackActiveDuration = duration; // Store original duration if needed
            OnKnockbackStart();
        }
    }

    protected virtual void OnKnockbackStart()
    {
        // Base implementation. Derived classes can override.
        // if (rb != null) rb.velocity = Vector2.zero; // Ensure velocity is zeroed
    }

    protected virtual void OnKnockbackEnd()
    {
        // Base implementation.
        if (rb != null) rb.linearVelocity = Vector2.zero; // Ensure AI is stopped.
    }
}