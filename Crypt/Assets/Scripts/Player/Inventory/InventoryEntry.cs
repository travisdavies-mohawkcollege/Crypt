using System;
using UnityEngine;

[Serializable]
public class InventoryEntry
{
    public GeneratedItem item;
    public int quantity;

    public InventoryEntry(GeneratedItem item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}
