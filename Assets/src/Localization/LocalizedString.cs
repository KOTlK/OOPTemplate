[System.Serializable]
public struct NameIdent {
    public string Name;
    public int    Ident;
}

[System.Serializable]
public struct LocalizedString {
    public int         Ident;
    public NameIdent[] Idents;

    public string Get() {
        return Locale.Get(Ident);
    }

    public string Get(int i) {
        return Locale.Get(Idents[i]);
    }
}