using System.Collections.Generic;
using UnityEngine;

// using static TextSaveFile.TokenType;

namespace TestNamespace {

[Version(12)]
[System.Serializable]
public struct TestStruct {
    public Vector3 VectorData;
    public Vector2 Vector2Data;
}

[Version(2)]
[System.Serializable]
public class TestClass {
    public int        IntData;
    public float      FloatData;
    public bool       Recursive;
    public TestStruct Recursion;
}

[Version(7)]
public class SaveTest : MonoBehaviour {
    public EntityManager    Em;
    public int              IntData;
    public Quaternion       Quaternion;
    public TestStruct       Struct;
    public TestClass        Class;
    public int[]            IntArray    = new int[32];
    public TestClass[]      ClassArray  = new TestClass[10];
    public TestStruct[]     StructArray = new TestStruct[10];
    public List<TestStruct> StructList  = new();
    public List<TestClass>  ClassList   = new();
    public TestEntity2      Entity;

    private void Awake() {
        SaveSystem.Init();
        ResourceManager.LoadBundle("test");
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F5)) {
            SaveSystem.Save(Saving, "Hello Save");
        }

        if (Input.GetKeyDown(KeyCode.F9)) {
            SaveSystem.Load(Loading, "Hello Save");
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            Em.CreateEntity("test_entity", Vector3.zero, Quaternion.identity);
        }
    }

    private void Saving(BinarySaveFile sf) {
        sf.Write(this);
    }

    private void Loading(BinarySaveFile sf) {
        sf.Read(this);
    }
}

} // namespace