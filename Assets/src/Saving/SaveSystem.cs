using System;
using System.IO;
using UnityEngine;

using static Assertions;

public static class SaveSystem {
    public enum SaveType {
        Text,
        Binary
    }

    public static uint Version = 2;
    public static event Action<ISaveFile> LoadingOver = delegate { };

    public static SaveType  Type = SaveType.Binary;
    public static ISaveFile Sf;

    public static string Extension = "save";

    public static void Init(SaveType type = SaveType.Binary) {
        ChangeSaveFileType(type);
        TypeVersion.Init();
    }

    public static void Dispose() {
        if(Sf != null) {
            Sf.Dispose();
        }
    }

    public static void ChangeSaveFileType(SaveType type) {
        if(Sf != null) {
            Sf.Dispose();
        }

        switch (type) {
            case SaveType.Text : {
                Sf = new TextSaveFile();
            }
            break;
            case SaveType.Binary : {
                Sf = new BinarySaveFile();
            }
            break;
        }
    }

    public static ISaveFile BeginSave() {
        TypeVersion.UpdateToCurrent();
        Sf.NewFile();
        Sf.Write(Version, nameof(Version));
        return Sf;
    }

    public static void EndSave(string directory, string name) {
        var path = $"{directory}/{name}.{Extension}";

        if(Directory.Exists(directory) == false) {
            Directory.CreateDirectory(directory);
        }

        if(File.Exists(path)) {
            File.Delete(path);
        }

        File.Create(path).Close();

        Sf.SaveToFile(path);
    }

    public static ISaveFile BeginLoading(string directory, string fileName) {
        var path = $"{directory}/{fileName}.{Extension}";
        Assert(SaveExist(directory, fileName), $"File {path} does not exist");
        Sf.LoadFromFile(path);
        Version = Sf.Read<uint>(nameof(Version));
        return Sf;
    }

    public static void EndLoading() {
        LoadingOver(Sf);
    }

    public static bool SaveExist(string directory, string fileName) {
        if(Directory.Exists(directory)) {
            return File.Exists($"{directory}/{fileName}.{Extension}");
        }

        return false;
    }
}