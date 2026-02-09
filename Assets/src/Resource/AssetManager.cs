using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

using static Assertions;

public delegate void OnAsyncAssetLoad(GameObject go);
public delegate void OnAsyncSceneLoad(SceneInstance scene);

public struct MeshLoadOperation {
    public MeshFilter Filter;
}

public struct MaterialLoadOperation {
    public Renderer Renderer;
}

public struct SceneLoadOperation {
    public OnAsyncSceneLoad Callback;
}

public static class AssetManager {
    public static Dictionary<AsyncOperationHandle<GameObject>, OnAsyncAssetLoad> AssetLoadingCallback;
    public static Dictionary<AsyncOperationHandle<IList<Mesh>>, MeshLoadOperation> MeshLoading;
    public static Dictionary<AsyncOperationHandle<IList<Material>>, MaterialLoadOperation> MaterialLoading;
    public static Dictionary<AsyncOperationHandle<SceneInstance>, SceneLoadOperation> SceneLoading;

    public static string CurrentDetailLevel = DetailLevelHigh;

    public const string DetailLevelLow  = "Low";
    public const string DetailLevelMid  = "Mid";
    public const string DetailLevelHigh = "High";

    public static void Init() {
        AssetLoadingCallback = new();
        AssetLoadingCallback.Clear();
        MeshLoading = new();
        MeshLoading.Clear();
        MaterialLoading = new();
        MaterialLoading.Clear();
        SceneLoading = new();
        SceneLoading.Clear();
    }

    public static void SetCurrentDetailLevel(string level) {
        // @TODO: Iterate over all loaded assets and load assets with new detail level.
        CurrentDetailLevel = level;
    }

    public static void LoadMeshAsync(object meshAddress, MeshFilter filter) {
        var list = ListPool<object>.Get();
        list.Add(meshAddress);
        list.Add(CurrentDetailLevel);
        var handle = Addressables.LoadAssetsAsync<Mesh>(list, null, Addressables.MergeMode.Intersection);

        ListPool<object>.Release(list);

        handle.Completed += OnMeshLoaded;

        var op = new MeshLoadOperation();

        op.Filter = filter;

        MeshLoading.Add(handle, op);
    }

    public static void LoadMaterialAsync(object matAddress, Renderer renderer) {
        var list = ListPool<object>.Get();
        list.Add(matAddress);
        list.Add(CurrentDetailLevel);
        var handle = Addressables.LoadAssetsAsync<Material>(list, null, Addressables.MergeMode.Intersection);

        ListPool<object>.Release(list);

        handle.Completed += OnMaterialLoaded;

        var op = new MaterialLoadOperation();

        op.Renderer = renderer;

        MaterialLoading.Add(handle, op);
    }

    public static T Load<T>(object key) 
    where T : UnityEngine.Object {
        var handle = Addressables.LoadAssetAsync<T>(key);
        var asset  = handle.WaitForCompletion();

        return asset;
    }

    public static T Instantiate<T>(object     key, 
                                   Vector3    position,
                                   Quaternion rotation,
                                   Transform  parent = null) 
    where T : UnityEngine.Object {
        var handle = Addressables.InstantiateAsync(key, position, rotation, parent);
        var asset  = handle.WaitForCompletion();

        LoadMaterialsAndMeshes(asset);

        return asset.GetComponent<T>();
    }

    public static T Instantiate<T>(object key, 
                                   Transform  parent = null) 
    where T : UnityEngine.Object {
        return Instantiate<T>(key, Vector3.zero, Quaternion.identity, parent);
    }

    public static List<GameObject> Instantiate(List<object> keys) {
        var list = ListPool<GameObject>.Get();

        var handle = Addressables.LoadAssetsAsync<GameObject>(keys, go => {
            list.Add(UnityEngine.Object.Instantiate(go));
        });

        handle.WaitForCompletion();

        return list;
    }

    public static List<GameObject> Instantiate(List<object>    keys, 
                                              List<Vector3>    positions, 
                                              List<Quaternion> rotations, 
                                              Transform        parent) {
        Assert(keys.Count == positions.Count &&
               positions.Count == rotations.Count, "Keys, positions and rotations counts should match.");
        var list   = ListPool<GameObject>.Get();
        var handle = Addressables.LoadAssetsAsync<GameObject>(keys, null);
        var assets = handle.WaitForCompletion();

        for (var i = 0; i < assets.Count; i++) {
            list.Add(UnityEngine.Object.Instantiate(assets[i], positions[i], rotations[i], parent));
        }

        return list;
    }

    public static void InstantiateAsync<T>(object           key,
                                           Vector3          position,
                                           Quaternion       rotation,
                                           Transform        parent = null,
                                           OnAsyncAssetLoad callback = null) {
        var handle = Addressables.InstantiateAsync(key, position, rotation, parent);

        handle.Completed += OnAssetInstantiate;

        if (callback != null) {
            AssetLoadingCallback.Add(handle, callback);
        }
    }

    public static void LoadScene(object scene, 
                                 LoadSceneMode mode = LoadSceneMode.Single) {
        var handle = Addressables.LoadSceneAsync(scene, mode);
        handle.WaitForCompletion();
    }

    public static void LoadSceneAsync(object           scene, 
                                      LoadSceneMode    mode = LoadSceneMode.Single,
                                      OnAsyncSceneLoad callback = null) {
        var handle = Addressables.LoadSceneAsync(scene, mode);
        var op     = new SceneLoadOperation();

        op.Callback = callback;

        SceneLoading.Add(handle, op);

        handle.Completed += OnSceneLoaded;
    }

    private static void OnAssetInstantiate(AsyncOperationHandle<GameObject> op) {
        var callback = AssetLoadingCallback[op];

        Assert(op.Status == AsyncOperationStatus.Succeeded, "Asset % is not loaded", op.DebugName);

        var obj = op.Result;

        LoadMaterialsAndMeshes(obj);

        callback.Invoke(op.Result);

        AssetLoadingCallback.Remove(op);
    }

    private static void OnMeshLoaded(AsyncOperationHandle<IList<Mesh>> op) {
        Assert(MeshLoading.ContainsKey(op), "Cannot get operation for mesh loading");
        var operation = MeshLoading[op];

        Assert(op.Status == AsyncOperationStatus.Succeeded, "Mesh loading operation failed.");
        Assert(op.Result.Count > 0, "Mesh loading operation failed, combination cannot be found.");

        var list = op.Result;

        operation.Filter.sharedMesh = op.Result[0];
        Addressables.Release(op);
        MeshLoading.Remove(op);
    }

    private static void OnMaterialLoaded(AsyncOperationHandle<IList<Material>> op) {
        Assert(MaterialLoading.ContainsKey(op), "Cannot get operation for mesh loading");
        var operation = MaterialLoading[op];

        Assert(op.Status == AsyncOperationStatus.Succeeded, "Mesh loading operation failed.");
        Assert(op.Result.Count > 0, "Mesh loading operation failed, combination cannot be found.");

        operation.Renderer.material = op.Result[0];
        Addressables.Release(op);
        MaterialLoading.Remove(op);
    }

    private static void LoadMaterialsAndMeshes(GameObject obj) {
        var loadMeshes = ListPool<LoadMeshOnInstantiate>.Get();

        obj.GetComponentsInChildren<LoadMeshOnInstantiate>(true, loadMeshes);

        foreach(var mesh in loadMeshes) {
            Assert(mesh.Mesh != null, "Error while trying to load mesh on object instantiation. Mesh's AssetReference is null.");
            Assert(mesh.Filter != null, "Error while trying to load mesh on object instantiation. Mesh's Filter is null.");
            LoadMeshAsync(mesh.Mesh, mesh.Filter);
            UnityEngine.Object.Destroy(mesh);
        }

        ListPool<LoadMeshOnInstantiate>.Release(loadMeshes);

        var loadMaterials = ListPool<LoadMaterialOnInstantiate>.Get();

        obj.GetComponentsInChildren<LoadMaterialOnInstantiate>(true, loadMaterials);

        foreach(var material in loadMaterials) {
            Assert(material.Material != null, "Error while trying to load material on object instantiation. Material's AssetReference is null.");
            Assert(material.Renderer != null, "Error while trying to load material on object instantiation. Material's Renderer is null.");
            LoadMaterialAsync(material.Material, material.Renderer);
            UnityEngine.Object.Destroy(material);
        }

        ListPool<LoadMaterialOnInstantiate>.Release(loadMaterials);
    }

    private static void OnSceneLoaded(AsyncOperationHandle<SceneInstance> op) {
        var sceneOp = SceneLoading[op];

        sceneOp.Callback.Invoke(op.Result);

        SceneLoading.Remove(op);
    }
}