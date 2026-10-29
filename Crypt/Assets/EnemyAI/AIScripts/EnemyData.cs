using UnityEngine;

[System.Serializable]
public struct EnemyStats
{
    public float maxHealth;
    public float damage;
}

[CreateAssetMenu(fileName = "Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Info")]
    public string EnemyName;
    public EnemyRarity EnemyTier;
    public GameObject EnemyPrefab;

    [Header("Combat")]
    public float combatRange = 1.5f;

    [Header("Base Stats (Level 1)")]
    public float maxHealth = 50f;
    public float damage = 8f;
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float alertedSpeed = 2.2f;

    [Header("Vision & Detection")]
    public float sightRange = 10f;
    [Range(10f, 360f)] public float sightAngle = 90f;
    public float noticeTime = 1.2f;
    public float forgetTime = 4f;

    [Header("Level Scaling")]
    public int maxLevel = 60;
    public float healthGrowth = 0.15f;
    public float damageGrowth = 0.1f;

    public EnemyStats GetStats(int level)
    {
        int bonusLevels = Mathf.Clamp(level, 1, maxLevel) - 1;

        EnemyStats stats = new EnemyStats();
        stats.maxHealth = maxHealth * (1f + healthGrowth * bonusLevels);
        stats.damage = damage * (1f + damageGrowth * bonusLevels);
        return stats;
    }
}