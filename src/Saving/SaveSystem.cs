using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

using static Assertions;

public enum SaveType {
    Text,
    Binary
}

public struct SaveFile {
    public string   Path;
    public string   Name;
    public DateTime Date;

    public override string ToString() {
        return $"{Name} --- {Path} --- {Date}";
    }
}

public static class SaveSystem {
    public delegate void SaveFunc(ISaveFile sf);
    public delegate void LoadFunc(ISaveFile sf);

    public static uint  Version = 2;
    public static event Action<ISaveFile> LoadingOver = delegate { };

    public static SaveType  Type = SaveType.Binary;
    public static ISaveFile Sf;

    public static int    MaxBackups     = 5;
    public static string SavesDirectory = $"{Application.persistentDataPath}/Saves";
    public static string Extension      = "save";

    private static List<SaveFile> SavesList = new();
    private static StringBuilder  Sb = new();

    public static void Init(SaveType type = SaveType.Binary) {
        ChangeSaveFileType(type);
        TypeVersion.Init();

        if(Directory.Exists(SavesDirectory) == false) {
            Directory.CreateDirectory(SavesDirectory);
        }
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

    public static void Save(SaveFunc func, string name) {
        var sf = BeginSave();
        func(sf);
        EndSave(name);
    }

    public static ISaveFile BeginSave() {
        TypeVersion.UpdateToCurrent();
        Sf.NewFile();
        Sf.Write(Version, nameof(Version));
        return Sf;
    }

    public static void EndSave(string name) {
        var path = $"{SavesDirectory}/{name}.{Extension}";

        if(Directory.Exists(SavesDirectory) == false) {
            Directory.CreateDirectory(SavesDirectory);
        }

        // backup saves
        if(File.Exists(path)) {
            var fileName   = path;
            var oldestDate = File.GetLastWriteTime(path);
            var curDate    = File.GetLastWriteTime(path);
            var oldestPath = path;

            var i = 0;

            for( ; i < MaxBackups; ++i) {
                fileName = $"{SavesDirectory}/{name}-{i}.{Extension}";

                if(File.Exists(fileName) == false) break;

                var date = File.GetLastWriteTime(fileName);

                if(date < oldestDate) {
                    oldestDate = date;
                    oldestPath = fileName;
                }
            }

            if(i == MaxBackups) {
                File.Delete(oldestPath);
                fileName = oldestPath;
            }

            File.Move(path, fileName);
        }

        File.Create(path).Close();

        Sf.SaveToFile(path);
    }

    public static void Load(LoadFunc func, string fileName) {
        var sf = BeginLoading(fileName);
        func(sf);
        EndLoading();
    }

    public static ISaveFile BeginLoading(string fileName) {
        var path = $"{SavesDirectory}/{fileName}.{Extension}";
        Assert(SaveExist(fileName), $"File {path} does not exist");
        Sf.LoadFromFile(path);
        Version = Sf.Read<uint>(nameof(Version));
        return Sf;
    }

    public static void EndLoading() {
        LoadingOver(Sf);
    }

    public static bool SaveExist(string fileName) {
        if(Directory.Exists(SavesDirectory)) {
            return File.Exists($"{SavesDirectory}/{fileName}.{Extension}");
        }

        return false;
    }

    public static SaveFile[] GetAllSaves() {
        SavesList.Clear();
        var ext = $".{Extension}";

        foreach (var path in Directory.EnumerateFiles(SavesDirectory)) {
            Sb.Clear();
            if (path.EndsWith(ext)) {
                var start = 0;
                var end   = 0;

                for (var i = path.Length - 1; i >= 0; --i) {
                    if (path[i] == '\\' || path[i] == '/') {
                        start = i+1;
                        break;
                    } else if (path[i] == '.') {
                        end = i;
                    }
                }

                var name = path.Substring(start, end - start);
                var save = new SaveFile();

                save.Path = path;
                save.Name = name;
                save.Date = File.GetLastWriteTime(path);

                SavesList.Add(save);
            }
        }

        return SavesList.ToArray();
    }
}