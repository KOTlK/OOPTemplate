using System;

public class SaveSystem : IDisposable {
    public enum SaveType {
        HumanReadable,
        Obfuscated
    }

    public float Version = 0.2f;
    public event Action<ISaveFile> LoadingOver = delegate { };

    public SaveType  Type = SaveType.Obfuscated;
    public ISaveFile Sf;

    public void Dispose() {
        if(Sf is ObfuscatedSaveFile) {
            ((ObfuscatedSaveFile)Sf).Dispose();
        }
    }

    public ISaveFile BeginSave() {
        if(Sf is ObfuscatedSaveFile) {
            ((ObfuscatedSaveFile)Sf).Dispose();
        }
        switch (Type) {
            case SaveType.HumanReadable : {
                Sf = new SaveFile();
            }
            break;
            case SaveType.Obfuscated : {
                Sf = new ObfuscatedSaveFile();
            }
            break;
        }
        Sf.NewFile(Version);
        return Sf;
    }

    public void EndSave(string path, string name) {
        Sf.SaveToFile(path, name);
    }

    public ISaveFile BeginLoading(string path) {
        if(Sf is ObfuscatedSaveFile) {
            ((ObfuscatedSaveFile)Sf).Dispose();
        }
        switch (Type) {
            case SaveType.HumanReadable : {
                Sf = new SaveFile();
            }
            break;
            case SaveType.Obfuscated : {
                Sf = new ObfuscatedSaveFile();
            }
            break;
        }

        Sf.NewFromExistingFile(path);

        return Sf;
    }

    public void EndLoading() {
        LoadingOver(Sf);
    }
}