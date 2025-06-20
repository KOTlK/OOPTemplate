using System;
using System.IO;
using UnityEngine;

using static Assertions;

public class SaveSystem : IDisposable {
    public enum SaveType {
        Text,
        Binary
    }

    public uint Version = 2;
    public event Action<ISaveFile> LoadingOver = delegate { };

    public SaveType  Type = SaveType.Binary;
    public ISaveFile Sf;

    public string Extension = "save";

    public SaveSystem() {
        ChangeSaveFileType(SaveType.Binary);
        TypeVersion.Init(Application.persistentDataPath);
    }

    public SaveSystem(SaveType type) {
        ChangeSaveFileType(type);
        TypeVersion.Init(Application.persistentDataPath);
    }

    public void Dispose() {
        if(Sf != null) {
            Sf.Dispose();
        }
    }

    public void ChangeSaveFileType(SaveType type) {
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

    public ISaveFile BeginSave() {
        Sf.NewFile();
        Sf.Write(Version, nameof(Version));
        return Sf;
    }

    public void EndSave(string directory, string name) {
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

    public ISaveFile BeginLoading(string directory, string fileName) {
        var path = $"{directory}/{fileName}.{Extension}";
        Assert(SaveExist(directory, fileName), $"File {path} does not exist");
        Sf.LoadFromFile(path);
        Version = Sf.Read<uint>(nameof(Version));
        return Sf;
    }

    public void EndLoading() {
        LoadingOver(Sf);
    }

    public bool SaveExist(string directory, string fileName) {
        if(Directory.Exists(directory)) {
            return File.Exists($"{directory}/{fileName}.{Extension}");
        }

        return false;
    }
}