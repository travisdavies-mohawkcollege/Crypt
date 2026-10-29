using UnityEngine;

// Performs scene-local Town setup after the persistent managers finish loading the scene.
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
        // The player itself is temporary, while PlayerManager preserves its inventory and equipment.
        playerManager.SpawnPlayerToSpawnPoint();
    }
}
