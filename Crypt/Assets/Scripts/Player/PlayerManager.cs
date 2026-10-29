using UnityEngine;
using UnityEngine.SceneManagement;

// Persists player-owned data across scenes while recreating the scene-local player GameObject.
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public GameObject playerPrefab;
    public GameObject player { get; private set; }
    private SceneController sceneController;
    private Inventory playerInventory = new();
    private EquipmentLoadout playerEquipment = new();
    public EquipmentLoadout PlayerEquipment => playerEquipment;
    public Inventory PlayerInventory => playerInventory;
    private Inventory bankInventory = new();
    public Inventory BankInventory => bankInventory;

    private void Awake()
    {
        // Ensure only one PlayerManager exists across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        playerInventory.inventoryID = "player.inventory";
        bankInventory.inventoryID = "bank.inventory";

        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        if(sceneName == "SampleScene")
        {
            SpawnPlayerToSpawnPoint();
        }

        Instance = this;
        BindSceneController();
    }

    private void OnEnable()
    {
        // Despawn before scene transitions so cameras, input, and UI do not survive accidentally.
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
        // Enforce a single scene-local player before creating the replacement.
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
        if (!player.TryGetComponent(out InventoryComponent inventoryComponent))
        {
            Debug.LogError("Spawned player does not have invetoryComponent");
            return;
        }
        // Bind the persistent data model to the newly spawned scene representation.
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
