using System.Xml.XPath;
using UnityEngine;

public static class EnemyXPDrop
{
    public static int GetSkeletonXP(int level)
    {
        float xp = 10f * Mathf.Pow(100f, (level - 1) / 49f);
        return (int)xp;
    }

    public static float GetLevelDifferenceMultiplier(int levelDifference)
    {
        const float changePerLevel = 0.10f;
        return Mathf.Clamp(1f + levelDifference * changePerLevel, 0f, 2f);
    }

    public static int GetSkeletonXPAwarded(int enemyLevel, int playerLevel)
    {
        float baseXp = GetSkeletonXP(enemyLevel);
        float multiplier = GetLevelDifferenceMultiplier(enemyLevel - playerLevel);
        return Mathf.RoundToInt(baseXp * multiplier);
    }
}
