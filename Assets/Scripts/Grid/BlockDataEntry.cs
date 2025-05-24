using UnityEngine;

[System.Serializable] // Makes it visible in Inspector if part of a List in a MonoBehaviour/ScriptableObject
public class BlockDataEntry
{
    public int gridX;
    public int gridY;
    public string blockID; // A unique identifier for the block type (e.g., prefab name, or a custom ID)
    // Add other serializable properties if needed, e.g.:
    // public float health;
    // public int orientation; // If blocks can be rotated/flipped

    public BlockDataEntry(int x, int y, string id)
    {
        gridX = x;
        gridY = y;
        blockID = id;
    }
}