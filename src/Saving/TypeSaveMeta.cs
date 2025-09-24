using System;
using System.Reflection;

public struct TypeSaveMeta {
    public FieldInfo[] Fields;
    public MethodInfo  Save;
    public MethodInfo  Load;
}