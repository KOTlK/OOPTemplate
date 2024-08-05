using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ResourceLink))]
public class ResourceLinkEditor : PropertyDrawer {
    private const string ResourcesPath = "Assets/Resources/";
    private const float  FoldoutHeight = 16f;

    private bool _foldout = false;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if(_foldout) {
            return FoldoutHeight + 
                   EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Reference")) + 
                   EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Path"));
        } else {
            return FoldoutHeight;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        _foldout = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, FoldoutHeight), _foldout, label);
        var indent = EditorGUI.indentLevel;

        if(_foldout) {
            var reference = property.FindPropertyRelative("Reference");
            var pathProp  = property.FindPropertyRelative("Path");
            var referenceRect = new Rect(position.x, FoldoutHeight, position.width, EditorGUI.GetPropertyHeight(reference));
            var pathRect = new Rect(position.x, FoldoutHeight + EditorGUI.GetPropertyHeight(reference), position.width, EditorGUI.GetPropertyHeight(pathProp));

            EditorGUI.indentLevel += 2;

            var obj = EditorGUI.ObjectField(referenceRect, 
                                            "Reference", 
                                            reference.objectReferenceValue,
                                            typeof(GameObject),
                                            false);

            if(obj) {
                var path = AssetDatabase.GetAssetPath(obj);

                if(path.StartsWith(ResourcesPath)) {
                    reference.objectReferenceValue = obj;
                    
                    path = path.Substring(ResourcesPath.Length);

                    //remove extension
                    for(var i = path.Length - 1; i >= 0; --i) {
                        if(path[i] == '.') {
                            path = path.Substring(0, i);
                            break;
                        }
                    }
                    pathProp.stringValue = path;
                } 

                
            } else {
                reference.boxedValue = null;
                pathProp.stringValue = " ";
            }

            var txt = EditorGUI.TextField(pathRect, "Path", pathProp.stringValue);

            if(reference.boxedValue == null) {
                pathProp.stringValue = txt;
            }

            EditorGUI.indentLevel = indent;
        }
        EditorGUI.EndProperty();
    }
}