using System;

[Serializable]
// Records one rolled bonus on a generated item; its value is resolved from the scaling table.
public class GeneratedBonus
{
    public StatType statType;
    public EElements element;

    public GeneratedBonus(StatType statType, EElements element)
    {
        this.statType = statType;
        this.element = element;
    }
}
