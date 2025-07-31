#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EntityNameVerification : AssetPostprocessor {
    void OnPostprocessPrefab(GameObject g) {
        if(g.TryGetComponent<Entity>(out var e)) {
            if(e.Name != e.name) {
                e.Name = e.name;
            }
        }
    }
}
#endif