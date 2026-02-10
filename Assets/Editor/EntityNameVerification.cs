#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

[InitializeOnLoad]
public static class AddressableChangeTracker {
    static AddressableChangeTracker() {
        AddressableAssetSettings.OnModificationGlobal -= OnAddressableModified;
        AddressableAssetSettings.OnModificationGlobal += OnAddressableModified;
    }

    private static void OnAddressableModified(AddressableAssetSettings settings,
                                              AddressableAssetSettings.ModificationEvent evt,
                                              object data) {
        if (evt != AddressableAssetSettings.ModificationEvent.EntryAdded &&
            evt != AddressableAssetSettings.ModificationEvent.EntryCreated &&
            evt != AddressableAssetSettings.ModificationEvent.EntryModified)
            return;

        if (data is AddressableAssetEntry entry) {
            SetGUID(entry, settings);
        } else if (data is System.Collections.IEnumerable entries) {
            foreach (AddressableAssetEntry e in entries) {
                SetGUID(e, settings);
            }
        }
    }

    private static void SetGUID(AddressableAssetEntry entry, 
                                AddressableAssetSettings settings) {
        var guid      = entry.guid;
        var address   = entry.address;
        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        var asset     = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (asset && asset.TryGetComponent(out Entity entity)) {
            entity.AssetAddress = address;
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif