using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    public string inventoryID;
    public List<InventoryEntry> inventoryEntries = new();
    [field: NonSerialized] public event System.Action Changed;

    public bool CanAdd(GeneratedItem item, int quantity, ItemDefinition definition)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot add a null item.");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("Quantity must be more than 0.");
            return false;
        }

        if (definition == null)
        {
            Debug.LogWarning("ItemDefinition cannot be null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.instanceID))
        {
            Debug.LogWarning("Cannot add an item without an instance ID.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.definitionID))
        {
            Debug.LogWarning("Cannot add an item without a definition ID.");
            return false;
        }

        if (item.definitionID != definition.ItemID)
        {
            Debug.LogWarning($"Item definition ID '{item.definitionID}' does not match provided definition '{definition.ItemID}'.");
            return false;
        }

        if (definition.StackLimit < 1)
        {
            Debug.LogWarning($"{definition.ItemName} has invalid stacklimit.");
            return false;
        }

        foreach (InventoryEntry entry in inventoryEntries)
        {
            if (entry?.item?.instanceID == item.instanceID)
            {
                Debug.LogWarning($"Inventory already contains instance ID {item.instanceID}.");
                return false;
            }
        }

        bool isStackable = definition.StackLimit > 1;

        if (isStackable)
        {
            foreach (InventoryEntry entry in inventoryEntries)
            {
                if (entry?.item == null) continue;

                bool sameDefinition = entry.item.definitionID == item.definitionID;
                if (!sameDefinition) continue;

                if (entry.quantity + quantity > definition.StackLimit)
                {
                    Debug.LogWarning("Requested quantity exceeds stack limit.");
                    return false;
                }

                return true;

            }

            if (quantity > definition.StackLimit)
            {
                Debug.LogWarning("Initial quantity exceeds the stack limit.");
                return false;
            }

            return true;
        }

        if (quantity != 1)
        {
            Debug.LogWarning("Generated gear cannot have more or less than 1 quantity.");
            return false;
        }

        return true;
    }

    public bool TryAdd(GeneratedItem item, int quantity, ItemDefinition definition)
    {
        if (!CanAdd(item, quantity, definition))
        {
            return false;
        }

        bool isStackable = definition.StackLimit > 1;

        if (isStackable)
        {
            foreach (InventoryEntry entry in inventoryEntries)
            {
                if (entry?.item == null) continue;

                bool sameDefinition = entry.item.definitionID == item.definitionID;
                if (!sameDefinition) continue;

                entry.quantity += quantity;

                Debug.Log($"Added {quantity} {definition.ItemName}. New quantity: {entry.quantity}");
                Changed?.Invoke();
                return true;
            }

            inventoryEntries.Add(new InventoryEntry(item, quantity));

            Debug.Log($"Added {quantity} {definition.ItemName} as a new entry.");
            Changed?.Invoke();
            return true;
        }

        inventoryEntries.Add(new InventoryEntry(item, 1));

        Debug.Log($"Inventory added instance ID {item.instanceID}.");
        Changed?.Invoke();
        return true;
    }

    public bool CanRemove(GeneratedItem item, int quantity, ItemDefinition definition)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot remove a null item.");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning("Quantity must be more than 0.");
            return false;
        }

        if (definition == null)
        {
            Debug.LogWarning("ItemDefinition cannot be null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.definitionID))
        {
            Debug.LogWarning("Cannot remove an item without a definition ID.");
            return false;
        }

        if (item.definitionID != definition.ItemID)
        {
            Debug.LogWarning($"Item definition ID '{item.definitionID}' does not match provided definition '{definition.ItemID}'.");
            return false;
        }

        if (definition.StackLimit < 1)
        {
            Debug.LogWarning($"{definition.ItemName} has invalid stacklimit.");
            return false;
        }

        bool isStackable = definition.StackLimit > 1;

        if (isStackable)
        {
            foreach (InventoryEntry entry in inventoryEntries)
            {
                if (entry?.item == null) continue;

                bool sameDefinition = entry.item.definitionID == item.definitionID;
                if (!sameDefinition) continue;

                if (quantity > entry.quantity)
                {
                    Debug.LogWarning($"Quantity of removal ({quantity}) is greater than quantity in inventory ({entry.quantity}).");
                    return false;
                }

                return true;
            }

            Debug.LogWarning($"Inventory does not contain {definition.ItemName}.");
            return false;
        }

        if (quantity != 1)
        {
            Debug.LogWarning("Cannot remove more or less than 1 of a nonstackable item.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.instanceID))
        {
            Debug.LogWarning("Cannot remove a nonstackable item without an instanceID.");
            return false;
        }

        foreach (InventoryEntry entry in inventoryEntries)
        {
            if (entry?.item?.instanceID == item.instanceID)
            {
                return true;
            }
        }

        Debug.LogWarning($"Inventory doesn't contain {definition.ItemName} with instanceID {item.instanceID}.");
        return false;
    }

    public bool TryRemove(GeneratedItem item, int quantity, ItemDefinition definition)
    {
        if (!CanRemove(item, quantity, definition))
        {
            return false;
        }

        bool isStackable = definition.StackLimit > 1;

        if (isStackable)
        {
            for (int i = 0; i < inventoryEntries.Count; i++)
            {
                InventoryEntry entry = inventoryEntries[i];

                if (entry?.item == null) continue;

                bool sameDefinition = entry.item.definitionID == item.definitionID;
                if (!sameDefinition) continue;

                entry.quantity -= quantity;

                Debug.Log($"Removed {quantity} {definition.ItemName}. Remaining quantity: {entry.quantity}");

                if (entry.quantity == 0)
                {
                    inventoryEntries.RemoveAt(i);

                    Debug.Log($"Removed {definition.ItemName} from inventory because quantity was 0.");
                }
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        for (int i = 0; i < inventoryEntries.Count; i++)
        {
            InventoryEntry entry = inventoryEntries[i];

            if (entry?.item?.instanceID != item.instanceID)
            {
                continue;
            }

            inventoryEntries.RemoveAt(i);

            Debug.Log($"Removed {definition.ItemName} with instanceID {item.instanceID}.");
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryFindByInstanceID(string instanceID, out InventoryEntry entryWithID)
    {
        entryWithID = null;

        if (string.IsNullOrWhiteSpace(instanceID))
        {
            return false;
        }

        foreach (InventoryEntry entry in inventoryEntries)
        {
            if (entry?.item?.instanceID != instanceID) continue;

            entryWithID = entry;
            return true;
        }

        return false;
    }

    public bool TryFindByDefinitionID(string definitionID, out InventoryEntry entryWithID)
    {
        entryWithID = null;

        if (string.IsNullOrWhiteSpace(definitionID))
        {
            return false;
        }

        foreach (InventoryEntry entry in inventoryEntries)
        {
            if (entry?.item?.definitionID != definitionID) continue;

            entryWithID = entry;
            return true;
        }

        return false;
    }
}