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
        property.isExpanded = EditorGUI.Foldout(
            GetNextLine(ref position),
            property.isExpanded,
            $"{label.text}: {modifierName}",
            true
        );

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

            // Value labels use their own line so the input fields remain usable in a narrow Inspector.
            lineCount += randomRange ? 7 : 5;
        }

        return GetHeight(lineCount);
    }

    private void DrawVectorFields(ref Rect position, SerializedProperty property, bool randomRange)
    {
        if (!randomRange)
        {
            DrawVectorField(ref position, property.FindPropertyRelative("vectorValue"), "Value");
            return;
        }

        DrawVectorField(ref position, property.FindPropertyRelative("minimumVectorValue"), "Minimum Value");
        DrawVectorField(ref position, property.FindPropertyRelative("maximumVectorValue"), "Maximum Value");
    }

    private void DrawVectorField(ref Rect position, SerializedProperty vectorProperty, string label)
    {
        EditorGUI.LabelField(GetNextLine(ref position), label);
        EditorGUI.PropertyField(GetNextLine(ref position), vectorProperty, GUIContent.none);
    }

    private void DrawIntFields(ref Rect position, SerializedProperty property, bool randomRange)
    {
        if (!randomRange)
        {
            DrawValueField(ref position, property.FindPropertyRelative("intValue"), "Value");
            return;
        }

        DrawValueField(ref position, property.FindPropertyRelative("minimumIntValue"), "Minimum Value");
        DrawValueField(ref position, property.FindPropertyRelative("maximumIntValue"), "Maximum Value");
    }

    private void DrawFloatFields(ref Rect position, SerializedProperty property, bool randomRange)
    {
        if (!randomRange)
        {
            DrawValueField(ref position, property.FindPropertyRelative("floatValue"), "Value");
            return;
        }

        DrawValueField(ref position, property.FindPropertyRelative("minimumFloatValue"), "Minimum Value");
        DrawValueField(ref position, property.FindPropertyRelative("maximumFloatValue"), "Maximum Value");
    }

    private void DrawValueField(ref Rect position, SerializedProperty valueProperty, string label)
    {
        EditorGUI.LabelField(GetNextLine(ref position), label);
        EditorGUI.PropertyField(GetNextLine(ref position), valueProperty, GUIContent.none);
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
        // Include spacing after the last line so neighbouring list entries do not touch.
        return lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }
}
