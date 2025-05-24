// GameSequence.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGameSequence", menuName = "Game Sequence/Game Sequence")]
public class GameSequence : ScriptableObject
{
    public string sequenceID; // Unique ID like "Chapter1_Intro", "WaveSet1", "BossApproach"
    public string sequenceDisplayName;
    public List<GameSequenceStep> steps = new List<GameSequenceStep>();
    public bool autoStartOnGameLoad = false; // Should this sequence try to start when the game/level loads?
    public bool isRepeatable = false;
    public List<string> prerequisiteSequenceIDs = new List<string>(); // IDs of other sequences that must be completed first
}