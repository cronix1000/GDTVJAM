// GridComponentBase.cs
using UnityEngine;
using System;

public abstract class GridComponentBase<TGridObject> : MonoBehaviour
{
    [Header("Grid Configuration Asset")]
    [SerializeField] private GridConfiguration gridConfiguration;

    [Header("Grid Settings (Overrides Config if set, or uses default)")]
    [SerializeField] private bool overrideConfigDimensions = false;
    [SerializeField] private int customWidth = 10;
    [SerializeField] private int customHeight = 10;
    [SerializeField] private bool overrideConfigCellSize = false;
    [SerializeField] private float customCellSize = 1f;
    [SerializeField] private Vector3 gridOriginOffset = Vector3.zero; // Offset from this GameObject's position

    public Grid<TGridObject> GridSystem { get; protected set; }

    protected virtual void Awake()
    {
        InitializeGrid();
    }

    public virtual void InitializeGrid()
    {
        int width = gridConfiguration != null && !overrideConfigDimensions ? gridConfiguration.width : customWidth;
        int height = gridConfiguration != null && !overrideConfigDimensions ? gridConfiguration.height : customHeight;
        float cellSize = gridConfiguration != null && !overrideConfigCellSize ? gridConfiguration.cellSize : customCellSize;
        Vector3 origin = transform.position + gridOriginOffset;

        GridSystem = new Grid<TGridObject>(width, height, cellSize, origin, CreateGridObject);

        GridSystem.OnGridObjectChanged += HandleGridObjectChanged;
    }

    protected abstract TGridObject CreateGridObject(Grid<TGridObject> grid, int x, int y);

    protected virtual void HandleGridObjectChanged(int x, int y, TGridObject newObject)
    {
         Debug.Log($"Grid object at ({x},{y}) changed to: {newObject}");
    }

    protected virtual void OnDrawGizmos()
    {
        if (Application.isPlaying && GridSystem != null)
        {
            DrawGridGizmos(GridSystem.Width, GridSystem.Height, GridSystem.CellSize, GridSystem.GetWorldPosition(0,0) - gridOriginOffset);
        }
        else // Preview in editor
        {
            int prevWidth = gridConfiguration != null && !overrideConfigDimensions ? gridConfiguration.width : customWidth;
            int prevHeight = gridConfiguration != null && !overrideConfigDimensions ? gridConfiguration.height : customHeight;
            float prevCellSize = gridConfiguration != null && !overrideConfigCellSize ? gridConfiguration.cellSize : customCellSize;
            DrawGridGizmos(prevWidth, prevHeight, prevCellSize, transform.position + gridOriginOffset);
        }
    }

    private void DrawGridGizmos(int w, int h, float cs, Vector3 org)
    {
        Gizmos.color = Color.cyan;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Gizmos.DrawLine(org + new Vector3(x, y) * cs, org + new Vector3(x, y + 1) * cs);
                Gizmos.DrawLine(org + new Vector3(x, y) * cs, org + new Vector3(x + 1, y) * cs);
            }
        }
        Gizmos.DrawLine(org + new Vector3(0, h) * cs, org + new Vector3(w, h) * cs);
        Gizmos.DrawLine(org + new Vector3(w, 0) * cs, org + new Vector3(w, h) * cs);
    }
}