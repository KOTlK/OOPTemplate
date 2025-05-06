using System.Text;
using System.Collections.Generic;
using UnityEngine;
using static Assertions;

public class ResourceSystem {
    public struct LoadedResource {
        public AssetBundle Bundle;
        public Object      Reference;
    }

    public Dictionary<string, LoadedResource> Links = new();
    public Dictionary<string, AssetBundle>    LoadedBundles = new();

    private readonly string Path = $"{Application.streamingAssetsPath}/AssetBundles";

    private readonly StringBuilder _sb = new();

    public T Load<T>(string name)
    where T : Object {
        if(Links.ContainsKey(name)) {
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
        } else {
            Debug.LogError($"Cannot load resource with name {name}. Did you load the corresponding bundle?");
            return null;
        }
    }

    public void LoadBundle(string name) {
        if(LoadedBundles.ContainsKey(name)) {
            Debug.LogError($"Bundle with name {name} is already loaded.");
            return;
        }

        var path   = $"{Path}/{name}";
        var bundle = AssetBundle.LoadFromFile(path);

        if(!bundle) {
            Debug.LogError($"Cannot load bundle with name {name}. Did you build your bundles, using \"Asset Bundles/Build Bundles\" button?");
            return;
        }

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

    public void UnloadBundle(string name) {
        if(LoadedBundles.ContainsKey(name) == false) {
            Debug.Log($"Bundle with name {name} is not loaded");
            return;
        }

        LoadedBundles[name].Unload(true);
        LoadedBundles.Remove(name);
    }

    private string AssetNameFromPath(string path, int len) {
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