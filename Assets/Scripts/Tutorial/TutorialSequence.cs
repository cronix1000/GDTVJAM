using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewTutorialSequence", menuName = "Tutorial/Tutorial Sequence")]
public class TutorialSequence : ScriptableObject
{
    public string sequenceID;
    [Tooltip("User-friendly name for the tutorial sequence.")]
    public string sequenceDisplayName;
    [Tooltip("The primary game state this sequence is generally associated with. Steps can override this.")]
    public GameState primaryTargetGameState = GameState.Playing;
    public List<TutorialStep> steps = new List<TutorialStep>();
    public bool startAutomatically = false; // If this sequence should try to start when its conditions are met
    public bool isMandatory = true;
    public List<string> prerequisiteSequenceIDs = new List<string>(); // Other sequences to complete first
}