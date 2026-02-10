using System.IO;
using System;
using System.Text;
using System.Reflection;
using UnityEngine;

using static Assertions;

[UnityEngine.Scripting.Preserve]
public static class Config {
    // Data here

    private static StringBuilder sb;

    public static string Path = $"{Application.streamingAssetsPath}/Config.cfg";

    public static void ParseVars() {

        if(!File.Exists(Path)) {
            Debug.LogError($"Config file does not exist. Path: {Path}");
            return;
        }

        var text  = File.ReadAllText(Path);
        var lexer = new Lexer(text);

        object currentCategory     = null;
        var    currentCategoryName = "";
        var    fields              = typeof(Config).GetFields();
        var    thisFields          = typeof(Config).GetFields();

        var token = lexer.EatToken();

        while(token.Type != (ushort)TokenType.EndOfFile) {
            switch(token.Type) {
                case '[' : {
                    var next     = lexer.PeekToken();
                    var nextNext = lexer.PeekToken(2);

                    Assert(next.Type == (ushort)TokenType.Ident, "Unexpected token. Expected %. Got %.", TokenType.Ident, (TokenType)next.Type);
                    Assert(nextNext.Type == ']', "Unexpected token. Expected %. Got %.", ']', (TokenType)next.Type);

                    var category = next.Value.Str;

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
                            fields          = flds;
                            break;
                        }
                    }

                    lexer.EatToken(); // eat ident
                    lexer.EatToken(); // eat bracket
                } break;
                case (ushort)TokenType.Ident : {
                    var name     = token.Value.Str;
                    var next     = lexer.PeekToken();
                    var negative = false;

                    if (next.Type == '-') {
                        negative = true;
                        lexer.EatToken(); // eat '-'
                    }

                    token = lexer.EatToken();

                    foreach(var field in fields) {
                        if (field.Name == name) {
                            SetValueByType(field, currentCategory, token, lexer, negative);
                            break;
                        }
                    }

                } break;
            }

            token = lexer.EatToken();
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

    private static void SetValueByType(FieldInfo field, object instance, Token currentToken, Lexer lexer, bool negative) {
        //if you need more types, add more cases and implement parsing for it
        switch(field.FieldType.ToString()) {
            case "System.String" : {
                UnexpectedToken(currentToken, (ushort)TokenType.String);
                field.SetValue(instance, currentToken.Value.Str);
            } break;
            case "System.Boolean" : {
                UnexpectedToken(currentToken, (ushort)TokenType.String);
                if (currentToken.Value.Str.ToLower() == "true") {
                    field.SetValue(instance, true);
                } else if (currentToken.Value.Str.ToLower() == "false") {
                    field.SetValue(instance, false);
                }
            } break;
            case "System.Single" : {
                UnexpectedToken(currentToken, (ushort)TokenType.FloatLiteral);
                var val = negative ?-(float)currentToken.Value.FPoint : 
                                     (float)currentToken.Value.FPoint;
                field.SetValue(instance, val);
            } break;
            case "System.Double" : {
                UnexpectedToken(currentToken, (ushort)TokenType.FloatLiteral);
                var val = negative ? -currentToken.Value.FPoint : 
                                      currentToken.Value.FPoint;
                field.SetValue(instance, val);
            } break;
            case "System.Int32" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = negative ? -(int)currentToken.Value.Integer : 
                                      (int)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.UInt32" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = (uint)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.Int64" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = negative ? -(long)currentToken.Value.Integer : 
                                      (long)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.UInt64" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.Int16" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = negative ? -(short)currentToken.Value.Integer : 
                                      (short)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.UInt16" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = (ushort)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.SByte" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = negative ? -(sbyte)currentToken.Value.Integer : 
                                      (sbyte)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "System.Byte" : {
                UnexpectedToken(currentToken, (ushort)TokenType.IntLiteral);
                var val = (byte)currentToken.Value.Integer;
                field.SetValue(instance, val);
            } break;
            case "UnityEngine.Vector2" : {
                UnexpectedToken(currentToken, '(');
                currentToken = lexer.EatToken();
                var x = ParseFloat(currentToken, lexer);
                UnexpectedToken(lexer.EatToken(), ',');
                var y = ParseFloat(lexer.EatToken(), lexer);
                UnexpectedToken(lexer.EatToken(), ')');
                var vec = new Vector2(x, y);

                if (negative) vec = -vec;

                field.SetValue(instance, vec);
            } break;
            case "UnityEngine.Vector3" : {
                UnexpectedToken(currentToken, '(');
                currentToken = lexer.EatToken();
                var x = ParseFloat(currentToken, lexer);
                UnexpectedToken(lexer.EatToken(), ',');
                var y = ParseFloat(lexer.EatToken(), lexer);
                UnexpectedToken(lexer.EatToken(), ',');
                var z = ParseFloat(lexer.EatToken(), lexer);
                UnexpectedToken(lexer.EatToken(), ')');
                var vec = new Vector3(x, y, z);

                if (negative) vec = -vec;

                field.SetValue(instance, vec);
            } break;
            case "UnityEngine.Vector4" : {
                UnexpectedToken(currentToken, '(');
                currentToken = lexer.EatToken();
                var x = ParseFloat(currentToken, lexer);
                UnexpectedToken(lexer.EatToken(), ',');
                var y = ParseFloat(lexer.EatToken(), lexer);
                UnexpectedToken(lexer.EatToken(), ',');
                var z = ParseFloat(lexer.EatToken(), lexer);
                UnexpectedToken(lexer.EatToken(), ',');
                var w = ParseFloat(lexer.EatToken(), lexer);
                UnexpectedToken(lexer.EatToken(), ')');
                var vec = new Vector4(x, y, z, w);

                if (negative) vec = -vec;

                field.SetValue(instance, vec);
            } break;
            default :
                Debug.LogError($"Cannot set field with type: {field.FieldType.ToString()}");
                break;
        }
    }

    private static void UnexpectedToken(Token token, ushort expected) {
        Assert(token.Type == (ushort)expected, "Unexpected token at: %:%. Expected %. Got %.",
            token.Line, 
            token.Column, 
            (TokenType)token.Type, 
            (TokenType)token.Type);
    }

    // parse float, leave current token at FloatLiteral
    private static float ParseFloat(Token token, Lexer lexer) {
        var negative = false;
        if (token.Type == '-') {
            negative = true;
            token    = lexer.EatToken();
        }

        UnexpectedToken(token, (ushort)TokenType.FloatLiteral);

        var f = (float)token.Value.FPoint;

        if (negative) f = -f;

        return f;
    }
}