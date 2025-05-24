
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class PlayerAbilityController : MonoBehaviour
    {
        private PlayerControls playerControls;
        private Vector2 lookPosition;
        private Camera mainCamera;
        
        private WeaponData weaponData;
        [SerializeField] private ShipShooting shipShooting;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found. Ensure your main camera is tagged 'MainCamera'.", this);
                enabled = false;
                return;
            }

            playerControls = new PlayerControls();

            // Setup Move input action
            playerControls.Player.Attack.performed += ctx => Attack();
            playerControls.Player.Attack.canceled += ctx => Attack();
            playerControls.Player.Look.performed += ctx => lookPosition = ctx.ReadValue<Vector2>();
            playerControls.Player.Look.canceled += ctx => lookPosition = Vector2.zero;
            
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
        
        private void Attack()
        {
            Debug.Log("Attack performed!");
            if (shipShooting != null)
            {
                Vector2 aimTargetScreenPosition = lookPosition; 

                if (Mouse.current != null)
                {
                    aimTargetScreenPosition = Mouse.current.position.ReadValue();
                }

                shipShooting.AttemptFire(aimTargetScreenPosition);
            }
            else
            {
                Debug.LogWarning("ShipShooting component not found or assigned!", this);
            }
        }
        
        
    }
