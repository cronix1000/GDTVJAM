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
    private float _gameTimerForBoss = 0f;
    private bool _bossSequenceTriggered = false;

    private GameSequence _currentSequence;
    private int _currentStepIndex = -1;
    private GameSequenceStep _activeStep;
    private bool _isWaitingForPlayerConfirmation = false;
    private bool _isStepProcessing = false;

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

        LoadCompletedSequences();
        if (storyDisplay) storyDisplay.continueButton.onClick.AddListener(HandlePlayerConfirmation);
    }

    void Start()
    {
        GameSequence autoStartSequence = allGameSequences.FirstOrDefault(s => s.autoStartOnGameLoad && CanStartSequence(s.sequenceID));
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
        if (!_bossSequenceTriggered && gameStateManager.currentGameState == GameState.Playing)
        {
            _gameTimerForBoss += Time.deltaTime;
            if (_gameTimerForBoss >= timeUntilBossSequence)
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
        GameSequence sequence = allGameSequences.FirstOrDefault(s => s.sequenceID == sequenceID);
        if (sequence == null) return false;
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
        if (_currentSequence != null)
        {
            Debug.LogWarning($"Trying to start sequence {sequenceID}, but sequence {_currentSequence.sequenceID} is already active. (Not interrupting for now)");
            // Add logic here if some sequences can interrupt others.
            return;
        }

        GameSequence sequenceToStart = allGameSequences.FirstOrDefault(s => s.sequenceID == sequenceID);
        _currentSequence = sequenceToStart;
        _currentStepIndex = -1;
        _isStepProcessing = false; // Ensure it's reset
        Debug.Log($"--- Starting Game Sequence: {_currentSequence.sequenceDisplayName} ({_currentSequence.sequenceID}) ---");
        ProcessNextStep();
    }

    private void ProcessNextStep()
    {
        if (_isStepProcessing || _currentSequence == null) return; // Prevent re-entry

        _currentStepIndex++;
        if (_currentStepIndex < _currentSequence.steps.Count)
        {
            _activeStep = _currentSequence.steps[_currentStepIndex];
            Debug.Log($"Processing Step [{_currentStepIndex}]: {_activeStep.stepName} (Type: {_activeStep.type})");
            StartCoroutine(ExecuteStep(_activeStep));
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
                if (storyDisplay && step.storyCardData)
                {
                    storyDisplay.Instance.ShowCard(step.storyCardData);
                    if (step.storyCardData.waitForConfirmation)
                    {
                        _isWaitingForPlayerConfirmation = true;
                        // Wait until player confirms
                        while (_isWaitingForPlayerConfirmation) yield return null;
                        storyDisplay.Instance.HideCard();
                    }
                    else
                    {
                        yield return new WaitForSeconds(step.storyCardData.displayDuration);
                        storyDisplay.HideCard();
                    }
                }
                break;

            case GameStepType.ModifySpawner:
                if (enemySpawnManager && step.spawnerProfile)
                {
                    enemySpawnManager.Instance.ApplyProfileToSpawner(step.targetSpawnerID, step.spawnerProfile);
                    Debug.Log($"Applied profile '{step.spawnerProfile.profileName}' to spawner '{step.targetSpawnerID ?? "all"}'.");
                }
                break;
            case GameStepType.WaitForDuration:
                if (step.waitDuration > 0)
                {
                    yield return new WaitForSeconds(step.waitDuration);
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

                    // Potentially wait for boss defeat? Or handle that in a subsequent ConditionCheck step
                    // For now, boss spawn itself is an event.
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

        // If not waiting for confirmation from a story beat that was handled above
        if (!_isWaitingForPlayerConfirmation)
        {
            _isStepProcessing = false;
            ProcessNextStep();
        }
    }

    private void HandlePlayerConfirmation()
    {
        if (_activeStep != null && _activeStep.type == GameStepType.StoryBeat && _activeStep.storyCardData.waitForConfirmation)
        {
            if (_isWaitingForPlayerConfirmation) // Ensure this is the confirmation we are waiting for
            {
                 _isWaitingForPlayerConfirmation = false;
                 // The coroutine ExecuteStep will now break its wait loop and proceed
                 // Or, if coroutine already finished and was just waiting for this flag:
                 if (_isStepProcessing) // if the coroutine for this step is still marked as processing (it should be if waiting)
                 {
                    // Stop the specific coroutine if it was only waiting, or let it finish and then advance.
                    // A simpler way is for the coroutine to yield until this flag is false.
                    // Once player confirms, the coroutine finishes its 'while' loop,
                    // then _isStepProcessing is set to false and ProcessNextStep is called.
                    // For immediate advancement after player confirmation:
                    // _isStepProcessing = false;
                    // ProcessNextStep();
                 }
            }
        }
    }

    private void CompleteCurrentSequence()
    {
        if (_currentSequence == null) return;

        Debug.Log($"--- Game Sequence Completed: {_currentSequence.sequenceDisplayName} ({_currentSequence.sequenceID}) ---");
        if (!_currentSequence.isRepeatable)
        {
            _completedSequenceIDs.Add(_currentSequence.sequenceID);
            SaveCompletedSequences();
        }

        string nextSeqID = _currentSequence.steps.LastOrDefault(s => s.type == GameStepType.EndSequence)?.nextSequenceIDToTrigger;

        _currentSequence = null;
        _activeStep = null;
        _currentStepIndex = -1;
        _isStepProcessing = false;


        if (!string.IsNullOrEmpty(nextSeqID))
        {
            TryStartSequence(nextSeqID);
        }
        else
        {
            // No next sequence chained, check for any other auto-start sequences
            // (though this might be redundant if Update loop or GameStateChange handles it)
            // if (gameStateManager != null) CheckAndStartAutoSequences(gameStateManager.currentGameState);
        }
    }


    private void SaveCompletedSequences()
    {
        PlayerPrefs.SetString(CompletedSequencesPrefsKey, string.Join(";", _completedSequenceIDs)); // Use a different separator
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

    }
}