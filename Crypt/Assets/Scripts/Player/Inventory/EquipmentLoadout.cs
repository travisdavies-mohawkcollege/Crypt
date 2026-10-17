using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
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
        EquippedItem equippedItem = equippedItems.Find(item => item.instanceID == instanceID);
        if (equippedItem == null) return false;

        equippedItems.Remove(equippedItem);
        Changed?.Invoke();
        return true;
    }
}