using UnityEngine;

// Everything the combat math cares about lives in this one file.
public enum Element
{
    Fire,
    Ice,
    Water,
    Air,
    Earth,
    Lightning,
    Arcane,
    Void,
    Light
}

// Implement this on the player (IDamageable) for element-based resistance
// Currentlyenemies fall back to plain IDamageable
public interface IElementalDamageable
{
    void TakeDamage(float damage, Element element);
}

// The one door every enemy hit goes through (melee swings and projectiles).
public static class DamageUtil
{
    public static void Hit(Component target, float amount, Element element)
    {
        if (target == null) return;

        IElementalDamageable elemental = target.GetComponentInParent<IElementalDamageable>();
        if (elemental != null)
        {
            elemental.TakeDamage(amount, element);
            return;
        }

        IDamageable plain = target.GetComponentInParent<IDamageable>();
        if (plain != null) plain.TakeDamage(amount);
    }

    private static readonly float[,] EnemyDamageMultipliers =
{
    // Fire   Water  Ice    Earth  Air    Lightning Void   Light  Arcane
    { 0.25f, 0.50f, 2.00f, 1.25f, 1.50f, 1.00f,    1.25f, 0.75f, 1.00f }, // Fire
    { 2.00f, 0.25f, 0.75f, 1.25f, 0.75f, 1.25f,    1.00f, 1.25f, 1.00f }, // Water
    { 0.50f, 1.75f, 0.25f, 1.50f, 1.25f, 1.00f,    1.00f, 1.00f, 1.25f }, // Ice
    { 0.75f, 2.00f, 1.25f, 0.50f, 0.75f, 2.00f,    0.75f, 0.75f, 0.75f }, // Earth
    { 1.25f, 1.50f, 1.00f, 0.75f, 0.75f, 1.25f,    1.00f, 0.75f, 1.25f }, // Air
    { 1.00f, 2.00f, 1.50f, 0.25f, 1.50f, 0.50f,    1.00f, 1.00f, 0.75f }, // Lightning
    { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 0.75f,    0.50f, 2.00f, 1.25f }, // Void
    { 0.50f, 1.25f, 1.25f, 1.25f, 0.50f, 1.00f,    2.00f, 0.50f, 0.75f }, // Light
    { 1.00f, 1.00f, 1.00f, 0.75f, 1.00f, 0.75f,    1.75f, 1.75f, 0.50f }  // Arcane
};

    public static float EnemyDamageAfterResists(Element enemyElement, Element damageElement, float damage)
    {
        int attackRow = GetElementChartIndex(damageElement);
        int enemyColumn = GetElementChartIndex(enemyElement);

        if (attackRow < 0 || enemyColumn < 0)
        {
            return damage;
        }

        return damage * EnemyDamageMultipliers[attackRow, enemyColumn];
    }

    private static int GetElementChartIndex(Element element)
    {
        return element switch
        {
            Element.Fire => 0,
            Element.Water => 1,
            Element.Ice => 2,
            Element.Earth => 3,
            Element.Air => 4,
            Element.Lightning => 5,
            Element.Void => 6,
            Element.Light => 7,
            Element.Arcane => 8,
            _ => -1
        };
    }
}