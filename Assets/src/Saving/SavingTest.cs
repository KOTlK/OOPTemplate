using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public struct SavingObject : ISave {
    public int Value;
    public float Value2;
    public NestedObject NestedObject;

    public void Save(SaveFile file) {
        file.Write(nameof(Value), Value);
        file.Write(nameof(Value2), Value2);
        file.WriteObject(nameof(NestedObject), NestedObject);
    }

    public void Load(SaveFile file) {
        Value = file.Read<int>(nameof(Value));
        // Debug.Log(Value);
        Value2 = file.Read<float>(nameof(Value2));
        // Debug.Log(Value2);
        NestedObject = file.ReadValueType<NestedObject>(nameof(NestedObject));
    }
}

[System.Serializable]
public struct NestedObject : ISave {
    public float Value1;
    public int Value2;
    public NestedDefaultObject Value3;

    public void Save(SaveFile sf) {
        sf.Write(nameof(Value1), Value1);
        sf.Write(nameof(Value2), Value2);
        sf.WriteObject(nameof(Value3), Value3);
    }

    public void Load(SaveFile sf) {
        Value1 = sf.Read<float>(nameof(Value1));
        // Debug.Log(Value1);
        Value2 = sf.Read<int>(nameof(Value2));
        // Debug.Log(Value2);
        Value3 = sf.ReadValueType<NestedDefaultObject>(nameof(Value3));
    }
}

[System.Serializable]
public struct NestedDefaultObject : ISave {
    public float Val1;
    public int Val2;

    public static readonly NestedDefaultObject Default = new NestedDefaultObject {
        Val1 = 20f,
        Val2 = 340
    };

    public void Save(SaveFile sf) {
        sf.Write(nameof(Val1), Val1);
        sf.Write(nameof(Val2), Val2);
    }

    public void Load(SaveFile sf) {
        Val1 = sf.Read(nameof(Val1), Default.Val1);
        // Debug.Log(Val1);
        Val2 = sf.Read(nameof(Val2), Default.Val2);
        // Debug.Log(Val2);
    }
}

public class SavingTest : MonoBehaviour {
    public EntityManager Em;
    public SaveFile Save;
    public SavingObject[] Objects = new SavingObject[10000];
    public NativeArray<int> NativeInts = new NativeArray<int>(10, Allocator.Persistent);
    public NativeArray<SavingObject> NativeObjects = new NativeArray<SavingObject>(10, Allocator.Persistent);
    public float[] Floats = new float[10000];
    public int[] Ints = new int[10240];
    public Vector3 Vector3;
    public Vector3Int Vector3Int;
    public Vector2 Vector2;
    public Vector2Int Vector2Int;
    public Vector4 Vector4;
    public Quaternion Quaternion;
    public Matrix4x4  Matrix;
    public double Double;
    public int    Int;
    public uint   UInt;
    public long   Long;
    public ulong  ULong;
    public short  Short;
    public ushort UShort;
    public byte   Byte;
    public sbyte  SByte;
    public bool   Bool;

    private void Start() {
        Save = new SaveFile();
    }

    private void Update() {
        var sw = new Stopwatch();

        if (Input.GetKeyDown(KeyCode.F5)) {
            sw.Start();
            Save.NewFile(0.01f);
            Save.WriteObjectArray(nameof(Objects), Objects.Length, Objects);
            Save.WriteArray(nameof(Floats), Floats.Length, Floats);
            Save.WriteArray(nameof(Ints), Ints.Length, Ints);
            Save.WriteObject(nameof(Em), Em);
            Save.Write(nameof(Vector3), Vector3);
            Save.Write(nameof(Vector3Int), Vector3Int);
            Save.Write(nameof(Vector2), Vector2);
            Save.Write(nameof(Vector2Int), Vector2Int);
            Save.Write(nameof(Vector4), Vector4);
            Save.Write(nameof(Quaternion), Quaternion);
            Save.Write(nameof(Matrix), Matrix);
            Save.Write(nameof(Double), Double);
            Save.Write(nameof(Int), Int);
            Save.Write(nameof(UInt), UInt);
            Save.Write(nameof(Long), Long);
            Save.Write(nameof(ULong), ULong);
            Save.Write(nameof(Short), Short);
            Save.Write(nameof(UShort), UShort);
            Save.Write(nameof(Byte), Byte);
            Save.Write(nameof(SByte), SByte);
            Save.Write(nameof(Bool), Bool);
            sw.Stop();

            Debug.Log($"Write Time: {sw.ElapsedMilliseconds}");

            sw.Restart();
            Save.SaveToFile(Application.persistentDataPath, "TestSave");
            sw.Stop();

            Debug.Log($"File Write Time: {sw.ElapsedMilliseconds}");
        }

        if(Input.GetKeyDown(KeyCode.F9)) {
            sw.Restart();
            Save.NewFromExistingFile($"{Application.persistentDataPath}/TestSave.sav");
            sw.Stop();

            Debug.Log($"File Parse Time: {sw.ElapsedMilliseconds}");

            sw.Restart();
            Objects    = Save.ReadValueObjectArray<SavingObject>(nameof(Objects));
            NativeObjects = Save.ReadNativeObjectArray<SavingObject>(nameof(Objects), Allocator.Persistent);
            NativeInts = Save.ReadNativeArray<int>(nameof(Ints), Allocator.Persistent);
            Floats     = Save.ReadArray<float>(nameof(Floats));
            Ints       = Save.ReadArray<int>(nameof(Ints));
            Vector3    = Save.Read<Vector3>(nameof(Vector3));
            Vector3Int = Save.Read<Vector3Int>(nameof(Vector3Int));
            Vector2    = Save.Read<Vector2>(nameof(Vector2));
            Vector2Int = Save.Read<Vector2Int>(nameof(Vector2Int));
            Vector4    = Save.Read<Vector4>(nameof(Vector4));
            Quaternion = Save.Read<Quaternion>(nameof(Quaternion));
            Matrix     = Save.Read<Matrix4x4>(nameof(Matrix));

            Double = Save.Read<double>(nameof(Double));
            Int    = Save.Read<int>(nameof(Int));
            UInt   = Save.Read<uint>(nameof(UInt));
            Long   = Save.Read<long>(nameof(Long));
            ULong  = Save.Read<ulong>(nameof(ULong));
            Short  = Save.Read<short>(nameof(Short));
            UShort = Save.Read<ushort>(nameof(UShort));
            Byte   = Save.Read<byte>(nameof(Byte));
            SByte  = Save.Read<sbyte>(nameof(SByte));
            Bool   = Save.Read<bool>(nameof(Bool));
            
            Save.ReadObject(nameof(Em), Em);
            sw.Stop();

            Debug.Log($"Reconstruction Time: {sw.ElapsedMilliseconds}");
        }
    }
}