using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    public List<Transform> AttackBlocks;
    public List<Transform> DefenseBlocks;
    public List<Transform> UtilityBlocks;
    public List<BlockDataEntry> currentBlockConfiguration = new List<BlockDataEntry>();
    public Transform blockContainer;
    
    [Header("Core Block Settings")]
    public Transform coreBlock; // Reference to the core block
    public Vector3 coreBlockOffset = Vector3.zero; // Optional offset for core block positioning

    public int blockCount = 0;

    [SerializeField]
    private MonoBehaviour[]
        gameplayComponentsToDisable; // Assign PlayerInput, ShipMovement, ShipShooting etc. in Inspector

    public bool IsConsideredLarge(float sizeThreshold)
    {
        // Example: based on number of blocks.
        // You could use mass, volume, or any other metric.
        return blockCount >= sizeThreshold;
    }

    public void AddBlock(Transform block, BlockType type)
    {
        switch (type)
        {
            case BlockType.Core:
                coreBlock = block;
                break;
            case BlockType.Attack:
                AttackBlocks.Add(block);
                break;
            case BlockType.Defense:
                DefenseBlocks.Add(block);
                break;
            case BlockType.Support:
                UtilityBlocks.Add(block);
                break;
        }

        blockCount++;
    }

    public void SetGameplayComponentsActive(bool isActive)
    {
        foreach (var component in gameplayComponentsToDisable)
        {
            if (component != null)
            {
                component.enabled = isActive;
            }
        }
    }

    public void UpdateBlockCount(int newCount)
    {
        blockCount = newCount;
    }

    public void UpdateShipConfigurationData(List<BlockDataEntry> newConfiguration)
    {
        currentBlockConfiguration = new List<BlockDataEntry>(newConfiguration); // Store a copy
        Debug.Log($"{name}: Internal block configuration data updated with {currentBlockConfiguration.Count} blocks.");

        // After updating data, you might want to immediately reflect this on the physical ship
        // if the builder wasn't modifying the live ship directly.
        // ApplyConfigurationToPhysicalShip();
    }

    public void ApplyConfigurationToPhysicalShip(PlayerCreatorGridManager gridManagerForPrefabs) 
    {
        if (blockContainer == null)
        {
            Debug.LogError($"{name}: Block Container not set! Cannot apply configuration.", this);
            return;
        }

        // 1. Clear existing physical blocks
        foreach (Transform child in blockContainer)
        {
            Destroy(child.gameObject);
        }

        // Clear block lists
        AttackBlocks.Clear();
        DefenseBlocks.Clear();
        UtilityBlocks.Clear();
        coreBlock = null;

        if (currentBlockConfiguration == null) return;

        // 2. Find core block position first
        BlockDataEntry coreEntry = null;
        Vector3 coreWorldPosition = Vector3.zero;
        
        foreach (BlockDataEntry entry in currentBlockConfiguration)
        {
            Block blockPrefab = gridManagerForPrefabs.GetBlockPrefabByID(entry.blockID);
            if (blockPrefab != null && blockPrefab.blockType == BlockType.Core)
            {
                coreEntry = entry;
                coreWorldPosition = gridManagerForPrefabs.PlayerGrid.GetWorldPosition(entry.gridX, entry.gridY);
                break;
            }
        }

        // 3. Instantiate blocks with positions relative to core block
        foreach (BlockDataEntry entry in currentBlockConfiguration)
        {
            Debug.Log($"Block Name: {entry.blockID}, at ({entry.gridX},{entry.gridY})");
            Block blockPrefab = gridManagerForPrefabs.GetBlockPrefabByID(entry.blockID);
            if (blockPrefab != null)
            {
                Vector3 blockWorldPos = gridManagerForPrefabs.PlayerGrid.GetWorldPosition(entry.gridX, entry.gridY);
                Vector3 relativePosition;

                if (coreEntry != null)
                {
                    // Position relative to core block
                    relativePosition = blockWorldPos - coreWorldPosition + coreBlockOffset;
                }
                else
                {
                    // No core block, use original position
                    relativePosition = gridManagerForPrefabs.PlayerGrid.GetLocalPosition(entry.gridX, entry.gridY);
                }

                Block newBlockInstance = Instantiate(blockPrefab, blockContainer);
                newBlockInstance.transform.localPosition = relativePosition;
                
                // Add to appropriate list and track core block
                AddBlock(newBlockInstance.transform, blockPrefab.blockType);
            }
            else
            {
                Debug.LogWarning(
                    $"{name}: Could not find block prefab for ID '{entry.blockID}' at ({entry.gridX},{entry.gridY})");
            }
        }

        Debug.Log($"{name}: Physical ship configuration applied with {currentBlockConfiguration.Count} blocks.");
        
        // Update block count
        UpdateBlockCount(currentBlockConfiguration.Count);
    }

    // Alternative method that uses the grid manager's core block tracking
    public void ApplyConfigurationToPhysicalShipWithCoreTracking(PlayerCreatorGridManager gridManager) 
    {
        if (blockContainer == null)
        {
            Debug.LogError($"{name}: Block Container not set! Cannot apply configuration.", this);
            return;
        }

        // 1. Clear existing physical blocks
        foreach (Transform child in blockContainer)
        {
            Destroy(child.gameObject);
        }

        // Clear block lists
        AttackBlocks.Clear();
        DefenseBlocks.Clear();
        UtilityBlocks.Clear();
        coreBlock = null;

        if (currentBlockConfiguration == null) return;

        // 2. Get core block position from grid manager
        Vector3 coreWorldPosition = Vector3.zero;
        bool hasCoreBlock = gridManager.HasCoreBlock;
        
        if (hasCoreBlock)
        {
            Vector2Int coreGridPos = gridManager.CoreBlockPosition;
            coreWorldPosition = gridManager.PlayerGrid.GetWorldPosition(coreGridPos.x, coreGridPos.y);
        }

        // 3. Instantiate blocks with positions relative to core block
        foreach (BlockDataEntry entry in currentBlockConfiguration)
        {
            Debug.Log($"Block Name: {entry.blockID}, at ({entry.gridX},{entry.gridY})");
            Block blockPrefab = gridManager.GetBlockPrefabByID(entry.blockID);
            if (blockPrefab != null)
            {
                Vector3 blockWorldPos = gridManager.PlayerGrid.GetWorldPosition(entry.gridX, entry.gridY);
                Vector3 relativePosition;

                if (hasCoreBlock)
                {
                    // Position relative to core block
                    relativePosition = blockWorldPos - coreWorldPosition + coreBlockOffset;
                }
                else
                {
                    // No core block, use original position
                    relativePosition = gridManager.PlayerGrid.GetLocalPosition(entry.gridX, entry.gridY);
                }

                Block newBlockInstance = Instantiate(blockPrefab, blockContainer);
                newBlockInstance.transform.localPosition = relativePosition;
                
                // Add to appropriate list and track core block
                AddBlock(newBlockInstance.transform, blockPrefab.blockType);
            }
            else
            {
                Debug.LogWarning(
                    $"{name}: Could not find block prefab for ID '{entry.blockID}' at ({entry.gridX},{entry.gridY})");
            }
        }

        Debug.Log($"{name}: Physical ship configuration applied with {currentBlockConfiguration.Count} blocks.");
        
        // Update block count
        UpdateBlockCount(currentBlockConfiguration.Count);
    }

    // Method to get all blocks positioned relative to the core block
    public List<Transform> GetAllBlocksRelativeToCore()
    {
        List<Transform> allBlocks = new List<Transform>();
        
        // Add core block first if it exists
        if (coreBlock != null)
        {
            allBlocks.Add(coreBlock);
        }
        
        // Add other blocks
        allBlocks.AddRange(AttackBlocks);
        allBlocks.AddRange(DefenseBlocks);
        allBlocks.AddRange(UtilityBlocks);
        
        return allBlocks;
    }

    // Method to center the ship around its core block
    public void CenterShipAroundCore()
    {
        if (coreBlock == null || blockContainer == null) return;
        
        // Calculate the offset needed to center the core block at the container's origin
        Vector3 coreLocalPos = coreBlock.localPosition;
        Vector3 offset = -coreLocalPos;
        
        // Apply offset to all blocks
        foreach (Transform child in blockContainer)
        {
            child.localPosition += offset;
        }
        
        Debug.Log($"{name}: Ship centered around core block with offset {offset}");
    }

    // Method to get the ship's center of mass based on block positions
    public Vector3 GetCenterOfMass()
    {
        if (blockContainer == null || blockContainer.childCount == 0)
            return transform.position;
        
        Vector3 centerOfMass = Vector3.zero;
        int blockCount = 0;
        
        foreach (Transform child in blockContainer)
        {
            centerOfMass += child.position;
            blockCount++;
        }
        
        if (blockCount > 0)
        {
            centerOfMass /= blockCount;
        }
        
        return centerOfMass;
    }

    // Method to validate ship configuration (e.g., ensure core block exists)
    public bool ValidateShipConfiguration()
    {
        bool isValid = true;
        
        // Check if core block exists when required
        if (coreBlock == null && currentBlockConfiguration.Count > 0)
        {
            Debug.LogWarning($"{name}: Ship configuration missing core block!");
            isValid = false;
        }
        
        // Check if all blocks are connected (this would require more complex logic)
        // For now, just check basic requirements
        
        return isValid;
    }
}