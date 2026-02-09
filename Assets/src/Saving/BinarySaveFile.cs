using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using static Assertions;

[UnityEngine.Scripting.Preserve]
public unsafe class BinarySaveFile : IDisposable {
    struct ArrayElement {
        public int    Index;
        public object Obj;
    }

    public byte[] ByteBuffer;
    public byte[] LoadedBytes;
    public Arena  Arena;
    public int    Pointer = 0;
    public Dictionary<Type, TypeSaveMeta> MetaData = new();

    private List<ArrayElement> _arrayElements = new();
    private object[]           _saveParams    = new object[1];

    private const uint InitialBufferLength = 5000;
    private const uint InitialCurrentBufferLength = 500;

    public BinarySaveFile() {
        _saveParams[0] = this;
        Arena = new (65536);
        Arena.Free();
    }

    public void Dispose() {
        ByteBuffer = null;
        // Arena.Dispose();
    }

    private void PushBytes(UnmanagedArray<byte> b) {
        uint len = (uint)Pointer + b.Length;
        if(len >= ByteBuffer.Length) {
            var newlen = len << 1;
            Array.Resize(ref ByteBuffer, (int)newlen);
        }

        for(uint i = 0; i < b.Length; ++i) {
            ByteBuffer[Pointer + i] = b[i];
        }

        Pointer += (int)b.Length;
    }


    public void NewFile() {
        Pointer = 0;
        if(ByteBuffer == null) {
            ByteBuffer = new byte[InitialBufferLength];
        }
    }

    public void LoadFromFile(string path) {
        Pointer = 0;
        LoadedBytes = File.ReadAllBytes(path);
    }

    public void SaveToFile(string path) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) {
            stream.Write(ByteBuffer, 0, Pointer);
        }
    }

    public void Write<T>(T obj) {
        if (obj == null) {
            var size = sizeof(IntPtr);
            if (size == 4) {
                Write<int>(0);
            } else {
                Write<long>(0);
            }
            return;
        }

        var type = obj.GetType();
        // Debug.Log($"Writing: {type.ToString()}");

        if (type == typeof(string)) {
            var str   = (string)(object)obj;
            var bytes = Parse(str);
            Write(str.Length);
            PushBytes(bytes);
            return;
        }

        if (type.IsArray) {
            // Debug.Log("ARRAY");
            var arr = (Array)(object)obj;
            Write(arr.Length);
            _arrayElements.Clear();

            for (var i = 0; i < arr.Length; i++) {
                var val = arr.GetValue(i);

                if (val != null) {
                    _arrayElements.Add(new ArrayElement {
                        Index = i,
                        Obj   = val
                    });
                }
            }

            Write(_arrayElements.Count);

            foreach(var elem in _arrayElements) {
                Write(elem.Index);
                Write(elem.Obj);
            }
            return;
        }

        if (type.IsPrimitive) {
            var bytes = Parse(obj);
            Assert(bytes.Data != null, "Cannot parse the data for type '%'", type.ToString());
            PushBytes(bytes);
            // Debug.Log("Primitive");
            return;
        }

        switch(type.FullName) {
            case "UnityEngine.Vector3"    :
            case "UnityEngine.Vector3Int" :
            case "UnityEngine.Vector2"    :
            case "UnityEngine.Vector2Int" :
            case "UnityEngine.Vector4"    :
            case "UnityEngine.Vector4Int" :
            case "UnityEngine.Quaternion" :
            case "UnityEngine.Matrix4x4"  :
                var bytes = Parse(obj);
                Assert(bytes.Data != null, "Cannot parse the data");
                PushBytes(bytes);
                // Debug.Log("UNITY");
                return;
        }

        if (type.IsSubclassOf(typeof(MonoBehaviour))) {
            var mono = (MonoBehaviour)(object)obj;
            var transform = mono.transform;

            Write(transform.position);
            Write(transform.rotation);
            Write(transform.localScale);
            Write(mono.gameObject.activeSelf);

            var rb   = mono.GetComponent<Rigidbody>();
            var rb2d = mono.GetComponent<Rigidbody2D>();

            if (rb) {
                Write(rb.position);
                Write(rb.linearVelocity);
                Write(rb.angularVelocity);
                Write(rb.mass);
                Write(rb.linearDamping);
                Write(rb.angularDamping);
            }

            if (rb2d) {
                Write(rb2d.position);
                Write(rb2d.linearVelocity);
                Write(rb2d.angularVelocity);
                Write(rb2d.mass);
                Write(rb2d.linearDamping);
                Write(rb2d.angularDamping);
            }
        }

        var meta = new TypeSaveMeta();

        if (MetaData.ContainsKey(type)) {
            meta = MetaData[type];
        } else {
            meta = ParseMeta(type);

            MetaData.Add(type, meta);
        }

        if (meta.Save != null) {
            // Debug.LogWarning("Save function!");
            meta.Save.Invoke(obj, _saveParams);
            return;
        }

        foreach(var field in meta.Fields) {
            // Debug.Log($"Begin write field: {field.Name}, {field.FieldType.ToString()}");
            Write(field.GetValue(obj));
        }
    }

    public T Read<T>(T obj = default(T), Type type = null) {
        if (type == typeof(object)) {
            var size = sizeof(IntPtr);
            IntPtr ptr;

            if (size == 4) {
                var i = Read<int>();
                ptr = (IntPtr)i;
            } else {
                var i = Read<long>();
                ptr = (IntPtr)i;
            }

            if (ptr == IntPtr.Zero) return (T)(object)null;

            return (T)(object)ptr;
        }

        if (type == null) {
            if (obj == null) {
                type = typeof(T);
            } else {
                type = obj.GetType();
            }
        }

        // Debug.Log($"Reading: {type}");

        if (type.IsPrimitive) {
            // Debug.Log("Reading Primitive");
            return Parse<T>(objectType: type);
        }

        switch(type.FullName) {
            case "UnityEngine.Vector3"    :
            case "UnityEngine.Vector3Int" :
            case "UnityEngine.Vector2"    :
            case "UnityEngine.Vector2Int" :
            case "UnityEngine.Vector4"    :
            case "UnityEngine.Vector4Int" :
            case "UnityEngine.Quaternion" :
            case "UnityEngine.Matrix4x4"  :
                // Debug.Log("Reading UNITY");
                return Parse<T>(objectType: type);
        }

        if (type == typeof(string)) {
            var len = Parse<int>();
            return (T)(object)Parse<string>(len, objectType: typeof(string));
        }

        if (type.IsArray) {
            // Debug.Log("Reading array");
            var len         = Read<int>();
            var nonEmptyLen = Read<int>();

            var arr = Array.CreateInstance(type.GetElementType(), len);

            for (var i = 0; i < nonEmptyLen; i++) {
                var index = Read<int>();
                var o     = Read<object>(null, type.GetElementType());

                arr.SetValue(o, index);
            }

            return (T)(object)arr;
        }

        // Debug.Log("Reading Complex");

        var meta = new TypeSaveMeta();

        if (MetaData.ContainsKey(type)) {
            meta = MetaData[type];
        } else {
            meta = ParseMeta(type);

            MetaData.Add(type, meta);
        }

        if (obj == null) {
            obj = (T)Activator.CreateInstance(type);
        }

        if (type.IsSubclassOf(typeof(MonoBehaviour))) {
            var mono      = (MonoBehaviour)(object)obj;
            var transform = mono.transform;

            transform.position   = Read<Vector3>();
            transform.rotation   = Read<Quaternion>();
            transform.localScale = Read<Vector3>();
            mono.gameObject.SetActive(Read<bool>());

            var rb   = mono.GetComponent<Rigidbody>();
            var rb2d = mono.GetComponent<Rigidbody2D>();

            if (rb) {
                rb.position        = Read<Vector3>();
                rb.linearVelocity        = Read<Vector3>();
                rb.angularVelocity = Read<Vector3>();
                rb.mass            = Read<float>();
                rb.linearDamping            = Read<float>();
                rb.angularDamping     = Read<float>();
            }

            if (rb2d) {
                rb2d.position        = Read<Vector2>();
                rb2d.linearVelocity        = Read<Vector2>();
                rb2d.angularVelocity = Read<float>();
                rb2d.mass            = Read<float>();
                rb2d.linearDamping            = Read<float>();
                rb2d.angularDamping     = Read<float>();
            }
        }

        if (meta.Load != null) {
            // Debug.LogWarning("Load function!");
            meta.Load.Invoke(obj, _saveParams);
            return obj;
        }

        foreach(var field in meta.Fields) {
            // Debug.Log($"{field.Name}, {field.FieldType.ToString()}");
            var version = TypeVersion.GetVersion(type);
            var vattr   = field.GetCustomAttribute(typeof(VAttribute));

            if (vattr != null) {
                if (version < ((VAttribute)vattr).Version) continue;
            }

            field.SetValue(obj, Read(field.GetValue(obj), field.FieldType));
        }

        return obj;
    }

    // Parsing
    private UnmanagedArray<byte> Parse<T>(T value) {
        var type = value.GetType().ToString();

        switch(type) {
            case "System.String" : {
                var val = (string)(object)value;
                var ret = Arena.Alloc<byte>(sizeof(char) * (uint)val.Length);

                for(var i = 0; i < val.Length; ++i) {
                    short a = (short)val[i];
                    ret[sizeof(short) * i] = (byte)(a & 0xff);
                    ret[sizeof(short) * i + 1] = (byte)((a >> 8) & 0xff);
                }

                return new UnmanagedArray<byte>(ret, sizeof(char) * (uint)val.Length);
            }
            case "UnityEngine.Vector3" : {
                var val = (Vector3)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Vector3));
                var ptr = (byte*)(&val);

                for(var i = 0; i < 3; ++i) {
                    for(var j = 0; j < sizeof(float); ++j) {
                        ret[i * sizeof(float) + j] = ptr[j + i * sizeof(float)];
                    }
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector3));
            }
            case "UnityEngine.Vector3Int" : {
                var val = (Vector3Int)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Vector3Int));

                for(var i = 0; i < 3; ++i) {
                    ret[0 + sizeof(int) * i] = (byte)(val[i] & 0xff);
                    ret[1 + sizeof(int) * i] = (byte)((val[i] >> 8) & 0xff);
                    ret[2 + sizeof(int) * i] = (byte)((val[i] >> 16) & 0xff);
                    ret[3 + sizeof(int) * i] = (byte)((val[i] >> 24) & 0xff);
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector3Int));
            }
            case "UnityEngine.Vector2" : {
                var val = (Vector2)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Vector2));
                var ptr = (byte*)(&val);

                for(var i = 0; i < 2; ++i) {
                    for(var j = 0; j < sizeof(float); ++j) {
                        ret[i * sizeof(float) + j] = ptr[j + i * sizeof(float)];
                    }
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector2));
            }
            case "UnityEngine.Vector2Int" : {
                var val = (Vector2Int)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Vector2Int));

                for(var i = 0; i < 2; ++i) {
                    ret[0 + sizeof(int) * i] = (byte)(val[i] & 0xff);
                    ret[1 + sizeof(int) * i] = (byte)((val[i] >> 8) & 0xff);
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector2Int));
            }
            case "UnityEngine.Vector4" : {
                var val = (Vector4)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Vector4));
                var ptr = (byte*)(&val);

                for(var i = 0; i < 4; ++i) {
                    for(var j = 0; j < sizeof(float); ++j) {
                        ret[i * sizeof(float) + j] = ptr[j + i * sizeof(float)];
                    }
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector4));
            }
            case "UnityEngine.Quaternion" : {
                var val = (Quaternion)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Vector3));
                var euler = val.eulerAngles;
                var ptr = (byte*)(&euler);

                for(var i = 0; i < 3; ++i) {
                    for(var j = 0; j < sizeof(float); ++j) {
                        ret[i * sizeof(float) + j] = ptr[j + i * sizeof(float)];
                    }
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector3));
            }
            case "UnityEngine.Matrix4x4" : {
                var val = (Matrix4x4)(object)value;
                var ret = Arena.Alloc<byte>((uint)sizeof(Matrix4x4));
                var ptr = (byte*)(&val);

                for(var i = 0; i < 16; ++i) {
                    for(var j = 0; j < sizeof(float); ++j) {
                        ret[i * sizeof(float) + j] = ptr[j + i * sizeof(float)];
                    }
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Matrix4x4));
            }
            case "System.Single" : {
                var val = (float)(object)value;
                var ret = Arena.Alloc<byte>(sizeof(float));
                var ptr = (byte*)(&val);

                for(var i = 0; i < sizeof(float); ++i) {
                    ret[i] = ptr[i];
                }

                return new UnmanagedArray<byte>(ret, sizeof(float));
            }
            case "System.Double" : {
                var val = (double)(object)value;
                var ret = Arena.Alloc<byte>(sizeof(double));
                var ptr = (byte*)(&val);

                for(var i = 0; i < sizeof(double); ++i) {
                    ret[i] = ptr[i];
                }

                return new UnmanagedArray<byte>(ret, sizeof(double));
            }
            case "System.Int16" : {
                var val   = (short)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(short));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(short));
            }
            case "System.UInt16" : {
                var val   = (ushort)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(ushort));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(ushort));
            }
            case "System.Int32" : {
                var val   = (int)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(int));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);
                ret[2] = (byte)((val >> 16) & 0xff);
                ret[3] = (byte)((val >> 24) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(int));
            }
            case "System.UInt32" : {
                var val   = (uint)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(uint));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);
                ret[2] = (byte)((val >> 16) & 0xff);
                ret[3] = (byte)((val >> 24) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(uint));
            }
            case "System.Int64" : {
                var val   = (long)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(long));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);
                ret[2] = (byte)((val >> 16) & 0xff);
                ret[3] = (byte)((val >> 24) & 0xff);
                ret[4] = (byte)((val >> 32) & 0xff);
                ret[5] = (byte)((val >> 40) & 0xff);
                ret[6] = (byte)((val >> 48) & 0xff);
                ret[7] = (byte)((val >> 56) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(long));
            }
            case "System.UInt64" : {
                var val   = (ulong)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(ulong));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);
                ret[2] = (byte)((val >> 16) & 0xff);
                ret[3] = (byte)((val >> 24) & 0xff);
                ret[4] = (byte)((val >> 32) & 0xff);
                ret[5] = (byte)((val >> 40) & 0xff);
                ret[6] = (byte)((val >> 48) & 0xff);
                ret[7] = (byte)((val >> 56) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(ulong));
            }
            case "System.Byte" : {
                var val   = (byte)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(byte));
                ret[0]    = val;

                return new UnmanagedArray<byte>(ret, sizeof(byte));
            }
            case "System.SByte" : {
                var val   = (sbyte)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(sbyte));
                ret[0]    = (byte)val;

                return new UnmanagedArray<byte>(ret, sizeof(sbyte));
            }
            case "System.Boolean" : {
                var val   = (bool)(object)value;
                var ret   = Arena.Alloc<byte>(sizeof(bool));
                var ptr   = (byte*)(&val);

                for(var i = 0; i < sizeof(bool); ++i) {
                    ret[i] = ptr[i];
                }

                return new UnmanagedArray<byte>(ret, sizeof(bool));
            }
        }
        return default;
    }

    private T Parse<T>(int stringLength = 0, T defaultValue = default(T), Type objectType = null) {
        string type;

        if (objectType == null) {
            type = typeof(T).ToString();
        } else {
            type = objectType.ToString();
        }

        switch (type) {
            case "System.String" : {
                var str = Arena.Alloc<char>((uint)stringLength);

                for(var i = 0; i < stringLength; ++i) {
                    short o = (short)(LoadedBytes[Pointer + 1 + sizeof(short) * i] << 8 |
                                      LoadedBytes[Pointer + sizeof(short) * i]);
                    str[i] = (char)o;
                }

                Pointer += stringLength * sizeof(short);

                return (T)(object)new string(str, 0, stringLength);
            }
            case "System.Byte" : {
                byte o = LoadedBytes[Pointer];
                Pointer += sizeof(byte);
                return (T)(object)o;
            }
            case "System.SByte" : {
                sbyte o = (sbyte)LoadedBytes[Pointer];
                Pointer += sizeof(sbyte);
                return (T)(object)o;
            }
            case "System.Int16" : {
                short o = (short)(LoadedBytes[Pointer + 1] << 8 |
                                  LoadedBytes[Pointer]);

                Pointer += sizeof(short);

                return (T)(object)o;
            }
            case "System.UInt16" : {
                ushort o = (ushort)(LoadedBytes[Pointer + 1] << 8 |
                                    LoadedBytes[Pointer]);

                Pointer += sizeof(ushort);

                return (T)(object)o;
            }
            case "System.Int32" : {
                int o = LoadedBytes[Pointer + 3] << 24 |
                        LoadedBytes[Pointer + 2] << 16 |
                        LoadedBytes[Pointer + 1] << 8  |
                        LoadedBytes[Pointer];

                Pointer += sizeof(int);

                return (T)(object)o;
            }
            case "System.UInt32" : {
                uint o = (uint)(LoadedBytes[Pointer + 3] << 24 |
                                LoadedBytes[Pointer + 2] << 16 |
                                LoadedBytes[Pointer + 1] << 8  |
                                LoadedBytes[Pointer]);
                Pointer += sizeof(uint);

                return (T)(object)o;
            }
            case "System.Int64" : {
                long o = (long)LoadedBytes[Pointer + 7] << 56 |
                         (long)LoadedBytes[Pointer + 6] << 48 |
                         (long)LoadedBytes[Pointer + 5] << 40 |
                         (long)LoadedBytes[Pointer + 4] << 32 |
                         (long)LoadedBytes[Pointer + 3] << 24 |
                         (long)LoadedBytes[Pointer + 2] << 16 |
                         (long)LoadedBytes[Pointer + 1] << 8  |
                         (long)LoadedBytes[Pointer];

                Pointer += sizeof(long);

                return (T)(object)o;
            }
            case "System.UInt64" : {
                ulong o = (ulong)LoadedBytes[Pointer + 7] << 56 |
                          (ulong)LoadedBytes[Pointer + 6] << 48 |
                          (ulong)LoadedBytes[Pointer + 5] << 40 |
                          (ulong)LoadedBytes[Pointer + 4] << 32 |
                          (ulong)LoadedBytes[Pointer + 3] << 24 |
                          (ulong)LoadedBytes[Pointer + 2] << 16 |
                          (ulong)LoadedBytes[Pointer + 1] << 8  |
                          (ulong)LoadedBytes[Pointer];

                Pointer += sizeof(ulong);

                return (T)(object)o;
            }
            case "System.Boolean" : {
                fixed(byte *ptr = LoadedBytes) {
                    bool *o = (bool*)(ptr + Pointer);
                    Pointer += sizeof(bool);
                    return (T)(object)*o;
                }
            }
            case "System.Single" : {
                fixed(byte *ptr = LoadedBytes) {
                    float *o = (float*)(ptr + Pointer);
                    Pointer  += sizeof(float);
                    return (T)(object)*o;
                }
            }
            case "System.Double" : {
                fixed(byte *ptr = LoadedBytes) {
                    double *o = (double*)(ptr + Pointer);
                    Pointer   += sizeof(double);
                    return (T)(object)*o;
                }
            }
            case "UnityEngine.Vector3" : {
                var ret = new Vector3();

                for(var i = 0; i < 3; ++i) {
                    fixed(byte *ptr = LoadedBytes) {
                        float *o = (float*)(ptr + (Pointer + i * sizeof(float)));
                        ret[i] = *o;
                    }
                }

                Pointer += 3 * sizeof(float);

                return (T)(object)ret;
            }
            case "UnityEngine.Vector3Int" : {
                var ret = new Vector3Int();

                for(var i = 0; i < 3; ++i) {
                    int o = (int)(LoadedBytes[Pointer + 3 + sizeof(int) * i] << 24 |
                                  LoadedBytes[Pointer + 2 + sizeof(int) * i] << 16 |
                                  LoadedBytes[Pointer + 1 + sizeof(int) * i] << 8  |
                                  LoadedBytes[Pointer + sizeof(int) * i]);

                    ret[i] = o;
                }

                Pointer += 3 * sizeof(int);

                return (T)(object)ret;
            }
            case "UnityEngine.Vector2" : {
                var ret = new Vector2();

                for(var i = 0; i < 2; ++i) {
                    fixed(byte *ptr = LoadedBytes) {
                        float *o = (float*)(ptr + (Pointer + i * sizeof(float)));
                        ret[i] = *o;
                    }
                }

                Pointer += 2 * sizeof(float);

                return (T)(object)ret;
            }
            case "UnityEngine.Vector2Int" : {
                var ret = new Vector2Int();

                for(var i = 0; i < 2; ++i) {
                    int o = (int)LoadedBytes[Pointer + 1 + sizeof(int) * i] << 8  |
                                 LoadedBytes[Pointer + sizeof(int) * i];

                    ret[i] = o;
                }

                Pointer += 2 * sizeof(int);

                return (T)(object)ret;
            }
            case "UnityEngine.Vector4" : {
                var ret = new Vector4();

                for(var i = 0; i < 4; ++i) {
                    fixed(byte *ptr = LoadedBytes) {
                        float *o = (float*)(ptr + (Pointer + i * sizeof(float)));
                        ret[i] = *o;
                    }
                }

                Pointer += 4 * sizeof(float);

                return (T)(object)ret;
            }
            case "UnityEngine.Quaternion" : {
                var euler = new Vector3();

                for(var i = 0; i < 3; ++i) {
                    fixed(byte *ptr = LoadedBytes) {
                        float *o = (float*)(ptr + (Pointer + i * sizeof(float)));
                        euler[i] = *o;
                    }
                }

                var ret = Quaternion.Euler(euler);
                Pointer += 3 * sizeof(float);

                return (T)(object)ret;
            }
            case "UnityEngine.Matrix4x4" : {
                var ret = new Matrix4x4();

                for(var i = 0; i < 16; ++i) {
                    fixed(byte *ptr = LoadedBytes) {
                        float *o = (float*)(ptr + (Pointer + i * sizeof(float)));
                        ret[i] = *o;
                    }
                }

                Pointer += 16 * sizeof(float);

                return (T)(object)ret;
            }
            default :
            Debug.LogError($"Can't parse type: {type}");
            return default(T);
        }
    }

    public unsafe struct UnmanagedArray<T>
    where T : unmanaged {
        public T   *Data;
        public uint Length;

        public UnmanagedArray(T *data, uint length) {
            Data = data;
            Length = length;
        }

        public T this[uint idx] {
            get {
                Assert(idx < Length);
                return Data[idx];
            }

            set {
                Assert(idx < Length);
                Data[idx] = value;
            }
        }
    }

    private static TypeSaveMeta ParseMeta(Type type) {
        var meta = new TypeSaveMeta();

        meta.Fields = type.GetFields(BindingFlags.Public    |
                                                  BindingFlags.NonPublic |
                                                  BindingFlags.Instance  |
                                                  BindingFlags.FlattenHierarchy)
                                       .Where((field) => {
                                          return field.GetCustomAttribute(typeof(DontSaveAttribute)) == null &&
                                                 field.Name != "m_value";
                                       })
                                       .ToArray();

        var methods = type.GetMethods(BindingFlags.Public |
                                      BindingFlags.NonPublic |
                                      BindingFlags.Instance |
                                      BindingFlags.IgnoreCase);

        foreach(var method in methods) {
            if (string.Compare(method.Name, "save", true) == 0) {
                var pars = method.GetParameters();

                if (pars.Length == 1 && pars[0].ParameterType == typeof(BinarySaveFile)) {
                    meta.Save = method;
                    break;
                }
            }
        }

        foreach(var method in methods) {
            if (string.Compare(method.Name, "load", true) == 0) {
                var pars = method.GetParameters();

                if (pars.Length == 1 && pars[0].ParameterType == typeof(BinarySaveFile)) {
                    meta.Load = method;
                    break;
                }
            }
        }

        return meta;
    }

    /*
    Don't need it, but it's boilerplate code and I don't want to rewrite it again if I need it somewhere else.

    public void WriteEnum(Enum e, string name = null) {
        var type = Enum.GetUnderlyingType(e.GetType()).ToString();

        switch(type) {
            case "System.Int32" :
                Write((int)Convert.ChangeType(e, typeof(int)));
            break;
            case "System.UInt32" :
                Write((uint)Convert.ChangeType(e, typeof(uint)));
            break;
            case "System.Int64" :
                Write((long)Convert.ChangeType(e, typeof(long)));
            break;
            case "System.UInt64" :
                Write((ulong)Convert.ChangeType(e, typeof(ulong)));
            break;
            case "System.Int16" :
                Write((short)Convert.ChangeType(e, typeof(short)));
            break;
            case "System.UInt16" :
                Write((ushort)Convert.ChangeType(e, typeof(ushort)));
            break;
            case "System.Byte" :
                Write((byte)Convert.ChangeType(e, typeof(byte)));
            break;
            case "System.SByte" :
                Write((sbyte)Convert.ChangeType(e, typeof(sbyte)));
            break;
            default :
                Debug.LogError($"Can't convert from {type} to any proper type");
            break;
        }
    }

    public T ReadEnum<T>(string name = null)
    where T : Enum {
        var type = Enum.GetUnderlyingType(typeof(T)).ToString();

        switch(type) {
            case "System.Int32" : {
                var val = Read<int>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.UInt32" : {
                var val = Read<uint>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.Int64" : {
                var val = Read<long>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.UInt64" : {
                var val = Read<ulong>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.Int16" : {
                var val = Read<short>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.UInt16" : {
                var val = Read<ushort>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.Byte" : {
                var val = Read<byte>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            case "System.SByte" : {
                var val = Read<sbyte>();
                return (T)Enum.ToObject(typeof(T), val);
            }
            default : {
                Debug.LogError($"Can't read enum with underlying type: {type}");
            }
            break;
        }

        return default;
    }
    */
}