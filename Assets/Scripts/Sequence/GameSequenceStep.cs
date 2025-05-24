// GameSequenceStep.cs (No need for a separate file if it's simple and only used by GameSequence)
// For organization, you might put enums in their own files.
using UnityEngine;

public enum GameStepType
{
    StoryBeat,          // Show character card with dialogue
    ModifySpawner,      // Change enemy spawner behavior/profile
    ControlSpawner,     // Enable/Disable spawner(s)
    WaitForDuration,    // Pause sequence progression for a time
    SpawnBoss,          // Trigger a boss spawn
    ConditionCheck,     // (Advanced) Wait for a game condition
    CustomEvent,        // Trigger a custom game event
    EndSequence         // Marks the end of this sequence
}

[System.Serializable]
public class GameSequenceStep
{
    public string stepName; // For editor readability
    public GameStepType type = GameStepType.StoryBeat;

    [Header("Story Beat Details")]
    [Tooltip("Data for character portrait, name, and dialogue.")]
    public CharacterCardData storyCardData;

    [Header("Spawner Modification")]
    [Tooltip("The new profile to apply to targeted spawners.")]
    public EnemySpawnerProfile spawnerProfile;
    [Tooltip("ID of the spawner to modify. Leave empty to affect all/default spawner.")]
    public string targetSpawnerID; // You might have an array for multiple spawners

    [Header("Spawner Control")]
    public bool enableSpawner = true; // Used with ControlSpawner type

    [Header("Wait Details")]
    [Tooltip("Duration in seconds to wait before proceeding.")]
    public float waitDuration = 5f;

    [Header("Boss Spawn Details")]
    public GameObject bossPrefab; // Assign your boss prefab
    public Transform bossSpawnPoint; // Assign a transform in the scene where the boss spawns
    public bool clearOtherEnemiesOnBossSpawn = true;
    // public AudioClip bossMusic; // Optional: trigger music directly

    [Header("Custom Event")]
    public string eventToRaise; // String identifier for a custom event

    [Header("End Sequence Options")]
    public string nextSequenceIDToTrigger; // Chain to another sequence
}