using System.IO;
using UnityEditor;
using UnityEngine;

public class BundlesBuilder {

    [MenuItem("Asset Bundles/Build Bundles")]
    public static void BuildBundles() {
        var path     = $"{Application.streamingAssetsPath}/AssetBundles";
        var platform = EditorUserBuildSettings.activeBuildTarget;

        if(Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }

        BuildPipeline.BuildAssetBundles(path,
                                        BuildAssetBundleOptions.ChunkBasedCompression |
                                        BuildAssetBundleOptions.ForceRebuildAssetBundle,
                                        platform);
    }
}
