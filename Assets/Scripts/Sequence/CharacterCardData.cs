// CharacterCardData.cs
using UnityEngine;
using UnityEngine.UI; // For Sprite if you use it directly, or just store path

[CreateAssetMenu(fileName = "NewCharacterCard", menuName = "Game Sequence/Character Card Data")]
public class CharacterCardData : ScriptableObject
{
    public string characterName;
    public Sprite characterPortrait; // Assign character image here
    [TextArea(3, 10)]
    public string dialogueText;
    public AudioClip voiceOverClip; // Optional
    public float displayDuration = 3f; // If not waiting for confirmation
    public bool waitForConfirmation = true; // Player must click to continue
}