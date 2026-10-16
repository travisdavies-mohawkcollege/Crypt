using System;

[Serializable]
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