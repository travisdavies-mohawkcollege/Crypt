using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootGenerator", menuName = "Loot/Loot Generator")]
public class LootGenerator : ScriptableObject
{
    private static readonly EElements[] elementValues = (EElements[])Enum.GetValues(typeof(EElements));

    public List<ItemDefinition> commonItems = new();
    public List<ItemDefinition> uncommonItems = new();
    public List<ItemDefinition> rareItems = new();
    public List<ItemDefinition> epicItems = new();
    public List<ItemDefinition> legendaryItems = new();
    public List<ItemDefinition> mythicItems = new();
    public List<ItemDefinition> secretItems = new();


    public GeneratedItem GenerateLoot(Rarity rarity)
    {
        List<ItemDefinition> itemPool = GetItemPool(rarity);

        if (itemPool == null || itemPool.Count == 0)
        {
            Debug.LogWarning($"No items configured for rarity {rarity}.");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, itemPool.Count);
        ItemDefinition selectedDefinition = itemPool[randomIndex];

        if (selectedDefinition == null)
        {
            Debug.LogWarning($"Loot pool for {rarity} contains a null definition.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(selectedDefinition.ItemID))
        {
            Debug.LogWarning($"Selected item '{selectedDefinition.name}' has no item ID.");
            return null;
        }

        string instanceID = Guid.NewGuid().ToString();
        int bonusCount = GetBonusCount(rarity);

        List<GeneratedBonus> bonusStats = GenerateBonusStats(selectedDefinition, bonusCount);

        GeneratedItem generatedLoot = new GeneratedItem(instanceID, selectedDefinition.ItemID, bonusStats);

        return generatedLoot;
    }

    private List<ItemDefinition> GetItemPool(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return commonItems;

            case Rarity.Uncommon:
                return uncommonItems;

            case Rarity.Rare:
                return rareItems;

            case Rarity.Epic:
                return epicItems;

            case Rarity.Legendary:
                return legendaryItems;

            case Rarity.Mythic:
                return mythicItems;

            case Rarity.Secret:
                return secretItems;

            default:
                return null;
        }
    }

    private int GetBonusCount(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return 0;

            case Rarity.Uncommon:
                return 1;

            case Rarity.Rare:
                return 2;

            case Rarity.Epic:
                return 3;

            case Rarity.Legendary:
                return 4;

            case Rarity.Mythic:
                return 5;

            case Rarity.Secret:
                return 5;

            default:
                return 0;
        }
    }

    public List<GeneratedBonus> GenerateBonusStats(ItemDefinition definition, int amount)
    {
        List<GeneratedBonus> generatedBonuses = new();

        if (definition == null)
        {
            Debug.LogWarning("Cannot generate bonuses without an ItemDefinition.");
            return generatedBonuses;
        }

        if (amount <= 0) return generatedBonuses;

        if (definition.allowedBonusStats == null || definition.allowedBonusStats.Count == 0)
        {
            Debug.LogWarning($"{definition.ItemName} has no allowed bonus stats.");
            return generatedBonuses;
        }

        List<StatType> availableStats = new(definition.allowedBonusStats);
        availableStats.RemoveAll(stat => stat == StatType.None);

        int bonusCount = Mathf.Min(amount, availableStats.Count);

        for (int i = 0; i < bonusCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableStats.Count);
            StatType selectedStat = availableStats[randomIndex];
            EElements selectedElement = selectedStat == StatType.EAttack || selectedStat == StatType.EResist ? GenerateElement() : EElements.None;

            generatedBonuses.Add(new GeneratedBonus(selectedStat, selectedElement));
            availableStats.RemoveAt(randomIndex);
        }

        return generatedBonuses;
    }

    private EElements GenerateElement()
    {
        if (elementValues.Length <= 1)
        {
            Debug.LogWarning("No elements are configured.");
            return EElements.None;
        }

        return elementValues[UnityEngine.Random.Range(1, elementValues.Length)];
    }

    public GeneratedItem GenerateLoot(int dungeonLevel)
    {
        Rarity rarity = RollRarity(dungeonLevel);
        return GenerateLoot(rarity);
    }

    public Rarity RollRarity(int dungeonLevel)
    {
        dungeonLevel = Mathf.Clamp(dungeonLevel, 1, 60);

        if (dungeonLevel >= 60) return Rarity.Secret;

        int rarityLevel = Mathf.Clamp(dungeonLevel + UnityEngine.Random.Range(-5, 6), 1, 59);

        if (rarityLevel <= 10) return Rarity.Common;
        if (rarityLevel <= 20) return Rarity.Uncommon;
        if (rarityLevel <= 30) return Rarity.Rare;
        if (rarityLevel <= 40) return Rarity.Epic;
        if (rarityLevel <= 50) return Rarity.Legendary;

        return Rarity.Mythic;
    }

}
