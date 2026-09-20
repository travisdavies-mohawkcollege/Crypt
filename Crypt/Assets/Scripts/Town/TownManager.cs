
using UnityEngine;
using UnityEngine.SceneManagement;

public class TownManager : MonoBehaviour
{
    private PlayerManager playerManager;

    void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
    }
    
    public void InitializeTown()
    {
        playerManager.SpawnPlayerToSpawnPoint();
    }

}
