using System.Text;
using System.Collections.Generic;
using UnityEngine;

using static Assertions;

public static class ResourceSystem {
    public struct LoadedResource {
        public AssetBundle Bundle;
        public Object      Reference;
    }

    public static Dictionary<string, LoadedResource> Links = new();
    public static Dictionary<string, AssetBundle>    LoadedBundles = new();

    private static readonly string Path = $"{Application.streamingAssetsPath}/AssetBundles";

    private static readonly StringBuilder _sb = new();

    public static T LoadAsset<T>(string name)
    where T : Object {
        Assert(Links.ContainsKey(name), $"Cannot load resource with name {name}. Did you load the corresponding bundle?");

        var resource = Links[name];
        if(resource.Reference) {
            if(resource.Reference is GameObject gameObject) {
                return gameObject.GetComponent<T>();
            } else {
                return (T)resource.Reference;
            }
        }

        var asset = resource.Bundle.LoadAsset(name);

        resource.Reference = asset;

        Links[name] = resource;

        if(asset is GameObject go) {
            return go.GetComponent<T>();
        } else {
            return (T)asset;
        }
    }

    public static void LoadBundle(string name) {
        Assert(LoadedBundles.ContainsKey(name) == false, $"Bundle with name {name} is already loaded.");

        var path   = $"{Path}/{name}";
        var bundle = AssetBundle.LoadFromFile(path);

        Assert(bundle != null, $"Cannot load bundle with name {name}. Did you build your bundles, using \"Asset Bundles/Build Bundles\" button?");

        LoadedBundles[name] = bundle;

        var assets = bundle.GetAllAssetNames();

        for(var i = 0; i < assets.Length; ++i) {
            var assetName = AssetNameFromPath(assets[i], assets[i].Length);

            if(Links.ContainsKey(assetName)) {
                if(Links[assetName].Reference) {
                    Object.Destroy(Links[assetName].Reference);
                }
            }

            Links[assetName] = new LoadedResource() {
                Bundle    = bundle,
                Reference = null
            };
        }
    }

    public static void UnloadBundle(string name) {
        Assert(LoadedBundles.ContainsKey(name), $"Bundle with name {name} is not loaded");

        LoadedBundles[name].Unload(true);
        LoadedBundles.Remove(name);
    }

    public static void UnloadAll() {
        Links.Clear();
        foreach(var (name, bundle) in LoadedBundles) {
            bundle.Unload(true);
        }

        LoadedBundles.Clear();
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
}