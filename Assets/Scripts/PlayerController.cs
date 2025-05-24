using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private Transform visualTransform; // Assign the child GameObject that holds the player's sprite/visuals
    [SerializeField] private Animator animator;         // Optional: For walk animations. Can be removed if not used.
    [SerializeField] private Rigidbody2D rb;           // Assign the Rigidbody2D component

    [Header("Aiming")]
    [Tooltip("Assign an empty GameObject here. Its position will follow the mouse in the game world.")]
    [SerializeField] private Transform mouseAimTarget;
    
    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Camera mainCamera;

    private void Awake()
    {
        // Cache components
        // If Rigidbody2D is on the same GameObject, you can also do: rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D not assigned on PlayerController.", this);
            enabled = false; // Disable script if essential components are missing
            return;
        }
        if (visualTransform == null)
        {
            Debug.LogWarning("VisualTransform not assigned on PlayerController. Rotation will not work.", this);
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Ensure your main camera is tagged 'MainCamera'.", this);
            enabled = false;
            return;
        }

        playerControls = new PlayerControls();

        // Setup Move input action
        playerControls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
        }
    }

    private void FixedUpdate()
    {
        // Apply movement
        if (rb != null)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        // Handle walk animation based on movement
        if (animator != null)
        {
            animator.SetBool("Walking", moveInput != Vector2.zero);
        }
    }

    private void Update()
    {
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (visualTransform == null || mainCamera == null) return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
    
        // This line is often a point of confusion for ScreenToWorldPoint's z parameter.
        // It calculates the world Z depth of the mouse position to be the same as the player's Z depth.
        // This is generally correct for ensuring the direction calculation is on the same 2D plane.
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, mainCamera.transform.position.z - transform.position.z));
    
        Vector2 directionToMouse = (Vector2)mouseWorldPosition - (Vector2)transform.position; // Cast to Vector2 for 2D direction

        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        visualTransform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }
    
    
}