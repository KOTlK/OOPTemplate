using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using static Locale;
using static Assertions;

public struct LocaleEntry {
    public int             Ident;
    public string          Text;
    public string          Comment;
    public LocalizationTag Tag;
}

public class LocalizationEditorWindow : EditorWindow {
    public List<LocaleEntry> Entries             = new List<LocaleEntry>();
    public Vector2           ScrollPosition;
    public int               NewIdent            = 0;
    public LocalizationTag   NewTag              = LocalizationTag.None;
    public string            NewText             = "";
    public string            NewComment          = "";
    public string            SearchFilter        = "";
    public bool              ShowAdvancedOptions = false;
    public string            SaveDirectory       = $"{Application.streamingAssetsPath}/Localization";
    public string            SaveName            = "eng";
    public string            Extension           = "loc";

    private const int IdentSpacesCount = 20;
    private const int TagSpacesCount   = 12;

    [MenuItem("Localization/Editor")]
    public static void ShowWindow() {
        GetWindow<LocalizationEditorWindow>("Localization Editor");
    }

    [MenuItem("Localization/Load English")]
    public static void LoadEnglish() {
        Locale.LoadLocalization("eng");
    }

    private void OnGUI() {
        // Search bar
        EditorGUILayout.BeginHorizontal();
        SearchFilter = EditorGUILayout.TextField("Search", SearchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(60))) {
            SearchFilter = "";
        }
        EditorGUILayout.EndHorizontal();

        // Add new
        EditorGUILayout.Space();

        if (GUILayout.Button("New")) {
            NewEntryPopupWindow.ShowPopup(this);
        }

        // Entries list
        EditorGUILayout.Space();
        GUILayout.Label("Entries List", EditorStyles.boldLabel);

        ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

        var filteredEntries = string.IsNullOrEmpty(SearchFilter)
            ? Entries
            : Entries.FindAll(e =>
                e.Ident.ToString().Contains(SearchFilter) ||
                e.Tag.ToString().Contains(SearchFilter) ||
                e.Text.ToLower().Contains(SearchFilter.ToLower()));

        for (int i = 0; i < filteredEntries.Count; i++) {
            var entry = new LocaleEntry();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID", GUILayout.Width(120));
            entry.Ident = EditorGUILayout.IntField(filteredEntries[i].Ident);
            if(GUILayout.Button("Copy", GUILayout.Width(50))) {
                EditorGUIUtility.systemCopyBuffer = entry.Ident.ToString();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tag", GUILayout.Width(120));
            entry.Tag = (LocalizationTag)EditorGUILayout.EnumPopup(filteredEntries[i].Tag);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Text", GUILayout.Width(120));
            entry.Text = EditorGUILayout.TextArea(filteredEntries[i].Text, GUILayout.Height(40));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Comment", GUILayout.Width(120));
            entry.Comment = EditorGUILayout.TextArea(filteredEntries[i].Comment, GUILayout.Height(40));
            EditorGUILayout.EndHorizontal();

            filteredEntries[i] = entry;

            if (GUILayout.Button("Remove")) {
                if (EditorUtility.DisplayDialog("Confirm Removal",
                    "Are you sure you want to remove this entry?",
                    "Yes", "No")) {
                    Entries.Remove(filteredEntries[i]);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        // Advanced options
        ShowAdvancedOptions = EditorGUILayout.Foldout(ShowAdvancedOptions, "Advanced Options");
        if (ShowAdvancedOptions) {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Save/Load
            EditorGUILayout.LabelField("Data Management", EditorStyles.boldLabel);
            SaveDirectory = EditorGUILayout.TextField("Save Path", SaveDirectory);
            SaveName      = EditorGUILayout.TextField("Name", SaveName);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) {
                SaveData();
            }
            if (GUILayout.Button("Load")) {
                LoadData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("Clear All Entries")) {
                if (EditorUtility.DisplayDialog("Confirm Clear",
                    "Are you sure you want to remove ALL Entries?",
                    "Yes", "No")) {
                    Entries.Clear();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // Status bar
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Total Entries: {Entries.Count} | Filtered: {filteredEntries.Count}", EditorStyles.centeredGreyMiniLabel);
    }

    private void SaveData() {
        var path = $"{SaveDirectory}/{SaveName}.{Extension}";

        if(!Directory.Exists(SaveDirectory)) {
            Directory.CreateDirectory(SaveDirectory);
        }

        if(File.Exists(path)) {
            File.Delete(path);
        }

        var sb = new StringBuilder();

        for(var i = 0; i < Entries.Count; ++i) {
            var entry = Entries[i];

            if(!string.IsNullOrEmpty(entry.Comment)) {
                sb.Append("/*\n");
                sb.Append(entry.Comment);
                sb.Append('\n');
                sb.Append("*/");
                sb.Append('\n');
            }

            var ident = entry.Ident.ToString();
            var tag   = entry.Tag.ToString();

            sb.Append(ident);
            sb.Append(' ', IdentSpacesCount - ident.Length);
            sb.Append(':');
            sb.Append(' ', 3);
            sb.Append(tag);
            sb.Append(' ', TagSpacesCount - tag.Length);
            sb.Append(':');
            sb.Append(' ', 3);
            sb.Append('"');
            for(var j = 0; j < entry.Text.Length; ++j) {
                if(entry.Text[j] == '\n') {
                    sb.Append('\\');
                    sb.Append('n');
                } else {
                    sb.Append(entry.Text[j]);
                }
            }
            sb.Append('"');
            sb.Append('\n');

        }

        var file = File.CreateText(path);
        file.Write(sb.ToString());
        file.Close();
    }

    private void LoadData() {
        var path = $"{SaveDirectory}/{SaveName}.{Extension}";
        if(!File.Exists(path)) {
            EditorUtility.DisplayDialog("File does not exist",
                                       $"File at path {path} does not exist!",
                                       "Ok");
        }

        var text   = File.ReadAllText(path);
        var tokens = new List<Token>();
        var error  = Tokenize(text, tokens);

        Entries.Clear();

        for(var i = 0; i < tokens.Count; ++i) {
            var token = tokens[i];
            switch(token.Type) {
                case TokenType.String : {
                    Assert(tokens[i - 1].Type == TokenType.Separator, $"Unexpected token. {token.Type}, while expecting {TokenType.Separator}");

                    // Ident : String
                    if (tokens[i-2].Type == TokenType.Ident) {
                        var entry = new LocaleEntry();

                        entry.Text    = token.String;
                        entry.Tag     = LocalizationTag.None;
                        entry.Ident   = tokens[i-2].Ident;
                        entry.Comment = "";

                        if (tokens[i-3].Type == TokenType.SingleCommentary ||
                            tokens[i-3].Type == TokenType.MultiCommentary) {
                            entry.Comment = tokens[i-3].String;
                        }

                        Entries.Add(entry);
                    } else {
                        // Ident : Tag : String
                        Assert(tokens[i - 2].Type == TokenType.Tag, $"Unexpected token. {token.Type}, while expecting {TokenType.Tag}");

                        Assert(tokens[i - 3].Type == TokenType.Separator, $"Unexpected token. {token.Type}, while expecting {TokenType.Separator}");

                        Assert(tokens[i - 4].Type == TokenType.Ident, $"Unexpected token. {token.Type}, while expecting {TokenType.Ident}");

                        var entry = new LocaleEntry();

                        entry.Text    = token.String;
                        entry.Tag     = tokens[i-2].Tag;
                        entry.Ident   = tokens[i-4].Ident;
                        entry.Comment = "";

                        if (tokens[i-5].Type == TokenType.SingleCommentary ||
                            tokens[i-5].Type == TokenType.MultiCommentary) {
                            entry.Comment = tokens[i-5].String;
                        }

                        Entries.Add(entry);
                    }
                } break;
                default : break;
            }
        }
    }
}