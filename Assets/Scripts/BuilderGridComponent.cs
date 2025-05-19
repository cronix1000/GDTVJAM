// BuilderGridComponent.cs
using UnityEngine;

public class BuilderGridComponent : GridComponentBase<MyBuilderCell>
{
    // Example: Reference to prefabs for builder visuals
    public GameObject structureAPrefab;
    public GameObject structureBPrefab;

    protected override MyBuilderCell CreateGridObject(Grid<MyBuilderCell> grid, int x, int y)
    {
        return new MyBuilderCell(grid, x, y);
    }
    
    public void PlaceStructure(Vector3 worldPosition, MyBuilderCell.CellContentType typeToPlace)
    {
        GridSystem.GetXY(worldPosition, out int x, out int y);
        if (GridSystem.IsValid(x,y))
        {
            MyBuilderCell cell = GridSystem.GetValue(x,y);
            if (cell != null && cell.ContentType == MyBuilderCell.CellContentType.Empty) // Check if empty
            {
                GameObject prefabToSpawn = null;
                switch(typeToPlace)
                {
                    case MyBuilderCell.CellContentType.StructureA:
                        prefabToSpawn = structureAPrefab;
                        break;
                    case MyBuilderCell.CellContentType.StructureB:
                        prefabToSpawn = structureBPrefab;
                        break;
                }
                cell.SetContent(typeToPlace, prefabToSpawn);
            }
        }
    }

    public void ClearCell(Vector3 worldPosition)
    {
        GridSystem.GetXY(worldPosition, out int x, out int y);
        if (GridSystem.IsValid(x,y))
        {
            MyBuilderCell cell = GridSystem.GetValue(x,y);
            cell?.SetContent(MyBuilderCell.CellContentType.Empty);
        }
    }
}