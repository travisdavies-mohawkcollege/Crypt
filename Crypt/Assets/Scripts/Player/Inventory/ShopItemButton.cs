using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemButton : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemPrice;
    [SerializeField] private Button buyButton;

    private Shop shop;
    private ShopListing listing;

    public void Initialize(Shop owningShop, ShopListing shopListing)
    {
        shop = owningShop;
        listing = shopListing;

        if (itemIcon != null)
        {
            itemIcon.sprite = listing.item.ItemSprite;
            itemIcon.enabled = listing.item.ItemSprite != null;
        }

        if (itemName != null) itemName.text = listing.item.ItemName;
        if (itemPrice != null) itemPrice.text = $"{listing.price} Gold";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
        }
    }

    private void Buy()
    {
        if (shop != null && listing != null) shop.Buy(listing);
    }
}