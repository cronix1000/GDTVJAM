using UnityEngine;

public class TimeUntilBossSpawnTimer : MonoBehaviour {
    [SerializeField] FloatScrptableObject bossSpawnTime;
    public float timeUntilBossSpawn;
    private float timer;

    void Start() {
        timer = bossSpawnTime.GetValue();
    }

    void Update() {
        if (timer > 0) {
            timer -= Time.deltaTime;
            if (timer <= 0) {
                OnBossSpawn();
            }
        }
    }

    void OnBossSpawn() {
        Debug.Log("Boss Spawned!");
        // Add your boss spawn logic here
    }
}