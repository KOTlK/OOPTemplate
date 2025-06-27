[System.Serializable]
public struct NameIdent {
    public string Name;
    public int    Ident;
}

[System.Serializable]
public struct LocalizedString {
    public int         Ident;
    public NameIdent[] Idents;
}