using UnityEngine;

public class Vendor : MonoBehaviour, IInteractable
{
    public string InteractText => GetInteractText();

    public void Interact(PlayerController player)
    {
        if (player == null || VendorProgress.Instance == null) return;

        if (!VendorProgress.Instance.IsUnlocked)
        {
            Debug.Log("The vendor must be unlocked at the upgrade sign.");
            return;
        }

        Shop shop = player.GetComponentInChildren<Shop>(true);

        if (shop == null)
        {
            Debug.LogError("The player does not contain a Shop component.", player);
            return;
        }

        shop.Open(player);
    }

    private string GetInteractText()
    {
        if (VendorProgress.Instance == null) return "Shop unavailable";
        if (!VendorProgress.Instance.IsUnlocked) return "Upgrade the shop at the sign";
        return $"Browse {VendorProgress.Instance.GetCurrentTierName()} gear";
    }
}