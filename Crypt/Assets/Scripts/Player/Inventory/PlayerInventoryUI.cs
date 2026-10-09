using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerInventoryUI : MonoBehaviour
{
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private InventoryRow rowPrefab;
    private InventorySortMode currentSortMode = InventorySortMode.ItemType;
    public Transform content;
    private Inventory displayedInventory;
    private List<InventoryRow> rows = new();

    private void OnDisable()
    {
        if (displayedInventory != null) displayedInventory.Changed -= Refresh;
    }

    public void BindInventory(Inventory inventory)
    {
        if (displayedInventory != null) displayedInventory.Changed -= Refresh;

        displayedInventory = inventory;

        if (displayedInventory != null) displayedInventory.Changed += Refresh;

        Refresh();
    }

    public void CloseInventory()
    {
        gameObject.SetActive(false);
        PlayerController player = FindAnyObjectByType<PlayerController>();
        player.SetCursorLocked(true);
    }

    public void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == null) continue;
            rows[i].gameObject.SetActive(false);
            Destroy(rows[i].gameObject);
        }

        rows.Clear();

        if (displayedInventory == null) return;
        if (itemCatalog == null || rowPrefab == null || content == null) return;

        List<InventoryEntry> sortedEntries = GetSortedEntries(currentSortMode);

        foreach (InventoryEntry entry in sortedEntries)
        {
            if (!itemCatalog.TryGetItemDefinition(entry.item.definitionID, out ItemDefinition definition)) continue;

            InventoryRow row = Instantiate(rowPrefab, content);
            row.Initialize(entry, definition, HandleEntrySelected);
            rows.Add(row);
        }
    }

    private void HandleEntrySelected(InventoryEntry entry)
    {
        Debug.Log($"Selected item {entry.item.instanceID}");
    }

    private ItemDefinition GetDefinition(InventoryEntry entry)
    {
        if (entry?.item == null) return null;
        itemCatalog.TryGetItemDefinition(entry.item.definitionID, out ItemDefinition definition);
        return definition;
    }

    private List<InventoryEntry> GetSortedEntries(InventorySortMode sortMode)
    {
        List<InventoryEntry> sortedEntries = displayedInventory.inventoryEntries
            .Where(entry => entry?.item != null)
            .ToList();

        switch (sortMode)
        {
            case InventorySortMode.Name:
                return sortedEntries
                    .OrderBy(entry => GetDefinition(entry)?.ItemName ?? "")
                    .ToList();

            case InventorySortMode.Rarity:
                return sortedEntries
                    .OrderByDescending(entry => GetDefinition(entry)?.ItemRarity ?? Rarity.Common)
                    .ThenBy(entry => GetDefinition(entry)?.ItemName ?? "")
                    .ToList();

            case InventorySortMode.ItemType:
                return sortedEntries
                    .OrderBy(entry => GetDefinition(entry)?.ItemType)
                    .ThenByDescending(entry => GetDefinition(entry)?.ItemRarity ?? Rarity.Common)
                    .ThenBy(entry => GetDefinition(entry)?.ItemName ?? "")
                    .ToList();

            case InventorySortMode.Quantity:
                return sortedEntries
                    .OrderByDescending(entry => entry.quantity)
                    .ThenBy(entry => GetDefinition(entry)?.ItemName ?? "")
                    .ToList();

            case InventorySortMode.BonusCount:
                return sortedEntries
                    .OrderByDescending(entry => entry.item.bonusStats?.Count ?? 0)
                    .ThenByDescending(entry => GetDefinition(entry)?.ItemRarity ?? Rarity.Common)
                    .ToList();

            default:
                return sortedEntries;
        }
    }

    public void SetSortMode(InventorySortMode sortMode)
    {
        currentSortMode = sortMode;
        Refresh();
    }
}
