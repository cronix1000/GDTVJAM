using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum GameState {StartMenu,Playing, Building, Paused, GameOver}
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState currentGameState = GameState.Playing; // Default to Playing for testing, might be StartMenu typically
    
    [Header("Builder System")]
    [SerializeField] private GameObject shipBuilderUIRoot;
    [SerializeField] private PlayerCreatorGridManager playerCreatorGridManager;
    [SerializeField] private GridConfiguration defaultGridConfiguration; // For normal building
    [SerializeField] private GridConfiguration largeGridConfiguration;   // For large building mode
    [SerializeField] private float playerSizeThresholdForLargeBuilder = 50f; // Example: if ship has > 50 blocks

    [Header("Player Ship")]
    [SerializeField] private Ship currentPlayerShip; 
    
    [Header("Cameras")]
    [SerializeField] public Camera builderCamera; 
    [SerializeField] public Camera mainCamera; 
    [SerializeField] private BuilderCameraController builderCameraController; // New: Script for builder camera movement

    [Header("Game Systems")]
    [SerializeField] private Transform GameHUD; 
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize to a sensible default if not set, e.g., Main Menu
        if (currentGameState == GameState.Playing && Time.timeSinceLevelLoad < 1f) // Avoid re-setting if already playing
        {
            // currentGameState = GameState.StartMenu; // Or your initial scene's state
        }
    }
    
    // Ensure this static method properly sets the instance's state
    public static void SetCurrentState(GameState newState) // Renamed for clarity from SetState to avoid conflict if BaseAI had one
    {
        if (Instance == null)
        {
            Debug.LogError("GameStateManager Instance is null. Cannot set state.");
            return;
        }
        Instance.currentGameState = newState;
        Debug.Log($"Game state changed to: {newState}");
        Instance.OnStateChanged(newState); // Optional: Call a method to handle state-specific logic
    }

    // Optional: Handle actions when state changes
    private void OnStateChanged(GameState newState)
    {
        // You can add logic here that needs to happen universally when a state changes
        // For example, ensuring correct UI is shown/hidden, etc.
    }
    
    public void EnterBuildMode()
    {
        if (currentGameState == GameState.Playing && currentPlayerShip)
        {
            SetCurrentState(GameState.Building);

            Time.timeScale = 0f; 

            DisablePlayerMovementAndAbilities();
            HideGameHUD();
          
            
            bool useLargeBuilder = currentPlayerShip.IsConsideredLarge(playerSizeThresholdForLargeBuilder);
            GridConfiguration activeGridConfig = useLargeBuilder ? largeGridConfiguration : defaultGridConfiguration;

            if (activeGridConfig == null)
            {
                Debug.LogError($"GameStateManager: The selected GridConfiguration ({(useLargeBuilder ? "Large" : "Default")}) is not assigned!", this);
                EnterPlayModeFromBuilding();
                return;
            }

            if (playerCreatorGridManager == null)
            {
                Debug.LogError("GameStateManager: PlayerCreatorGridManager is not assigned!", this);
                EnterPlayModeFromBuilding(); // Revert
                return;
            }

            shipBuilderUIRoot.SetActive(true); 
            playerCreatorGridManager.gameObject.SetActive(true);
            playerCreatorGridManager.InitializeBuilder(activeGridConfig, currentPlayerShip);
            // playerCreatorGridManager.RefreshHighlights(); 

            ActivateBuilderCamera(useLargeBuilder); 
        }
        else
        {
            Debug.LogWarning("Cannot enter build mode. Not in Playing state or currentPlayerShip is null.");
        }
    }
    
    public void ExitBuildModeAndSaveChanges() 
    {
        if (currentGameState == GameState.Building)
        {
            if (playerCreatorGridManager != null && currentPlayerShip != null)
            {
                List<BlockDataEntry> currentBuiltConfiguration = playerCreatorGridManager.GetCurrentGridContentAsData();

                currentPlayerShip.UpdateShipConfigurationData(currentBuiltConfiguration);
                
                currentPlayerShip.ApplyConfigurationToPhysicalShip(playerCreatorGridManager);

                Debug.Log("Changes saved to current ship instance.");
            }
            else
            {
                Debug.LogError("Cannot save changes: GridManager or CurrentShip is null.");
            }

            // Then proceed to switch back to play mode
            SetCurrentState(GameState.Playing);
            Time.timeScale = 1f;
            ShowGameHUD();
            EnablePlayerMovementAndAbilities();
            if (shipBuilderUIRoot) shipBuilderUIRoot.SetActive(false);
            if (playerCreatorGridManager) playerCreatorGridManager.gameObject.SetActive(false);
            ActivateMainCamera();
        }
    }

    public void EnterPlayModeFromBuilding()
    {
        if (currentGameState == GameState.Building)
        {
            ExitBuildModeAndSaveChanges();
            SetCurrentState(GameState.Playing); // Use the static setter
            
            Time.timeScale = 1f; 
            
            ShowGameHUD();
            EnablePlayerMovementAndAbilities();
            
            if (shipBuilderUIRoot) shipBuilderUIRoot.SetActive(false); 
            if (playerCreatorGridManager) playerCreatorGridManager.gameObject.SetActive(false); 
            
            ActivateMainCamera();
        }
    }

    private void EnablePlayerMovementAndAbilities()
    {
        if (currentPlayerShip)
        {
            currentPlayerShip.SetGameplayComponentsActive(true); 
        }
    }
    
    private void DisablePlayerMovementAndAbilities()
    {
        if (currentPlayerShip)
        {
            currentPlayerShip.SetGameplayComponentsActive(false); 
        }
    }

    private void HideGameHUD()
    {
        if (GameHUD) GameHUD.gameObject.SetActive(false);
    }
    
    private void ShowGameHUD()
    {
        if (GameHUD) GameHUD.gameObject.SetActive(true);
    }   

    // Modified ActivateBuilderCamera
    private void ActivateBuilderCamera(bool isLargeMode)
    {
        if (builderCamera) builderCamera.gameObject.SetActive(true);
        if (mainCamera) mainCamera.gameObject.SetActive(false);

        if (builderCameraController != null)
        {
            builderCameraController.SetMovementActive(isLargeMode);
            if (isLargeMode)
            {
                // Optional: Apply specific settings for the large builder camera
                // e.g., builderCameraController.SetViewLimits(someLargeLimits);
                // builderCameraController.ZoomToFit(playerCreatorGridManager.GetCurrentGridWorldSize());
            }
            else
            {
                builderCameraController.ResetView(); // Reset to default non-moving view if not large mode
            }
        }
    }
    
    private void ActivateMainCamera()
    {
        if (mainCamera) mainCamera.gameObject.SetActive(true);
        if (builderCamera) builderCamera.gameObject.SetActive(false);

        // Ensure builder camera movement is off when switching to main camera
        if (builderCameraController != null)
        {
            builderCameraController.SetMovementActive(false);
        }
    }
}