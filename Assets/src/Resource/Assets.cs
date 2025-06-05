using System.Text;
using System.Collections.Generic;
using UnityEngine;

using static Assertions;

[System.Serializable]
public struct AssetHandle {
    public uint Hash;
}

public static class Assets {
    public struct LoadedAsset {
        public string      Name;
        public uint        Hash;
        public AssetBundle Bundle;
        public Object      Reference;
        public bool        Occupied;
    }

    public static Dictionary<string, AssetBundle> LoadedBundles;
    public static LoadedAsset[]                   LoadedAssets;

    public static uint Count  = 1;
    public static uint Length = 1024;

    private static readonly StringBuilder _sb = new();
    private static readonly string Path = $"{Application.streamingAssetsPath}/AssetBundles";

    private const float ResizeFactor = 0.7f;

    public static void InitializeAssets() {
        Length = 1024;
        Count  = 1;
        LoadedAssets  = new LoadedAsset[Length];
        LoadedBundles = new();
        LoadedBundles.Clear();
        _sb.Clear();
    }

    public static void FreeAssets() {
        UnloadAll();
        Length = 1024;
        Count  = 1;
        LoadedAssets  = null;
        LoadedBundles = null;
        _sb.Clear();
    }

    public static AssetHandle LoadAsset(string name) {
        var hash = (uint)name.GetHashCode();
        var id   = GetIdIfExist(hash, out var exist);

        if(exist) {
            return new AssetHandle {
                Hash = hash
            };
        }

        AssetBundle bundle    = null;
        Object      reference = null;

        foreach(var (_, bndl) in LoadedBundles) {
            if(bndl.Contains(name)) {
                bundle    = bndl;
                reference = bundle.LoadAsset(name);
            }
        }

        Assert(bundle, $"Cannot load asset {name}. Did yout load the bundle, containing the asset?");

        var handle = new AssetHandle();
        var asset  = new LoadedAsset();
        id = GetNewId(hash);

        handle.Hash     = hash;
        asset.Name      = name;
        asset.Hash      = hash;
        asset.Occupied  = true;
        asset.Bundle    = bundle;
        asset.Reference = reference;

        LoadedAssets[id] = asset;

        Count++;

        if(Count / Length >= ResizeFactor) {
            Resize(Length * 2);
        }

        return handle;
    }

    public static void LoadBundle(string name) {
        Assert(LoadedBundles.ContainsKey(name) == false, $"Bundle named {name} is already loaded.");

        var path   = $"{Path}/{name}";
        var bundle = AssetBundle.LoadFromFile(path);

        Assert(bundle != null, $"Cannot load bundle \"{name}\". Did you build your bundles, using \"Asset Bundles/Build Bundles\" button?");

        LoadedBundles[name] = bundle;
    }

    public static void UnloadBundle(string name) {
        Assert(LoadedBundles.ContainsKey(name), $"Bundle with name {name} is not loaded");

        LoadedBundles[name].Unload(true);
        LoadedBundles.Remove(name);
    }

    public static void UnloadAll() {
        foreach(var (name, bundle) in LoadedBundles) {
            bundle.Unload(true);
        }

        for(var i = 0; i < Length; ++i) {
            LoadedAssets[i] = new LoadedAsset() {
                Name      = "",
                Hash      = 0,
                Bundle    = null,
                Reference = null,
                Occupied  = false
            };
        }

        LoadedBundles.Clear();
    }

    public static T Instantiate<T>(string name)
    where T : Object {
        var hash = (uint)name.GetHashCode();
        var id   = GetIdIfExist(hash, out var exist);

        if(!exist) {
            var handle = LoadAsset(name);
            return Instantiate<T>(handle);
        }

        T asset;

        if(LoadedAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)LoadedAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset);
    }

    public static T Instantiate<T>(string name, Vector3 position, Quaternion rotation)
    where T : Object {
        var hash = (uint)name.GetHashCode();
        var id   = GetIdIfExist(hash, out var exist);

        if(!exist) {
            var handle = LoadAsset(name);
            return Instantiate<T>(handle, position, rotation);
        }

        T asset;

        if(LoadedAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)LoadedAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset, position, rotation);
    }

    public static T Instantiate<T>(string name, Vector3 position, Quaternion rotation, Transform parent)
    where T : Object {
        var hash = (uint)name.GetHashCode();
        var id   = GetIdIfExist(hash, out var exist);

        if(!exist) {
            var handle = LoadAsset(name);
            return Instantiate<T>(handle, position, rotation, parent);
        }

        T asset;

        if(LoadedAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)LoadedAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset, position, rotation, parent);
    }

    public static T Instantiate<T>(in AssetHandle handle)
    where T : Object {
        var id = GetId(handle.Hash);

        T asset;

        if(LoadedAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)LoadedAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset);
    }

    public static T Instantiate<T>(in AssetHandle handle, Vector3 position, Quaternion rotation)
    where T : Object {
        var id = GetId(handle.Hash);

        T asset;

        if(LoadedAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)LoadedAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset);
    }

    public static T Instantiate<T>(in AssetHandle handle,
                                      Vector3     position,
                                      Quaternion  rotation,
                                      Transform   parent)
    where T : Object {
        var id = GetId(handle.Hash);

        T asset;

        if(LoadedAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)LoadedAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset, position, rotation, parent);
    }

    private static string AssetNameFromPath(string path, int len) {
        _sb.Clear();
        for(var i = 0; i < len; ++i) {
            switch(path[i]) {
                case '/' : {
                    _sb.Clear();
                } break;

                case '\\' : {
                    _sb.Clear();
                } break;

                case '.' : {
                    var str = _sb.ToString();
                    _sb.Clear();
                    return str;
                }

                default : {
                    _sb.Append(path[i]);
                } break;
            }
        }

        _sb.Clear();

        return "";
    }

    private static uint GetNewId(uint hash) {
        var id = hash % Length;

        if(LoadedAssets[id].Occupied == false) {
            return id;
        } else {
            for(var i = id; i < Length; ++i) {
                if(LoadedAssets[i].Occupied == false) {
                    return i;
                } else if(LoadedAssets[i].Hash == hash) {
                    return i;
                }
            }

            Resize(Length * 2);

            return GetNewId(hash);
        }
    }

    private static uint GetId(uint hash) {
        var id = hash % Length;

        if(LoadedAssets[id].Occupied && LoadedAssets[id].Hash == hash) {
            return id;
        } else {
            id++;

            while(LoadedAssets[id].Occupied) {
                if(LoadedAssets[id].Hash == hash) {
                    return id;
                }

                id++;
            }
        }

        return 0;
    }

    private static uint GetIdIfExist(uint hash, out bool exist) {
        var id = hash % Length;

        if(LoadedAssets[id].Occupied && LoadedAssets[id].Hash == hash) {
            exist = true;
            return id;
        } else {
            id++;

            while(LoadedAssets[id].Occupied) {
                if(LoadedAssets[id].Hash == hash) {
                    exist = true;
                    return id;
                }

                id++;
            }
        }

        exist = false;
        return 0;
    }


    private static void Resize(uint newSize) {
        var arr = new LoadedAsset[newSize];
        var len = Length;

        Length = newSize;

        for(var i = 0; i < len; ++i) {
            if(LoadedAssets[i].Occupied) {
                var id = GetNewId(LoadedAssets[i].Hash);
                arr[id] = LoadedAssets[i];
            }
        }

        LoadedAssets = arr;
    }

}