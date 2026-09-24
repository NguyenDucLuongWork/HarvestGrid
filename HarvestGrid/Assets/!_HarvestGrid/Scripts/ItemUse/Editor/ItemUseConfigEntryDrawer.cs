using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ItemUseConfigEntry))]
public class ItemUseConfigEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var typeProp = property.FindPropertyRelative("type");
        var plantProp = property.FindPropertyRelative("plantPrototype");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        var typeRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.PropertyField(typeRect, typeProp, label);

        if (IsAddPlant(typeProp))
        {
            var plantRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            EditorGUI.PropertyField(plantRect, plantProp, new GUIContent("Plant Prototype"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative("type");
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        int lines = IsAddPlant(typeProp) ? 2 : 1;
        return lines * lineHeight + (lines - 1) * spacing;
    }

    // Safer than a raw cast, since enumValueIndex is an index into the
    // declared values, not necessarily the underlying int value.
    private bool IsAddPlant(SerializedProperty typeProp)
    {
        var values = System.Enum.GetValues(typeof(ItemUseType));
        if (typeProp.enumValueIndex < 0 || typeProp.enumValueIndex >= values.Length)
            return false;

        var value = (ItemUseType)values.GetValue(typeProp.enumValueIndex);
        return value == ItemUseType.AddPlant;
    }
}