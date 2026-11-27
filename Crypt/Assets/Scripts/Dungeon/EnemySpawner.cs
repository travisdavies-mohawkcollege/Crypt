using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool spawnWaves = true;
    [SerializeField] private float spawnTimerMax;
    private float spawnTimer;


    private void Start()
    {
        if (spawnOnStart) SpawnEnemies();
    }

    private void Update()
    {
        if(spawnWaves == false) return;
        spawnTimer -= Time.deltaTime;
        if(spawnTimer <= 0)
        {
            SpawnEnemies();
            spawnTimer = spawnTimerMax;
        }
    }

    public void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;

            GameObject selectedEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Instantiate(selectedEnemy, spawnPoint.position, spawnPoint.rotation);
        }
    }
}