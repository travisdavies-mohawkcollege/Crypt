using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ElementalManaVisual
{
    public EElements element;
    public Sprite manaBarSprite;
}

public class PlayerUIBars : MonoBehaviour
{
    [Header("Health and Stamina")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill1;
    [SerializeField] private Image staminaFill2;
    [SerializeField] private Image staminaFill3;

    [Header("Elemental Mana")]
    [SerializeField] private Image manaFill;
    [SerializeField] private Sprite defaultManaBarSprite;
    [SerializeField] private List<ElementalManaVisual> manaVisuals = new();

    [Header("Gold")]
    [SerializeField] private TextMeshProUGUI goldText;

    private const string GoldDefinitionID = "resource.gold";
    private Inventory playerInventory;
    private PlayerCombatStats stats;
    private PlayerController playerController;
    private PlayerElementalMana elementalMana;

    private void OnEnable()
    {
        stats = GetComponentInParent<PlayerCombatStats>();
        playerController = GetComponentInParent<PlayerController>();
        elementalMana = GetComponentInParent<PlayerElementalMana>();

        if (stats != null) stats.Changed += RefreshUI;
        if (playerController != null) playerController.ActiveElementChanged += RefreshMana;
        if (elementalMana != null) elementalMana.Changed += RefreshMana;

        RefreshUI();
        RefreshMana();

        if (PlayerManager.Instance == null) return;

        playerInventory = PlayerManager.Instance.PlayerInventory;
        playerInventory.Changed += RefreshGold;
        RefreshGold();
    }

    private void OnDisable()
    {
        if (stats != null) stats.Changed -= RefreshUI;
        if (playerController != null) playerController.ActiveElementChanged -= RefreshMana;
        if (elementalMana != null) elementalMana.Changed -= RefreshMana;
        if (playerInventory != null) playerInventory.Changed -= RefreshGold;
    }

    private void RefreshUI()
    {
        if (stats == null) return;

        float maxHealth = stats.MaxHealth;
        healthFill.fillAmount = maxHealth > 0f ? Mathf.Clamp01(stats.CurrentHealth / maxHealth) : 0f;
        staminaFill1.fillAmount = Mathf.Clamp01(stats.CurrentStamina / 100f);
        staminaFill2.fillAmount = Mathf.Clamp01((stats.CurrentStamina - 100f) / 100f);
        staminaFill3.fillAmount = Mathf.Clamp01((stats.CurrentStamina - 200f) / 100f);
    }

    private void RefreshMana()
    {
        if (playerController == null || elementalMana == null || manaFill == null) return;

        EElements activeElement = playerController.ActiveElement;

        if (activeElement == EElements.None)
        {
            manaFill.sprite = null;
            manaFill.enabled = false;
            manaFill.fillAmount = 0f;
            return;
        }

        float currentMana = elementalMana.GetCurrent(activeElement);
        float maximumMana = elementalMana.GetMaximum(activeElement);
        manaFill.fillAmount = maximumMana > 0f ? Mathf.Clamp01(currentMana / maximumMana) : 0f;

        ElementalManaVisual visual = manaVisuals.Find(entry => entry.element == activeElement);
        manaFill.sprite = visual != null ? visual.manaBarSprite : null;
        manaFill.enabled = manaFill.sprite != null;
    }

    private void RefreshGold()
    {
        if (playerInventory == null || goldText == null) return;

        int goldAmount = 0;

        if (playerInventory.TryFindByDefinitionID(GoldDefinitionID, out InventoryEntry goldEntry)) goldAmount = goldEntry.quantity;

        goldText.text = $"Gold: {goldAmount}";
    }
}