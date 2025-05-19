// MyBuilderCell.cs
// Example object to store in the grid for a builder

using UnityEngine;

public class MyBuilderCell
{
    private Grid<MyBuilderCell> grid; // Reference to the parent grid
    private int x, y;

    public enum CellContentType { Empty, StructureA, StructureB }
    public CellContentType ContentType { get; private set; }
    public GameObject PlacedGameObject { get; private set; } // Reference to the instantiated visual

    public MyBuilderCell(Grid<MyBuilderCell> grid, int x, int y)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
        this.ContentType = CellContentType.Empty;
    }

    public void SetContent(CellContentType newType, GameObject visualPrefab = null)
    {
        // Clear old visual if any
        if (PlacedGameObject != null)
        {
            UnityEngine.Object.Destroy(PlacedGameObject);
            PlacedGameObject = null;
        }

        this.ContentType = newType;

        if (visualPrefab != null && newType != CellContentType.Empty)
        {
            Vector3 spawnPosition = grid.GetWorldPosition(x, y) + new Vector3(grid.CellSize, grid.CellSize) * 0.5f; // Center of cell
            PlacedGameObject = UnityEngine.Object.Instantiate(visualPrefab, spawnPosition, Quaternion.identity);
            // Optionally parent to the grid component's transform
        }
        grid.TriggerGridObjectChanged(x,y); // Notify grid that this cell has changed
    }

    public override string ToString()
    {
        return $"{ContentType}";
    }
}