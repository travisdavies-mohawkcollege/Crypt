using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

// Persists player-owned data across scenes while recreating the scene-local player GameObject.
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public bool deathFlag = false;

    public GameObject playerPrefab;
    public GameObject player { get; private set; }
    private SceneController sceneController;
    private Inventory playerInventory = new();
    private Inventory backupPlayerInventory;
    private EquipmentLoadout playerEquipment = new();
    public EquipmentLoadout PlayerEquipment => playerEquipment;
    private EquipmentLoadout backupPlayerEquipment;
    public Inventory PlayerInventory => playerInventory;
    private Inventory bankInventory = new();
    public Inventory BankInventory => bankInventory;
    bool hasDungeonBackup = false;
    bool handlingDeath = false;
    [SerializeField] private ItemDefinition startingWeapon;
    [SerializeField] private ItemDefinition goldDefinition;
    private bool startingItemsGranted;
    public PlayerCombatStats PlayerCombatStats { get; private set; }

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

        BindSceneController();

        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        if (sceneName == "SampleScene")
        {
            SpawnPlayerToSpawnPoint();
        }

        Instance = this;

        GrantStartingItems();

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
            p.Death -= KillPlayer;
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

        if (!player.TryGetComponent(out PlayerCombatStats combatStats))
        {
            Debug.LogError("spawned player does not have combat stats");
            return;
        }
        PlayerCombatStats = combatStats;
        //Subscribe to the player's death
        PlayerController newPlayer = player.GetComponent<PlayerController>();
        newPlayer.Death += KillPlayer;
        inventoryComponent.BindInventory(playerInventory);
    }

    public void DespawnPlayer()
    {
        if (player != null)
        {
            if (player.TryGetComponent(out PlayerController playerController))
            {
                playerController.Death -= KillPlayer;
            }
            Destroy(player);
            player = null;
            PlayerCombatStats = null;
        }
    }

    public void KillPlayer()
    {
        if (handlingDeath) return;
        handlingDeath = true;

        DespawnPlayer();
        if (hasDungeonBackup)
        {
            if (backupPlayerInventory != null) playerInventory = CloneInventory(backupPlayerInventory);
            if (backupPlayerEquipment != null) playerEquipment = CloneEquipmentLoadout(backupPlayerEquipment);
        }

        if (sceneController == null)
        {
            BindSceneController();
        }

        if (sceneController != null)
        {
            sceneController.LoadTown();
        }

        handlingDeath = false;
    }

    public void BackupPlayerState()
    {
        backupPlayerInventory = CloneInventory(playerInventory);
        backupPlayerEquipment = CloneEquipmentLoadout(playerEquipment);
        hasDungeonBackup = true;
    }

    private Inventory CloneInventory(Inventory source)
    {
        Inventory copy = new Inventory
        {
            inventoryID = source.inventoryID
        };

        foreach (InventoryEntry entry in source.inventoryEntries)
        {
            if (entry?.item == null) continue;

            List<GeneratedBonus> copiedBonuses = new();

            if (entry.item.bonusStats != null)
            {
                foreach (GeneratedBonus bonus in entry.item.bonusStats)
                {
                    copiedBonuses.Add(new GeneratedBonus(bonus.statType, bonus.element));
                }
            }

            GeneratedItem copiedItem = new GeneratedItem(entry.item.instanceID, entry.item.definitionID, copiedBonuses);

            copy.inventoryEntries.Add(new InventoryEntry(copiedItem, entry.quantity));
        }

        return copy;
    }

    private EquipmentLoadout CloneEquipmentLoadout(EquipmentLoadout source)
    {
        EquipmentLoadout copy = new EquipmentLoadout();

        if (source == null) return copy;

        foreach (EquippedItem equippedItem in source.EquippedItems)
        {
            if (equippedItem == null) continue;

            if (string.IsNullOrWhiteSpace(equippedItem.instanceID)) continue;

            copy.SetEquippedItem(equippedItem.slot, equippedItem.instanceID);
        }

        return copy;
    }

    private void GrantStartingItems()
    {
        if (startingItemsGranted || startingWeapon == null) return;

        GeneratedItem fireOrb = new GeneratedItem(Guid.NewGuid().ToString(), startingWeapon.ItemID, new List<GeneratedBonus>());


        if (!playerInventory.TryAdd(fireOrb, 1, startingWeapon)) return;

        playerEquipment.SetEquippedItem(EquipmentSlot.MainHand, fireOrb.instanceID);

        //TryAddGold(100);

        startingItemsGranted = true;
    }

    public bool TryAddGold(int amount)
    {
        if (amount <= 0 || goldDefinition == null) return false;
        GeneratedItem gold = new GeneratedItem(System.Guid.NewGuid().ToString(), goldDefinition.ItemID, new List<GeneratedBonus>());
        return playerInventory.TryAdd(gold, amount, goldDefinition);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0 || goldDefinition == null) return false;
        if (!playerInventory.TryFindByDefinitionID(goldDefinition.ItemID, out InventoryEntry goldEntry)) return false;
        if (goldEntry.quantity < amount) return false;
        return playerInventory.TryRemove(goldEntry.item, amount, goldDefinition);
    }

   

}
