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

        // Get mouse position in screen coordinates
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        // Convert mouse position to world coordinates
        // We use the player's Z position to ensure the conversion is on the correct 2D plane
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, mainCamera.transform.position.z - transform.position.z));
        // More robust for perspective camera: project onto a plane at player's depth
        // Ray cameraRay = mainCamera.ScreenPointToRay(mouseScreenPosition);
        // Plane groundPlane = new Plane(Vector3.forward, new Vector3(0,0,transform.position.z)); // Assumes Z is forward for 2D
        // float rayLength;
        // if (groundPlane.Raycast(cameraRay, out rayLength))
        // {
        //    mouseWorldPosition = cameraRay.GetPoint(rayLength);
        // }


        // Calculate direction from player to mouse
        Vector2 directionToMouse = (Vector2)mouseWorldPosition - (Vector2)transform.position; // Cast to Vector2 for 2D direction

        // Calculate angle
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        // Apply rotation to visualTransform
        // Adjust the offset (-90f) if your sprite's default orientation is not "upwards"
        visualTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}