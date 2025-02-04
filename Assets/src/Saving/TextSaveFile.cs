using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Collections;
using System;

using static Assertions;

public class TextSaveFile : SaveFileBase {
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

    public string[]      LoadedLines;
    public int           LinesCount;
    public StringBuilder Sb = new();
    public int           CurrentOffset;
    public ObjectNode    Root;
    public Stack<ObjectNode> ObjectStack = new();

    public const int Offset = 4;
    public const string Separator = " : ";
    
    public override void NewFile(uint version) {
        Sb.Clear();
        base.NewFile(version);
        Sb.AppendLine();
    }

    protected override void LoadFile(string path) {
        Root = new ObjectNode {
            Fields = new(),
            NestedObjects = new()
        };

        LoadedLines = File.ReadAllLines(path);
        LinesCount = LoadedLines.Length;
        ObjectStack.Clear();

        var startLine = 0;

        while(startLine < LinesCount) {
            var line = LoadedLines[startLine].TrimStart().TrimEnd();
            var separateLine = line.Split(Separator);
            if(separateLine.Length > 1 && separateLine[1] == "{") {
                startLine++;
                SaveObject(separateLine[0], ParseObject(ref startLine, ObjectNode.Create()));
            } else if(separateLine.Length == 2) {
                Root.Fields.Add(new Field(separateLine[0], separateLine[1]));
                startLine++;
            } else {
                startLine++;
            }
        }
        
        ObjectStack.Push(Root);
    }

    protected override void SaveFile(string path) {
        File.CreateText(path).Close();
        File.WriteAllText(path, Sb.ToString());
    }

    public override void Write<T>(T value, string name = null) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(Parse(value));
        NextLine();
    }

    public override void WriteObject(ISave save, string name = null) {
        BeginObject(name);
        save.Save(this);
        EndObject();
    }

    public override void WriteArray<T>(int itemsCount, T[] arr, string name = null) {
        BeginObject(name);
        Write(itemsCount, "Count");

        for(var i = 0; i < itemsCount; ++i) {
            Write(arr[i], $"{name}ArrayElement{i}");
            NextLine();
        }
        EndObject();
    }

    public override void WriteObjectArray<T>(int itemsCount, T[] arr, string name = null) {
        BeginObject(name);
        Write(itemsCount, "Count");

        for(var i = 0; i < itemsCount; ++i) {
            WriteObject(arr[i], $"{name}ArrayElement{i}");
            NextLine();
        }
        EndObject();
    }

    public override void WriteNativeArray<T>(int itemsCount, NativeArray<T> arr, string name = null) {
        BeginObject(name);
        Write(itemsCount, "Count");

        for(var i = 0; i < itemsCount; ++i) {
            Write(arr[i], $"{name}ArrayElement{i}");
            NextLine();
        }
        EndObject();
    }

    public override void WritePackedEntity(PackedEntity e, uint id, string name = null) {
        BeginObject(name);
        Write(id, "Id");
        Write(e.Tag, nameof(e.Tag));
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
        WriteObject(e.Prefab, nameof(e.Prefab));
        Write(e.transform.position, "Position");
        Write(e.transform.rotation, "Orientation");
        Write(e.transform.localScale, "Scale");
        e.Save(this);
        EndObject();
    }

    public override void WriteEnum(Enum e, string name = null) {
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

    public override T Read<T>(string name = null, T defaultValue = default(T)) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                return Parse<T>(field.Value);
            }
        }

        return defaultValue;
    }

    public override T[] ReadArray<T>(string name = null) {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = Read<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public override void ReadObject(ISave obj, string name = null) {
        BeginReadObject(name);
        obj.Load(this);
        EndReadObject();
    }

    public override T ReadValueType<T>(string name = null) {
        var ret = default(T);

        BeginReadObject(name);
        ret.Load(this);
        EndReadObject();

        return ret;
    }

    public override T ReadEnum<T>(string name) {
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

    public override PackedEntity ReadPackedEntity(EntityManager em, string name = null) {
        BeginReadObject(name);
        var ent = new PackedEntity();
        var id  = Read<uint>("Id");
        ent.Tag = Read<uint>(nameof(ent.Tag));
        ent.Type = ReadEnum<EntityType>(nameof(ent.Type));
        ent.Alive = Read<bool>(nameof(ent.Alive));
        if(ent.Alive) {
            ent.Entity = ReadEntity(nameof(ent.Entity), em, ent.Tag);
        } else {
            em.PushEmptyEntity(id);
        }
        EndReadObject();

        return ent;
    }
    
    private Entity ReadEntity(string name, EntityManager em, uint tag) {
        BeginReadObject(name);
        Entity entity = null;
        var handle = ReadValueType<EntityHandle>(nameof(entity.Handle));
        var type   = ReadEnum<EntityType>(nameof(entity.Type));
        var flags  = ReadEnum<EntityFlags>(nameof(entity.Flags));
        var link   = ReadValueType<ResourceLink>(nameof(entity.Prefab));
        var position = Read<Vector3>("Position");
        var orientation = Read<Quaternion>("Orientation");
        var scale = Read<Vector3>("Scale");
        entity = em.RecreateEntity(link, tag, position, orientation, scale, type, flags);
        entity.Load(this);
        Assert(handle.Id == entity.Handle.Id, $"Entity Id's are not identical while reading entity. Recreated Id: {entity.Handle.Id}, Saved Id: {handle.Id}");
        EndReadObject();

        return entity;
    }

    public override T[] ReadObjectArray<T>(Func<T> createObjectFunc, string name = null) {
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

    public override T[] ReadUnmanagedObjectArray<T>(string name = null) {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public override T[] ReadValueObjectArray<T>(string name = null) {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }
        
        EndReadObject();

        return arr;
    }

    public override NativeArray<T> ReadNativeObjectArray<T>(Allocator allocator, string name = null) {
        BeginReadObject(name);
        var count = Read<int>("Count");
        var arr = new NativeArray<T>(count, allocator);

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }
        
        EndReadObject();

        return arr;
    }

    public override NativeArray<T> ReadNativeArray<T>(Allocator allocator, string name = null) {
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
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append('{');
        CurrentOffset += Offset;
        NextLine();
    }

    private void NextLine() {
        Sb.AppendLine();
        Sb.Append(' ', CurrentOffset);
    }

    private void EndObject() {
        Sb.AppendLine();
        CurrentOffset -= Offset;
        Sb.Append(' ', CurrentOffset);
        Sb.Append('}');
        NextLine();
    }

    private void AddNameSeparator() {
        Sb.Append(Separator);
    }

    private ObjectNode ParseObject(ref int startLine, ObjectNode node = default) {
        while(true) {
            var line = LoadedLines[startLine].TrimStart().TrimEnd();

            if(string.IsNullOrEmpty(line)) {
                startLine++;
                continue;
            }

            if(line[0] == '}') {
                startLine++;
                break;
            }

            var separateLine = line.Split(Separator);

            if(separateLine[1] == "{") {
                startLine++;
                node.PushObject(separateLine[0], ParseObject(ref startLine, ObjectNode.Create()));
            } else {
                node.PushField(new Field(separateLine[0], separateLine[1]));
                startLine++;
            }
        }

        return node;
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

}