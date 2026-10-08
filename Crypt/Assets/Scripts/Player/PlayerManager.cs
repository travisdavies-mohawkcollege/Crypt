using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public GameObject playerPrefab;
    public GameObject player { get; private set; }
    private SceneController sceneController;
    private Inventory playerInventory = new();

    private void Awake()
    {
        // Ensure only one PlayerManager exists across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        playerInventory.inventoryID = "player.inventory";

        Instance = this;
        BindSceneController();
    }

    private void OnEnable()
    {
        if (sceneController == null)
        {
            BindSceneController();
        }

        if (sceneController != null)
        {
            sceneController.sceneLoadEvent.RemoveListener(DespawnPlayer);
            sceneController.sceneLoadEvent.AddListener(DespawnPlayer);
        }
    }

    private void OnDisable()
    {
        if (sceneController != null)
        {
            sceneController.sceneLoadEvent.RemoveListener(DespawnPlayer);
        }
    }

    private void BindSceneController()
    {
        sceneController = FindAnyObjectByType<SceneController>();
    }

    public void SpawnPlayerToSpawnPoint()
    {
        DespawnPlayer();
        PlayerController[] existingPlayers = FindObjectsByType<PlayerController>();
        foreach (var p in existingPlayers)
        {
            Destroy(p.gameObject);
        }

        // Find scene spawn point
        SpawnPoint spawnScript = FindAnyObjectByType<SpawnPoint>();
        if (spawnScript == null)
        {
            Debug.LogError("PlayerManager: No SpawnPoint component found in current scene!");
            return;
        }

        // Instantiate player clone
        Transform spawnPoint = spawnScript.transform;
        player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        if(!player.TryGetComponent(out InventoryComponent inventoryComponent))
        {
            Debug.LogError("Spawned player does not have invetoryComponent");
            return;
        }
        inventoryComponent.BindInventory(playerInventory);
    }

    public void DespawnPlayer()
    {
        if (player != null)
        {
            Destroy(player);
            player = null;
        }
    }
}