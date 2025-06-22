using System.IO;
using UnityEngine;
using Unity.Collections;
using System;

using static Assertions;
using static Context;

public unsafe class BinarySaveFile : ISaveFile {
    public byte[] ByteBuffer;
    public byte[] LoadedBytes;
    public int    Pointer = 0;

    public const uint InitialBufferLength = 5000;
    public const uint InitialCurrentBufferLength = 500;

    public void Dispose() {
        ByteBuffer = null;
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

    public void Write<T>(T value, string name = null) {
        var type  = typeof(T);

        if(type == typeof(string)) {
            var str = (string)(object)value;
            var bytes = Parse(str);
            Write(str.Length);
            PushBytes(bytes);
        } else {
            var bytes = Parse(value);
            if(bytes.Data != null) {
                PushBytes(bytes);
            }
        }
    }

    public void WriteObject(ISave save, string name = null) {
        save.Save(this);
    }

    public void WriteArray<T>(T[] arr, int itemsCount, string name = null) {
        Write(itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write(arr[i]);
        }
    }

    public void WriteObjectArray<T>(T[] arr, int itemsCount, string name = null)
    where T : ISave {
        Write(itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            WriteObject(arr[i]);
        }
    }

    public void WriteNativeArray<T>(NativeArray<T> arr, int itemsCount, string name = null)
    where T : unmanaged {
        Write(itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write(arr[i]);
        }
    }

    public void WritePackedEntity(PackedEntity e, uint id, string name = null) {
        Write(id);
        WriteEnum(e.Type);
        Write(e.Alive);
        if(e.Alive) {
            WriteEntity(null, e.Entity);
        }
    }

    private void WriteEntity(string name, Entity e) {
        WriteObject(e.Handle);
        WriteEnum(e.Type);
        WriteEnum(e.Flags);
        Write(e.Name);
        Write(e.transform.position);
        Write(e.transform.rotation);
        Write(e.transform.localScale);
        e.Save(this);
    }

    public void WriteEnum(Enum e, string name = null) {
        var type = Enum.GetUnderlyingType(e.GetType()).ToString();

        switch(type) {
            case "System.Int32" : {
                Write((int)Convert.ChangeType(e, typeof(int)));
            }
            break;
            case "System.UInt32" : {
                Write((uint)Convert.ChangeType(e, typeof(uint)));
            }
            break;
            case "System.Int64" : {
                Write((long)Convert.ChangeType(e, typeof(long)));
            }
            break;
            case "System.UInt64" : {
                Write((ulong)Convert.ChangeType(e, typeof(ulong)));
            }
            break;
            case "System.Int16" : {
                Write((short)Convert.ChangeType(e, typeof(short)));
            }
            break;
            case "System.UInt16" : {
                Write((ushort)Convert.ChangeType(e, typeof(ushort)));
            }
            break;
            case "System.Byte" : {
                Write((byte)Convert.ChangeType(e, typeof(byte)));
            }
            break;
            case "System.SByte" : {
                Write((sbyte)Convert.ChangeType(e, typeof(sbyte)));
            }
            break;
            default : {
                Debug.LogError($"Can't convert from {type} to any proper type");
            }
            break;
        }
    }

    public T Read<T>(string name = null, T defaultValue = default(T)) {
        var type = typeof(T);
        if(type == typeof(string)) {
            var len = Parse<int>();
            return (T)(object)Parse<string>(len);
        } else {
            return Parse<T>();
        }
    }

    public T[] ReadArray<T>(string name = null) {
        var count = Read<int>();
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = Read<T>();
        }

        return arr;
    }

    public void ReadObject(ISave obj, string name = null) {
        obj.Load(this);
    }

    public T ReadValueType<T>(string name = null)
    where T : ISave {
        var ret = default(T);

        ret.Load(this);

        return ret;
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

    public PackedEntity ReadPackedEntity(EntityManager em, string name = null) {
        var ent = new PackedEntity();
        var id  = Read<uint>();
        ent.Type = ReadEnum<EntityType>(nameof(ent.Type));
        ent.Alive = Read<bool>();
        if(ent.Alive) {
            ent.Entity = ReadEntity(null, em);
        } else {
            em.PushEmptyEntity(id);
        }

        return ent;
    }

    private Entity ReadEntity(string name, EntityManager em) {
        Entity entity = null;
        var handle = ReadValueType<EntityHandle>();
        var type   = ReadEnum<EntityType>(nameof(entity.Type));
        var flags  = ReadEnum<EntityFlags>(nameof(entity.Flags));
        var link   = Read<string>(nameof(entity.Name));
        var position = Read<Vector3>();
        var orientation = Read<Quaternion>();
        var scale = Read<Vector3>();
        entity = em.RecreateEntity(link, position, orientation, scale, type, flags);
        entity.Load(this);
        Assert(handle.Id == entity.Handle.Id, $"Entity Id's are not identical while reading entity. Recreated Id: {entity.Handle.Id}, Saved Id: {handle.Id}");

        return entity;
    }

    public T[] ReadObjectArray<T>(Func<T> createObjectFunc, string name = null)
    where T : ISave {
        var count = Read<int>();
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            var obj = createObjectFunc();
            ReadObject(obj);
            arr[i] = obj;
        }

        return arr;
    }

    public T[] ReadUnmanagedObjectArray<T>(string name = null)
    where T : unmanaged, ISave {
        var count = Read<int>();
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>(null);
        }

        return arr;
    }

    public T[] ReadValueObjectArray<T>(string name = null)
    where T : struct, ISave {
        var count = Read<int>();
        var arr = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>(null);
        }

        return arr;
    }

    public NativeArray<T> ReadNativeObjectArray<T>(Allocator allocator, string name = null)
    where T : unmanaged, ISave {
        var count = Read<int>();
        var arr = new NativeArray<T>(count, allocator);

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>(null);
        }

        return arr;
    }

    public NativeArray<T> ReadNativeArray<T>(Allocator allocator, string name = null)
    where T : unmanaged {
        var count = Read<int>();
        var arr   = new NativeArray<T>(count, allocator);

        for(var i = 0; i < count; ++i) {
            arr[i] = Read<T>();
        }

        return arr;
    }

    // Parsing
    private UnmanagedArray<byte> Parse<T>(T value) {
        var type = typeof(T).ToString();

        switch(type) {
            case "System.String" : {
                var val = (string)(object)value;
                var ret = SingleFrameArena.Alloc<byte>(sizeof(char) * (uint)val.Length);

                for(var i = 0; i < val.Length; ++i) {
                    short a = (short)val[i];
                    ret[sizeof(short) * i] = (byte)(a & 0xff);
                    ret[sizeof(short) * i + 1] = (byte)((a >> 8) & 0xff);
                }

                return new UnmanagedArray<byte>(ret, sizeof(char) * (uint)val.Length);
            }
            case "UnityEngine.Vector3" : {
                var val = (Vector3)(object)value;
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Vector3));
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
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Vector3Int));

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
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Vector2));
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
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Vector2Int));

                for(var i = 0; i < 2; ++i) {
                    ret[0 + sizeof(int) * i] = (byte)(val[i] & 0xff);
                    ret[1 + sizeof(int) * i] = (byte)((val[i] >> 8) & 0xff);
                }

                return new UnmanagedArray<byte>(ret, (uint)sizeof(Vector2Int));
            }
            case "UnityEngine.Vector4" : {
                var val = (Vector4)(object)value;
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Vector4));
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
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Vector3));
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
                var ret = SingleFrameArena.Alloc<byte>((uint)sizeof(Matrix4x4));
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
                var ret = SingleFrameArena.Alloc<byte>(sizeof(float));
                var ptr = (byte*)(&val);

                for(var i = 0; i < sizeof(float); ++i) {
                    ret[i] = ptr[i];
                }

                return new UnmanagedArray<byte>(ret, sizeof(float));
            }
            case "System.Double" : {
                var val = (double)(object)value;
                var ret = SingleFrameArena.Alloc<byte>(sizeof(double));
                var ptr = (byte*)(&val);

                for(var i = 0; i < sizeof(double); ++i) {
                    ret[i] = ptr[i];
                }

                return new UnmanagedArray<byte>(ret, sizeof(double));
            }
            case "System.Int16" : {
                var val   = (short)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(short));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(short));
            }
            case "System.UInt16" : {
                var val   = (ushort)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(ushort));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(ushort));
            }
            case "System.Int32" : {
                var val   = (int)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(int));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);
                ret[2] = (byte)((val >> 16) & 0xff);
                ret[3] = (byte)((val >> 24) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(int));
            }
            case "System.UInt32" : {
                var val   = (uint)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(uint));

                ret[0] = (byte)(val & 0xff);
                ret[1] = (byte)((val >> 8) & 0xff);
                ret[2] = (byte)((val >> 16) & 0xff);
                ret[3] = (byte)((val >> 24) & 0xff);

                return new UnmanagedArray<byte>(ret, sizeof(uint));
            }
            case "System.Int64" : {
                var val   = (long)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(long));

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
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(ulong));

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
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(byte));
                ret[0]    = val;

                return new UnmanagedArray<byte>(ret, sizeof(byte));
            }
            case "System.SByte" : {
                var val   = (sbyte)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(sbyte));
                ret[0]    = (byte)val;

                return new UnmanagedArray<byte>(ret, sizeof(sbyte));
            }
            case "System.Boolean" : {
                var val   = (bool)(object)value;
                var ret   = SingleFrameArena.Alloc<byte>(sizeof(bool));
                var ptr   = (byte*)(&val);

                for(var i = 0; i < sizeof(bool); ++i) {
                    ret[i] = ptr[i];
                }

                return new UnmanagedArray<byte>(ret, sizeof(bool));
            }
        }
        return default;
    }

    private T Parse<T>(int stringLength = 0, T defaultValue = default(T)) {
        var type = typeof(T).ToString();

        switch (type) {
            case "System.String" : {
                var str = SingleFrameArena.Alloc<char>((uint)stringLength);

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
}