using System;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private LootGenerator generator;
    [SerializeField] private InventoryComponent inventoryComponent;
    [SerializeField, Min(1)] private int lootRolls = 1;

    private string interactText = "Open Chest";
    public string InteractText => interactText;

    private int dungeonLevel;
    private bool initialized;
    private bool lootGenerated;

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

        if (inventoryComponent == null)
        {
            inventoryComponent = GetComponent<InventoryComponent>();
        }

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

        GenerateLoot();
        initialized = true;
    }

    public void AlignChest(Vector3Int direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0f, -90f, 0f);
        gameObject.transform.localRotation = targetRotation;
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

        Debug.Log($"Opened level {dungeonLevel} chest containing {inventoryComponent.Inventory.inventoryEntries.Count} inventory entries.");

        // Open chest ui
    }
}