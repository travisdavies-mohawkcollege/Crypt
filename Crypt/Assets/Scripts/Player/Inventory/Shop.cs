using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class ShopListing
{
    public ItemDefinition item;
    public int price;
}

public class Shop : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private Transform itemContent;
    [SerializeField] private ShopItemButton itemButtonPrefab;
    [SerializeField] private TMP_Text shopTitle;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Services")]
    [SerializeField] private ShopService shopService;

    [Header("Stock")]
    [SerializeField] private List<ShopListing> commonStock = new();
    [SerializeField] private List<ShopListing> uncommonStock = new();
    [SerializeField] private List<ShopListing> rareStock = new();

    private PlayerController player;
    private PlayerUIController playerUI;
    private Inventory playerInventory;

    private void Awake()
    {
        if (shopCanvas == null) shopCanvas = gameObject;
        if (shopService == null) shopService = GetComponent<ShopService>();
    }

    private void OnDisable()
    {
        UnsubscribeFromInventory();
    }

    public void Open(PlayerController interactingPlayer)
    {
        if (interactingPlayer == null || VendorProgress.Instance == null) return;
        if (!VendorProgress.Instance.IsUnlocked) return;

        player = interactingPlayer;
        playerUI = player.GetComponent<PlayerUIController>();
        playerInventory = PlayerManager.Instance.PlayerInventory;

        shopCanvas.SetActive(true);

        if (playerUI != null) playerUI.SetCursorLocked(false);

        SubscribeToInventory();
        RebuildShop();
        RefreshGold();

        if (feedbackText != null) feedbackText.text = string.Empty;
    }

    public void Close()
    {
        UnsubscribeFromInventory();

        if (shopCanvas != null) shopCanvas.SetActive(false);
        if (playerUI != null) playerUI.SetCursorLocked(true);

        player = null;
        playerUI = null;
        playerInventory = null;
    }

    public void Buy(ShopListing listing)
    {
        if (shopService == null)
        {
            SetFeedback("ShopService is missing.");
            return;
        }

        bool purchaseSucceeded = shopService.TryBuy(listing, out string message);
        SetFeedback(message);

        if (purchaseSucceeded) RefreshGold();
    }

    private void RebuildShop()
    {
        if (itemContent == null || itemButtonPrefab == null) return;

        foreach (Transform child in itemContent) Destroy(child.gameObject);

        List<ShopListing> currentStock = GetCurrentStock();

        foreach (ShopListing listing in currentStock)
        {
            if (listing == null || listing.item == null) continue;
            ShopItemButton newButton = Instantiate(itemButtonPrefab, itemContent);
            newButton.Initialize(this, listing);
        }

        if (shopTitle != null) shopTitle.text = $"{VendorProgress.Instance.GetCurrentTierName()} Gear";
    }

    private List<ShopListing> GetCurrentStock()
    {
        switch (VendorProgress.Instance.VendorLevel)
        {
            case 1: return commonStock;
            case 2: return uncommonStock;
            case 3: return rareStock;
            default: return new List<ShopListing>();
        }
    }

    private void SubscribeToInventory()
    {
        if (playerInventory == null) return;
        playerInventory.Changed -= RefreshGold;
        playerInventory.Changed += RefreshGold;
    }

    private void UnsubscribeFromInventory()
    {
        if (playerInventory != null) playerInventory.Changed -= RefreshGold;
    }

    private void RefreshGold()
    {
        if (goldText == null) return;

        int goldAmount = 0;

        if (playerInventory != null && playerInventory.TryFindByDefinitionID("resource.gold", out InventoryEntry goldEntry)) goldAmount = goldEntry.quantity;

        goldText.text = $"{goldAmount} Gold";
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
    }
}