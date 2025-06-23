using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Collections;
using System;

using static Assertions;

public class TextSaveFile : ISaveFile {
    public struct Field {
        public string Name;
        public string Value;

        public Field(string name, string value) {
            Name = name;
            Value = value;
        }
    }

    public struct ObjectNode {
        public List<Field> Fields;
        public Dictionary<string, ObjectNode> NestedObjects;

        public static ObjectNode Create() {
            return new ObjectNode() {
                Fields = new(),
                NestedObjects = new()
            };
        }

        public void PushField(Field field) {
            Fields.Add(field);
        }

        public void PushObject(string name, ObjectNode node) {
            NestedObjects.Add(name, node);
        }
    }

    public enum TokenType : ushort {
        LParent     = '{',
        RParent     = '}',
        LBracket    = '(',
        RBracket    = ')',
        Separator   = ':',
        Ident       = 256,
        Value       = 257,
    }

    public struct Token {
        public TokenType Type;
        public string    Ident;
        public string    Value;
        public int       Line;
        public int       Column;
    }

    public StringBuilder     Sb = new();
    public ObjectNode        Root;
    public Stack<ObjectNode> ObjectStack = new();
    public List<Token>       Tokens;

    public void NewFile() {
        Sb.Clear();
        if(Tokens == null) {
            Tokens = new();
        } else {
            Tokens.Clear();
        }
    }

    public void Dispose() {}

    public void LoadFromFile(string path) {
        Assert(File.Exists(path));
        Root = new ObjectNode {
            Fields = new(),
            NestedObjects = new()
        };
        ObjectStack.Clear();

        var text = File.ReadAllText(path);

        Tokens = Tokenize(text);

        Root = ObjectNode.Create();

        for (var i = 0; i < Tokens.Count; ++i) {
            var token = Tokens[i];

            switch(token.Type) {
                case TokenType.Value : {
                    var field = ParseField(i);

                    Root.PushField(field);
                } break;
                case TokenType.LParent : {
                    var obj = ParseObject(ref i, out var name);

                    Root.PushObject(name, obj);
                } break;
                default : {
                } break;
            }
        }

        ObjectStack.Push(Root);
    }

    public void SaveToFile(string path) {
        var indent = 0;

        for(var i = 0; i < Tokens.Count; ++i) {
            var token = Tokens[i];

            switch(token.Type) {
                case TokenType.Ident :
                    Sb.Append('\t', indent);
                    Sb.Append(token.Ident);
                    Sb.Append(' ');
                    break;
                case TokenType.Separator :
                    Sb.Append((char)TokenType.Separator);
                    Sb.Append(' ');
                    break;
                case TokenType.Value :
                    Sb.Append(token.Value);
                    Sb.Append('\n');
                    break;
                case TokenType.LParent :
                    Sb.Append((char)TokenType.LParent);
                    Sb.Append('\n');
                    indent++;
                    break;
                case TokenType.RParent :
                    indent--;
                    Sb.Append('\t', indent);
                    Sb.Append((char)TokenType.RParent);
                    Sb.Append('\n');
                    break;
            }
        }

        File.WriteAllText(path, Sb.ToString());
    }

    public void Write<T>(T value, string name = null) {
        var token = new Token();

        token.Type  = TokenType.Ident;
        token.Ident = name;

        Tokens.Add(token);

        token = new();

        token.Type = TokenType.Separator;

        Tokens.Add(token);

        token = new();

        token.Type  = TokenType.Value;
        token.Value = Parse(value);

        Tokens.Add(token);
    }

    public void WriteObject(ISave save, string name = null) {
        BeginObject(name);

        save.Save(this);

        EndObject();
    }

    public void WriteArray<T>(T[] arr, int itemsCount, string name = null) {
        BeginObject(name);

        Write(itemsCount, "Count");

        for(var i = 0; i < itemsCount; ++i) {
            Write(arr[i], $"{name}ArrayElement{i}");
        }

        EndObject();
    }

    public void WriteObjectArray<T>(T[] arr, int itemsCount, string name = null)
    where T : ISave {
        BeginObject(name);

        Write(itemsCount, "Count");

        for(var i = 0; i < itemsCount; ++i) {
            WriteObject(arr[i], $"{name}ArrayElement{i}");
        }

        EndObject();
    }

    public void WriteNativeArray<T>(NativeArray<T> arr, int itemsCount, string name = null)
    where T : unmanaged {
        BeginObject(name);

        Write(itemsCount, "Count");

        for(var i = 0; i < itemsCount; ++i) {
            Write(arr[i], $"{name}ArrayElement{i}");
        }

        EndObject();
    }

    public void WritePackedEntity(PackedEntity e, uint id, string name = null) {
        BeginObject(name);
        Write(id, "Id");
        WriteEnum(e.Type, nameof(e.Type));
        Write(e.Alive, nameof(e.Alive));
        if(e.Alive) {
            WriteEntity(nameof(e.Entity), e.Entity);
        }
        EndObject();
    }

    private void WriteEntity(string name, Entity e) {
        BeginObject(name);
        WriteObject(e.Handle, nameof(e.Handle));
        WriteEnum(e.Type, nameof(e.Type));
        WriteEnum(e.Flags, nameof(e.Flags));
        Write(e.Name, nameof(e.Name));
        Write(e.transform.position, "Position");
        Write(e.transform.rotation, "Orientation");
        Write(e.transform.localScale, "Scale");
        e.Save(this);
        EndObject();
    }

    public void WriteEnum(Enum e, string name = null) {
        var type = Enum.GetUnderlyingType(e.GetType()).ToString();

        switch(type) {
            case "System.Int32" : {
                Write((int)Convert.ChangeType(e, typeof(int)), name);
            }
            break;
            case "System.UInt32" : {
                Write((uint)Convert.ChangeType(e, typeof(uint)), name);
            }
            break;
            case "System.Int64" : {
                Write((long)Convert.ChangeType(e, typeof(long)), name);
            }
            break;
            case "System.UInt64" : {
                Write((ulong)Convert.ChangeType(e, typeof(ulong)), name);
            }
            break;
            case "System.Int16" : {
                Write((short)Convert.ChangeType(e, typeof(short)), name);
            }
            break;
            case "System.UInt16" : {
                Write((ushort)Convert.ChangeType(e, typeof(ushort)), name);
            }
            break;
            case "System.Byte" : {
                Write((byte)Convert.ChangeType(e, typeof(byte)), name);
            }
            break;
            case "System.SByte" : {
                Write((sbyte)Convert.ChangeType(e, typeof(sbyte)), name);
            }
            break;
            default : {
                Debug.LogError($"Can't convert from {type} to any proper type");
            }
            break;
        }
    }

    public T Read<T>(string name = null, T defaultValue = default(T)) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                return Parse<T>(field.Value);
            }
        }

        return defaultValue;
    }

    public T[] ReadArray<T>(string name = null) {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = Read<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public void ReadObject(ISave obj, string name = null) {
        BeginReadObject(name);
        obj.Load(this);
        EndReadObject();
    }

    public T ReadValueType<T>(string name = null)
    where T : ISave {
        var ret = default(T);

        BeginReadObject(name);
        ret.Load(this);
        EndReadObject();

        return ret;
    }

    public T ReadEnum<T>(string name)
    where T : Enum {
        var type = Enum.GetUnderlyingType(typeof(T)).ToString();

        switch(type) {
            case "System.Byte" : {
                var val = Read<byte>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.SByte" : {
                var val = Read<sbyte>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.Int16" : {
                var val = Read<short>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.UInt16" : {
                var val = Read<ushort>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.Int32" : {
                var val = Read<int>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.UInt32" : {
                var val = Read<uint>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.Int64" : {
                var val = Read<long>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.UInt64" : {
                var val = Read<ulong>(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            default : {
                Debug.LogError($"Can't read enum with underlying type: {type}");
            }
            break;
        }

        return default;
    }

    public PackedEntity ReadPackedEntity(EntityManager em, string name = null) {
        BeginReadObject(name);
        var ent = new PackedEntity();
        var id  = Read<uint>("Id");
        ent.Type = ReadEnum<EntityType>(nameof(ent.Type));
        ent.Alive = Read<bool>(nameof(ent.Alive));
        if(ent.Alive) {
            ent.Entity = ReadEntity(nameof(ent.Entity), em);
        } else {
            em.PushEmptyEntity(id);
        }
        EndReadObject();

        return ent;
    }

    private Entity ReadEntity(string name, EntityManager em) {
        BeginReadObject(name);
        Entity entity = null;
        var handle = ReadValueType<EntityHandle>(nameof(entity.Handle));
        var type   = ReadEnum<EntityType>(nameof(entity.Type));
        var flags  = ReadEnum<EntityFlags>(nameof(entity.Flags));
        var link   = Read<string>(nameof(entity.Name));
        var position = Read<Vector3>("Position");
        var orientation = Read<Quaternion>("Orientation");
        var scale = Read<Vector3>("Scale");
        entity = em.RecreateEntity(link, position, orientation, scale, type, flags);
        entity.Load(this);
        Assert(handle.Id == entity.Handle.Id, $"Entity Id's are not identical while reading entity. Recreated Id: {entity.Handle.Id}, Saved Id: {handle.Id}");
        EndReadObject();

        return entity;
    }

    public T[] ReadObjectArray<T>(Func<T> createObjectFunc, string name = null)
    where T : ISave {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            var obj = createObjectFunc();
            ReadObject(obj, $"{name}ArrayElement{i}");
            arr[i] = obj;
        }

        EndReadObject();

        return arr;
    }

    public T[] ReadUnmanagedObjectArray<T>(string name = null)
    where T : unmanaged, ISave {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public T[] ReadValueObjectArray<T>(string name = null)
    where T : struct, ISave {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<T> ReadNativeObjectArray<T>(Allocator allocator, string name = null)
    where T : unmanaged, ISave {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr = new NativeArray<T>(count, allocator);

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<T> ReadNativeArray<T>(Allocator allocator, string name = null)
    where T : unmanaged {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr   = new NativeArray<T>(count, allocator);

        for(var i = 0; i < count; ++i) {
            arr[i] = Read<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    private ObjectNode GetCurrentNode() {
        return ObjectStack.Peek();
    }

    private void BeginReadObject(string name) {
        if(ObjectStack.Count == 0) {
            ObjectStack.Push(Root);
            var currentNode = GetCurrentNode();

            if(currentNode.NestedObjects.TryGetValue(name, out var obj)) {
                ObjectStack.Push(obj);
            }
        } else {
            var currentNode = GetCurrentNode();

            if(currentNode.NestedObjects.TryGetValue(name, out var obj)) {
                ObjectStack.Push(obj);
            }
        }
    }

    private void EndReadObject() {
        if(ObjectStack.Count == 0) {
            ObjectStack.Push(Root);
        } else {
            ObjectStack.Pop();

            if(ObjectStack.Count == 0) {
                ObjectStack.Push(Root);
            }
        }
    }

    private void BeginObject(string name) {
        var token = new Token();

        token.Type  = TokenType.Ident;
        token.Ident = name;
        Tokens.Add(token);

        token       = new();
        token.Type  = TokenType.Separator;
        Tokens.Add(token);

        token       = new();
        token.Type  = TokenType.LParent;
        Tokens.Add(token);
    }

    private void EndObject() {
        var token   = new Token();
        token.Type  = TokenType.RParent;
        Tokens.Add(token);
    }

    private void SaveObject(string name, ObjectNode node) {
        Root.PushObject(name, node);
    }

    // Parsing

    private string Parse<T>(T value) {
        var type = typeof(T).ToString();

        switch(type) {
            case "UnityEngine.Vector3" : {
                var val = (Vector3)(object)value;
                return $"({val.x}, {val.y}, {val.z})";
            }
            case "UnityEngine.Vector3Int" : {
                var val = (Vector3Int)(object)value;
                return $"({val.x}, {val.y}, {val.z})";
            }
            case "UnityEngine.Vector2" : {
                var val = (Vector2)(object)value;
                return $"({val.x}, {val.y})";
            }
            case "UnityEngine.Vector2Int" : {
                var val = (Vector2Int)(object)value;
                return $"({val.x}, {val.y})";
            }
            case "UnityEngine.Vector4" : {
                var val = (Vector4)(object)value;
                return $"({val.x}, {val.y}, {val.z}, {val.w})";
            }
            case "UnityEngine.Quaternion" : {
                var val = (Quaternion)(object)value;
                return $"({val.x}, {val.y}, {val.z}, {val.w})";
            }
            case "UnityEngine.Matrix4x4" : {
                var val = (Matrix4x4)(object)value;
                return $"{val.GetRow(0)}, {val.GetRow(1)}, {val.GetRow(2)}, {val.GetRow(3)}";
            }
        }
        return value.ToString();
    }

    private T Parse<T>(string value, T defaultValue = default(T)) {
        var type = typeof(T).ToString();

        switch (type) {
            case "System.String" : {
                return (T)(object)value;
            }
            case "System.Byte" : {
                if(byte.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.SByte" : {
                if(sbyte.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.Int16" : {
                if(short.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.UInt16" : {
                if(ushort.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.Int32" : {
                if(int.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.UInt32" : {
                if(uint.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.Int64" : {
                if(long.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.UInt64" : {
                if(ulong.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.Boolean" : {
                if(bool.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.Single" : {
                if(float.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "System.Double" : {
                if(double.TryParse(value, out var v)) {
                    return (T)(object)v;
                } else {
                    return defaultValue;
                }
            }
            case "UnityEngine.Vector3" : {
                var ret = new Vector3();
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 3; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return (T)(object)ret;
            }
            case "UnityEngine.Vector3Int" : {
                var ret = new Vector3Int();
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 3; ++i) {
                    if(int.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return (T)(object)ret;
            }
            case "UnityEngine.Vector2" : {
                var ret = new Vector2();
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 2; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return (T)(object)ret;
            }
            case "UnityEngine.Vector2Int" : {
                var ret = new Vector2Int();
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 2; ++i) {
                    if(int.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return (T)(object)ret;
            }
            case "UnityEngine.Vector4" : {
                var ret = new Vector4();
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 4; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return (T)(object)ret;
            }
            case "UnityEngine.Quaternion" : {
                var ret = new Quaternion();
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 4; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return (T)(object)ret;
            }
            case "UnityEngine.Matrix4x4" : {
                var ret = new Matrix4x4();
                var currentRow = 0;
                var currentVector = new Vector4();
                var currentComp   = 0;
                var currString = "";

                for(var i = 0; i < value.Length; ++i) {
                    if(value[i] == '(') {
                        currentVector = new Vector4();
                        currentComp = 0;
                        currString = "";
                    } else if (value[i] == ')' && i == value.Length - 1) {
                        if(float.TryParse(currString, out var val)) {
                            currentVector[currentComp] = val;
                        }
                        ret.SetRow(currentRow, currentVector);
                    } else if(value[i] == ')' && value[i + 1] == ',') {
                        if(float.TryParse(currString, out var val)) {
                            currentVector[currentComp] = val;
                        }

                        ret.SetRow(currentRow++, currentVector);
                    } else if (value[i] == ',' && value[i - 1] != ')') {
                        if(float.TryParse(currString, out var val)) {
                            currentVector[currentComp] = val;
                        }

                        currentComp++;
                        currString = "";
                    } else if(value[i] != '(' && value[i] != ')' && value[i] != ' ' && value[i] != ',') {
                        currString += value[i];
                    }
                }

                return (T)(object)ret;
            }
            default :
            Debug.LogError($"Can't parse type: {type}");
            return default(T);
        }
    }

    private List<Token> Tokenize(string text) {
        var tokens = new List<Token>();
        var sb     = new StringBuilder();
        var len    = text.Length;
        var line   = 0;
        var col    = 0;

        for (var i = 0; i < len; ++i, col++) {
            var c = text[i];

            switch(c) {
                case ' '  : break;
                case '\r' : break;
                case '\t' : break;
                case '\n' : {
                    line++;
                    if(tokens[tokens.Count-1].Type == TokenType.LParent) break;
                    if(tokens[tokens.Count-1].Type == TokenType.RParent) break;

                    var last = FindLastCharExceptSpace(text, i);

                    if(last == '\n') break;

                    var token = new Token();

                    token.Type   = TokenType.Value;
                    token.Value  = sb.ToString();
                    token.Line   = line;
                    token.Column = col - token.Value.Length - 1;

                    sb.Clear();
                    tokens.Add(token);
                    col = 0;
                } break;
                case (char)TokenType.Separator : {
                    var token = new Token();

                    token.Type   = TokenType.Ident;
                    token.Ident  = sb.ToString();
                    token.Line   = line;
                    token.Column = col - token.Ident.Length - 1;
                    sb.Clear();
                    tokens.Add(token);

                    token = new();

                    token.Type   = TokenType.Separator;
                    token.Line   = line;
                    token.Column = col - 1;
                    tokens.Add(token);
                } break;
                case (char)TokenType.LParent : {
                    var token = new Token();

                    token.Type   = TokenType.LParent;
                    token.Line   = line;
                    token.Column = col - 1;
                    tokens.Add(token);
                } break;
                case (char)TokenType.RParent : {
                    var token = new Token();

                    token.Type   = TokenType.RParent;
                    token.Line   = line;
                    token.Column = col - 1;
                    tokens.Add(token);
                } break;
                case '"' : {
                    i++;
                    col++;

                    for ( ; i < len; ++i, ++col) {
                        if(text[i] == '"' && text[i - 1] != '\\') {
                            break;
                        }

                        sb.Append(text[i]);
                    }
                } break;
                case (char)TokenType.LBracket : {
                    for ( ; i < len; ++i, ++col) {
                        if(text[i] == (char)TokenType.RBracket) {
                            sb.Append(text[i]);
                            break;
                        }

                        sb.Append(text[i]);
                    }
                } break;
                default : {
                    sb.Append(c);
                } break;
            }
        }

        return tokens;
    }

    private char FindLastCharExceptSpace(string text, int i) {
        i--;
        for ( ; i >= 0; --i) {
            if(text[i] != ' ' && text[i] != '\r') return text[i];
        }

        return ' ';
    }

    private void UnexpectedToken(TokenType expecting, Token getting) {
        Debug.LogError($"Unexpected token. Expecting {expecting}, getting {getting.Type} at {getting.Line}:{getting.Column}");
    }

    private Field ParseField(int valueIndex) {
        if(Tokens[valueIndex - 1].Type != TokenType.Separator) {
            UnexpectedToken(TokenType.Separator, Tokens[valueIndex - 1]);
            return new Field();
        }

        if(Tokens[valueIndex - 2].Type != TokenType.Ident) {
            UnexpectedToken(TokenType.Ident, Tokens[valueIndex - 2]);
            return new Field();
        }

        return new Field(Tokens[valueIndex - 2].Ident, Tokens[valueIndex].Value);
    }

    private ObjectNode ParseObject(ref int i, out string name) {
        var node = ObjectNode.Create();

        if (Tokens[i - 1].Type != TokenType.Separator) {
            UnexpectedToken(TokenType.Separator, Tokens[i - 1]);
            name = "";
            return node;
        }

        if (Tokens[i - 2].Type != TokenType.Ident) {
            UnexpectedToken(TokenType.Ident, Tokens[i - 2]);
            name = "";
            return node;
        }

        name = Tokens[i - 2].Ident;

        i++;

        for ( ; i < Tokens.Count; ++i) {
            var token = Tokens[i];

            if(token.Type == TokenType.RParent) break;

            switch(token.Type) {
                case TokenType.Value : {
                    var field = ParseField(i);

                    node.PushField(field);
                } break;
                case TokenType.LParent : {
                    var obj = ParseObject(ref i, out var nm);

                    node.PushObject(nm, obj);
                } break;
            }
        }

        return node;
    }
}