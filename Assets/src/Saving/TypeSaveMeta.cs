using System.Reflection;

[UnityEngine.Scripting.Preserve]
public struct TypeSaveMeta {
    public FieldInfo[] Fields;
    public MethodInfo  Save;
    public MethodInfo  Load;
}