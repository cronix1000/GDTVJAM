// GridConfiguration.cs
using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Grid System/Grid Configuration", order = 0)]
public class GridConfiguration : ScriptableObject
{
    public int width = 20;
    public int height = 15;
    public float cellSize = 1.0f;
}