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
}