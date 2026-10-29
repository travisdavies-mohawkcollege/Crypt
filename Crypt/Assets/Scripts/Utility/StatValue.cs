using System;
using UnityEngine;

[Serializable]
// Serializable stat/value pair used for base values authored in the Inspector.
public class StatValue
{
    public StatType statType;
    public float value;
}
