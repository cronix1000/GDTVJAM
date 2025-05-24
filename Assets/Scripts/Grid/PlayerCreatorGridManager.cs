// PlayerCreatorGridManager.cs
using UnityEngine;
using UnityEngine.Tilemaps; // If you use Tilemaps for highlighting
using System.Collections.Generic;
using System.Linq;
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
    
    [Header("Core Block Settings")]
    public bool requireCoreBlock = true; // Toggle for core block requirement
    public Block coreBlockPrefab; // The core block prefab
    private Block placedCoreBlock; // Reference to the placed core block
    private Vector2Int coreBlockPosition; // Position of the core block in grid coordinates

    [Header("Highlighting")]
    public GameObject highlightValidPrefab;   // Prefab for green highlight
    public GameObject highlightInvalidPrefab; // Prefab for red highlight (optional)
    private List<GameObject> currentHighlights = new List<GameObject>();

    [Header("Player Data (Conceptual)")]
    private List<Block> placedPlayerBlocks = new List<Block>(); // To store the actual player's composition

    public Grid<PlayerGridCell> PlayerGrid => playerGrid;
    public Block PlacedCoreBlock => placedCoreBlock;
    public Vector2Int CoreBlockPosition => coreBlockPosition;
    public bool HasCoreBlock => placedCoreBlock != null;

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

        // Handle core block placement rules
        if (requireCoreBlock)
        {
            bool isPlacingCoreBlock = (blockToPlacePrefab.blockType == BlockType.Core || 
                                     (coreBlockPrefab != null && blockToPlacePrefab == coreBlockPrefab));
            
            // If no core block exists and we're not placing a core block, can't place
            if (!HasCoreBlock && !isPlacingCoreBlock)
            {
                return false;
            }
            
            // If core block exists and we're trying to place another core block, can't place
            if (HasCoreBlock && isPlacingCoreBlock)
            {
                return false;
            }
            
            // If placing the first core block, allow it anywhere
            if (!HasCoreBlock && isPlacingCoreBlock)
            {
                return true;
            }
        }

        // If the grid is completely empty, any block can be placed (if core block is not required)
        bool isFirstBlock = placedPlayerBlocks.Count == 0;
        if (isFirstBlock && !requireCoreBlock)
        {
            return true;
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
        // Auto-select core block if it's required and not placed yet
        if (requireCoreBlock && !HasCoreBlock && coreBlockPrefab != null)
        {
            blockPrefab = coreBlockPrefab;
        }
        
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
                
                // Track core block placement
                if (requireCoreBlock && (blockPrefab.blockType == BlockType.Core || 
                    (coreBlockPrefab != null && blockPrefab == coreBlockPrefab)))
                {
                    placedCoreBlock = newBlockInstance;
                    coreBlockPosition = new Vector2Int(x, y);
                    Debug.Log($"Core block placed at ({x},{y})");
                }

                // Debug.Log($"Placed {newBlockInstance.blockType} at ({x},{y})");

                // Update visuals of neighbors if they need to react to new connection
                UpdateNeighborVisuals(x,y);
            }
        }
    }

    void RemoveBlock(int x, int y)
    {
        if (!playerGrid.IsValid(x,y)) return;

        PlayerGridCell cell = playerGrid.GetValue(x,y);
        if(cell != null && !cell.IsEmpty())
        {
            Block removedBlock = cell.PlacedBlock;
            
            // Check if removing core block
            if (requireCoreBlock && removedBlock == placedCoreBlock)
            {
                // If core block is being removed, check if there are other blocks
                if (placedPlayerBlocks.Count > 1)
                {
                    Debug.LogWarning("Cannot remove core block while other blocks are connected!");
                    return; // Prevent removal
                }
                else
                {
                    placedCoreBlock = null;
                    coreBlockPosition = Vector2Int.zero;
                }
            }
            
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

        if (!currentSelectedBlockPrefab || !highlightValidPrefab)
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
                else if (highlightInvalidPrefab) // Optional: Show invalid spots
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

    // Get blocks positioned relative to the core block
    public List<Block> GetPlayerBlocksRelativeToCore()
    {
        List<Block> relativeBlocks = new List<Block>();
        
        if (!HasCoreBlock)
        {
            return GetPlayerBlocks(); // Return normal list if no core block
        }

        // Add core block first
        relativeBlocks.Add(placedCoreBlock);
        
        // Add other blocks with positions relative to core
        foreach (Block block in placedPlayerBlocks)
        {
            if (block != placedCoreBlock)
            {
                // Get the block's grid position
                PlayerGridCell blockCell = block._gridCell;
                if (blockCell != null)
                {
                    Vector2Int blockGridPos = new Vector2Int(blockCell.X, blockCell.Y);
                    Vector2Int relativePos = blockGridPos - coreBlockPosition;
                    
                    // You can store this relative position in the block or use it for ship assembly
                    // For now, just add to the list
                    relativeBlocks.Add(block);
                }
            }
        }
        
        return relativeBlocks;
    }
    
    private void ClearGrid()
    {
        foreach (Block block in placedPlayerBlocks)
        {
            Destroy(block.gameObject);
        }
        placedPlayerBlocks.Clear();
        placedCoreBlock = null;
        coreBlockPosition = Vector2Int.zero;
        playerGrid.ClearGrid(); // Assuming you have a method to clear the grid
    }

    public void ResetGrid()
    {
        ClearGrid();
        InitializeGridSystem(); // Reinitialize if needed
    }
    
    public void InitializeBuilder(GridConfiguration newGridConfig, Ship shipToLoadFrom)
    {
        if (newGridConfig == null)
        {
            Debug.LogError("InitializeBuilder called with a null GridConfiguration!", this);
            gameObject.SetActive(false); // Deactivate if setup fails
            return;
        }
        this.gridConfig = newGridConfig;
        gameObject.SetActive(true);

        // 1. Re-initialize Grid<PlayerGridCell>
        ReinitializeGridSystemInternal(); // Clears old grid, creates new based on currentActiveGridConfig

        // 2. Load data from the ship (parent) if it has a configuration
        if (shipToLoadFrom != null && shipToLoadFrom.currentBlockConfiguration != null && shipToLoadFrom.currentBlockConfiguration.Count > 0)
        {
            LoadGridContentFromData(shipToLoadFrom.currentBlockConfiguration);
        }
        else
        {
            ClearAllBlocksFromGrid();
            // Place core block if required
            if (requireCoreBlock && coreBlockPrefab != null)
            {
                PlaceBlock(coreBlockPrefab, gridConfig.width / 2, gridConfig.height / 2); // Place in center
            }
        }

        RefreshHighlights();
    }
    
    private void ReinitializeGridSystemInternal()
    {
        if (gridConfig == null) return;

        // Clear existing visual blocks if any (important if re-initializing mid-session)
        ClearAllBlocksFromGridVisualsOnly();

        Vector3 origin = transform.position + gridOriginOffset; // Or however you determine origin for the builder
        playerGrid = new Grid<PlayerGridCell>(
            gridConfig.width,
            gridConfig.height,
            gridConfig.cellSize,
            origin,
            (g, x, y) => new PlayerGridCell(g, x, y) // Your PlayerGridCell factory
        );
        // playerGrid.OnGridObjectChanged += HandleGridObjectChangedForVisuals; // Re-subscribe if you use this
        Debug.Log($"PlayerCreatorGridManager: Grid system re-initialized with config: {gridConfig.name} ({gridConfig.width}x{gridConfig.height})");
    }

    // Loads grid content from a list of BlockDataEntry
    public void LoadGridContentFromData(List<BlockDataEntry> blockEntries)
    {
        if (playerGrid == null)
        {
            Debug.LogError("PlayerGrid not initialized. Cannot load content.", this);
            return;
        }
        ClearAllBlocksFromGrid(); // Clear existing blocks first

        if (blockEntries == null) return;

        // First pass: Place core block if it exists
        if (requireCoreBlock)
        {
            BlockDataEntry coreEntry = blockEntries.FirstOrDefault(entry => 
                GetBlockPrefabByID(entry.blockID)?.blockType == BlockType.Core);
            
            if (coreEntry != null)
            {
                Block coreBlockPrefab = GetBlockPrefabByID(coreEntry.blockID);
                if (coreBlockPrefab != null)
                {
                    PlayerGridCell coreCell = playerGrid.GetValue(coreEntry.gridX, coreEntry.gridY);
                    if (coreCell != null && coreCell.IsEmpty())
                    {
                        PlaceBlockOnGridInternal(coreBlockPrefab, coreEntry.gridX, coreEntry.gridY, coreCell);
                        placedCoreBlock = coreCell.PlacedBlock;
                        coreBlockPosition = new Vector2Int(coreEntry.gridX, coreEntry.gridY);
                    }
                }
            }
        }

        // Second pass: Place other blocks
        foreach (BlockDataEntry entry in blockEntries)
        {
            Block blockPrefab = GetBlockPrefabByID(entry.blockID);
            if (blockPrefab != null && blockPrefab.blockType != BlockType.Core)
            {
                PlayerGridCell cell = playerGrid.GetValue(entry.gridX, entry.gridY);
                if (cell != null && cell.IsEmpty())
                {
                    PlaceBlockOnGridInternal(blockPrefab, entry.gridX, entry.gridY, cell);
                }
                else if(cell == null)
                {
                     Debug.LogWarning($"Cell ({entry.gridX},{entry.gridY}) is outside the current grid dimensions. Cannot place block '{entry.blockID}'.");
                }
                else if(!cell.IsEmpty())
                {
                     Debug.LogWarning($"Cell ({entry.gridX},{entry.gridY}) is already occupied. Cannot place block '{entry.blockID}'.");
                }
            }
        }
        RefreshHighlights();
    }

    // Internal method to actually place a block and update cell data
    private void PlaceBlockOnGridInternal(Block blockPrefab, int x, int y, PlayerGridCell cell)
    {
        Vector3 worldPos = playerGrid.GetWorldPosition(x, y) + new Vector3(playerGrid.CellSize, playerGrid.CellSize) * 0.5f;
        Block newBlockInstance = Instantiate(blockPrefab, worldPos, Quaternion.identity, blockParentTransform); // blockParentTransform for builder
        newBlockInstance.Initialize(blockPrefab.blockType, cell); // Or however your Block initializes
        cell.SetBlock(newBlockInstance); // This should update PlayerGridCell and trigger OnGridObjectChanged
        placedPlayerBlocks.Add(newBlockInstance);
    }

    // Gets the current state of the grid as a list of BlockDataEntry
    public List<BlockDataEntry> GetCurrentGridContentAsData()
    {
        var entries = new List<BlockDataEntry>();
        if (playerGrid == null) return entries;

        for (int x = 0; x < playerGrid.Width; x++)
        {
            for (int y = 0; y < playerGrid.Height; y++)
            {
                PlayerGridCell cell = playerGrid.GetValue(x, y);
                if (cell != null && !cell.IsEmpty() && cell.PlacedBlock != null)
                {
                    // Assume Block.cs has a unique 'blockID' string property or use prefab.name
                    string id = cell.PlacedBlock.name.Replace("(Clone)", "").Trim(); // Or a custom ID field
                    entries.Add(new BlockDataEntry(x, y, id));
                    Debug.Log(id);
                }
            }
        }
        return entries;
    }
    
    public void ClearAllBlocksFromGrid()
    {
        if (playerGrid == null) return;
        for (int x = 0; x < playerGrid.Width; x++)
        {
            for (int y = 0; y < playerGrid.Height; y++)
            {
                PlayerGridCell cell = playerGrid.GetValue(x, y);
                cell?.ClearBlock(); // ClearBlock in PlayerGridCell should destroy GameObject and reset data
            }
        }
        placedPlayerBlocks.Clear();
        placedCoreBlock = null;
        coreBlockPosition = Vector2Int.zero;
        // Any visual representation on a tilemap etc. should also be cleared here.
        RefreshHighlights(); // Usually good to refresh after clearing
    }
    
    private void ClearAllBlocksFromGridVisualsOnly() // If you only want to destroy GameObjects
    {
        if (blockParentTransform != null) // Assuming blockParentTransform holds instantiated blocks
        {
            foreach (Transform child in blockParentTransform)
            {
                Destroy(child.gameObject);
            }
        }
        // Reset core block tracking
        placedCoreBlock = null;
        coreBlockPosition = Vector2Int.zero;
        placedPlayerBlocks.Clear();
        
        // If PlayerGridCell data also needs reset without destroying/recreating PlayerGrid, do that here.
        if (playerGrid != null)
        {
            for (int x = 0; x < playerGrid.Width; x++)
            {
                for (int y = 0; y < playerGrid.Height; y++)
                {
                    PlayerGridCell cell = playerGrid.GetValue(x, y);
                    // cell?.SoftClear(); // A method on PlayerGridCell that resets data but doesn't expect a GO
                }
            }
        }
    }

    public Block GetBlockPrefabByID(string entryBlockID)
    {
        switch (entryBlockID)
        {
            case "Core":
                return availableBlockPrefabs.FirstOrDefault(b => b.blockType == BlockType.Core);
            case "Attack":
                return availableBlockPrefabs.FirstOrDefault(b => b.blockType == BlockType.Attack);
            case "Defense":
                return availableBlockPrefabs.FirstOrDefault(b => b.blockType == BlockType.Defense);
            case "Support":
                return availableBlockPrefabs.FirstOrDefault(b => b.blockType == BlockType.Support);
            default:
                return availableBlockPrefabs.FirstOrDefault(b => b.blockType == BlockType.Attack);
        }
    }
}