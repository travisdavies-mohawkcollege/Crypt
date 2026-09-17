using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject playerPrefab;

    public void SpawnPlayerInDungeon()
    {
        SpawnPoint spawnScript = FindFirstObjectByType<SpawnPoint>();
        GameObject spawnPoint = spawnScript.gameObject;
        Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }
}
