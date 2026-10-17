using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRow : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button selectButton;

    private InventoryEntry inventoryEntry;
    private Action<InventoryEntry> selectedCallback;

    private void Awake()
    {
        selectButton.onClick.AddListener(HandleSelected);
    }

    public void Initialize(InventoryEntry entry, ItemDefinition definition, Action<InventoryEntry> onSelected)
    {
        inventoryEntry = entry;
        selectedCallback = onSelected;

        itemIcon.sprite = definition.ItemSprite;
        itemNameText.text = definition.ItemName;
        rarityText.text = definition.ItemRarity.ToString();
        quantityText.text = entry.quantity > 1 ? entry.quantity.ToString() : "";
    }

    private void HandleSelected()
    {
        selectedCallback?.Invoke(inventoryEntry);
    }

    private void OnDestroy()
    {
        selectButton.onClick.RemoveListener(HandleSelected);
    }
}