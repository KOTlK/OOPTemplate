using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

public static class BuildScript {
    public static void BuildWithAddressables() {
        AddressableAssetSettings.BuildPlayerContent();
    }
}