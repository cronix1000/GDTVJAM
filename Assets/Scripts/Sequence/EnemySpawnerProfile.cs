// EnemySpawnerProfile.cs (Example)
using UnityEngine;
// using System.Collections.Generic; // If you have a list of enemy types here

[CreateAssetMenu(fileName = "NewSpawnerProfile", menuName = "Game Sequence/Enemy Spawner Profile")]
public class EnemySpawnerProfile : ScriptableObject
{
    public string profileName = "Default Waves";
    // Add properties that your EnemySpawner system can understand:
    // public List<EnemyTypeFrequency> enemyComposition;
    // public float spawnInterval = 5f;
    // public int maxConcurrentEnemies = 10;
    // public bool isActive = true;
    // ... etc.
}