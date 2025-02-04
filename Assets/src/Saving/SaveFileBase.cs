using UnityEngine;
using System;

using static Assertions;
using Unity.Collections;
using System.IO;

public abstract class SaveFileBase : ISaveFile, IDisposable {
    public uint Version = 1;
    public const string Extension = ".sav";

    public virtual void Dispose() {
        
    }

    public virtual void NewFile(uint version) {
        Version = version;
        Write(version, nameof(Version));
    }

    public void SaveToFile(string path, string name) {
        path += $"/{name}{Extension}";
        if(File.Exists(path)) {
            File.Delete(path);
        }

        SaveFile(path);
    }

    public void NewFromExistingFile(string path) {
        Assert(path.EndsWith(Extension), $"File should end with {Extension}");

        if(File.Exists(path)) {
            LoadFile(path);
            Version = Read<uint>(nameof(Version));
            Debug.Log(Version);
        } else {
            Debug.LogError($"File at: {path} does not exist");
        }
    }

    public abstract void Write<T>(T value, string name = null);
    public abstract void WriteObject(ISave save, string name = null);
    public abstract void WriteArray<T>(int itemsCount, T[] arr, string name = null);
    public abstract void WriteObjectArray<T>(int itemsCount, T[] arr, string name = null) where T : ISave;
    public abstract void WriteNativeArray<T>(int itemsCount, NativeArray<T> arr, string name = null) where T : unmanaged;
    public abstract void WritePackedEntity(PackedEntity e, uint id, string name = null);
    public abstract void WriteEnum(Enum e, string name = null);
    public abstract T    Read<T>(string name = null, T defaultValue = default);
    public abstract T[]  ReadArray<T>(string name = null);
    public abstract void ReadObject(ISave obj, string name = null);
    public abstract T    ReadValueType<T>(string name = null) where T : ISave;
    public abstract T    ReadEnum<T>(string name = null) where T : Enum;
    public abstract PackedEntity   ReadPackedEntity(EntityManager em, string name = null);
    public abstract T[]            ReadObjectArray<T>(Func<T> createObjectFunc, string name = null) where T : ISave;
    public abstract T[]            ReadUnmanagedObjectArray<T>(string name = null) where T : unmanaged, ISave;
    public abstract T[]            ReadValueObjectArray<T>(string name = null) where T : struct, ISave;
    public abstract NativeArray<T> ReadNativeObjectArray<T>(Allocator allocator, string name = null) where T : unmanaged, ISave;
    public abstract NativeArray<T> ReadNativeArray<T>(Allocator allocator, string name = null) where T : unmanaged;

    protected abstract void LoadFile(string path);
    protected abstract void SaveFile(string path);
}