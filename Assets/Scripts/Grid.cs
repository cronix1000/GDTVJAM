using System;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
public class Grid<TGridObject>
{
    public event Action<int, int, TGridObject> OnGridObjectChanged; // Event for when a cell's value changes

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private TGridObject[,] gridArray;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid<TGridObject>, int, int, TGridObject> createGridObjectCallback)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];

        if (createGridObjectCallback != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridArray[x, y] = createGridObjectCallback(this, x, y);
                }
            }
        }
    }

    // Get world position from grid coordinates (typically cell's bottom-left)
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * cellSize + originPosition;
    }

    // Get grid coordinates from world position
    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    // Check if coordinates are within grid bounds
    public bool IsValid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    // Set the value of a grid cell by grid coordinates
    public void SetValue(int x, int y, TGridObject value)
    {
        if (IsValid(x, y))
        {
            gridArray[x, y] = value;
            OnGridObjectChanged?.Invoke(x, y, value);
        }
    }

    // Set the value of a grid cell by world position
    public void SetValue(Vector3 worldPosition, TGridObject value)
    {
        GetXY(worldPosition, out int x, out int y);
        SetValue(x, y, value);
    }

    // Get the value of a grid cell by grid coordinates
    public TGridObject GetValue(int x, int y)
    {
        return IsValid(x, y) ? gridArray[x, y] : default(TGridObject);
    }

    // Get the value of a grid cell by world position
    public TGridObject GetValue(Vector3 worldPosition)
    {
        GetXY(worldPosition, out int x, out int y);
        return GetValue(x, y);
    }

    // Trigger the change event for a specific cell (useful if TGridObject modifies itself)
    public void TriggerGridObjectChanged(int x, int y)
    {
        if (IsValid(x, y))
        {
            OnGridObjectChanged?.Invoke(x, y, gridArray[x,y]);
        }
    }
}

