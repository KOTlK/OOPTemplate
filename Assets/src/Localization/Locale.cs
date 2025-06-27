using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using static Assertions;

/*
TODO:
    Make Editor window
*/

public enum LocalizationTag {
    None = 0,
    UI   = 1
}

public struct LocaleUnit {
    public int             Ident;
    public string          Text;
    public LocalizationTag Tag;
}

public static class Locale {
    public static event Action LocalizationLoaded = delegate {};

    public enum Error {
        OK                = 0,
        DirectoryNotExist = 1,
        FileNotExist      = 2,
        ParsingError      = 3,
        UnexpectedToken   = 4,
    }

    public enum TokenType : ushort {
        Separator        = ':',
        Ident            = 256,
        String           = 257,
        SingleCommentary = 258,
        MultiCommentary  = 259,
        Tag              = 260,
    }

    public struct Token {
        public int             Ident;
        public string          String;
        public TokenType       Type;
        public LocalizationTag Tag;
    }

    public static List<Token>                 Tokens;
    public static Dictionary<int, LocaleUnit> Table;

    private static StringBuilder sb;

    private const  string Extension = "loc";
    private static string Dir       = $"{Application.streamingAssetsPath}/Localization";

    public static Error LoadLocalization(string name) {
        var path = $"{Dir}/{name}.{Extension}";

        if(!Directory.Exists(Dir)) {
            Debug.LogError($"Directory {Dir} does not exist");
            return Error.DirectoryNotExist;
        }

        if(!File.Exists(path)) {
            Debug.LogError($"File {path} does not exist");
            return Error.FileNotExist;
        }

        Tokens = new();

        var text = File.ReadAllText(path);
        var err  = Tokenize(text, Tokens);

        if(err != 0) {
            return err;
        }

        Table = new();
        Table.Clear();

        // DEBUG
        // foreach (var token in Tokens) {
        //     if (token.Type == TokenType.Ident) {
        //         Debug.Log($"Ident : {token.Ident}");
        //     } else if (token.Type == TokenType.Tag) {
        //         Debug.Log($"Tag : {token.Tag}");
        //     } else if (token.Type == TokenType.String) {
        //         Debug.Log($"Text : {token.String}");
        //     } else {
        //         Debug.Log($"{token.Type}");
        //     }
        // }

        // Handle tokens
        for(var i = 0; i < Tokens.Count; i++) {
            var token = Tokens[i];

            switch(token.Type) {
                case TokenType.String : {
                    Assert(Tokens[i - 1].Type == TokenType.Separator, $"Unexpected token. {token.Type}, while expecting {TokenType.Separator}");

                    // Ident : String
                    if (Tokens[i-2].Type == TokenType.Ident) {
                        var unit = new LocaleUnit();

                        unit.Text  = token.String;
                        unit.Tag   = LocalizationTag.None;
                        unit.Ident = Tokens[i-2].Ident;

                        Table.Add(Tokens[i-2].Ident, unit);
                    } else {
                        // Ident : Tag : String
                        Assert(Tokens[i - 2].Type == TokenType.Tag, $"Unexpected token. {token.Type}, while expecting {TokenType.Tag}");

                        Assert(Tokens[i - 3].Type == TokenType.Separator, $"Unexpected token. {token.Type}, while expecting {TokenType.Separator}");

                        Assert(Tokens[i - 4].Type == TokenType.Ident, $"Unexpected token. {token.Type}, while expecting {TokenType.Ident}");

                        var unit = new LocaleUnit();

                        unit.Text  = token.String;
                        unit.Tag   = Tokens[i-2].Tag;
                        unit.Ident = Tokens[i-4].Ident;

                        Table.Add(Tokens[i - 4].Ident, unit);
                    }
                } break;
                default : break;
            }
        }

        LocalizationLoaded();

        return Error.OK;
    }

    public static string Get(int hash) {
        return Table[hash].Text;
    }

    public static string Get(LocalizedString str) {
        return Table[str.Ident].Text;
    }

    public static string Get(NameIdent str) {
        return Table[str.Ident].Text;
    }

    public static LocalizationTag GetTag(int hash) {
        return Table[hash].Tag;
    }

    public static LocalizationTag GetTag(LocalizedString str) {
        return Table[str.Ident].Tag;
    }

    public static LocalizationTag GetTag(NameIdent str) {
        return Table[str.Ident].Tag;
    }

    public static bool Has(int hash) {
        if(Table != null && Table.ContainsKey(hash)) {
            return true;
        }

        return false;
    }

    public static Error Tokenize(string text, List<Token> tokens) {
        var len  = text.Length;
        var line = 1;

        sb     = new();
        sb.Clear();
        tokens.Clear();

        for(var i = 0; i < len; ++i) {
            var c = text[i];
            switch(c) {
                case ' '  : break;
                case '\r' : break;
                case '\t' : break;
                case '/' : {
                    if(text[i + 1] == '/') {
                        i += 2;
                        sb.Clear();

                        for ( ; i < len; ++i) {
                            if(text[i] == '\n') {
                                break;
                            }

                            sb.Append(text[i]);
                        }

                        var str = sb.ToString();
                        sb.Clear();

                        var token = new Token();

                        token.Type   = TokenType.SingleCommentary;
                        token.String = str;

                        tokens.Add(token);
                        line++;
                    } else if(text[i + 1] == '*') {
                        i += 2;
                        sb.Clear();

                        for ( ; i < len; ++i) {
                            if (text[i] == '\n') line++;

                            if (text[i] == '*' && text[i + 1] == '/') {
                                i++;
                                break;
                            }

                            sb.Append(text[i]);
                        }

                        var str = sb.ToString();
                        sb.Clear();

                        var token = new Token();

                        token.Type   = TokenType.MultiCommentary;
                        token.String = str;

                        tokens.Add(token);
                    }
                } break;
                case '\n' : {
                    line++;
                } break;
                case '"'  : {
                    var str   = ParseString(text, ref i);
                    var token = new Token();

                    token.Type   = TokenType.String;
                    token.String = str;

                    tokens.Add(token);
                } break;
                case (char)TokenType.Separator : {
                    if(tokens.Count > 1) {
                        var last = tokens[tokens.Count - 2];
                        if(last.Type == TokenType.Ident) {
                            var tagString = sb.ToString();
                            sb.Clear();
                            if(Enum.TryParse(tagString, true, out LocalizationTag tag)) {
                                // C# делали конченые кретины, поэтому объявить здесь token я не могу,
                                // т.к он объявлен ниже в абсолютно другом скоупе.
                                // Желаю всем, кто причастен к этому, выпадения прямой кишки.
                                var tkn = new Token();

                                tkn.Type = TokenType.Tag;
                                tkn.Tag  = tag;

                                tokens.Add(tkn);

                                tkn = new();

                                tkn.Type = TokenType.Separator;

                                tokens.Add(tkn);
                            } else {
                                Debug.LogError($"Can't identify tag at {line}. The tag was: {tagString}.");
                            }
                            break;
                        }
                    }


                    var keyString = sb.ToString();
                    if(!int.TryParse(keyString, out var key)) {
                        foreach(var tkn in tokens) {
                            Debug.Log(tkn.Type);
                        }
                        Debug.LogError($"Cannot parse key at line {line}. The input string was: {keyString}.");
                        return Error.ParsingError;
                    }

                    sb.Clear();

                    var token = new Token();

                    token.Type = TokenType.Ident;
                    token.Ident  = key;

                    tokens.Add(token);

                    token = new();

                    token.Type = TokenType.Separator;

                    tokens.Add(token);
                } break;
                default : {
                    sb.Append(c);
                } break;
            }
        }

        return Error.OK;
    }

    private static string ParseString(string text, ref int i) {
        sb.Clear();
        var len = text.Length;
        i++;

        while(i < len) {
            if(text[i] == '"' && text[i - 1] != '\\') {
                break;
            }

            if((text[i] == '\\' && text[i+1] == 'n') &&
               (text[i - 1] != '\\')) {
                sb.Append('\n');
                i += 2;
                continue;
            }

            sb.Append(text[i++]);
        }

        var str = sb.ToString();
        sb.Clear();
        return str;
    }
}