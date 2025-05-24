using UnityEngine;

public class BuilderBlockSelector : MonoBehaviour
{
    public PlayerCreatorGridManager gridManager;
    public Block blockPrefab;
    
    public void SelectBlock()
    {
        gridManager.SelectBlockPrefabForPlacement(blockPrefab);
    }
}
