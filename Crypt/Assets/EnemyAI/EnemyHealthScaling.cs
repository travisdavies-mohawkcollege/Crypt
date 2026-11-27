using UnityEngine;

public static class EnemyHealthScaling
{
    public static float GetRangedSkeletonHP(int level)
    {
        if (level <= 50)
        {
            return 125f * Mathf.Pow(240f, (level - 1) / 49f);
        }

        return 30000f * Mathf.Pow(10f, (level - 50) / 10f);
    }

    public static float GetMeleeSkeletonHP(int level)
    {
        if (level <= 50)
        {
            return 100f * Mathf.Pow(450f, (level - 1) / 49f);
        }

        return 45000f * Mathf.Pow(275000f / 45000f, (level - 50) / 10f);
    }
}

