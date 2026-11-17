using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIBars : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill1;
    [SerializeField] private Image staminaFill2;
    [SerializeField] private Image staminaFill3;
    [SerializeField] private TextMeshProUGUI goldText;
    private Inventory playerInventory;
    private const string GoldDefinitionID = "resource.gold";

    private PlayerCombatStats stats;

    private void OnEnable()
    {
        stats = GetComponentInParent<PlayerCombatStats>();
        if (stats != null)
        {
            stats.Changed += RefreshUI;
            RefreshUI();
        }
        if (PlayerManager.Instance == null) return;
        playerInventory = PlayerManager.Instance.PlayerInventory;
        playerInventory.Changed += RefreshGold;
        RefreshGold();
    }

    private void OnDisable()
    {
        if (stats != null) stats.Changed -= RefreshUI;
        if (playerInventory != null) playerInventory.Changed -= RefreshGold;
    }

    private void RefreshUI()
    {
        float maxHealth = stats.MaxHealth;
        healthFill.fillAmount = maxHealth > 0f ? Mathf.Clamp01(stats.CurrentHealth / maxHealth) : 0f;
        staminaFill1.fillAmount = Mathf.Clamp01(stats.CurrentStamina / 100f);
        staminaFill2.fillAmount = Mathf.Clamp01((stats.CurrentStamina - 100f) / 100f);
        staminaFill3.fillAmount = Mathf.Clamp01((stats.CurrentStamina - 200f) / 100f);
    }

    private void RefreshGold()
    {
        int goldAmount = 0;
        if (playerInventory.TryFindByDefinitionID(GoldDefinitionID, out InventoryEntry goldEntry)) goldAmount = goldEntry.quantity;
        goldText.text = goldAmount.ToString();
    }
}
