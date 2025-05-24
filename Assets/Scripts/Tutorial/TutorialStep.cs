using UnityEngine;
using System.Collections.Generic; // For List

public enum HighlightType { None, UIElement, WorldObject, ScreenArea }
public enum CompletionTrigger { OnConfirmation, OnEvent, OnPlayerAction, Timed }

[System.Serializable]
public class TutorialStep
{
    public string stepID; // Unique identifier for this step
    [TextArea(3, 5)]
    public string instructionText;
    public GameState targetGameState = GameState.Playing; // Which game state this step is for

    [Header("Highlighting")]
    public HighlightType highlightType = HighlightType.None;
    public string highlightTargetNameOrPath; // e.g., "Canvas/BuildMenu/AttackBlockButton" or "PlayerShip/TurretMount1"
    public GameObject highlightTargetReference; // More robust: Assign UI GameObject directly in Inspector (if step is a MonoBehaviour or SO with scene context)
    public Vector3 highlightWorldPositionOffset; // Offset if target is WorldObject
    public Vector2 highlightScreenPosition;    // For ScreenArea
    public Vector2 highlightSize = new Vector2(100, 50); // For ScreenArea or UI element override
    public bool pointArrowAtTarget = false;

    [Header("Completion")]
    public CompletionTrigger completionTrigger = CompletionTrigger.OnConfirmation;
    public string eventToWaitFor;         // If OnEvent (e.g., "PlayerLeveledUp", "BlockPlaced_Attack")
    public float durationForTimed = 3f;   // If Timed
    public bool autoAdvanceOnTrigger = true; // If true, no "Next" button needed after event/action

    [Header("Behavior")]
    public bool pauseGameForStep = false; // Pauses Time.timeScale if in Playing state
    public bool waitForUIInteraction = true; // If true, needs a "Next" or "Got it" button click to proceed (unless autoAdvanceOnTrigger)
    public List<string> prerequisiteStepIDs = new List<string>(); // IDs of steps that must be done first

    public TutorialStep(string id, string text, GameState state = GameState.Playing)
    {
        stepID = id;
        instructionText = text;
        targetGameState = state;
    }
}