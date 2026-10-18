using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerCombatStats : MonoBehaviour
{
    [Serializable]
    private class ElementalValue
    {
        public EElements element;
        public float value;
    }

    [Header("References")]
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private StatScalingTable statScalingTable;

    [Header("Base Resources")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseMaxStamina = 100f;

    [Header("Base Stats")]
    [SerializeField] private List<StatValue> baseStats = new();
    [SerializeField] private List<ElementalValue> baseElementalAttack = new();
    [SerializeField] private List<ElementalValue> baseElementalResistance = new();

    private readonly Dictionary<StatType, float> stats = new();
    private readonly Dictionary<EElements, float> elementalAttack = new();
    private readonly Dictionary<EElements, float> elementalResistance = new();
    private bool initialized;

    public event Action Changed;
    public string CurrentStatsText => BuildStatsText();

    public float CurrentHealth { get; private set; }
    public float CurrentStamina { get; private set; }
    public float MaxHealth => GetStat(StatType.Health);
    public float MaxStamina => GetStat(StatType.BonusStamina);
    public bool IsDead => CurrentHealth <= 0f;

    private void OnEnable()
    {
        SubscribeToEquipmentChanges();
        RebuildStats();
    }

    private void Start()
    {
        SubscribeToEquipmentChanges();
        RebuildStats();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.PlayerEquipment.Changed -= HandleEquipmentChanged;
    }

    private void SubscribeToEquipmentChanges()
    {
        if (PlayerManager.Instance == null) return;
        PlayerManager.Instance.PlayerEquipment.Changed -= HandleEquipmentChanged;
        PlayerManager.Instance.PlayerEquipment.Changed += HandleEquipmentChanged;
    }

    private void HandleEquipmentChanged()
    {
        RebuildStats();
        LogCurrentStats();
    }

    public void RebuildStats()
    {
        float previousHealth = CurrentHealth;
        float previousStamina = CurrentStamina;

        stats.Clear();
        elementalAttack.Clear();
        elementalResistance.Clear();

        AddStat(StatType.Health, baseMaxHealth);
        AddStat(StatType.BonusStamina, baseMaxStamina);

        foreach (StatValue stat in baseStats)
        {
            if (stat == null) continue;
            AddStat(stat.statType, stat.value);
        }

        foreach (ElementalValue value in baseElementalAttack)
        {
            if (value == null) continue;
            AddElementalAttack(value.element, value.value);
        }

        foreach (ElementalValue value in baseElementalResistance)
        {
            if (value == null) continue;
            AddElementalResistance(value.element, value.value);
        }

        AddEquipmentStats();

        if (!initialized)
        {
            CurrentHealth = MaxHealth;
            CurrentStamina = MaxStamina;
            initialized = true;
        }
        else
        {
            CurrentHealth = Mathf.Clamp(previousHealth, 0f, MaxHealth);
            CurrentStamina = Mathf.Clamp(previousStamina, 0f, MaxStamina);
        }

        Changed?.Invoke();
    }

    public void LogCurrentStats()
    {
        Debug.Log($"=== Gear Changed ===\n{CurrentStatsText}", this);
    }

    private string BuildStatsText()
    {
        StringBuilder text = new();
        text.AppendLine($"Health: {CurrentHealth:0.##} / {MaxHealth:0.##}");
        text.AppendLine($"Stamina: {CurrentStamina:0.##} / {MaxStamina:0.##}");
        text.AppendLine();
        text.AppendLine("General Stats:");

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            if (statType == StatType.None || statType == StatType.Health || statType == StatType.BonusStamina || statType == StatType.EAttack || statType == StatType.EResist) continue;
            text.AppendLine($"{statType}: {GetStat(statType):0.##}");
        }

        text.AppendLine();
        text.AppendLine("Elemental Stats:");

        bool hasElementalStats = false;

        foreach (EElements element in Enum.GetValues(typeof(EElements)))
        {
            if (element == EElements.None) continue;

            float attack = GetElementalAttack(element);
            float resistance = GetElementalResistance(element);

            if (Mathf.Approximately(attack, 0f) && Mathf.Approximately(resistance, 0f)) continue;

            text.AppendLine($"{element}: Attack {attack:0.##}, Resistance {resistance:0.##}");
            hasElementalStats = true;
        }

        if (!hasElementalStats) text.AppendLine("None");

        return text.ToString().TrimEnd();
    }

    private void AddEquipmentStats()
    {
        if (PlayerManager.Instance == null || itemCatalog == null) return;

        HashSet<string> processedInstances = new();

        foreach (EquippedItem equippedItem in PlayerManager.Instance.PlayerEquipment.EquippedItems)
        {
            if (equippedItem == null || string.IsNullOrWhiteSpace(equippedItem.instanceID)) continue;
            if (!processedInstances.Add(equippedItem.instanceID)) continue;

            if (!PlayerManager.Instance.PlayerInventory.TryFindByInstanceID(equippedItem.instanceID, out InventoryEntry inventoryEntry))
            {
                Debug.LogWarning($"Equipped item '{equippedItem.instanceID}' was not found in the player inventory.", this);
                continue;
            }

            GeneratedItem generatedItem = inventoryEntry.item;

            if (!itemCatalog.TryGetItemDefinition(generatedItem.definitionID, out ItemDefinition definition))
            {
                Debug.LogWarning($"Could not find item definition '{generatedItem.definitionID}'.", this);
                continue;
            }

            AddItemBaseStats(definition);
            AddGeneratedBonuses(generatedItem, definition);
        }
    }

    private void AddItemBaseStats(ItemDefinition definition)
    {
        if (definition?.baseStats == null) return;

        foreach (StatValue stat in definition.baseStats)
        {
            if (stat == null) continue;
            AddStat(stat.statType, definition.ItemElement, stat.value);
        }
    }

    private void AddGeneratedBonuses(GeneratedItem generatedItem, ItemDefinition definition)
    {
        if (generatedItem?.bonusStats == null || statScalingTable == null) return;

        foreach (GeneratedBonus bonus in generatedItem.bonusStats)
        {
            if (bonus == null || bonus.statType == StatType.None) continue;

            float value = statScalingTable.GetBonusValue(bonus.statType, definition.ItemRarity);
            AddStat(bonus.statType, bonus.element, value);
        }
    }

    private void AddStat(StatType statType, EElements element, float value)
    {
        switch (statType)
        {
            case StatType.EAttack:
                AddElementalAttack(element, value);
                return;

            case StatType.EResist:
                AddElementalResistance(element, value);
                return;

            default:
                AddStat(statType, value);
                return;
        }
    }

    private void AddStat(StatType statType, float value)
    {
        if (statType == StatType.None) return;
        stats[statType] = GetStat(statType) + value;
    }

    private void AddElementalAttack(EElements element, float value)
    {
        if (element == EElements.None) return;
        elementalAttack[element] = GetElementalAttack(element) + value;
    }

    private void AddElementalResistance(EElements element, float value)
    {
        if (element == EElements.None) return;
        elementalResistance[element] = GetElementalResistance(element) + value;
    }

    public float GetStat(StatType statType)
    {
        return stats.TryGetValue(statType, out float value) ? value : 0f;
    }

    public float GetElementalAttack(EElements element)
    {
        return elementalAttack.TryGetValue(element, out float value) ? value : 0f;
    }

    public float GetElementalResistance(EElements element)
    {
        return elementalResistance.TryGetValue(element, out float value) ? value : 0f;
    }

    public float GetMoveSpeed(float defaultSpeed)
    {
        float flatSpeed = defaultSpeed + GetStat(StatType.MoveSpeed);
        return flatSpeed * (1f + GetStat(StatType.MoveSpeedPercent) / 100f);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Changed?.Invoke();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        Changed?.Invoke();
    }

    public bool TrySpendStamina(float amount)
    {
        if (amount < 0f || CurrentStamina < amount) return false;
        CurrentStamina -= amount;
        Changed?.Invoke();
        return true;
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f) return;
        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
        Changed?.Invoke();
    }
}
