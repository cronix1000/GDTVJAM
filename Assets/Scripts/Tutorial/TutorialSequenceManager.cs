// TutorialSequenceManager.cs (Singleton, or accessible globally)
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like Any, All

public class TutorialSequenceManager : MonoBehaviour
{
    public static TutorialSequenceManager Instance { get; private set; }

    [Header("Tutorial Setup")]
    public List<TutorialSequence> allTutorialSequences; // Assign all tutorial SOs here
    public TutorialUIManager uiManager;                  // Assign in Inspector

    [Header("Progression Tracking")]
    private HashSet<string> _completedStepIDs = new HashSet<string>();
    private HashSet<string> _completedSequenceIDs = new HashSet<string>();
    private const string CompletedStepsPlayerPrefsKey = "TutorialCompletedSteps";
    private const string CompletedSequencesPlayerPrefsKey = "TutorialCompletedSequences";


    private TutorialSequence _currentSequence;
    private int _currentStepIndex = -1;
    private TutorialStep _currentStep;
    private float _stepTimer = 0f;

    private GameStateManager _gameStateManager; // To get current game state and cameras

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make it persist across scenes if needed
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (uiManager == null) Debug.LogError("TutorialUIManager not assigned!", this);
        LoadProgress();
    }

    void Start()
    {
        _gameStateManager = GameStateManager.Instance; // Assuming GameStateManager is also a singleton
        if (_gameStateManager == null) Debug.LogError("GameStateManager not found!", this);

        // Subscribe to game state changes if GameStateManager has an event for it
        // GameStateManager.OnGameStateChanged += HandleGameStateChanged;

        // Attempt to start any auto-start tutorials for the initial game state
        CheckAndStartAutoSequences(_gameStateManager.currentGameState);
    }

    // Example of how you might listen to game state changes
    public void HandleGameStateChanged(GameState newState) // Call this from GameStateManager.SetCurrentState
    {
        Debug.Log($"Tutorial Manager: Game state changed to {newState}");
        if (_currentStep != null && _currentStep.targetGameState != GameState.StartMenu && _currentStep.targetGameState != newState)
        {
            Debug.Log($"Tutorial step '{_currentStep.stepID}' is not for state {newState}. Pausing tutorial display.");
            uiManager?.HideAllTutorialUI(); // Hide UI if current step isn't for this state
                                           // Don't necessarily stop the sequence, just hide current step UI
        }
        else if (_currentStep != null)
        {
            ProcessCurrentStepInternal(); // Re-process (e.g., re-show UI if it was hidden)
        }
        else
        {
            // No active sequence, check if a new auto-sequence should start for this state
            CheckAndStartAutoSequences(newState);
        }
    }


    void Update()
    {
        if (_currentStep == null || uiManager == null) return;

        // Handle Timed completion
        if (_currentStep.completionTrigger == CompletionTrigger.Timed)
        {
            _stepTimer += Time.unscaledDeltaTime; // Use unscaled time if game can be paused
            if (_stepTimer >= _currentStep.durationForTimed)
            {
                CompleteAndAdvance("Timed duration met");
            }
        }

        // Update highlight if target is a WorldObject that moves (more advanced)
        if (_currentStep.highlightType == HighlightType.WorldObject && _currentStep.highlightTargetReference != null)
        {
            // uiManager.UpdateWorldHighlightPosition(_currentStep); // UIManager would need this method
        }
    }

    private void CompleteAndAdvance(string timedDurationMet)
    {
        if (_currentStep != null && _currentStep.completionTrigger == CompletionTrigger.Timed)
        {
            Debug.Log($"Completing step '{_currentStep.stepID}' due to timed duration.");
            AdvanceTutorialStep(timedDurationMet);
        }
    }


    public void TryStartSequence(string sequenceID)
    {
        if (_currentSequence != null && _currentSequence.sequenceID == sequenceID)
        {
            Debug.LogWarning($"Tutorial sequence '{sequenceID}' is already active.");
            return;
        }
        if (_completedSequenceIDs.Contains(sequenceID))
        {
            Debug.Log($"Tutorial sequence '{sequenceID}' has already been completed.");
            return;
        }

        TutorialSequence sequenceToStart = allTutorialSequences.FirstOrDefault(s => s.sequenceID == sequenceID);
        if (sequenceToStart != null)
        {
            // Check prerequisite sequences
            if (sequenceToStart.prerequisiteSequenceIDs.Any(prereqID => !_completedSequenceIDs.Contains(prereqID)))
            {
                Debug.Log($"Cannot start sequence '{sequenceID}', prerequisites not met.");
                return;
            }

            _currentSequence = sequenceToStart;
            _currentStepIndex = -1; // Will be incremented to 0 by AdvanceTutorialStep
            Debug.Log($"Starting tutorial sequence: {_currentSequence.sequenceDisplayName}");
            AdvanceTutorialStep();
        }
        else
        {
            Debug.LogError($"Tutorial sequence with ID '{sequenceID}' not found.");
        }
    }

    public void AdvanceTutorialStep(string reason = "Next button pressed") // Optional reason for logging
    {
        if (_currentSequence == null) return;

        // Mark previous step as complete if there was one
        if (_currentStep != null && !_completedStepIDs.Contains(_currentStep.stepID))
        {
            _completedStepIDs.Add(_currentStep.stepID);
            // Debug.Log($"Step '{_currentStep.stepID}' completed. Reason: {reason}");
            SaveProgress();
        }

        // Restore time scale if previous step paused it
        if (_currentStep != null && _currentStep.pauseGameForStep && _gameStateManager.currentGameState == GameState.Playing)
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        _currentStepIndex++;
        if (_currentStepIndex < _currentSequence.steps.Count)
        {
            _currentStep = _currentSequence.steps[_currentStepIndex];
            ProcessCurrentStepInternal();
        }
        else
        {
            CompleteCurrentSequence();
        }
    }

    private void ProcessCurrentStepInternal()
    {
        if (_currentStep == null || uiManager == null || _gameStateManager == null) return;

        // Check prerequisites for the step itself
        if (_currentStep.prerequisiteStepIDs.Any(prereqID => !_completedStepIDs.Contains(prereqID)))
        {
            Debug.Log($"Step '{_currentStep.stepID}' prerequisites not met. Trying to advance past it.");
            AdvanceTutorialStep("Step prerequisites not met"); // Skip it
            return;
        }

        // Check if step is for the current game state
        if (_currentStep.targetGameState != _gameStateManager.currentGameState)
        {
            Debug.Log($"Step '{_currentStep.stepID}' is for state {_currentStep.targetGameState}, current is {_gameStateManager.currentGameState}. Hiding UI, waiting for correct state.");
            uiManager.HideAllTutorialUI(); // Hide step if not relevant to current game state
            return; // Don't process further until state matches
        }

        Debug.Log($"Processing step: {_currentStep.stepID} - {_currentStep.instructionText}");
        _stepTimer = 0f; // Reset timer for timed steps

        if (_currentStep.pauseGameForStep && _gameStateManager.currentGameState == GameState.Playing)
        {
            Time.timeScale = 0f;
        }

        // Update active camera for UI Manager
        Camera activeCam = _gameStateManager.currentGameState == GameState.Building ?
                           GameStateManager.Instance.builderCamera : // Assuming GameStateManager exposes these
                           GameStateManager.Instance.mainCamera;
        uiManager.SetActiveTutorialCamera(activeCam);
        uiManager.ShowStep(_currentStep);


        // Handle non-confirmation triggers
        if (_currentStep.completionTrigger == CompletionTrigger.OnEvent)
        {
            // TODO: Subscribe to a global event system for _currentStep.eventToWaitFor
            // Example: EventManager.StartListening(_currentStep.eventToWaitFor, HandleEventCompletion);
            Debug.Log($"Waiting for event: {_currentStep.eventToWaitFor}");
        }
        else if (_currentStep.completionTrigger == CompletionTrigger.OnPlayerAction)
        {
            // TODO: Player actions need to call a method here, or raise an event
            Debug.Log($"Waiting for player action corresponding to: {_currentStep.eventToWaitFor}"); // Using eventToWaitFor as placeholder for action ID
        }
        else if (_currentStep.completionTrigger == CompletionTrigger.Timed && !_currentStep.waitForUIInteraction)
        {
            // Timer handled in Update, will call CompleteAndAdvance
        }
    }

    // Called by external game systems when a relevant event/action occurs
    public void TriggerExternalCompletion(string eventOrActionID)
    {
        if (_currentStep != null &&
            (_currentStep.completionTrigger == CompletionTrigger.OnEvent && _currentStep.eventToWaitFor == eventOrActionID) ||
            (_currentStep.completionTrigger == CompletionTrigger.OnPlayerAction && _currentStep.eventToWaitFor == eventOrActionID)) // Using eventToWaitFor as placeholder for action ID
        {
            Debug.Log($"External trigger '{eventOrActionID}' matched for step '{_currentStep.stepID}'.");
            // TODO: Unsubscribe from event if necessary: EventManager.StopListening(...)
            if (_currentStep.autoAdvanceOnTrigger)
            {
                AdvanceTutorialStep($"Trigger: {eventOrActionID}");
            }
            else
            {
                // Mark as completable, might still need "Next" button if more text to read
                _completedStepIDs.Add(_currentStep.stepID); // Mark complete but wait for button
                uiManager.nextButton.gameObject.SetActive(true); // Show next button
            }
        }
    }


    private void CompleteCurrentSequence()
    {
        if (_currentSequence == null) return;

        Debug.Log($"Tutorial sequence '{_currentSequence.sequenceDisplayName}' completed.");
        _completedSequenceIDs.Add(_currentSequence.sequenceID);
        uiManager?.HideAllTutorialUI();

        // Restore time scale if last step paused it
        if (_currentStep != null && _currentStep.pauseGameForStep && _gameStateManager.currentGameState == GameState.Playing)
        {
             if(Time.timeScale == 0f) Time.timeScale = 1f;
        }

        _currentSequence = null;
        _currentStep = null;
        _currentStepIndex = -1;
        SaveProgress();

        // Check for any chained auto-start sequences
        CheckAndStartAutoSequences(_gameStateManager.currentGameState);
    }

    public void SkipCurrentSequence()
    {
        if (_currentSequence != null && !_currentSequence.isMandatory) // Or a specific canBeSkipped flag
        {
            Debug.Log($"Skipping tutorial sequence: {_currentSequence.sequenceDisplayName}");
            // Mark all steps in sequence as complete to prevent re-triggering if they are prerequisites elsewhere
            foreach (var step in _currentSequence.steps)
            {
                if (!_completedStepIDs.Contains(step.stepID)) _completedStepIDs.Add(step.stepID);
            }
            CompleteCurrentSequence(); // This will also mark sequence as complete
        }
    }

    private void CheckAndStartAutoSequences(GameState forState)
    {
        foreach (var seq in allTutorialSequences)
        {
            if (seq.startAutomatically &&
                !_completedSequenceIDs.Contains(seq.sequenceID) && seq.prerequisiteSequenceIDs.All(prereqID => _completedSequenceIDs.Contains(prereqID)))
            {
                TryStartSequence(seq.sequenceID);
                break; // Start one auto-sequence at a time
            }
        }
    }


    private void SaveProgress()
    {
        PlayerPrefs.SetString(CompletedStepsPlayerPrefsKey, string.Join(",", _completedStepIDs));
        PlayerPrefs.SetString(CompletedSequencesPlayerPrefsKey, string.Join(",", _completedSequenceIDs));
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        if (PlayerPrefs.HasKey(CompletedStepsPlayerPrefsKey))
        {
            string[] ids = PlayerPrefs.GetString(CompletedStepsPlayerPrefsKey).Split(',');
            _completedStepIDs = new HashSet<string>(ids.Where(id => !string.IsNullOrEmpty(id)));
        }
        if (PlayerPrefs.HasKey(CompletedSequencesPlayerPrefsKey))
        {
            string[] ids = PlayerPrefs.GetString(CompletedSequencesPlayerPrefsKey).Split(',');
            _completedSequenceIDs = new HashSet<string>(ids.Where(id => !string.IsNullOrEmpty(id)));
        }
        Debug.Log($"Loaded tutorial progress: {_completedStepIDs.Count} steps, {_completedSequenceIDs.Count} sequences completed.");
    }
}