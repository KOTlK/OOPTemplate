using System;

[AttributeUsage(AttributeTargets.Field)]
public class DontSaveAttribute : Attribute {
}

[AttributeUsage(AttributeTargets.Field)]
public class VAttribute : Attribute {
    public uint Version;

    public VAttribute(uint version) {
        Version = version;
    }
}