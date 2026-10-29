using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// Authoritative mapping from equipment slots to generated item instance IDs.
public class EquipmentLoadout
{
    [SerializeField] private List<EquippedItem> equippedItems = new();

    public IReadOnlyList<EquippedItem> EquippedItems => equippedItems;
    [field: NonSerialized] public event Action Changed;

    public bool TryGetEquippedItem(EquipmentSlot slot, out EquippedItem equippedItem)
    {
        equippedItem = equippedItems.Find(item => item.slot == slot);
        return equippedItem != null;
    }

    public bool IsEquipped(string instanceID)
    {
        if (string.IsNullOrWhiteSpace(instanceID)) return false;
        return equippedItems.Exists(item => item.instanceID == instanceID);
    }

    public bool SetEquippedItem(EquipmentSlot slot, string instanceID)
    {
        // Replacing a slot updates the existing record so each slot remains unique.
        EquippedItem existingItem = equippedItems.Find(item => item.slot == slot);

        if (existingItem != null)
        {
            if (existingItem.instanceID == instanceID) return false;
            existingItem.instanceID = instanceID;
            Changed?.Invoke();
            return true;
        }

        equippedItems.Add(new EquippedItem(slot, instanceID));
        Changed?.Invoke();
        return true;
    }

    public bool RemoveEquippedItem(string instanceID)
    {
        // Unequip by instance ID because the same definition may exist multiple times.
        EquippedItem equippedItem = equippedItems.Find(item => item.instanceID == instanceID);
        if (equippedItem == null) return false;

        equippedItems.Remove(equippedItem);
        Changed?.Invoke();
        return true;
    }
}
