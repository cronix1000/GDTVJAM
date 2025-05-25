// GameSequenceManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Sequence;

public class GameSequenceManager : MonoBehaviour
{
    public static GameSequenceManager Instance { get; private set; }

    [Header("Sequence Configuration")]
    public List<GameSequence> allGameSequences; // Assign all your GameSequence SOs here
    public string initialSequenceID; // ID of the sequence to start with if not using autoStartOnGameLoad

    [Header("System References")]
    public CharacterFrameUI storyDisplay; // Assign your Story UI manager
    public EnemySpawnManager enemySpawnManager; // Assign your Enemy Spawn manager
    public GameStateManager gameStateManager; // Assign your GameState Manager
    // public MusicManager musicManager; // Assign if used

    [Header("Boss Timing")]
    public float timeUntilBossSequence = 300f; // 5 minutes example
    public string bossApproachSequenceID; // The ID of the sequence that leads to/spawns the boss
    private FloatScrptableObject bossSpawnTime;
    private bool _bossSequenceTriggered = false;

    private GameSequence _currentSequence;
    private int _currentStepIndex = -1;
    private GameSequenceStep _activeStep;
    private bool _isWaitingForPlayerConfirmation = false;
    private bool _isStepProcessing = false;
    private Coroutine _currentStepCoroutine; // Track the current step coroutine

    private HashSet<string> _completedSequenceIDs = new HashSet<string>();
    private const string CompletedSequencesPrefsKey = "GameSequencesCompleted";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if it needs to persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        

        //LoadCompletedSequences();
        if (storyDisplay && storyDisplay.continueButton) 
        {
            storyDisplay.continueButton.onClick.AddListener(HandlePlayerConfirmation);
        }
    }

    void Start()
    {
        GameSequence autoStartSequence = allGameSequences.FirstOrDefault(s => s.autoStartOnGameLoad && CanStartSequence(s.sequenceID));
        bossSpawnTime.value = 0f; // Initialize the spawn time
        Debug.Log($"Auto-start sequence found: {autoStartSequence?.sequenceID}");
        if (autoStartSequence != null)
        {
            TryStartSequence(autoStartSequence.sequenceID);
        }
        else if (!string.IsNullOrEmpty(initialSequenceID) && CanStartSequence(initialSequenceID))
        {
            TryStartSequence(initialSequenceID);
        }
    }

    void Update()
    {
        if (!_bossSequenceTriggered && gameStateManager && gameStateManager.currentGameState == GameState.Playing)
        {
            bossSpawnTime.value += Time.deltaTime;
            if (bossSpawnTime.value >= timeUntilBossSequence)
            {
                Debug.Log("Time for boss sequence!");
                if (_currentSequence == null || _currentSequence.sequenceID != bossApproachSequenceID) // Avoid restarting if already on it
                {
                   TryStartSequence(bossApproachSequenceID); // TryStartSequence handles prerequisites
                }
                _bossSequenceTriggered = true; // Prevent re-triggering
            }
        }
    }

    public bool CanStartSequence(string sequenceID)
    {
        if (string.IsNullOrEmpty(sequenceID)) return false;
        
        GameSequence sequence = allGameSequences.FirstOrDefault(s => s.sequenceID == sequenceID);
        if (sequence == null) 
        {
            Debug.LogWarning($"Sequence with ID '{sequenceID}' not found in allGameSequences list.");
            return false;
        }
        
        if (!sequence.isRepeatable && _completedSequenceIDs.Contains(sequenceID)) return false;
        return !sequence.prerequisiteSequenceIDs.Any(prereqID => !_completedSequenceIDs.Contains(prereqID));
    }

    public void TryStartSequence(string sequenceID)
    {
        if (!CanStartSequence(sequenceID))
        {
            Debug.LogWarning($"Cannot start sequence: {sequenceID}. Conditions not met or already completed.");
            return;
        }
        
        // Stop current sequence if one is running
        if (_currentSequence != null)
        {
            Debug.LogWarning($"Stopping current sequence {_currentSequence.sequenceID} to start new sequence {sequenceID}.");
            StopCurrentSequence();
        }

        GameSequence sequenceToStart = allGameSequences.FirstOrDefault(s => s.sequenceID == sequenceID);
        if (sequenceToStart == null)
        {
            Debug.LogError($"Sequence with ID '{sequenceID}' not found!");
            return;
        }
        
        _currentSequence = sequenceToStart;
        _currentStepIndex = -1;
        _isStepProcessing = false;
        _isWaitingForPlayerConfirmation = false;
        
        Debug.Log($"--- Starting Game Sequence: {_currentSequence.sequenceDisplayName} ({_currentSequence.sequenceID}) ---");
        ProcessNextStep();
    }

    private void StopCurrentSequence()
    {
        if (_currentStepCoroutine != null)
        {
            StopCoroutine(_currentStepCoroutine);
            _currentStepCoroutine = null;
        }
        
        // Clean up any UI that might be showing
        if (storyDisplay && storyDisplay.Instance)
        {
            storyDisplay.Instance.HideCard();
        }
        
        _currentSequence = null;
        _activeStep = null;
        _currentStepIndex = -1;
        _isStepProcessing = false;
        _isWaitingForPlayerConfirmation = false;
    }

    private void ProcessNextStep()
    {
        if (_isStepProcessing || _currentSequence == null) return; // Prevent re-entry

        _currentStepIndex++;
        if (_currentStepIndex < _currentSequence.steps.Count)
        {
            _activeStep = _currentSequence.steps[_currentStepIndex];
            Debug.Log($"Processing Step [{_currentStepIndex}]: {_activeStep.stepName} (Type: {_activeStep.type})");
            _currentStepCoroutine = StartCoroutine(ExecuteStep(_activeStep));
        }
        else
        {
            CompleteCurrentSequence();
        }
    }

    private IEnumerator ExecuteStep(GameSequenceStep step)
    {
        _isStepProcessing = true;
        _isWaitingForPlayerConfirmation = false;

        switch (step.type)
        {
            case GameStepType.StoryBeat:
                if (storyDisplay && storyDisplay.Instance && step.storyCardData)
                {
                    storyDisplay.Instance.ShowCard(step.storyCardData);
                    if (step.storyCardData.waitForConfirmation)
                    {
                        _isWaitingForPlayerConfirmation = true;
                        // Wait until player confirms
                        while (_isWaitingForPlayerConfirmation) 
                        {
                            yield return null;
                        }
                        storyDisplay.Instance.HideCard();
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(step.storyCardData.displayDuration);
                        storyDisplay.Instance.HideCard();
                    }
                }
                break;

            case GameStepType.ModifySpawner:
                if (enemySpawnManager && enemySpawnManager.Instance && step.spawnerProfile)
                {
                    enemySpawnManager.Instance.ApplyProfileToSpawner(step.targetSpawnerID, step.spawnerProfile);
                    Debug.Log($"Applied profile '{step.spawnerProfile.profileName}' to spawner '{step.targetSpawnerID ?? "all"}'.");
                }
                break;
                
            case GameStepType.ControlSpawner:
                if (enemySpawnManager && enemySpawnManager.Instance)
                {
                    if (step.enableSpawner)
                    {
                        enemySpawnManager.Instance.EnableSpawner(step.targetSpawnerID);
                        Debug.Log($"Enabled spawner '{step.targetSpawnerID ?? "all"}'.");
                    }
                    else
                    {
                        enemySpawnManager.Instance.DisableSpawner(step.targetSpawnerID);
                        Debug.Log($"Disabled spawner '{step.targetSpawnerID ?? "all"}'.");
                    }
                }
                break;
                
            case GameStepType.WaitForDuration:
                if (step.waitDuration > 0)
                {
                    yield return new WaitForSecondsRealtime(step.waitDuration);
                }
                break;

            case GameStepType.SpawnBoss:
                if (enemySpawnManager && step.bossPrefab && step.bossSpawnPoint)
                {
                    if (step.clearOtherEnemiesOnBossSpawn)
                    {
                        enemySpawnManager.ClearAllActiveEnemies();
                    }
                    enemySpawnManager.SpawnBoss(step.bossPrefab, step.bossSpawnPoint);
                    Debug.Log($"Boss '{step.bossPrefab.name}' spawned at {step.bossSpawnPoint.name}.");
                    // if (musicManager && step.bossMusic) musicManager.PlayMusicTrack(step.bossMusic);
                }
                break;

            case GameStepType.CustomEvent:
                if (!string.IsNullOrEmpty(step.eventToRaise))
                {
                    // GlobalEventManager.RaiseEvent(step.eventToRaise); // Assuming you have a global event system
                    Debug.Log($"Custom event raised: {step.eventToRaise}");
                }
                break;

             case GameStepType.EndSequence:
                // This case is mostly handled by CompleteCurrentSequence, which will check nextSequenceID
                break;

            // TODO: Implement ConditionCheck
            case GameStepType.ConditionCheck:
                Debug.LogWarning("ConditionCheck step type not fully implemented yet.");
                break;
        }

        _isStepProcessing = false;
        _currentStepCoroutine = null;
        
        // Only proceed to next step if we're not waiting for confirmation
        if (!_isWaitingForPlayerConfirmation)
        {
            ProcessNextStep();
        }
    }

    private void HandlePlayerConfirmation()
    {
        if (_isWaitingForPlayerConfirmation && _activeStep != null && 
            _activeStep.type == GameStepType.StoryBeat && 
            _activeStep.storyCardData != null && 
            _activeStep.storyCardData.waitForConfirmation)
        {
            _isWaitingForPlayerConfirmation = false;
            // The ExecuteStep coroutine will now exit its while loop and proceed to ProcessNextStep
        }
    }

    private void CompleteCurrentSequence()
    {
        if (_currentSequence == null) return;

        Debug.Log($"--- Game Sequence Completed: {_currentSequence.sequenceDisplayName} ({_currentSequence.sequenceID}) ---");
        
        string sequenceID = _currentSequence.sequenceID;
        if (!_currentSequence.isRepeatable)
        {
            _completedSequenceIDs.Add(sequenceID);
            SaveCompletedSequences();
        }

        string nextSeqID = _currentSequence.steps.LastOrDefault(s => s.type == GameStepType.EndSequence)?.nextSequenceIDToTrigger;

        // Clear current sequence state
        _currentSequence = null;
        _activeStep = null;
        _currentStepIndex = -1;
        _isStepProcessing = false;
        _isWaitingForPlayerConfirmation = false;
        _currentStepCoroutine = null;

        // Start next sequence if specified
        if (!string.IsNullOrEmpty(nextSeqID))
        {
            TryStartSequence(nextSeqID);
        }
    }

    private void SaveCompletedSequences()
    {
        PlayerPrefs.SetString(CompletedSequencesPrefsKey, string.Join(";", _completedSequenceIDs));
        PlayerPrefs.Save();
    }

    private void LoadCompletedSequences()
    {
        if (PlayerPrefs.HasKey(CompletedSequencesPrefsKey))
        {
            string[] ids = PlayerPrefs.GetString(CompletedSequencesPrefsKey).Split(';');
            _completedSequenceIDs = new HashSet<string>(ids.Where(id => !string.IsNullOrEmpty(id)));
        }
        Debug.Log($"Loaded completed game sequences: {_completedSequenceIDs.Count}");
    }

    // Call this if a boss is defeated and a sequence needs to know
    public void ReportBossDefeated()
    {
        Debug.Log("Boss defeated reported to GameSequenceManager.");
        // You might want to trigger a specific sequence here or set a flag
    }
    
    // Utility method to force start a sequence (bypassing prerequisites)
    public void ForceStartSequence(string sequenceID)
    {
        GameSequence sequence = allGameSequences.FirstOrDefault(s => s.sequenceID == sequenceID);
        if (sequence == null)
        {
            Debug.LogError($"Sequence with ID '{sequenceID}' not found!");
            return;
        }
        
        if (_currentSequence != null)
        {
            Debug.LogWarning($"Force stopping current sequence {_currentSequence.sequenceID} to start {sequenceID}.");
            StopCurrentSequence();
        }
        
        _currentSequence = sequence;
        _currentStepIndex = -1;
        _isStepProcessing = false;
        _isWaitingForPlayerConfirmation = false;
        
        Debug.Log($"--- Force Starting Game Sequence: {_currentSequence.sequenceDisplayName} ({_currentSequence.sequenceID}) ---");
        ProcessNextStep();
    }
    
    // Debug method to check current sequence state
    public void LogCurrentSequenceState()
    {
        if (_currentSequence == null)
        {
            Debug.Log("No sequence currently running.");
        }
        else
        {
            Debug.Log($"Current Sequence: {_currentSequence.sequenceID}, Step: {_currentStepIndex}/{_currentSequence.steps.Count - 1}, Processing: {_isStepProcessing}, Waiting: {_isWaitingForPlayerConfirmation}");
        }
    }
}