using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

public class PlayerInventoryUI : MonoBehaviour
{
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private InventoryRow rowPrefab;
    [SerializeField] private InventorySelectionPanel selectPrefab;
    [SerializeField] private GameObject bankButton;
    [SerializeField] private GameObject playerButton;
    public RectTransform canvasRectTransform;
    private InventorySortMode currentSortMode = InventorySortMode.ItemType;
    public Transform content;
    private Inventory displayedInventory;
    private Inventory transferDestination;

    private List<InventoryRow> rows = new();

    private void OnDisable()
    {
        if (displayedInventory != null) displayedInventory.Changed -= Refresh;
        if (PlayerManager.Instance != null) PlayerManager.Instance.PlayerEquipment.Changed -= Refresh;
    }

    public void BindInventory(Inventory inventory, Inventory destination = null)
    {
        if (displayedInventory != null) displayedInventory.Changed -= Refresh;
        if (PlayerManager.Instance != null) PlayerManager.Instance.PlayerEquipment.Changed -= Refresh;

        displayedInventory = inventory;
        transferDestination = destination;

        if (displayedInventory != null) displayedInventory.Changed += Refresh;
        if (PlayerManager.Instance != null && displayedInventory == PlayerManager.Instance.PlayerInventory) PlayerManager.Instance.PlayerEquipment.Changed += Refresh;

        bool bankIsOpen = PlayerManager.Instance != null && (displayedInventory == PlayerManager.Instance.BankInventory || transferDestination == PlayerManager.Instance.BankInventory);

        if (bankButton != null) bankButton.SetActive(bankIsOpen);
        if (playerButton != null) playerButton.SetActive(bankIsOpen);

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
        if (entry?.item == null || Mouse.current == null) return;
        if (selectPrefab == null || canvasRectTransform == null || itemCatalog == null) return;
        if (!itemCatalog.TryGetItemDefinition(entry.item.definitionID, out ItemDefinition definition)) return;

        InventorySelectionPanel newPanel = Instantiate(selectPrefab, canvasRectTransform);

        RectTransform panelRectTransform = newPanel.GetComponent<RectTransform>();

        Canvas canvas = canvasRectTransform.GetComponentInParent<Canvas>();

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, Mouse.current.position.ReadValue(), uiCamera, out Vector2 localPoint);

        newPanel.InitializeSelectPanel(entry, displayedInventory, transferDestination, this, definition);

        panelRectTransform.anchoredPosition = localPoint;
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
