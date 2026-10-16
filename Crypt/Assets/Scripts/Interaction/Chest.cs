using System;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] public LootGenerator generator;
    [SerializeField] private InventoryComponent inventoryComponent;
    [SerializeField] private ItemDropService itemDropService;
    [SerializeField] private Transform dropOrigin;
    [SerializeField, Min(1)] private int lootRolls = 1;

    private string interactText = "Open Chest";
    public string InteractText => interactText;

    private int dungeonLevel;
    private bool initialized;
    private bool lootGenerated;
    private bool opened;

    [Header("Testing")]
    [SerializeField] private bool initializeForTesting;
    [SerializeField, Range(1, 60)] private int testDungeonLevel = 5;

    private void Start()
    {
        if (initializeForTesting && !initialized)
        {
            InitializeChest(testDungeonLevel);
        }
    }

    public void InitializeChest(int dungeonLevel)
    {
        if (initialized)
        {
            Debug.LogWarning("Chest has already been initialized.");
            return;
        }

        if (generator == null)
        {
            Debug.LogError("Chest does not have a LootGenerator assigned.");
            return;
        }

        if (itemDropService == null)
        {
            Debug.LogError("Chest does not have an ItemDropService assigned.");
            return;
        }

        if (inventoryComponent == null) inventoryComponent = GetComponent<InventoryComponent>();

        if (inventoryComponent == null)
        {
            Debug.LogError("Chest does not have an InventoryComponent.");
            return;
        }

        this.dungeonLevel = Mathf.Clamp(dungeonLevel, 1, 60);

        if (!inventoryComponent.HasInventory)
        {
            inventoryComponent.CreateInventory($"chest.{Guid.NewGuid():N}");
        }

        if (dropOrigin == null) dropOrigin = transform;

        GenerateLoot();
        initialized = true;
    }

    public void AlignChest(Vector3Int direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0f, -90f, 0f);
        transform.localRotation = targetRotation;
    }

    public void GenerateLoot()
    {
        if (lootGenerated) return;

        if (generator == null || inventoryComponent == null || !inventoryComponent.HasInventory)
        {
            Debug.LogError("Chest cannot generate loot because it has not been initialized correctly.");
            return;
        }

        for (int i = 0; i < lootRolls; i++)
        {
            GeneratedItem generatedItem = generator.GenerateLoot(dungeonLevel);

            if (generatedItem == null)
            {
                Debug.LogWarning($"Chest failed to generate loot for dungeon level {dungeonLevel}.");
                continue;
            }

            if (!inventoryComponent.TryAdd(generatedItem, 1))
            {
                Debug.LogWarning($"Chest could not add generated item '{generatedItem.definitionID}'.");
            }
        }

        lootGenerated = true;
    }

    public void Interact(PlayerController player)
    {
        if (!initialized)
        {
            Debug.LogWarning("Cannot open a chest that has not been initialized.");
            return;
        }

        if (opened) return;

        if (itemDropService == null || dropOrigin == null)
        {
            Debug.LogError("Chest cannot drop its loot.");
            return;
        }

        List<InventoryEntry> entriesToDrop = new(inventoryComponent.Inventory.inventoryEntries);
        int droppedEntries = 0;

        foreach (InventoryEntry entry in entriesToDrop)
        {
            if (entry?.item == null || entry.quantity <= 0) continue;

            if (itemDropService.TryDrop(inventoryComponent.Inventory, entry.item, entry.quantity, dropOrigin))
            {
                droppedEntries++;
            }
        }

        if (inventoryComponent.Inventory.inventoryEntries.Count == 0)
        {
            opened = true;
            interactText = "Empty Chest";
        }

        Debug.Log($"Opened level {dungeonLevel} chest and dropped {droppedEntries} inventory entries.");
    }
}