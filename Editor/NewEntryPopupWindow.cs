using UnityEngine;
using UnityEditor;

public class NewEntryPopupWindow : EditorWindow {
    public static LocaleEntry              Entry;
    public static LocalizationEditorWindow Main;

    public static NewEntryPopupWindow Open;

    public static void ShowPopup(LocalizationEditorWindow main) {
        if(Open != null) {
            EditorUtility.DisplayDialog("Error", "Can't open another New Entry window, close the previous one", "OK");
            return;
        }

        var window = GetWindow<NewEntryPopupWindow>();
        Main = main;
        window.titleContent = new GUIContent("Add New Localization Entry");
        window.minSize = new Vector2(400, 200);
        window.maxSize = new Vector2(400, 200);
        window.ShowUtility();
        Entry.Ident = Random.Range(int.MinValue, int.MaxValue);
        Open = window;
    }

    private void OnDestroy() {
        Entry = new();
        Open  = null;
    }

    private void OnGUI() {
        EditorGUILayout.Space();

        EditorGUILayout.Space();
        GUILayout.Label("Add New Entry", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Random", GUILayout.Width(70))) {
            Entry.Ident = Random.Range(int.MinValue, int.MaxValue);
        }
        Entry.Ident = EditorGUILayout.IntField("Identifier", Entry.Ident);
        if(GUILayout.Button("Copy", GUILayout.Width(50))) {
            EditorGUIUtility.systemCopyBuffer = Entry.Ident.ToString();
        }
        EditorGUILayout.EndHorizontal();

        Entry.Tag = (LocalizationTag)EditorGUILayout.EnumPopup("Tag", Entry.Tag);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Text", GUILayout.Width(120));
        Entry.Text = EditorGUILayout.TextArea(Entry.Text, GUILayout.Height(40));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Comment", GUILayout.Width(120));
        Entry.Comment = EditorGUILayout.TextArea(Entry.Comment, GUILayout.Height(40));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(100))) {
            this.Close();
        }

        if (GUILayout.Button("Save", GUILayout.Width(100))) {
            if (string.IsNullOrEmpty(Entry.Text)) {
                EditorUtility.DisplayDialog("Error", "Text cannot be empty", "OK");
                return;
            }

            if (Main.Entries.Exists(e => e.Ident == Entry.Ident)) {
                if (EditorUtility.DisplayDialog("Identifier Exists",
                    $"Identifier {Entry.Ident} already exists. Overwrite?",
                    "Yes", "No")) {
                    var index = Main.Entries.FindIndex(e => e.Ident == Entry.Ident);
                    Main.Entries[index] = new LocaleEntry {
                        Ident   = Entry.Ident,
                        Tag     = Entry.Tag,
                        Text    = Entry.Text,
                        Comment = Entry.Comment
                    };
                }
            }
            else {
                Main.Entries.Add(new LocaleEntry {
                    Ident   = Entry.Ident,
                    Tag     = Entry.Tag,
                    Text    = Entry.Text,
                    Comment = Entry.Comment
                });
            }

            this.Close();
        }

        EditorGUILayout.EndHorizontal();
    }
}