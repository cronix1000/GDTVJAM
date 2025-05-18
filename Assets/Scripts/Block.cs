using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public int blockID;
    public string blockName;
    public List<Block> adjacentBlocks;
    public bool isActive;
    public List<Edge> BlockEdges;
    public bool isSurrounded;
}