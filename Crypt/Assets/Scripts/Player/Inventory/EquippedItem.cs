using System;

[Serializable]
// Serializable loadout entry that references an owned generated item by instance ID.
public class EquippedItem
{
    public EquipmentSlot slot;
    public string instanceID;

    public EquippedItem(EquipmentSlot slot, string instanceID)
    {
        this.slot = slot;
        this.instanceID = instanceID;
    }
}
