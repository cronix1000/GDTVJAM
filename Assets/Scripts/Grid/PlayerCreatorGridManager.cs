// PlayerCreatorGridManager.cs
using UnityEngine;
using UnityEngine.Tilemaps; // If you use Tilemaps for highlighting
using System.Collections.Generic;
using UnityEngine.EventSystems; // To ignore UI clicks

public class PlayerCreatorGridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public GridConfiguration gridConfig; // Assign your ScriptableObject config
    public Vector3 gridOriginOffset = Vector3.zero;
    private Grid<PlayerGridCell> playerGrid;

    [Header("Block Management")]
    public Block[] availableBlockPrefabs; // Assign all your block prefabs here
    public Block currentSelectedBlockPrefab; // Set this via UI
    public Transform blockParentTransform; // Parent for instantiated blocks (for organization)

    [Header("Highlighting")]
    public GameObject highlightValidPrefab;   // Prefab for green highlight
    public GameObject highlightInvalidPrefab; // Prefab for red highlight (optional)
    private List<GameObject> currentHighlights = new List<GameObject>();

    [Header("Player Data (Conceptual)")]
    private List<Block> placedPlayerBlocks = new List<Block>(); // To store the actual player's composition

    public Grid<PlayerGridCell> PlayerGrid => playerGrid;

    void Awake()
    {
        InitializeGridSystem();
    }

    void Update()
    {
        HandleMouseInput();
        UpdateHighlights(); // Continuously update highlights based on selected block
    }

    public void InitializeGridSystem()
    {
        if (gridConfig == null)
        {
            Debug.LogError("GridConfiguration not assigned!");
            return;
        }
        Vector3 origin = transform.position + gridOriginOffset;
        playerGrid = new Grid<PlayerGridCell>(
            gridConfig.width,
            gridConfig.height,
            gridConfig.cellSize,
            origin,
            (g, x, y) => new PlayerGridCell(g, x, y) // Factory function for PlayerGridCell
        );
        playerGrid.OnGridObjectChanged += (x, y, cell) => RefreshHighlights(); // Refresh highlights if grid changes
        Debug.Log($"Player Creator Grid Initialized: {playerGrid.Width}x{playerGrid.Height}");
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Prevent placement if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (currentSelectedBlockPrefab == null)
            {
                // Debug.Log("No block selected for placement.");
                return;
            }

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // Ensure it's on the 2D plane

            playerGrid.GetXY(mouseWorldPos, out int x, out int y);

            if (playerGrid.IsValid(x, y))
            {
                PlaceBlock(currentSelectedBlockPrefab, x, y);
            }
        }
        else if (Input.GetMouseButtonDown(1)) // Right-click to remove block
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            playerGrid.GetXY(mouseWorldPos, out int x, out int y);
            RemoveBlock(x,y);
        }
    }

    public void SelectBlockPrefabForPlacement(Block blockPrefab)
    {
        currentSelectedBlockPrefab = blockPrefab;
        RefreshHighlights(); // Update highlights when selection changes
    }


    bool CanPlaceBlock(Block blockToPlacePrefab, int targetX, int targetY)
    {
        if (!playerGrid.IsValid(targetX, targetY)) return false;

        PlayerGridCell targetCell = playerGrid.GetValue(targetX, targetY);
        if (targetCell == null || !targetCell.IsEmpty()) return false; // Cell is not valid or already occupied

        // If the grid is completely empty, any block (usually a "Core" block) can be placed.
        // For this system, we might require connecting to an existing block unless it's a special starting block.
        bool isFirstBlock = placedPlayerBlocks.Count == 0;

        if (isFirstBlock)
        {
            // Rule: First block can be of type Core, or any if no Core type exists/is enforced
            return blockToPlacePrefab.blockType == BlockType.Attack || availableBlockPrefabs.Length > 0; // Or some other logic for the first piece
        }

        // Check for at least one valid connection to an adjacent block
        bool canConnectToAnyNeighbor = false;
        EdgeDirection[] directions = (EdgeDirection[])System.Enum.GetValues(typeof(EdgeDirection));

        foreach (EdgeDirection dir in directions)
        {
            GetNeighborCoords(targetX, targetY, dir, out int neighborX, out int neighborY);

            if (playerGrid.IsValid(neighborX, neighborY))
            {
                PlayerGridCell neighborCell = playerGrid.GetValue(neighborX, neighborY);
                if (neighborCell != null && !neighborCell.IsEmpty())
                {
                    Block neighborBlock = neighborCell.PlacedBlock;
                    EdgeDirection oppositeDir = Block.GetOppositeDirection(dir);

                    // Check if the block we want to place can connect on its side facing the neighbor,
                    // AND if the neighbor can connect on its side facing the block we want to place.
                    if (blockToPlacePrefab.CanConnectOnSide(dir) && neighborBlock.CanConnectOnSide(oppositeDir))
                    {
                        canConnectToAnyNeighbor = true;
                        break; // Found a valid connection
                    }
                }
            }
        }
        return canConnectToAnyNeighbor;
    }

    void PlaceBlock(Block blockPrefab, int x, int y)
    {
        if (CanPlaceBlock(blockPrefab, x, y))
        {
            PlayerGridCell cell = playerGrid.GetValue(x, y);
            if (cell != null && cell.IsEmpty())
            {
                Vector3 worldPos = playerGrid.GetWorldPosition(x, y) + new Vector3(playerGrid.CellSize, playerGrid.CellSize) * 0.5f; // Center of cell
                Block newBlockInstance = Instantiate(blockPrefab, worldPos, Quaternion.identity, blockParentTransform);
                newBlockInstance.Initialize(blockPrefab.blockType, cell); // Pass cell reference

                cell.SetBlock(newBlockInstance);
                placedPlayerBlocks.Add(newBlockInstance);
                // Debug.Log($"Placed {newBlockInstance.blockType} at ({x},{y})");

                // Update visuals of neighbors if they need to react to new connection
                UpdateNeighborVisuals(x,y);
            }
        }
        else
        {
            // Debug.LogWarning($"Cannot place {blockPrefab.blockType} at ({x},{y}). Invalid location or connection.");
            // Optionally provide feedback to the player (e.g., sound effect)
        }
    }

    void RemoveBlock(int x, int y)
    {
        if (!playerGrid.IsValid(x,y)) return;

        PlayerGridCell cell = playerGrid.GetValue(x,y);
        if(cell != null && !cell.IsEmpty())
        {
            Block removedBlock = cell.PlacedBlock;
            placedPlayerBlocks.Remove(removedBlock);
            cell.ClearBlock(); // This will destroy the GameObject and notify grid
            // Debug.Log($"Removed block at ({x},{y})");

            // Update visuals of neighbors as a connection is now lost
            UpdateNeighborVisuals(x,y);
        }
    }

    void UpdateNeighborVisuals(int x, int y)
    {
        EdgeDirection[] directions = (EdgeDirection[])System.Enum.GetValues(typeof(EdgeDirection));
        foreach (EdgeDirection dir in directions)
        {
            GetNeighborCoords(x, y, dir, out int neighborX, out int neighborY);
            if (playerGrid.IsValid(neighborX, neighborY))
            {
                PlayerGridCell neighborCell = playerGrid.GetValue(neighborX, neighborY);
                neighborCell?.PlacedBlock?.UpdateVisualsBasedOnConnections();
            }
        }
    }


    void ClearHighlights()
    {
        foreach (GameObject highlight in currentHighlights)
        {
            Destroy(highlight);
        }
        currentHighlights.Clear();
    }

    public void RefreshHighlights() // Public so UI can call it if needed
    {
        UpdateHighlights();
    }

    void UpdateHighlights()
    {
        ClearHighlights();

        if (currentSelectedBlockPrefab == null || highlightValidPrefab == null)
        {
            return;
        }

        for (int x = 0; x < playerGrid.Width; x++)
        {
            for (int y = 0; y < playerGrid.Height; y++)
            {
                if (CanPlaceBlock(currentSelectedBlockPrefab, x, y))
                {
                    Vector3 pos = playerGrid.GetWorldPosition(x, y) + new Vector3(playerGrid.CellSize, playerGrid.CellSize) * 0.5f;
                    GameObject highlight = Instantiate(highlightValidPrefab, pos, Quaternion.identity, transform); // Parent to manager for cleanup
                    currentHighlights.Add(highlight);
                }
                else if (highlightInvalidPrefab != null) // Optional: Show invalid spots
                {
                    PlayerGridCell cell = playerGrid.GetValue(x, y);
                    if(cell != null && cell.IsEmpty()){ // Only show invalid on empty cells where we MIGHT want to place
                        // More complex logic could be here if invalid highlight is too noisy
                        // For now, let's skip invalid highlights unless specifically designed
                    }
                }
            }
        }
    }

    void GetNeighborCoords(int x, int y, EdgeDirection direction, out int neighborX, out int neighborY)
    {
        neighborX = x;
        neighborY = y;
        switch (direction)
        {
            case EdgeDirection.Top:    neighborY = y + 1; break;
            case EdgeDirection.Right:  neighborX = x + 1; break;
            case EdgeDirection.Bottom: neighborY = y - 1; break;
            case EdgeDirection.Left:   neighborX = x - 1; break;
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (Application.isPlaying && playerGrid != null)
        {
            DrawGridGizmosInternal(playerGrid.Width, playerGrid.Height, playerGrid.CellSize, playerGrid.GetWorldPosition(0, 0));
        }
        else if (gridConfig != null) // Preview in editor if config is set
        {
            DrawGridGizmosInternal(gridConfig.width, gridConfig.height, gridConfig.cellSize, transform.position + gridOriginOffset);
        }
    }

    private void DrawGridGizmosInternal(int w, int h, float cs, Vector3 org)
    {
        Gizmos.color = Color.gray;
        for (int x = 0; x <= w; x++) // Draw vertical lines
        {
            Gizmos.DrawLine(org + new Vector3(x * cs, 0, 0), org + new Vector3(x * cs, h * cs, 0));
        }
        for (int y = 0; y <= h; y++) // Draw horizontal lines
        {
            Gizmos.DrawLine(org + new Vector3(0, y * cs, 0), org + new Vector3(w * cs, y * cs, 0));
        }
    }

    // Call this when the player finalizes their creation
    public List<Block> GetPlayerBlocks()
    {
        return new List<Block>(placedPlayerBlocks); // Return a copy
    }
}