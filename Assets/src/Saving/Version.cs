/*  Usage:
  - Mark class/struct with "Version" attribute and pass current version into constructor:
     [Version(10)]
     public class Type {}
  - Include file, following: 
     using static TypeVersion;
  - Call funcion:
     var version = GetVersion<Type>();
*/

using System;
using System.Collections.Generic;
using System.Reflection;

using static Assertions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class VersionAttribute : Attribute {
    public uint CurrentVersion;

    public VersionAttribute(uint currentVersion) {
        CurrentVersion = currentVersion;
    }
}

public static class TypeVersion {
    private static Dictionary<Type, uint> _versions;

    public static void Init() {
        _versions = new();
        _versions.Clear();

        var types = typeof(VersionAttribute).Assembly.GetTypes();

        foreach(var type in types) {
            var attr = type.GetCustomAttribute(typeof(VersionAttribute));

            if(attr != null) {
                _versions.Add(type, ((VersionAttribute)attr).CurrentVersion);
            }
        }
    }

    public static uint GetVersion<T>() {
        Assert(typeof(T).GetCustomAttribute(typeof(VersionAttribute)) != null, $"Type \"{typeof(T).ToString()}\" does not have \"Version\" attribute.");
        return _versions[typeof(T)];
    }
}