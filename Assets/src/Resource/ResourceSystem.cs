using System.Collections.Generic;
using UnityEngine;
using static Assertions;

public interface IResourceSystem {
    T Load<T>(ResourceLink link) where T : Component;
    void Preload(params ResourceLink[] links);
    void Unload(ResourceLink link);
}

[System.Serializable]
public struct ResourceLink : ISave {
    public string     Path; // Actual path to the resource
    public GameObject Reference; // Used as reference to resource in Resources folder, do not use it.

    public void Save(ISaveFile sf) {
        sf.Write(nameof(Path), Path);
    }

    public void Load(ISaveFile sf) {
        Path = sf.Read<string>(nameof(Path));
    }
}

public class ResourceSystem : IResourceSystem {
    public Dictionary<string, GameObject> Links = new();

    public T Load<T>(ResourceLink link) 
    where T : Component {
        if(Links.ContainsKey(link.Path)) {
            return Links[link.Path].GetComponent<T>();
        }

        var asset = Resources.Load<T>(link.Path);
        Links.Add(link.Path, asset.gameObject);

        return asset;
    }

    public void Preload(params ResourceLink[] links) {
        for(var i = 0; i < links.Length; ++i) {
            Assert(Links.ContainsKey(links[i].Path) == false, $"Trying to preload already loaded asset, duplicate path is: {links[i].Path}");

            var asset = Resources.Load<GameObject>(links[i].Path);
            Links.Add(links[i].Path, asset);
        }
    }

    public void Unload(ResourceLink link) {
        Assert(Links.ContainsKey(link.Path), $"Resource at path {link.Path} is not loaded, can't unload it");

        Links.Remove(link.Path);
    }
}