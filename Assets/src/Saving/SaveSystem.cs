using System;

public class SaveSystem {
    public float Version = 0.2f;
    public event Action<SaveFile> LoadingOver = delegate { };
    public SaveFile     Sf;

    public SaveFile BeginSave() {
        Sf = new SaveFile();
        Sf.NewFile(Version);
        return Sf;
    }

    public void EndSave(string path, string name) {
        Sf.SaveToFile(path, name);
    }

    public SaveFile BeginLoading(string path) {
        Sf = new SaveFile();

        Sf.NewFromExistingFile(path);

        return Sf;
    }

    public void EndLoading() {
        LoadingOver(Sf);
    }
}