using System;
using UnityEngine;

[Serializable]
public class RuneModifier
{
    public ModifierType modifierType;
    public ModifierOperation operation;
    public ModifierValueMode valueMode;

    public Vector3Int vectorValue;
    public Vector3Int minimumVectorValue;
    public Vector3Int maximumVectorValue;

    public bool boolValue;

    public int intValue;
    public int minimumIntValue;
    public int maximumIntValue;

    public float floatValue;
    public float minimumFloatValue;
    public float maximumFloatValue;

    public Vector3Int GetVectorValue()
    {
        if (valueMode == ModifierValueMode.Fixed) return vectorValue;

        return new Vector3Int(
            UnityEngine.Random.Range(minimumVectorValue.x, maximumVectorValue.x + 1),
            UnityEngine.Random.Range(minimumVectorValue.y, maximumVectorValue.y + 1),
            UnityEngine.Random.Range(minimumVectorValue.z, maximumVectorValue.z + 1)
        );
    }

    public int GetIntValue()
    {
        if (valueMode == ModifierValueMode.Fixed) return intValue;
        return UnityEngine.Random.Range(minimumIntValue, maximumIntValue + 1);
    }

    public float GetFloatValue()
    {
        if (valueMode == ModifierValueMode.Fixed) return floatValue;
        return UnityEngine.Random.Range(minimumFloatValue, maximumFloatValue);
    }
}