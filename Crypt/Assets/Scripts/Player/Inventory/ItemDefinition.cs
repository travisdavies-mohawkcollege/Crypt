
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Items")]
public class ItemDefinition : ScriptableObject
{
    public string ItemID;
    public string ItemName;
    public string ItemDescription;
    public Rarity ItemRarity;
    public Sprite ItemSprite;
    public int CardSlots;
    public GameObject ItemPrefab;
    public ItemType ItemType;
    public int StackLimit;

    public List<StatValue> baseStats = new();
    public List<StatType> allowedBonusStats = new();

    public bool TryGetBaseStat(StatType statType, out float value)
    {
        foreach(StatValue stat in baseStats)
        {
            if(stat.statType == statType)
            {
                value = stat.value;
                return true;
            }
        }

        value = 0f;
        return false;
    }
}
