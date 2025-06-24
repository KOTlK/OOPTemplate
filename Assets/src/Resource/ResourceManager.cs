using System.Text;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

using Object = UnityEngine.Object;
using static Assertions;

[System.Serializable]
public struct AssetHandle {
    public uint Hash;
}

public delegate void OnAssetLoad();
public delegate void OnBundleLoad();

public enum AssetLoadingType : byte {
    SingleAsset,
    MultipleAssets,
    SingleBundle,
    MultipleBundles
}

public class AssetDescription {
    public uint             Hash;
    public AssetLoadingType Type;
    public OnAssetLoad  OnAssetLoad;
    public OnBundleLoad OnBundleLoad;

    public AssetDescription       Parent;
    public AsyncOperation         Operation;
    public List<uint>             Batch;
    public List<AsyncOperation>   Operations;
}

public static class ResourceManager {
    public enum AssetState : byte {
        Loading,
        Loaded
    }

    public struct Asset {
        public AssetState  State;
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

    private static Dictionary<AsyncOperation, AssetHandle> LoadingAssets;

    private static readonly StringBuilder _sb = new();
    private static readonly string Path = $"{Application.streamingAssetsPath}/AssetBundles";

    private const uint LoadFactor = 70;

    public static void Initialize() {
        Length = 1024;
        Count  = 0;
        AllAssets     = new Asset[Length];
        LoadingAssets = new();
        BundleByAssetHash  = new();
        LoadingAssets.Clear();
        BundleByAssetHash.Clear();
        _sb.Clear();
    }

    public static void Free() {
        UnloadAll();
        Length = 1024;
        Count  = 0;
        AllAssets     = null;
        BundleByAssetHash  = null;
        LoadingAssets = null;
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

        return BeginAsyncAssetLoading(name, hash, BundleByAssetHash[hash], onLoad);
    }

    // Load multiple assets asynchronously. The first handle points to the head of the batch.
    // So, getting it's loading progress will return loading progress of the whole batch.
    public static List<AssetHandle> LoadAssetsAsync(OnAssetLoad onLoad, params string[] names) {
        var handle = new AssetHandle();
        var res    = ListPool<AssetHandle>.Get();
        var hash   = (uint)names[0].GetHashCode();

        Assert(BundleByAssetHash.ContainsKey(hash),
               $"Cannot load the asset {names[0]}, Did you load the bundle, containing it?");

        var id = GetIdIfExist(hash, out var exist);
        var parentDesc = new AssetDescription();
        var batch      = ListPool<uint>.Get();
        var ops        = ListPool<AsyncOperation>.Get();

        parentDesc.Hash        = hash;
        parentDesc.Type        = AssetLoadingType.MultipleAssets;
        parentDesc.OnAssetLoad = onLoad;
        parentDesc.Parent      = null;
        parentDesc.Batch       = batch;
        parentDesc.Operations  = ops;

        if(exist) {
            handle.Hash = hash;
            res.Add(handle);
        } else {
            var bundle = BundleByAssetHash[hash];
            var op     = bundle.LoadAssetAsync(names[0]);

            parentDesc.Operation = op;

            op.completed += OneOfAssetsLoaded;

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
                batch.Add(id);
            } else {
                var bundle = BundleByAssetHash[hash];
                var op     = bundle.LoadAssetAsync(names[i]);
                var desc   = new AssetDescription();

                desc.Hash        = hash;
                desc.Type        = AssetLoadingType.SingleAsset;
                desc.Parent      = parentDesc;
                desc.Operation   = op;

                op.completed += OneOfAssetsLoaded;

                handle = PushLoading(names[i], desc, op, null);
                res.Add(handle);
                batch.Add(GetId(hash));
                ops.Add(op);
            }
        }

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

        asset.State    = AssetState.Loaded;
        asset.Name     = name;
        asset.Hash     = bundleHash;
        asset.Bundle   = bundle;
        asset.Occupied = true;

        AllAssets[id] = asset;
    }

    public static AssetHandle LoadBundleAsync(string name, OnBundleLoad onLoad) {
        Assert(BundleByAssetHash.ContainsKey((uint)name.GetHashCode()) == false, $"Bundle named {name} is already loaded.");

        var path = $"{Path}/{name}";
        var op   = AssetBundle.LoadFromFileAsync(path);
        var desc = new AssetDescription {
            Type         = AssetLoadingType.SingleBundle,
            OnBundleLoad = onLoad,
            Operation    = op
        };

        op.completed += AsyncBundleLoadingComplete;

        var handle = PushLoading(name, desc, op);

        return handle;
    }

    public static AssetHandle LoadBundlesAsync(OnBundleLoad onLoad, params string[] names) {
        Assert(BundleByAssetHash.ContainsKey((uint)names[0].GetHashCode()) == false, $"Bundle named {names[0]} is already loaded.");

        var path   = $"{Path}/{names[0]}";
        var hash   = (uint)(names[0].GetHashCode());
        var op     = AssetBundle.LoadFromFileAsync(path);
        var ops    = ListPool<AsyncOperation>.Get();
        var batch  = ListPool<uint>.Get();
        var parent = new AssetDescription {
            Type         = AssetLoadingType.MultipleBundles,
            OnBundleLoad = onLoad,
            Operation    = op,
            Batch        = batch,
            Operations   = ops,
            Parent       = null
        };

        op.completed += OneOfBundlesLoadingComplete;

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
                Parent       = parent
            };

            var childHandle = PushLoading(names[i], desc, op);

            op.completed += OneOfBundlesLoadingComplete;

            batch.Add(GetId(hash));
            ops.Add(op);
        }

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
        LoadingAssets.Clear();
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
        if(AllAssets[id].State == AssetState.Loaded) return 1f;

        switch(AllAssets[id].Description.Type) {
            case AssetLoadingType.SingleAsset :
            case AssetLoadingType.SingleBundle : {
                return AllAssets[id].Description.Operation.progress;
            }

            case AssetLoadingType.MultipleAssets :
            case AssetLoadingType.MultipleBundles : {
                var desc     = AllAssets[id].Description;
                var progress = desc.Operation.progress;
                var count    = desc.Operations.Count;

                for(var i = 0; i < count; ++i) {
                    progress += desc.Operations[i].progress;
                }

                progress /= count + 1;

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

    private static AssetHandle BeginAsyncAssetLoading(string name, uint hash, AssetBundle bundle, OnAssetLoad onLoad) {
        var operation = bundle.LoadAssetAsync(name);
        var desc      = new AssetDescription {
            Type        = AssetLoadingType.SingleAsset,
            OnAssetLoad = onLoad,
            Operation   = operation
        };

        var handle = PushLoading(name, desc, operation, bundle);

        operation.completed += AsyncAssetLoadingComplete;

        return handle;
    }

    private static void AsyncAssetLoadingComplete(AsyncOperation op) {
        op.completed -= AsyncAssetLoadingComplete;
        var operation = (AssetBundleRequest)op;

        var loadHandle = LoadingAssets[operation];
        var id         = GetId(loadHandle.Hash);

        AllAssets[id].State     = AssetState.Loaded;
        AllAssets[id].Reference = operation.asset;

        AllAssets[id].Description.OnAssetLoad();
    }

    private static void AsyncBundleLoadingComplete(AsyncOperation op) {
        op.completed -= AsyncBundleLoadingComplete;

        // WTF???
        // AsyncOperation->Resource..Operation->AssetBundleCreateRequest braindead
        var operation = (AssetBundleCreateRequest)op;
        var handle    = LoadingAssets[operation];
        var id        = GetId(handle.Hash);
        var desc      = AllAssets[id].Description;
        var bundle    = operation.assetBundle;
        var names     = bundle.GetAllAssetNames();

        foreach(var assetPath in names) {
            var assetName = AssetNameFromPath(assetPath, assetPath.Length);
            var assetHash = (uint)assetName.GetHashCode();
            BundleByAssetHash.Add(assetHash, bundle);
        }

        AllAssets[id].State  = AssetState.Loaded;
        AllAssets[id].Bundle = bundle;

        LoadingAssets.Remove(operation);

        desc.OnBundleLoad();
    }

    private static void OneOfBundlesLoadingComplete(AsyncOperation op) {
        op.completed -= OneOfBundlesLoadingComplete;

        var operation = (AssetBundleCreateRequest)op;
        var handle    = LoadingAssets[operation];
        var id        = GetId(handle.Hash);
        var desc      = AllAssets[id].Description;
        var bundle    = operation.assetBundle;
        var names     = bundle.GetAllAssetNames();

        foreach(var assetPath in names) {
            var assetName = AssetNameFromPath(assetPath, assetPath.Length);
            var assetHash = (uint)assetName.GetHashCode();

            BundleByAssetHash.Add(assetHash, bundle);
        }

        AllAssets[id].Bundle = bundle;

        if(desc.Parent == null) {
            TryEndBundleLoading(desc);
        } else {
            TryEndBundleLoading(desc.Parent);
        }
    }

    private static void OneOfAssetsLoaded(AsyncOperation op) {
        op.completed -= OneOfAssetsLoaded;
        Debug.Log("Asset Loaded");

        var operation = (AssetBundleRequest)op;
        var handle    = LoadingAssets[operation];
        var id        = GetId(handle.Hash);
        var desc      = AllAssets[id].Description;

        AllAssets[id].Reference = operation.asset;

        if(desc.Parent == null) {
            TryEndAssetLoading(desc);
        } else {
            TryEndAssetLoading(desc.Parent);
        }
    }

    private static AssetHandle PushLoading(string name, AssetDescription desc, AsyncOperation op, AssetBundle bundle = null) {
        var hash = (uint)name.GetHashCode();
        var id   = GetNewId(hash);

        var handle = new AssetHandle();
        var asset  = new Asset();

        desc.Hash   = hash;
        handle.Hash = hash;

        asset.State       = AssetState.Loading;
        asset.Name        = name;
        asset.Hash        = hash;
        asset.Bundle      = bundle;
        asset.Occupied    = true;
        asset.Description = desc;

        AllAssets[id] = asset;

        LoadingAssets.Add(op, handle);

        return handle;
    }

    private static void TryEndBundleLoading(AssetDescription parent) {
        var id = GetId(parent.Hash);

        foreach(var child in parent.Operations) {
            if(child.isDone == false) return;
        }

        AllAssets[id].State  = AssetState.Loaded;

        LoadingAssets.Remove(parent.Operation);

        foreach(var child in parent.Batch) {
            ref var asset = ref AllAssets[child];
            asset.State   = AssetState.Loaded;

            LoadingAssets.Remove(asset.Description.Operation);
        }

        parent.OnBundleLoad();
    }

    private static void TryEndAssetLoading(AssetDescription parent) {
        var id = GetId(parent.Hash);

        foreach(var child in parent.Operations) {
            if(child.isDone == false) return;
        }

        AllAssets[id].State  = AssetState.Loaded;

        LoadingAssets.Remove(parent.Operation);

        foreach(var child in parent.Batch) {
            ref var asset = ref AllAssets[child];
            asset.State   = AssetState.Loaded;

            if(asset.Description.Operation != null) {
                LoadingAssets.Remove(asset.Description.Operation);
            }
        }

        parent.OnAssetLoad();
    }
}