using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Generated it with chatgpt, I don't think any sane person would do it manually.
[CustomPropertyDrawer(typeof(NameIdent))]
public class NameIdentDrawer : PropertyDrawer {
    public static readonly Dictionary<string, bool> FoldoutStates = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        var key = property.propertyPath;
        if (!FoldoutStates.ContainsKey(key)) {
            FoldoutStates[key] = false;
        }

        var nameProp  = property.FindPropertyRelative("Name");
        var identProp = property.FindPropertyRelative("Ident");

        // Foldout rectangle
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        FoldoutStates[key] = EditorGUI.Foldout(foldoutRect, FoldoutStates[key], label, true);

        if (FoldoutStates[key]) {
            // Calculate heights
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing    = EditorGUIUtility.standardVerticalSpacing;
            var yPos       = position.y + lineHeight + spacing;

            // Name field
            var nameRect = new Rect(
                position.x + 15,
                yPos,
                position.width - 15,
                lineHeight
            );
            yPos += lineHeight + spacing;

            // Ident field
            var identRect = new Rect(
                position.x + 15,
                yPos,
                position.width - 15,
                lineHeight
            );
            yPos += lineHeight + spacing;

            // Localized text field (multiline)
            var textHeight = CalculateTextHeight(identProp.intValue);
            var valueRect = new Rect(
                position.x + 15,
                yPos,
                position.width - 15,
                textHeight
            );

            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Name"));
            EditorGUI.PropertyField(identRect, identProp, new GUIContent("Identifier"));

            if (Locale.Has(identProp.intValue)) {
                GUI.enabled = false;
                EditorGUI.TextArea(valueRect, Locale.Get(identProp.intValue));
                GUI.enabled = true;
            }
            else {
                EditorGUI.HelpBox(valueRect, "No localization found", MessageType.Warning);
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        var key = property.propertyPath;

        if (FoldoutStates.ContainsKey(key) && FoldoutStates[key]) {
            var baseHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
            var identProp  = property.FindPropertyRelative("Ident");
            var textHeight = CalculateTextHeight(identProp.intValue);

            return baseHeight + textHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        return EditorGUIUtility.singleLineHeight;
    }

    private float CalculateTextHeight(int ident) {
        if (!Locale.Has(ident)) {
            return EditorGUIUtility.singleLineHeight; // Height for warning box
        }

        var text = Locale.Get(ident);
        if (string.IsNullOrEmpty(text)) {
            return EditorGUIUtility.singleLineHeight;
        }

        // Calculate required height based on text content
        var style = EditorStyles.textArea;
        var width = EditorGUIUtility.currentViewWidth - 30; // Account for indentation

        return style.CalcHeight(new GUIContent(text), width);
    }
}

[CustomPropertyDrawer(typeof(LocalizedString))]
public class LocalizedStringDrawer : PropertyDrawer {
    private bool showIdents = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var identProp = property.FindPropertyRelative("Ident");
        var identsProp = property.FindPropertyRelative("Idents");

        // Calculate rects
        var mainRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var identRect = new Rect(position.x + 20, position.y + EditorGUIUtility.singleLineHeight, position.width - 20, EditorGUIUtility.singleLineHeight);
        var valueRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2, position.width, EditorGUIUtility.singleLineHeight);
        var identsRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 3, position.width, EditorGUIUtility.singleLineHeight * (identsProp.arraySize + 1));

        // Draw foldout
        showIdents = EditorGUI.Foldout(foldoutRect, showIdents, label, true);

        if (showIdents) {
            // Draw main ident
            EditorGUI.PropertyField(identRect, identProp, new GUIContent("Ident"));

            // Draw localized value if available
            if (Locale.Has(identProp.intValue)) {
                GUI.enabled = false;
                EditorGUI.TextField(valueRect, "Localized Text", Locale.Get(identProp.intValue));
                GUI.enabled = true;
            }
            else {
                EditorGUI.HelpBox(valueRect, "No localization found for main ID, Load default with Localization/Load English", MessageType.Warning);
            }

            // Draw idents array
            EditorGUI.PropertyField(identsRect, identsProp, new GUIContent("Idents"), true);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        var identsProp = property.FindPropertyRelative("Idents");
        float height = EditorGUIUtility.singleLineHeight;

        if (showIdents)
        {
            height += EditorGUIUtility.singleLineHeight * 2; // For ident and value fields
            height += EditorGUI.GetPropertyHeight(identsProp, true); // For idents array
        }

        return height;
    }
}