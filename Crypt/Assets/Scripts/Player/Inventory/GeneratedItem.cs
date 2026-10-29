using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// Runtime item instance containing stable identity and bonuses rolled during generation.
public class GeneratedItem
{
    public string instanceID;
    public string definitionID;
    public List<GeneratedBonus> bonusStats = new();

    public GeneratedItem(string instanceID, string definitionID, List<GeneratedBonus> bonusStats)
    {
        this.instanceID = instanceID;
        this.definitionID = definitionID;
        this.bonusStats = bonusStats;
    }
}
