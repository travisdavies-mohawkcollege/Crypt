using UnityEngine;

public static class CombatMath
{
    private const float MaximumResistance = 80f;
    private const float MinimumResistance = -100f;

    public static float CalculateDamageAfterResistance(float rawDamage, float resistance)
    {
        //temp until real formulas done
        if (rawDamage <= 0f) return 0f;

        resistance = Mathf.Clamp(resistance, MinimumResistance, MaximumResistance);

        float multiplier = 1f - resistance / 100f;
        return Mathf.Max(0f, rawDamage * multiplier);
    }
}
