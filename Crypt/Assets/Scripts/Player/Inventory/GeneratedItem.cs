using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
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
