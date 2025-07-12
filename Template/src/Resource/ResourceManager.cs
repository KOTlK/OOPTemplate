using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

using Object = UnityEngine.Object;
using static Assertions;
using static Coroutines;

[System.Serializable]
public struct AssetHandle {
    public uint Hash;
}

public delegate void OnAssetLoad();

public enum AssetLoadingType : byte {
    SingleAsset,
    MultipleAssets,
    SingleBundle,
    MultipleBundles
}

public class AssetDescription {
    public uint             Hash;
    public AssetLoadingType Type;

    public AsyncOperation         Operation;
    public List<AssetDescription> LoadList;
}

public static class ResourceManager {
    public enum AssetStatus : byte {
        Loading,
        Loaded
    }

    public struct Asset {
        public AssetStatus Status;
        public string      Name;
        public uint        Hash;
        public AssetBundle Bundle;
        public Object      Reference;
        public bool        Occupied;
        public bool        Tombstone;

        public AssetDescription Description;
    }

    public static Asset[]                         AllAssets;
    public static Dictionary<uint, AssetBundle>   BundleByAssetHash;

    public static uint Count  = 0;
    public static uint Length = 1024;

    private static readonly StringBuilder _sb = new();
    private static readonly string Path = $"{Application.streamingAssetsPath}/AssetBundles";

    private const uint LoadFactor = 70;

    public static void Initialize() {
        Length = 1024;
        Count  = 0;
        AllAssets     = new Asset[Length];
        BundleByAssetHash  = new();
        BundleByAssetHash.Clear();
        _sb.Clear();
    }

    public static void Free() {
        UnloadAll();
        Length = 1024;
        Count  = 0;
        AllAssets     = null;
        BundleByAssetHash  = null;
        _sb.Clear();
    }

    public static AssetHandle LoadAsset(string name) {
        var hash = (uint)name.GetHashCode();

        Assert(BundleByAssetHash.ContainsKey(hash),
               $"Cannot load the asset {name}, Did you load the bundle, containing it?");

        var id   = GetIdIfExist(hash, out var exist);

        if(exist) {
            return new AssetHandle {
                Hash = hash
            };
        }

        var bundle    = BundleByAssetHash[hash];
        var reference = bundle.LoadAsset(name);
        var handle    = new AssetHandle();
        var asset     = new Asset();

        id = GetNewId(hash);

        handle.Hash     = hash;
        asset.Name      = name;
        asset.Hash      = hash;
        asset.Occupied  = true;
        asset.Bundle    = bundle;
        asset.Reference = reference;

        AllAssets[id] = asset;

        return handle;
    }

    public static AssetHandle LoadAssetAsync(string name, OnAssetLoad onLoad) {
        var hash = (uint)name.GetHashCode();

        Assert(BundleByAssetHash.ContainsKey(hash),
               $"Cannot load the asset {name}, Did you load the bundle, containing it?");

        var id = GetIdIfExist(hash, out var exist);

        if(exist) {
            onLoad();
            return new AssetHandle {
                Hash = hash
            };
        }

        var bundle    = BundleByAssetHash[hash];
        var operation = bundle.LoadAssetAsync(name);
        var desc      = new AssetDescription {
            Type        = AssetLoadingType.SingleAsset,
            Operation   = operation
        };

        var handle = PushLoading(name, desc, operation, bundle);

        BeginCoroutine(LoadSingleAsset(desc, onLoad));

        return handle;
    }

    // Load multiple assets asynchronously. The first handle points to the head of the batch.
    // So, getting it's loading progress will return loading progress of the whole batch.
    public static List<AssetHandle> LoadAssetsAsync(OnAssetLoad onLoad, params string[] names) {
        var handle = new AssetHandle();
        var res    = ListPool<AssetHandle>.Get();
        var hash   = (uint)names[0].GetHashCode();

        Assert(BundleByAssetHash.ContainsKey(hash),
               $"Cannot load the asset {names[0]}, Did you load the bundle, containing it?");

        var id         = GetIdIfExist(hash, out var exist);
        var parentDesc = new AssetDescription();
        var list       = ListPool<AssetDescription>.Get();

        parentDesc.Hash        = hash;
        parentDesc.Type        = AssetLoadingType.MultipleAssets;
        parentDesc.LoadList    = list;

        if(exist) {
            handle.Hash = hash;
            res.Add(handle);
        } else {
            var bundle = BundleByAssetHash[hash];
            var op     = bundle.LoadAssetAsync(names[0]);

            parentDesc.Operation = op;

            list.Add(parentDesc);

            handle = PushLoading(names[0], parentDesc, op, null);

            res.Add(handle);
        }

        for(var i = 1; i < names.Length; ++i) {
            hash = (uint)names[i].GetHashCode();

            Assert(BundleByAssetHash.ContainsKey(hash),
                   $"Cannot load the asset {names[i]}, Did you load the bundle, containing it?");

            id = GetIdIfExist(hash, out exist);

            if(exist) {
                handle.Hash = hash;
                res.Add(handle);
            } else {
                var bundle = BundleByAssetHash[hash];
                var op     = bundle.LoadAssetAsync(names[i]);
                var desc   = new AssetDescription();

                desc.Hash        = hash;
                desc.Type        = AssetLoadingType.SingleAsset;
                desc.Operation   = op;

                handle = PushLoading(names[i], desc, op, null);
                res.Add(handle);
                list.Add(desc);
            }
        }

        BeginCoroutine(LoadMultipleAssets(list, onLoad));

        return res;
    }

    public static void LoadBundle(string name) {
        var path   = $"{Path}/{name}";
        var bundle = AssetBundle.LoadFromFile(path);

        Assert(bundle != null, $"Cannot load bundle \"{name}\". Did you build your bundles, using \"Asset Bundles/Build Bundles\" button?");

        var names = bundle.GetAllAssetNames();

        foreach(var assetPath in names) {
            var assetName = AssetNameFromPath(assetPath, assetPath.Length);
            var hash      = (uint)assetName.GetHashCode();
            BundleByAssetHash.Add(hash, bundle);
        }

        var bundleHash = (uint)name.GetHashCode();
        var id = GetNewId(bundleHash);

        var asset = new Asset();

        asset.Status    = AssetStatus.Loaded;
        asset.Name     = name;
        asset.Hash     = bundleHash;
        asset.Bundle   = bundle;
        asset.Occupied = true;

        AllAssets[id] = asset;
    }

    public static AssetHandle LoadBundleAsync(string name, OnAssetLoad onLoad) {
        Assert(BundleByAssetHash.ContainsKey((uint)name.GetHashCode()) == false, $"Bundle named {name} is already loaded.");

        var path = $"{Path}/{name}";
        var op   = AssetBundle.LoadFromFileAsync(path);
        var desc = new AssetDescription {
            Type        = AssetLoadingType.SingleBundle,
            Operation   = op
        };

        var handle = PushLoading(name, desc, op);

        BeginCoroutine(LoadSingleAsset(desc, onLoad));

        return handle;
    }

    public static AssetHandle LoadBundlesAsync(OnAssetLoad onLoad, params string[] names) {
        Assert(BundleByAssetHash.ContainsKey((uint)names[0].GetHashCode()) == false, $"Bundle named {names[0]} is already loaded.");

        var path   = $"{Path}/{names[0]}";
        var hash   = (uint)(names[0].GetHashCode());
        var op     = AssetBundle.LoadFromFileAsync(path);
        var list   = ListPool<AssetDescription>.Get();
        var parent = new AssetDescription {
            Type         = AssetLoadingType.MultipleBundles,
            Operation    = op,
            LoadList     = list
        };

        list.Add(parent);

        var handle = PushLoading(names[0], parent, op);

        var len = names.Length;

        for(var i = 1; i < len; ++i) {
            Assert(BundleByAssetHash.ContainsKey((uint)names[i].GetHashCode()) == false, $"Bundle named {names[i]} is already loaded.");
            path = $"{Path}/{names[i]}";
            hash = (uint)(names[i].GetHashCode());
            op   = AssetBundle.LoadFromFileAsync(path);
            var desc = new AssetDescription {
                Type         = AssetLoadingType.MultipleBundles,
                Operation    = op,
            };

            var childHandle = PushLoading(names[i], desc, op);

            list.Add(desc);
        }

        BeginCoroutine(LoadMultipleAssets(list, onLoad));

        return handle;
    }

    public static void UnloadBundle(string name) {
        Assert(BundleByAssetHash.ContainsKey((uint)name.GetHashCode()), $"Bundle with name {name} is not loaded");
        var bundleHash = (uint)name.GetHashCode();
        var bundleId   = GetId(bundleHash);

        var names = AllAssets[bundleId].Bundle.GetAllAssetNames();

        for(var i = 0; i < names.Length; ++i) {
            var assetName = AssetNameFromPath(names[i], names[i].Length);
            var hash      = (uint)assetName.GetHashCode();
            var id        = GetIdIfExist(hash, out var exist);

            if(exist) {
                Remove(id);
            }

            BundleByAssetHash.Remove(hash);
        }

        AllAssets[bundleId].Bundle.Unload(true);
        Remove(bundleId);
    }

    public static void UnloadAll() {
        for(var i = 0; i < Length; ++i) {
            var asset = AllAssets[i];

            if(asset.Bundle != null) {
                asset.Bundle.Unload(true);
            }

            AllAssets[i] = new Asset() {
                Name      = "",
                Hash      = 0,
                Bundle    = null,
                Reference = null,
                Occupied  = false
            };
        }

        BundleByAssetHash.Clear();
        Count = 0;
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

        if(AllAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)AllAssets[id].Reference;
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

        if(AllAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)AllAssets[id].Reference;
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

        if(AllAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)AllAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset, position, rotation, parent);
    }

    public static T Instantiate<T>(in AssetHandle handle)
    where T : Object {
        var id = GetId(handle.Hash);

        T asset;

        if(AllAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)AllAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset);
    }

    public static T Instantiate<T>(in AssetHandle handle, Vector3 position, Quaternion rotation)
    where T : Object {
        var id = GetId(handle.Hash);

        T asset;

        if(AllAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)AllAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset, position, rotation);
    }

    public static T Instantiate<T>(in AssetHandle handle,
                                      Vector3     position,
                                      Quaternion  rotation,
                                      Transform   parent)
    where T : Object {
        var id = GetId(handle.Hash);

        T asset;

        if(AllAssets[id].Reference is GameObject go) {
            asset = go.GetComponent<T>();
        } else {
            asset = (T)AllAssets[id].Reference;
        }

        return Object.Instantiate<T>(asset, position, rotation, parent);
    }

    public static float GetLoadingProgress(AssetHandle handle) {
        var id = GetId(handle.Hash);
        if(AllAssets[id].Status == AssetStatus.Loaded) return 1f;

        switch(AllAssets[id].Description.Type) {
            case AssetLoadingType.SingleAsset :
            case AssetLoadingType.SingleBundle : {
                return AllAssets[id].Description.Operation.progress;
            }

            case AssetLoadingType.MultipleAssets :
            case AssetLoadingType.MultipleBundles : {
                var desc     = AllAssets[id].Description;
                var progress = 0f;
                var count    = desc.LoadList.Count;

                for(var i = 0; i < count; ++i) {
                    progress += desc.LoadList[i].Operation.progress;
                }

                progress /= count;

                return progress;
            }
        }

        return 0f;
    }

    public static uint GetId(uint hash) {
        var id = hash % Length;

        if(AllAssets[id].Hash == hash) {
            return id;
        } else {
            uint i = 1;

            while(true) {
                id = DoubleHash(hash, i);
                if(AllAssets[id].Hash == hash) break;
                if(AllAssets[id].Tombstone == false) break;

                i++;
            }
        }

        return id;
    }

    public static uint GetIdIfExist(uint hash, out bool exist) {
        var id = hash % Length;

        if(AllAssets[id].Occupied && AllAssets[id].Hash == hash) {
            exist = true;
            return id;
        } else {
            uint i = 1;

            while(true) {
                id = DoubleHash(hash, i);

                if(AllAssets[id].Hash == hash) break;

                if(AllAssets[id].Tombstone == false) {
                    exist = false;
                    return 0;
                }

                i++;
            }
        }

        exist = true;
        return id;
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

    private static void Remove(uint i) {
        AllAssets[i].Occupied  = false;
        AllAssets[i].Reference = null;
        AllAssets[i].Bundle    = null;
        Count--;
    }

    private static uint GetNewId(uint hash) {
        uint id = hash % Length;

        if(AllAssets[id].Occupied == false) {
            return id;
        } else {
            uint i = 1;

            while(true) {
                id = DoubleHash(hash, i);
                if(AllAssets[id].Occupied == false) {
                    break;
                }
                i++;
            }

            Count++;

            if(Count * 100 / Length >= LoadFactor) {
                Resize(Length * 2);
            }

            AllAssets[id].Tombstone = true;

            return id;
        }
    }

    private static uint DoubleHash(uint hash, uint i) {
        return (1 + (hash + i * (hash % (Length / 2)))) % Length;
    }

    private static void Resize(uint newSize) {
        var prev  = AllAssets;
        var len   = Length;
        AllAssets = new Asset[newSize];

        Length = newSize;

        for(var i = 0; i < len; ++i) {
            if(prev[i].Occupied) {
                var id = GetNewId(prev[i].Hash);
                AllAssets[id] = prev[i];
            }
        }
    }

    private static AssetHandle PushLoading(string name, AssetDescription desc, AsyncOperation op, AssetBundle bundle = null) {
        var hash = (uint)name.GetHashCode();
        var id   = GetNewId(hash);

        var handle = new AssetHandle();
        var asset  = new Asset();

        desc.Hash   = hash;
        handle.Hash = hash;

        asset.Status       = AssetStatus.Loading;
        asset.Name        = name;
        asset.Hash        = hash;
        asset.Bundle      = bundle;
        asset.Occupied    = true;
        asset.Description = desc;

        AllAssets[id] = asset;

        // LoadingAssets.Add(op, handle);

        return handle;
    }

    private static IEnumerator LoadSingleAsset(AssetDescription ass, OnAssetLoad onLoad) {
        while(ass.Operation.isDone == false) yield return null;

        if(ass.Type == AssetLoadingType.SingleBundle) {
            var operation  = (AssetBundleCreateRequest)ass.Operation;
            var id         = GetId(ass.Hash);
            var bundle     = operation.assetBundle;
            var names      = bundle.GetAllAssetNames();

            foreach(var assetPath in names) {
                var assetName = AssetNameFromPath(assetPath, assetPath.Length);
                var assetHash = (uint)assetName.GetHashCode();

                BundleByAssetHash.Add(assetHash, bundle);
            }

            AllAssets[id].Status  = AssetStatus.Loaded;
            AllAssets[id].Bundle = operation.assetBundle;
        } else {
            var operation  = (AssetBundleRequest)ass.Operation;
            var id         = GetId(ass.Hash);

            AllAssets[id].Status     = AssetStatus.Loaded;
            AllAssets[id].Reference = operation.asset;
        }

        onLoad();
    }

    private static IEnumerator LoadMultipleAssets(List<AssetDescription> list, OnAssetLoad onLoad) {
        var count = list.Count;

        while(true) {
            var allDone = true;

            for(var i = 0; i < count; ++i) {
                if(!list[i].Operation.isDone) {
                    allDone = false;
                    break;
                }
            }

            if(allDone) break;

            yield return null;
        }

        if(list[0].Type == AssetLoadingType.MultipleAssets) {
            for(var i = 0; i < count; ++i) {
                var desc      = list[i];
                var operation = (AssetBundleRequest)desc.Operation;
                var id        = GetId(desc.Hash);

                AllAssets[id].Reference = operation.asset;
                AllAssets[id].Status     = AssetStatus.Loaded;
            }
        } else {
            for(var i = 0; i < count; ++i) {
                var desc      = list[i];
                var operation = (AssetBundleCreateRequest)desc.Operation;
                var id        = GetId(desc.Hash);
                var bundle    = operation.assetBundle;
                var names     = bundle.GetAllAssetNames();

                foreach(var assetPath in names) {
                    var assetName = AssetNameFromPath(assetPath, assetPath.Length);
                    var assetHash = (uint)assetName.GetHashCode();

                    BundleByAssetHash.Add(assetHash, bundle);
                }

                AllAssets[id].Bundle = bundle;
                AllAssets[id].Status  = AssetStatus.Loaded;
            }
        }

        onLoad();

        ListPool<AssetDescription>.Release(list);
    }
}