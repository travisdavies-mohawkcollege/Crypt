using UnityEngine;

[CreateAssetMenu(fileName = "TransferService", menuName = "Inventory/Transfer Service")]
public class TransferService : ScriptableObject
{
    [SerializeField] private ItemCatalog itemCatalog;

    public bool TryTransfer(Inventory fromInventory, Inventory toInventory, GeneratedItem item, int quantity)
    {
        if (fromInventory == null)
        {
            Debug.LogWarning("Source inventory cannot be null.");
            return false;
        }

        if (toInventory == null)
        {
            Debug.LogWarning("Destination inventory cannot be null.");
            return false;
        }

        if (fromInventory == toInventory)
        {
            Debug.LogWarning("Cannot transfer an item to the same inventory.");
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning("Transferred item cannot be null.");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("Transfer quantity must be greater than 0.");
            return false;
        }

        if (itemCatalog == null)
        {
            Debug.LogError("TransferService does not have an ItemCatalog assigned.");
            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out ItemDefinition definition))
        {
            Debug.LogWarning($"Could not find definition '{item.definitionID}'.");
            return false;
        }

        if (!fromInventory.CanRemove(item, quantity, definition))
        {
            return false;
        }

        if (!toInventory.CanAdd(item, quantity, definition))
        {
            return false;
        }

        if (!fromInventory.TryRemove(item, quantity, definition))
        {
            Debug.LogError("Transfer validation succeeded, but removal failed.");
            return false;
        }

        if (!toInventory.TryAdd(item, quantity, definition))
        {
            Debug.LogError("Transfer validation succeeded, but addition failed. Attempting rollback.");

            if (!fromInventory.TryAdd(item, quantity, definition))
            {
                Debug.LogError("Transfer rollback failed.");
            }

            return false;
        }

        Debug.Log($"Transferred {quantity} {definition.ItemName} from {fromInventory.inventoryID} to {toInventory.inventoryID}.");
        return true;
    }
}