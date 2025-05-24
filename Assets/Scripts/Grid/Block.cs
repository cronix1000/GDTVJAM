// Block.cs
using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    [Header("Block Definition")]
    public BlockType blockType;
    public string displayName = "Block";
    [TextArea] public string description = "A standard block.";

    [Header("Connectivity (Which sides of THIS block can connect)")]
    public bool canConnectTop = true;
    public bool canConnectBottom = true;
    public bool canConnectLeft = true;
    public bool canConnectRight = true;

    [Header("Visuals")]
    public SpriteRenderer mainSpriteRenderer; // Assign in prefab
    // Potentially add connector sprites for each edge that can be enabled/disabled

     public PlayerGridCell _gridCell { private set; get; } // Reference to the cell it's placed in

    public void Initialize(BlockType type, PlayerGridCell cell)
    {
        this.blockType = type;
        this._gridCell = cell;
        // You could further customize appearance based on BlockType here
        // e.g., mainSpriteRenderer.sprite = GetSpriteForType(type);
        UpdateVisualsBasedOnConnections();
    }

    public bool CanConnectOnSide(EdgeDirection side)
    {
        switch (side)
        {
            case EdgeDirection.Top: return canConnectTop;
            case EdgeDirection.Right: return canConnectRight;
            case EdgeDirection.Bottom: return canConnectBottom;
            case EdgeDirection.Left: return canConnectLeft;
            default: return false;
        }
    }

    // This method could be called to update visuals if needed,
    // e.g., showing active connection points.
    public void UpdateVisualsBasedOnConnections()
    {
        // TODO: Implement visual changes if desired, like enabling/disabling edge connector sprites
        // For example, if a block is placed next to this one, you might change the edge sprite.
    }

    // Helper to get the opposite direction
    public static EdgeDirection GetOppositeDirection(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.Top: return EdgeDirection.Bottom;
            case EdgeDirection.Right: return EdgeDirection.Left;
            case EdgeDirection.Bottom: return EdgeDirection.Top;
            case EdgeDirection.Left: return EdgeDirection.Right;
            default: throw new System.ArgumentOutOfRangeException(nameof(direction), "Invalid direction");
        }
    }
}
    
    // BlockType.cs
    public enum BlockType
    {
        Attack,
        Defense,
        Support,
        Core
    }
    
    // EdgeDirection.cs
    public enum EdgeDirection
    {
        Top,
        Right,
        Bottom,
        Left
    }
