using System.Collections.Generic;
using UnityEngine;

public class ShopService : MonoBehaviour
{
    public bool TryBuy(ShopListing listing, out string message)
    {
        message = string.Empty;

        if (listing == null || listing.item == null)
        {
            message = "This item is unavailable.";
            return false;
        }

        if (listing.price <= 0)
        {
            message = "This item has an invalid price.";
            return false;
        }

        if (PlayerManager.Instance == null)
        {
            message = "PlayerManager could not be found.";
            return false;
        }

        GeneratedItem purchasedItem = new GeneratedItem(System.Guid.NewGuid().ToString(), listing.item.ItemID, new List<GeneratedBonus>());
        Inventory playerInventory = PlayerManager.Instance.PlayerInventory;

        if (!playerInventory.CanAdd(purchasedItem, 1, listing.item))
        {
            message = "This item cannot be added to your inventory.";
            return false;
        }

        if (!PlayerManager.Instance.TrySpendGold(listing.price))
        {
            message = $"You need {listing.price} Gold.";
            return false;
        }

        if (!playerInventory.TryAdd(purchasedItem, 1, listing.item))
        {
            PlayerManager.Instance.TryAddGold(listing.price);
            message = "The purchase failed. Your Gold was returned.";
            return false;
        }

        message = $"Purchased {listing.item.ItemName}.";
        return true;
    }
}