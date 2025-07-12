/*
    Usage:
      - Mark class/struct with "Version" attribute and pass current version into constructor:
         [Version(10)]
         public class Type {}
      - For simple access, include file, following:
         using static TypeVersion;
      - Call funcion:
         var version = GetVersion<Type>();
         or
         var version = this.GetVersion();
         where this - instance of any type

    Limitations:
        Version should be above 0.
        Do not downgrade versions.
*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using static Assertions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class VersionAttribute : Attribute {
    public uint CurrentVersion;

    public VersionAttribute(uint currentVersion) {
        CurrentVersion = currentVersion;
    }
}

public static class TypeVersion {
    private static Dictionary<string, uint> Versions;
    private static StringBuilder            Sb;

    private static string Path = $"{Application.streamingAssetsPath}/TypeVersions.version";

    public static void Init() {
        Versions = new();
        Versions.Clear();

        Sb = new();
        Sb.Clear();

        if(File.Exists(Path) == false) {
            UpdateToCurrent();
        } else {
            var text = File.ReadAllText(Path);
            var len  = text.Length;
            var name = "";

            var i = 0;

            for( ; i < len; ++i) {
                switch(text[i]) {
                    case '\r' : break;
                    case ' '  : break;
                    case '\t' : break;
                    case ':' :
                        name = Sb.ToString();
                        Sb.Clear();
                        break;
                    case '\n' :
                        var ver = Sb.ToString();
                        Sb.Clear();
                        if(uint.TryParse(ver, out var version)) {
                            Versions.Add(name, version);
                        } else {
                            UnityEngine.Debug.LogError("Cannot parse version");
                        }
                        break;
                    default :
                        Sb.Append(text[i]);
                        break;
                }
            }
        }
    }

    public static void UpdateToCurrent() {
        Versions = new();
        Versions.Clear();

        Sb = new();
        Sb.Clear();

        var types = typeof(VersionAttribute).Assembly.GetTypes();

        foreach(var type in types) {
            var attr = (VersionAttribute)type.GetCustomAttribute(typeof(VersionAttribute));

            if(attr != null) {
                var v = attr.CurrentVersion;
                Versions.Add(type.FullName, v);

                Sb.AppendLine($"{type.FullName}\t:\t{v}");
            }
        }

        if(File.Exists(Path)) {
            File.Delete(Path);
        }

        File.WriteAllText(Path, Sb.ToString());
        Sb.Clear();
    }

    public static uint GetVersion<T>(this T inst) {
        Assert(typeof(T).GetCustomAttribute(typeof(VersionAttribute)) != null, $"Type \"{typeof(T).ToString()}\" does not have \"Version\" attribute.");
        var name = typeof(T).FullName;

        if(Versions.ContainsKey(name) == false) {
            return 0;
        }

        return Versions[name];
    }

    public static uint GetVersion<T>() {
        Assert(typeof(T).GetCustomAttribute(typeof(VersionAttribute)) != null, $"Type \"{typeof(T).ToString()}\" does not have \"Version\" attribute.");
        var name = typeof(T).FullName;

        if(Versions.ContainsKey(name) == false) {
            return 0;
        }

        return Versions[name];
    }
}