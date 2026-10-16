using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentService", menuName = "Inventory/Equipment Service")]
public class EquipmentService : ScriptableObject
{
    [SerializeField] private ItemCatalog itemCatalog;

    public bool CanEquip(Inventory inventory, EquipmentLoadout loadout, GeneratedItem item, out ItemDefinition definition)
    {
        definition = null;

        if (inventory == null || loadout == null || item == null) return false;

        if (itemCatalog == null)
        {
            Debug.LogError("EquipmentService has no ItemCatalog assigned.");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(item.instanceID) || string.IsNullOrWhiteSpace(item.definitionID)) return false;

        if (!itemCatalog.TryGetItemDefinition(item.definitionID, out definition))
        {
            Debug.LogWarning($"Could not find definition '{item.definitionID}'.");
            return false;
        }

        if (definition.EquipSlot == EquipmentSlot.None)
        {
            Debug.LogWarning($"{definition.ItemName} cannot be equipped.");
            return false;
        }

        if (definition.StackLimit != 1)
        {
            Debug.LogWarning("Stackable items cannot be equipped.");
            return false;
        }

        if (!inventory.TryFindByInstanceID(item.instanceID, out InventoryEntry inventoryEntry))
        {
            Debug.LogWarning($"Inventory does not contain instance ID {item.instanceID}.");
            return false;
        }

        if (inventoryEntry.item.definitionID != definition.ItemID) return false;
        if (loadout.IsEquipped(item.instanceID)) return false;

        return true;
    }

    public bool TryEquip(Inventory inventory, EquipmentLoadout loadout, GeneratedItem item)
    {
        if (!CanEquip(inventory, loadout, item, out ItemDefinition definition)) return false;

        if (!loadout.SetEquippedItem(definition.EquipSlot, item.instanceID)) return false;

        Debug.Log($"Equipped {definition.ItemName} in {definition.EquipSlot}.");
        return true;
    }

    public bool TryUnequip(EquipmentLoadout loadout, GeneratedItem item)
    {
        if (loadout == null || item == null) return false;
        if (!loadout.RemoveEquippedItem(item.instanceID)) return false;

        Debug.Log($"Unequipped instance ID {item.instanceID}.");
        return true;
    }
}