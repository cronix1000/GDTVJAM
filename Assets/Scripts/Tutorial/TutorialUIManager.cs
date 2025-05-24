using UnityEngine;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Text instructionText; // Use TextMeshProUGUI if using TMP
    public Button nextButton;
    public Canvas tutorialCanvas; // Main Canvas for tutorial UI

    [Header("Highlight Prefabs")]
    public GameObject uiHighlightPrefab; // Prefab for UI element highlights
    public GameObject worldHighlightPrefab; // Prefab for world object highlights

    private GameObject currentHighlight; // Currently active highlight
    private Camera activeCamera; // Camera for rendering/positioning

    void Awake()
    {
        // Initialize next button listener
        nextButton.onClick.AddListener(OnNextButtonClicked);
        HideAllTutorialUI();
    }

    // Show UI elements for a tutorial step
    public void ShowStep(TutorialStep step)
    {
        instructionText.text = step.instructionText;
        instructionText.gameObject.SetActive(true);

        // Show next button if step doesn't wait for external triggers
        nextButton.gameObject.SetActive(!step.waitForUIInteraction);

        CreateHighlight(step);
    }

    // Hide all tutorial UI elements
    public void HideAllTutorialUI()
    {
        instructionText.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        ClearHighlight();
    }

    // Set the active camera for world space rendering
    public void SetActiveTutorialCamera(Camera cam)
    {
        activeCamera = cam;
        if (tutorialCanvas.renderMode == RenderMode.WorldSpace)
            tutorialCanvas.worldCamera = cam;
    }

    // Handle next button click
    private void OnNextButtonClicked()
    {
        TutorialSequenceManager.Instance.AdvanceTutorialStep();
    }

    // Create highlight based on step configuration
    private void CreateHighlight(TutorialStep step)
    {
        ClearHighlight();

        if (step.highlightType == HighlightType.None || step.highlightTargetReference == null)
            return;

        switch (step.highlightType)
        {
            case HighlightType.UIElement:
                HighlightUIElement(step.highlightTargetReference);
                break;
            case HighlightType.WorldObject:
                HighlightWorldObject(step.highlightTargetReference);
                break;
        }
    }

    private void HighlightUIElement(GameObject uiTarget)
    {
        if (uiHighlightPrefab == null) return;

        RectTransform targetRect = uiTarget.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            currentHighlight = Instantiate(uiHighlightPrefab, targetRect);
            // Adjust highlight position/size as needed
        }
    }

    private void HighlightWorldObject(GameObject worldTarget)
    {
        if (worldHighlightPrefab == null) return;

        currentHighlight = Instantiate(worldHighlightPrefab);
        FollowTarget followScript = currentHighlight.AddComponent<FollowTarget>();
        followScript.Initialize(worldTarget.transform, activeCamera);
    }

    private void ClearHighlight()
    {
        if (currentHighlight != null)
            Destroy(currentHighlight);
    }

    void Update()
    {
        // Update world highlight position if needed
        if (currentHighlight != null && currentHighlight.activeInHierarchy)
        {
            FollowTarget followScript = currentHighlight.GetComponent<FollowTarget>();
            if (followScript != null) followScript.UpdatePosition();
        }
    }
}

// Helper class for world object following
public class FollowTarget : MonoBehaviour
{
    private Transform target;
    private Camera viewCamera;

    public void Initialize(Transform targetTransform, Camera camera)
    {
        target = targetTransform;
        viewCamera = camera;
    }

    public void UpdatePosition()
    {
        if (target == null || viewCamera == null) return;

        // Position highlight at target's world position
        transform.position = target.position;

        // Face the camera if using billboard-style highlights
        transform.LookAt(transform.position + viewCamera.transform.rotation * Vector3.forward,
                        viewCamera.transform.rotation * Vector3.up);
    }
}