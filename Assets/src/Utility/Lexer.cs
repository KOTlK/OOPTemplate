using System.Runtime.InteropServices;
using System.Text;
using static Assertions;

public enum TokenType : ushort {
    Uninitialized  = 0,
    Ident          = 256,
    Directive      = 257,
    IntLiteral     = 258,
    FloatLiteral   = 259,
    EndOfFile      = 260,
    Special        = 261,
    String         = 262,
}

[StructLayout(LayoutKind.Explicit)]
public struct Value {
    [FieldOffset(0)] public ulong  Integer;
    [FieldOffset(0)] public double FPoint;
    [FieldOffset(0)] public string Str;
    [FieldOffset(0)] public char   C;
}

public struct Token {
    public uint  Type;
    public Value Value;
    public uint  Line;
    public uint  Column;
};

public class Lexer {
    private string        _text;
    private StringBuilder _sb;
    private Token[]       _tokens;
    private uint          _maxTokensAhead;
    private uint          _tokensLength;
    private uint          _currentToken;
    private uint          _lineStart;
    private uint          _textPtr;
    private uint          _line;

    public Lexer (string txt, uint maxTokensAhead = 4) {
        _text            = txt;
        _sb              = new();
        _currentToken    = 0;
        _line            = 1;
        _lineStart       = 1;
        _textPtr         = 0;
        _maxTokensAhead  = maxTokensAhead;
        _tokensLength    = maxTokensAhead * 2 + 1;
        _tokens          = new Token[_tokensLength];

        for (var i = 0; i < _tokens.Length; i++) {
            _tokens[i] = new Token() {
                Type   = (ushort)TokenType.Uninitialized,
                Value  = new Value() {Integer = 0 },
                Line   = 0,
                Column = 0
            };
        }
    }

    public Token GetCurrentToken() {
        return _tokens[_currentToken];
    }

    public Token EatToken() {
        uint  next = (_currentToken + 1) % _tokensLength;
        Token token;

        if (_tokens[next].Type == (ushort)TokenType.Uninitialized) {
            token = ParseNextToken();
            _tokens[next] = token;
        } else {
            token = _tokens[next];
        }

        _currentToken = next;
        _tokens[(_currentToken + _maxTokensAhead) % _tokensLength] = new() {
            Type   = (ushort)TokenType.Uninitialized,
            Value  = new Value() {Integer = 0 },
            Line   = 0,
            Column = 0
        };

        return token;
    }

    public Token PeekToken(uint next = 1) {
        Assert(next <= _maxTokensAhead, "Cannot peek more than _maxTokensAhead(%).", _maxTokensAhead);
        uint  next_index = (_currentToken + next) % _tokensLength;
        Token token;

        if (_tokens[next_index].Type == (ushort)TokenType.Uninitialized) {
            token               = ParseNextToken();
            _tokens[next_index] = token;
        } else {
            token = _tokens[next_index];
        }

        return token;
    }

    public Token PrevToken(uint prev = 1) {
        Assert(prev <= _maxTokensAhead, "Cannot peek more than _maxTokensAhead(%).", _maxTokensAhead);
        uint  next = (_currentToken - prev) % _tokensLength;
        Token token;

        if (_tokens[next].Type == (ushort)TokenType.Uninitialized) {
            token         = ParseNextToken();
            _tokens[next] = token;
        } else {
            token = _tokens[next];
        }

        return token;
    }

    public Token ParseNextToken() {
        for(;;) {
            if (_textPtr >= _text.Length) {
                Token token = new();
                token.Type   = (ushort)TokenType.EndOfFile;
                token.Line   = _line;
                token.Column = _textPtr - _lineStart;
                return token;
            }

            // indexing array with int but not uint. fucking c#
            char c = _text[(int)_textPtr];

            switch(c) {
                case '\0' : {
                    Token token = new();
                    token.Type   = (ushort)TokenType.EndOfFile;
                    token.Line   = _line;
                    token.Column = _textPtr - _lineStart;

                    return token;
                }
                case ' ' : {
                    _textPtr++;
                } break;
                case '\r' : {
                    _textPtr++;
                } break;
                case '\t' : {
                    _textPtr++;
                } break;
                case '\n' : {
                    _line++;
                    _textPtr++;
                    _lineStart = _textPtr;
                } break;
                case '#' : {
                    uint start = _textPtr;
                    _sb.Clear();
                    _textPtr++;
                    c = _text[(int)_textPtr];

                    while (c != ' ') {
                        if (!IsLetter(c) && c != '_') break;
                        _sb.Append(c);
                        _textPtr++;
                        c = _text[(int)_textPtr];
                    }

                    var str = _sb.ToString();
                    _sb.Clear();

                    Token token = new();

                    token.Type   = (ushort)TokenType.Directive;
                    token.Value  = new(){Str = str};
                    token.Line   = _line;
                    token.Column = start - _lineStart;

                    return token;
                }
                case '/' : {
                    if (_text[(int)_textPtr + 1] == '/') {
                        c = _text[(int)(++_textPtr)]; // /
                        while (c != '\n') {
                            _textPtr++;
                            if (_textPtr >= _text.Length) break;
                            c = _text[(int)_textPtr];
                        }
                        _textPtr++;
                    } else if (_text[(int)_textPtr + 1] == '*') {
                        _textPtr++; // *
                        _textPtr++;

                        while (_textPtr < _text.Length) {
                            if (_text[(int)_textPtr] == '/' &&
                                _text[(int)_textPtr - 1] == '*') {
                                _textPtr++;
                                break;
                            }
                            _textPtr++;
                        }
                    }
                } break;
                case '\"' : {
                    uint start = _textPtr;
                    _sb.Clear();
                    _textPtr++;
                    c = _text[(int)_textPtr];

                    while (c != '\"') {
                        _sb.Append(c);
                        _textPtr++;
                        c = _text[(int)_textPtr];
                    }

                    _textPtr++;

                    var str = _sb.ToString();
                    _sb.Clear();

                    Token token = new();

                    token.Type   = (ushort)TokenType.String;
                    token.Value  = new() {Str = str};
                    token.Line   = _line;
                    token.Column = start - _lineStart;

                    return token;
                }
                default : {
                    if (IsLetter(c)) {
                        // parse TokenType.Ident
                        uint start = _textPtr;
                        _sb.Clear();

                        while (c != ' ') {
                            if (c == '\n') break;
                            if (c == '\r') break;
                            if (c == '\t') break;
                            if (IsSpecial(c) && c != '_') break;

                            _sb.Append(c);
                            _textPtr++;
                            if (_textPtr >= _text.Length) break;
                            c = _text[(int)_textPtr];
                        }

                        var str = _sb.ToString();
                        _sb.Clear();

                        Token token = new();

                        token.Type   = (ushort)TokenType.Ident;
                        token.Value  = new() {Str = str};
                        token.Line   = _line;
                        token.Column = start - _lineStart;

                        return token;
                    }

                    if (IsNumber(c)) {
                        // parse number
                        uint start = _textPtr;
                        _sb.Clear();

                        while (c != ' ') {
                            if (!IsNumber(c) &&
                                c != '.'      &&
                                c != 'f'      &&
                                c != 'l'      &&
                                c != 'F'      && 
                                c != 'L') break;

                            _sb.Append(c);

                            _textPtr++;

                            if (_textPtr >= _text.Length) break;
                            c = _text[(int)_textPtr];
                        }

                        var str = _sb.ToString();
                        _sb.Clear();

                        Token token = new();

                        if (str.Contains('.')) {
                            token.Type = (ushort)TokenType.FloatLiteral;

                            if (str.EndsWith('f') ||
                                str.EndsWith('F') ||
                                str.EndsWith('l') ||
                                str.EndsWith('L')
                                ) {
                                var num = str.Substring(0, str.Length - 2);
                                var f   = float.Parse(num);
                                token.Value = new() {FPoint = f};
                            } else {
                                var f   = float.Parse(str);
                                token.Value = new() {FPoint = f};
                            }
                        } else {
                            token.Type = (ushort)TokenType.IntLiteral;
                            var i = ulong.Parse(str);
                            token.Value = new() {Integer = i};
                        }

                        token.Line   = _line;
                        token.Column = start - _lineStart;

                        return token;
                    }

                    Token tkn = new();

                    tkn.Type   = c;
                    tkn.Value  = new() {C = c};
                    tkn.Line   = _line;
                    tkn.Column = _textPtr - _lineStart;

                    _textPtr++;

                    return tkn;
                }
            }
        }
    }

    static bool IsLetter(char c) {
        return (c >= 65 && c <= 90) || (c >= 97 && c <= 122);
    }

    static bool IsNumber(char c) {
        return (c >= 48 && c <= 57);
    }

    static bool IsSpecial(char c) {
        return ((c >= 33 &&  c <= 47) ||
                (c >= 58 &&  c <= 64) ||
                (c >= 91 &&  c <= 96) ||
                (c >= 123 && c <= 126));
    }
}