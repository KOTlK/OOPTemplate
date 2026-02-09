using UnityEditor;
using UnityEngine;

// Ебал юнити.
public static class ExtractMeshesMenu {
    [MenuItem("Assets/Extract Meshes", false, 100)]
    private static void ExtractMeshes() {
        foreach (var obj in Selection.objects) {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            var nameEnd = path.Length;

            for (; nameEnd > 0;) {
                if (path[nameEnd - 1] == '/') break;
                nameEnd--;
            }

            var pathWithoutName = path.Substring(0, nameEnd);

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var sub in subAssets) {
                if (sub is MeshFilter meshFilter) {
                    var newPath = pathWithoutName + meshFilter.sharedMesh.name + ".asset";
                    var meshCopy = Object.Instantiate(meshFilter.sharedMesh);
                    AssetDatabase.CreateAsset(meshCopy, newPath);
                }
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Extract Meshes", true)]
    private static bool ValidateExtractMeshes() {
        return Selection.activeObject != null;
    }
}