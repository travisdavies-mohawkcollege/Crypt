using System;
using UnityEngine;

[Serializable]
// Stores an item reference and the quantity represented by one inventory row.
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
