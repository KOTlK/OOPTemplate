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
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using static Assertions;
using System.Text;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class VersionAttribute : Attribute {
    public uint CurrentVersion;

    public VersionAttribute(uint currentVersion) {
        CurrentVersion = currentVersion;
    }
}

public static class TypeVersion {
    public struct NameVersion {
        public string Name;
        public uint   Version;
    }

    private static Dictionary<string, uint> _versions = new();
    
    private static char[]            Buffer = new char[256];
    private static int               CurrentLen = 0;
    private static List<NameVersion> Versions = new();
    private static StringBuilder     StringBuilder = new();

    private const string             VersionName = "TypeVersions.version";

    private static void PushToBuffer(char c) {
        Buffer[CurrentLen++] = c;
    }

    private static string Flush() {
        var str = new string(Buffer, 0, CurrentLen);
        CurrentLen = 0;
        return str;
    }

    public static void Init(string originalPath) {
        var path = originalPath + $"/{VersionName}";
        _versions.Clear();
        StringBuilder.Clear();
        Versions.Clear();
        CurrentLen = 0;

        if(File.Exists(path) == false) {
            UpdateToCurrent(path);
        } else {
            var text = File.ReadAllText(path);
            var len  = text.Length;
            var name = "";

            for(var i = 0; i < len; ++i) {
                if(text[i] == ':') {
                    name = Flush();
                } else if(text[i] == ';') {
                    if(uint.TryParse(Flush(), out var version)) {
                        _versions.Add(name, version);
                    } else {
                        UnityEngine.Debug.Log("Cannot parse version");
                    }
                } else if(text[i] != '\n' && text[i] != '\r'){
                    PushToBuffer(text[i]);
                }
            }
        }
    }

    public static void UpdateToCurrent(string path) {
        _versions.Clear();

        var types = typeof(VersionAttribute).Assembly.GetTypes();

        foreach(var type in types) {
            var attr = type.GetCustomAttribute(typeof(VersionAttribute));

            if(attr != null) {
                _versions.Add(type.FullName, ((VersionAttribute)attr).CurrentVersion);

                Versions.Add(new NameVersion {
                    Name    = type.FullName,
                    Version = ((VersionAttribute)attr).CurrentVersion
                });
            }
        }

        for(var i = 0; i < Versions.Count; ++i) {
            StringBuilder.AppendLine($"{Versions[i].Name}:{Versions[i].Version};");
        }

        if(File.Exists(path)) {
            File.Delete(path);
        }

        File.WriteAllText(path, StringBuilder.ToString());
    }

    public static uint GetVersion<T>() {
        Assert(typeof(T).GetCustomAttribute(typeof(VersionAttribute)) != null, $"Type \"{typeof(T).ToString()}\" does not have \"Version\" attribute.");
        return _versions[typeof(T).Name];
    }
}