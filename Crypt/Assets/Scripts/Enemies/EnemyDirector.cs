using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyDirector : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField] List<EnemyData> weakEnemies = new List<EnemyData>();
    List<EnemyData> mediumEnemies = new();
    List<EnemyData> strongEnemies = new();

    
    int enemyAllowance;
    private bool directorStarted = false;



    private List<Transform> enemySpawnPoints = new();

    public void InitializeDirector(int EnemyAllowance)
    {
        enemyAllowance = EnemyAllowance;
    }

    public void ToggleDirector(bool on)
    {
        directorStarted = on;
        EnemySpawnPoint[] spawnPoints = FindObjectsByType<EnemySpawnPoint>();
        foreach(EnemySpawnPoint spawnpoint in spawnPoints)
        {
            enemySpawnPoints.Add(spawnpoint.transform);
        }
    }

    public void SpawnEnemies()
    {
        for(int i = 0; i < enemyAllowance; i++)
        {
            if(enemySpawnPoints.Count == 0)
            {
                break;
            }
            Transform chosenSpawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Count)];
            GameObject enemy = Instantiate(weakEnemies[Random.Range(0, weakEnemies.Count)].EnemyPrefab, chosenSpawnPoint.position, Quaternion.identity);
            enemySpawnPoints.Remove(chosenSpawnPoint);
        }   
    }
    

}
