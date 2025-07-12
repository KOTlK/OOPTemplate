using System.IO;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

/*
    Config example, config file example: Assets/StreamingAssets/Config.cfg

Structs are categories

public struct Volume {
    public float Music;
    public float Sound;
    public float Voice;
}

public static class Vars {
    public static bool   IsInvinsible;
    public static Volume Volume;

    ...
}
*/

public enum ConfigTokenType : ushort {
    Comment    = ';',
    LSqbracket = '[',
    RSqbracket = ']',
    Value      = 257,
    Ident      = 258,
    // There are no tokens for text or vectors, it's all just value
}

public struct ConfigToken {
    public ConfigTokenType  Type;
    public string           Ident;
    public string           Comment;
    public string           Value;
}

public static class Config {
    // Data here

    public static List<ConfigToken> Tokens;

    private static StringBuilder sb;

    private static string Path = $"{Application.streamingAssetsPath}/Config.cfg";

    public static void ParseVars() {
        Tokens = new();
        Tokens.Clear();

        if(!File.Exists(Path)) {
            Debug.LogError($"Config file does not exist. Path: {Path}");
            return;
        }

        var text = File.ReadAllText(Path);

        Tokenize(text);

        object      currentCategory     = null;
        var         currentCategoryName = "";
        var         fields              = typeof(Config).GetFields();
        var         thisFields          = typeof(Config).GetFields();

        for(var i = 0; i < Tokens.Count; ++i) {
            var token = Tokens[i];

            switch(token.Type) {
                case ConfigTokenType.RSqbracket : {
                    if(Tokens[i - 1].Type != ConfigTokenType.Ident ||
                       Tokens[i - 2].Type != ConfigTokenType.LSqbracket) {
                        Debug.LogError($"Unexpected token.{Tokens[i - 1].Type} while waiting {ConfigTokenType.Ident}");
                        return;
                    }

                    var category = Tokens[i-1].Ident;

                    if(currentCategory != null) {
                        foreach(var field in thisFields) {
                            if(field.FieldType.ToString() == currentCategoryName) {
                                field.SetValue(null, currentCategory);
                            }
                        }
                    }

                    currentCategoryName = category;

                    var type = Type.GetType(category);

                    if(type == null) {
                        break;
                    }

                    var flds = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                    foreach(var field in thisFields) {
                        if(field.FieldType.ToString() == category) {
                            currentCategory = field.GetValue(null);
                            fields = flds;
                            break;
                        }
                    }
                } break;
                case ConfigTokenType.Value : {
                    if(Tokens[i - 1].Type != ConfigTokenType.Ident) {
                        Debug.LogError($"Unexpected token.{Tokens[i - 1].Type} while waiting {ConfigTokenType.Ident}");
                        return;
                    }

                    var name  = Tokens[i - 1].Ident;
                    var value = Tokens[i].Value;

                    foreach(var field in fields) {
                        if(field.Name == name) {
                            SetValueByType(field, currentCategory, value);
                            break;
                        }
                    }
                } break;
            }
        }

        // If the category was set, apply changes
        if(currentCategory != null) {
            foreach(var field in thisFields) {
                if(field.FieldType.ToString() == currentCategoryName) {
                    field.SetValue(null, currentCategory);
                    break;
                }
            }
        }
    }

    private static void SetValueByType(FieldInfo field, object instance, string value) {
        //if you need more types, add more cases and implement parsing for it
        switch(field.FieldType.ToString()) {
            case "System.String" : {
                field.SetValue(instance, value);
            } break;
            case "System.Boolean" : {
                if(value.ToLower() == "true") {
                    field.SetValue(instance, true);
                }else if(value.ToLower() == "false") {
                    field.SetValue(instance, false);
                }
            } break;
            case "System.Single" : {
                field.SetValue(instance, Single.Parse(value));
            } break;
            case "System.Double" : {
                field.SetValue(instance, Double.Parse(value));
            } break;
            case "System.Int32" : {
                field.SetValue(instance, Int32.Parse(value));
            } break;
            case "System.UInt32" : {
                field.SetValue(instance, UInt32.Parse(value));
            } break;
            case "System.Int64" : {
                field.SetValue(instance, Int64.Parse(value));
            } break;
            case "System.UInt64" : {
                field.SetValue(instance, UInt64.Parse(value));
            } break;
            case "System.Int16" : {
                field.SetValue(instance, Int16.Parse(value));
            } break;
            case "System.UInt16" : {
                field.SetValue(instance, UInt16.Parse(value));
            } break;
            case "System.SByte" : {
                field.SetValue(instance, SByte.Parse(value));
            } break;
            case "System.Byte" : {
                field.SetValue(instance, Byte.Parse(value));
            } break;
            case "UnityEngine.Vector2" : {
                var vec   = Vector2.zero;
                var coord = 0;

                sb.Clear();
                for(var i = 0; i < value.Length; ++i) {
                    switch(value[i]) {
                        case ' ' : break;
                        case '(' : break;
                        case ',' : {
                            vec[coord++] = float.Parse(sb.ToString());
                            sb.Clear();
                        } break;
                        case ')' : {
                            vec[coord] = float.Parse(sb.ToString());
                            sb.Clear();
                        } break;
                        default : {
                            sb.Append(value[i]);
                        } break;
                    }
                }

                field.SetValue(instance, vec);
            } break;
            case "UnityEngine.Vector3" : {
                var vec   = Vector3.zero;
                var coord = 0;

                sb.Clear();
                for(var i = 0; i < value.Length; ++i) {
                    switch(value[i]) {
                        case ' ' : break;
                        case '(' : break;
                        case ',' : {
                            vec[coord++] = float.Parse(sb.ToString());
                            sb.Clear();
                        } break;
                        case ')' : {
                            vec[coord] = float.Parse(sb.ToString());
                            sb.Clear();
                        } break;
                        default : {
                            sb.Append(value[i]);
                        } break;
                    }
                }

                field.SetValue(instance, vec);
            } break;
            case "UnityEngine.Vector4" : {
                var vec   = Vector4.zero;
                var coord = 0;

                sb.Clear();
                for(var i = 0; i < value.Length; ++i) {
                    switch(value[i]) {
                        case ' ' : break;
                        case '(' : break;
                        case ',' : {
                            vec[coord++] = float.Parse(sb.ToString());
                            sb.Clear();
                        } break;
                        case ')' : {
                            vec[coord] = float.Parse(sb.ToString());
                            sb.Clear();
                        } break;
                        default : {
                            sb.Append(value[i]);
                        } break;
                    }
                }

                field.SetValue(instance, vec);
            } break;
            default :
                Debug.LogError($"Cannot set field with type: {field.FieldType.ToString()}");
                break;
        }
    }

    private static void Tokenize(string text) {
        Tokens = new();
        sb     = new();
        Tokens.Clear();
        sb.Clear();

        var len = text.Length;

        for(var i = 0; i < len; ++i) {
            var c = text[i];

            switch(c) {
                case '\r' : break;
                case '"'  : {
                    i++;

                    for (; i < len; ++i) {
                        if (text[i] == '"' && text[i-1] != '\\') {
                            break;
                        }

                        if (text[i] == '\\' &&
                            text[i + 1] == 'n') {
                            sb.Append('\n');
                            i++;
                            continue;
                        }

                        sb.Append(text[i]);
                    }
                } break;
                case '(' : {
                    for(; i < len; ++i) {
                        if(text[i] == ')') {
                            sb.Append(text[i]);
                            break;
                        }

                        sb.Append(text[i]);
                    }
                } break;
                case '\n' : {
                    var last = Tokens[Tokens.Count - 1];
                    if(last.Type == ConfigTokenType.Comment ||
                       last.Type == ConfigTokenType.RSqbracket ||
                       text[i - 1] == '\n') break;

                    var token = new ConfigToken();

                    token.Type  = ConfigTokenType.Value;
                    token.Value = sb.ToString();

                    Tokens.Add(token);
                    sb.Clear();
                } break;
                case '\t' :
                case ' '  : {
                    if(text[i + 1] == ' ' ||
                       text[i + 1] == '\t') {
                       break;
                    }
                    var token = new ConfigToken();

                    token.Type  = ConfigTokenType.Ident;
                    token.Ident = sb.ToString();

                    Tokens.Add(token);
                    sb.Clear();
                } break;
                case '['  : {
                    var token = new ConfigToken();
                    token.Type = ConfigTokenType.LSqbracket;

                    Tokens.Add(token);
                } break;
                case ']' : {
                    var token = new ConfigToken();

                    token.Type  = ConfigTokenType.Ident;
                    token.Ident = sb.ToString();
                    sb.Clear();

                    Tokens.Add(token);

                    token = new();

                    token.Type = ConfigTokenType.RSqbracket;

                    Tokens.Add(token);
                } break;
                case ';' : {
                    sb.Clear();
                    i++;
                    for(; i < len; i++) {
                        if(text[i] == '\n') break;
                        sb.Append(text[i]);
                    }

                    var token = new ConfigToken();

                    token.Type    = ConfigTokenType.Comment;
                    token.Comment = sb.ToString();

                    Tokens.Add(token);
                    sb.Clear();
                } break;
                default : {
                    sb.Append(c);
                } break;
            }
        }

        if(sb.Length > 0) {
            var token = new ConfigToken();

            token.Type  = ConfigTokenType.Value;
            token.Value = sb.ToString();

            Tokens.Add(token);
        }
    }
}