using UnityEngine;

public class TownManager : MonoBehaviour
{
    private PlayerManager playerManager;

    private void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
    }

    private void Start()
    {
        InitializeTown();
    }

    public void InitializeTown()
    {
        playerManager.SpawnPlayerToSpawnPoint();
    }
}