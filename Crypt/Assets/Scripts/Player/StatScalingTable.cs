using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatRarityValues
{
    public StatType statType;
    public float common;
    public float uncommon;
    public float rare;
    public float epic;
    public float legendary;
    public float mythic;
    public float secret;

    public float GetValue(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return common;

            case Rarity.Uncommon:
                return uncommon;

            case Rarity.Rare:
                return rare;

            case Rarity.Epic:
                return epic;

            case Rarity.Legendary:
                return legendary;

            case Rarity.Mythic:
                return mythic;

            case Rarity.Secret:
                return secret;

            default:
                return 0f;
        }
    }
}

[CreateAssetMenu(fileName = "StatScalingTable", menuName = "Stats/Stat Scaling Table")]
public class StatScalingTable : ScriptableObject
{
    [SerializeField] private List<StatRarityValues> bonusValues = new();
    private Dictionary<StatType, StatRarityValues> lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void OnValidate()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<StatType, StatRarityValues>();

        foreach (StatRarityValues values in bonusValues)
        {
            if (values == null || values.statType == StatType.None) continue;

            if (!lookup.TryAdd(values.statType, values)) Debug.LogWarning($"StatScalingTable contains more than one entry for {values.statType}.", this);
        }
    }

    public bool TryGetBonusValue(StatType statType, Rarity rarity, out float value)
    {
        if (lookup == null) BuildLookup();

        if (lookup.TryGetValue(statType, out StatRarityValues values))
        {
            value = values.GetValue(rarity);
            return true;
        }

        value = 0f;
        return false;
    }

    public float GetBonusValue(StatType statType, Rarity rarity)
    {
        return TryGetBonusValue(statType, rarity, out float value) ? value : 0f;
    }
}
