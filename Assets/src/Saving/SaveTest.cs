using UnityEngine;

using static TextSaveFile.TokenType;

namespace TestNamespace {

[Version(12)]
[System.Serializable]
public struct TestStruct : ISave {
    public Vector3 VectorData;
    public Vector2 Vector2Data;

    public void Save(ISaveFile sf) {
        sf.Write(VectorData, nameof(VectorData));
        sf.Write(Vector2Data, nameof(Vector2Data));
    }

    public void Load(ISaveFile sf) {
        VectorData = sf.Read<Vector3>(nameof(VectorData));
        Vector2Data = sf.Read<Vector2>(nameof(Vector2Data));
    }
}

[Version(2)]
[System.Serializable]
public class TestClass : ISave {
    public int        IntData;
    public float      FloatData;
    public bool       Recursive;
    public TestStruct Recursion;

    public void Save(ISaveFile sf) {
        sf.Write(IntData, nameof(IntData));
        sf.Write(FloatData, nameof(FloatData));

        if(this.GetVersion() > 1) {
            sf.Write(Recursive, nameof(Recursive));

            if(Recursive) {
                sf.WriteObject(Recursion, nameof(Recursion));
            }
        }
    }

    public void Load(ISaveFile sf) {
        IntData   = sf.Read<int>(nameof(IntData));
        FloatData = sf.Read<float>(nameof(FloatData));

        if(this.GetVersion() > 1) {
            Recursive = sf.Read<bool>(nameof(Recursive));

            if(Recursive) {
                Recursion = sf.ReadValueType<TestStruct>(nameof(Recursion));
            }
        }
    }
}


[Version(7)]
public class SaveTest : MonoBehaviour, ISave {
    public int        IntData;
    public Quaternion Quaternion;
    public TestStruct Struct;
    public TestClass  Class;

    public void Save(ISaveFile sf) {
        sf.Write(IntData, nameof(IntData));
        sf.Write(Quaternion, nameof(Quaternion));
        sf.WriteObject(Struct, nameof(Struct));
        sf.WriteObject(Class, nameof(Class));
    }

    public void Load(ISaveFile sf) {
        IntData    = sf.Read<int>(nameof(IntData));
        Quaternion = sf.Read<Quaternion>(nameof(Quaternion));
        Struct     = sf.ReadValueType<TestStruct>(nameof(Struct));
        sf.ReadObject(Class, nameof(Class));
    }

    private void Awake() {
        // foreach(var save in SaveSystem.GetAllSaves()) {
        //     Debug.Log(save);
        // }

        // SaveSystem.Init(SaveType.Text);
        // SaveSystem.Save(Saving, "Hello Save");

        // SaveSystem.Load(Loading, "Hello Save-0");

        var path = $"{Application.persistentDataPath}/Saves/TestSave.save";
        var sf   = new TextSaveFile();

        // sf.NewFile();
        // sf.Write(1, "Version");
        // sf.WriteObject(this, "This");
        // sf.SaveToFile(path);

        sf.LoadFromFile(path);
        var v = sf.Read<int>("Version");
        sf.ReadObject(this, "This");

        // foreach (var token in sf.Tokens) {
        //     if (token.Type == Value) {
        //         Debug.Log($"{token.Type}, {token.Value}");
        //     } else if (token.Type == Ident) {
        //         Debug.Log($"{token.Type}, {token.Ident}");
        //     } else {
        //         Debug.Log(token.Type);
        //     }
        // }
    }

    private void Saving(ISaveFile sf) {
        sf.WriteObject(this, "This");
    }

    private void Loading(ISaveFile sf) {
        sf.ReadObject(this, "This");
    }
}

} // namespace