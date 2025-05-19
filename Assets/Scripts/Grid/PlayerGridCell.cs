// PlayerGridCell.cs
using UnityEngine;

public class PlayerGridCell
{
    public Grid<PlayerGridCell> ParentGrid { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public Block PlacedBlock { get; private set; }

    public PlayerGridCell(Grid<PlayerGridCell> parentGrid, int x, int y)
    {
        ParentGrid = parentGrid;
        X = x;
        Y = y;
    }

    public void SetBlock(Block blockInstance)
    {
        if (PlacedBlock != null && PlacedBlock != blockInstance)
        {
            Object.Destroy(PlacedBlock.gameObject); // Clean up old block
        }
        PlacedBlock = blockInstance;
        ParentGrid.TriggerGridObjectChanged(X, Y);
    }

    public void ClearBlock()
    {
        if (PlacedBlock != null)
        {
            Object.Destroy(PlacedBlock.gameObject);
            PlacedBlock = null;
        }
        ParentGrid.TriggerGridObjectChanged(X, Y);
    }

    public bool IsEmpty()
    {
        return PlacedBlock == null;
    }

    public override string ToString()
    {
        return IsEmpty() ? "Empty" : PlacedBlock.blockType.ToString();
    }
}