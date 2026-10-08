using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemCatalog itemCatalog;

    private string interactText = "Pickup Item";
    public string InteractText => interactText;

    private GeneratedItem associatedItem;
    private int quantity;
    private ItemDefinition itemDefinition;

    public bool AssignItem(GeneratedItem item, int quantity)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot assign a null item to DroppedItem.");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("Dropped item quantity must be greater than 0.");
            return false;
        }

        if (itemCatalog == null)
        {
            Debug.LogError("DroppedItem does not have an ItemCatalog assigned.");
            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out ItemDefinition definition))
        {
            Debug.LogWarning($"Could not find item definition '{item.definitionID}'.");
            return false;
        }

        associatedItem = item;
        itemDefinition = definition;
        this.quantity = quantity;

        interactText = $"Pick up {itemDefinition.ItemName}";

        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot pick up an item without a player.");
            return;
        }

        if (associatedItem == null)
        {
            Debug.LogWarning("DroppedItem has not been assigned an item.");
            return;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("DroppedItem has an invalid quantity.");
            return;
        }

        if (!player.TryGetComponent(out InventoryComponent inventoryComponent))
        {
            Debug.LogWarning("Player has no InventoryComponent.");
            return;
        }

        if (inventoryComponent.TryAdd(associatedItem, quantity))
        {
            Debug.Log($"Picked up {quantity} {itemDefinition.ItemName}.");
            Destroy(gameObject);
        }
    }
}