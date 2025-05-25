using System;
using UnityEngine;

namespace DefaultNamespace.Sequence
{
    public class EnemySpawnManager : MonoBehaviour
    {
        public EnemySpawnManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }


        public void ApplyProfileToSpawner(string stepTargetSpawnerID, EnemySpawnerProfile stepSpawnerProfile)
        {
            throw new System.NotImplementedException();
        }

        public void ClearAllActiveEnemies()
        {
            throw new System.NotImplementedException();
        }

        public void SpawnBoss(GameObject stepBossPrefab, Transform stepBossSpawnPoint)
        {
            throw new System.NotImplementedException();
        }

        public void EnableSpawner(string targetSpawnerID)
        {
            throw new NotImplementedException();
        }

        public void DisableSpawner(string targetSpawnerID)
        {
            throw new NotImplementedException();
        }
    }
}