using Unity.Collections;
using System;

public interface ISaveFile : IDisposable {
    void NewFile();
    void SaveToFile(string path);
    void LoadFromFile(string path);
    void Write<T>(T value, string name = null);
    void WriteObject(ISave save, string name = null);
    void WriteArray<T>(T[] arr, int itemsCount, string name = null);
    void WriteObjectArray<T>(T[] arr, int itemsCount, string name = null)
        where T : ISave;
    void WriteNativeArray<T>(NativeArray<T> arr, int itemsCount, string name = null)
        where T : unmanaged;
    void WritePackedEntity(PackedEntity e, uint id, string name = null);
    void WriteEnum(Enum e, string name = null);
    T Read<T>(string name = null, T defaultValue = default(T));
    T[] ReadArray<T>(string name = null);
    void ReadObject(ISave obj, string name = null);

    T ReadValueType<T>(string name = null)
        where T : ISave;
    T ReadEnum<T>(string name = null)
        where T : Enum;
    PackedEntity ReadPackedEntity(EntityManager em, string name = null);
    T[] ReadObjectArray<T>(Func<T> createObjectFunc, string name = null)
        where T : ISave;
    T[] ReadUnmanagedObjectArray<T>(string name = null)
        where T : unmanaged, ISave;
    T[] ReadValueObjectArray<T>(string name = null)
        where T : struct, ISave;
    NativeArray<T> ReadNativeObjectArray<T>(Allocator allocator, string name = null)
        where T : unmanaged, ISave;
    NativeArray<T> ReadNativeArray<T>(Allocator allocator, string name = null)
        where T : unmanaged;
}