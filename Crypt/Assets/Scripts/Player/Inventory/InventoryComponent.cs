using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    [SerializeField]
    private ItemCatalog itemCatalog;

    private Inventory inventory;
    public Inventory Inventory => inventory;
    public bool HasInventory => inventory != null;

    public void BindInventory(Inventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogError("Cannot bind a null inventory.");
            return;
        }

        if (this.inventory != null)
        {
            Debug.LogWarning("InventoryComponent already has an inventory.");
            return;
        }
        this.inventory = inventory;
    }

    public void CreateInventory(string inventoryID)
    {
        inventory = new Inventory();
        inventory.inventoryID = inventoryID;
    }

    public bool TryAdd(GeneratedItem item, int quantity)
    {

        if (inventory == null)
        {
            Debug.LogError("InventoryComponent has no inventory bound.");
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning("Cannot add a null item.");
            return false;
        }

        if (itemCatalog == null)
        {
            Debug.LogError(
                "InventoryComponent has no ItemCatalog assigned."
            );

            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out ItemDefinition definition))
        {
            Debug.LogWarning($"Could not find definition '{item.definitionID}'.");

            return false;
        }

        return inventory.TryAdd(item, quantity, definition);
    }

    public bool TryRemove(GeneratedItem item, int quantity)
    {
        if (inventory == null)
        {
            Debug.LogError("InventoryComponent has no inventory bound.");
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning("Cannot remove a null item.");
            return false;
        }

        if (itemCatalog == null)
        {
            Debug.LogError(
                "InventoryComponent has no ItemCatalog assigned."
            );

            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out ItemDefinition definition))
        {
            Debug.LogWarning($"Could not find definition '{item.definitionID}'.");

            return false;
        }

        return inventory.TryRemove(item, quantity, definition);
    }
}