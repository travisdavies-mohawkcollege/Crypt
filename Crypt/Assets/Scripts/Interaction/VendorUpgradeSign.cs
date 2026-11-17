using TMPro;
using UnityEngine;

public class VendorUpgradeSign : MonoBehaviour, IInteractable
{
    public string InteractText => GetInteractText();

    [SerializeField] private TMP_Text upgradeCost;

    private void Start()
    {
        if (VendorProgress.Instance == null)
        {
            Debug.LogError("VendorUpgradeSign could not find VendorProgress.", this);
            return;
        }

        VendorProgress.Instance.Changed += RefreshText;
        RefreshText();
    }

    private void OnDestroy()
    {
        if (VendorProgress.Instance != null) VendorProgress.Instance.Changed -= RefreshText;
    }

    public void Interact(PlayerController player)
    {
        if (VendorProgress.Instance == null) return;
        VendorProgress.Instance.TryUpgrade();
        RefreshText();
    }

    private void RefreshText()
    {
        if (upgradeCost == null || VendorProgress.Instance == null) return;

        if (VendorProgress.Instance.IsFullyUpgraded)
        {
            upgradeCost.text = "Shop fully upgraded.";
            return;
        }

        int cost = VendorProgress.Instance.GetUpgradeCost();
        string nextTier = GetNextTierName();
        upgradeCost.text = $"{cost} Gold to unlock {nextTier} gear.";
    }

    private string GetInteractText()
    {
        if (VendorProgress.Instance == null) return "Shop unavailable";
        if (VendorProgress.Instance.IsFullyUpgraded) return "Shop fully upgraded";
        return $"Upgrade shop for {VendorProgress.Instance.GetUpgradeCost()} Gold";
    }

    private string GetNextTierName()
    {
        switch (VendorProgress.Instance.VendorLevel + 1)
        {
            case 1: return "Common";
            case 2: return "Uncommon";
            case 3: return "Rare";
            default: return "Rare";
        }
    }
}