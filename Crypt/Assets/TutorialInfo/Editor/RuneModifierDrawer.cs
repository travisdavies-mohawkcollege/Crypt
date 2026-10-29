using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RuneModifier))]
public class RuneModifierDrawer : PropertyDrawer
{
    private enum ValueType
    {
        Vector,
        Bool,
        Int,
        Float
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty modifierType = property.FindPropertyRelative("modifierType");
        SerializedProperty operation = property.FindPropertyRelative("operation");
        SerializedProperty valueMode = property.FindPropertyRelative("valueMode");

        string modifierName = modifierType.enumDisplayNames[modifierType.enumValueIndex];
        Rect currentLine = GetNextLine(ref position);

        property.isExpanded = EditorGUI.Foldout(currentLine, property.isExpanded, $"{label.text}: {modifierName}", true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        EditorGUI.PropertyField(GetNextLine(ref position), modifierType);

        ModifierType selectedType = (ModifierType)modifierType.intValue;
        ValueType selectedValueType = GetValueType(selectedType);

        if (selectedValueType == ValueType.Bool)
        {
            SerializedProperty boolValue = property.FindPropertyRelative("boolValue");
            EditorGUI.PropertyField(GetNextLine(ref position), boolValue, new GUIContent("Value"));
        }
        else
        {
            EditorGUI.PropertyField(GetNextLine(ref position), operation);
            EditorGUI.PropertyField(GetNextLine(ref position), valueMode);

            bool randomRange = (ModifierValueMode)valueMode.intValue == ModifierValueMode.RandomRange;

            switch (selectedValueType)
            {
                case ValueType.Vector:
                    DrawVectorFields(ref position, property, randomRange);
                    break;

                case ValueType.Int:
                    DrawIntFields(ref position, property, randomRange);
                    break;

                case ValueType.Float:
                    DrawFloatFields(ref position, property, randomRange);
                    break;
            }
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 1;

        if (!property.isExpanded) return GetHeight(lineCount);

        SerializedProperty modifierType = property.FindPropertyRelative("modifierType");
        ValueType selectedValueType = GetValueType((ModifierType)modifierType.intValue);

        if (selectedValueType == ValueType.Bool)
        {
            lineCount += 2;
        }
        else
        {
            SerializedProperty valueMode = property.FindPropertyRelative("valueMode");
            bool randomRange = (ModifierValueMode)valueMode.intValue == ModifierValueMode.RandomRange;

            lineCount += randomRange ? 5 : 4;
        }

        return GetHeight(lineCount);
    }

    private void DrawVectorFields(ref Rect position, SerializedProperty property, bool randomRange)
    {
        if (!randomRange)
        {
            EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("vectorValue"), new GUIContent("Value"));
            return;
        }

        EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("minimumVectorValue"), new GUIContent("Minimum Value"));
        EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("maximumVectorValue"), new GUIContent("Maximum Value"));
    }

    private void DrawIntFields(ref Rect position, SerializedProperty property, bool randomRange)
    {
        if (!randomRange)
        {
            EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("intValue"), new GUIContent("Value"));
            return;
        }

        EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("minimumIntValue"), new GUIContent("Minimum Value"));
        EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("maximumIntValue"), new GUIContent("Maximum Value"));
    }

    private void DrawFloatFields(ref Rect position, SerializedProperty property, bool randomRange)
    {
        if (!randomRange)
        {
            EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("floatValue"), new GUIContent("Value"));
            return;
        }

        EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("minimumFloatValue"), new GUIContent("Minimum Value"));
        EditorGUI.PropertyField(GetNextLine(ref position), property.FindPropertyRelative("maximumFloatValue"), new GUIContent("Maximum Value"));
    }

    private ValueType GetValueType(ModifierType modifierType)
    {
        switch (modifierType)
        {
            case ModifierType.GridSize:
                return ValueType.Vector;

            case ModifierType.LightsOut:
            case ModifierType.GenerateBranches:
                return ValueType.Bool;

            case ModifierType.TargetRooms:
            case ModifierType.NumberOfBranches:
            case ModifierType.BranchLength:
                return ValueType.Int;

            case ModifierType.LoopChance:
            case ModifierType.StraightChance:
            case ModifierType.NextFloorChanceMin:
                return ValueType.Float;

            default:
                return ValueType.Int;
        }
    }

    private Rect GetNextLine(ref Rect position)
    {
        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        return line;
    }

    private float GetHeight(int lineCount)
    {
        return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}