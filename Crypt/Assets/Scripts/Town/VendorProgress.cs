using System;
using UnityEngine;

public class VendorProgress : MonoBehaviour
{
    public static VendorProgress Instance { get; private set; }

    private const string VendorLevelKey = "LichVendorLevel";
    private const int MaximumVendorLevel = 3;

    public int VendorLevel { get; private set; }
    public int MaxVendorLevel => MaximumVendorLevel;
    public bool IsUnlocked => VendorLevel > 0;
    public bool IsFullyUpgraded => VendorLevel >= MaximumVendorLevel;

    public event Action Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetUpgradeCost()
    {
        if (IsFullyUpgraded) return 0;
        return (VendorLevel + 1) * 10;
    }

    public bool TryUpgrade()
    {
        if (IsFullyUpgraded) return false;

        if (PlayerManager.Instance == null)
        {
            Debug.LogError("VendorProgress could not find PlayerManager.", this);
            return false;
        }

        int upgradeCost = GetUpgradeCost();

        if (!PlayerManager.Instance.TrySpendGold(upgradeCost))
        {
            Debug.Log($"The player needs {upgradeCost} Gold to upgrade the vendor.");
            return false;
        }

        VendorLevel++;
        Save();
        Changed?.Invoke();

        Debug.Log($"Vendor upgraded to level {VendorLevel}: {GetCurrentTierName()}.");
        return true;
    }

    public string GetCurrentTierName()
    {
        switch (VendorLevel)
        {
            case 0: return "Locked";
            case 1: return "Common";
            case 2: return "Uncommon";
            case 3: return "Rare";
            default: return "Locked";
        }
    }

    private void Load()
    {
        VendorLevel = Mathf.Clamp(PlayerPrefs.GetInt(VendorLevelKey, 0), 0, MaximumVendorLevel);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(VendorLevelKey, VendorLevel);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        VendorLevel = 0;
        Save();
        Changed?.Invoke();
    }
}