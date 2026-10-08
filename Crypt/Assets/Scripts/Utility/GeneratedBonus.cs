using System;

[Serializable]
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