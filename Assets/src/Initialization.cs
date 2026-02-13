using UnityEngine;
using UnityEngine.AddressableAssets;
using Reflex.Attributes;

public struct InitializationCompleted {
}

public class Initialization : MonoBehaviour {
    public AssetReference UI;

    [Inject] private UIEntityManager _em;

    private void Awake() {
        var canvas    = FindAnyObjectByType<Canvas>();
        var ui   = _em.CreateEntity<UI>(UI, 
                                        Vector3.zero, 
                                        Quaternion.identity, 
                                        canvas.transform);

        Events.RaiseGeneral<InitializationCompleted>(new());
    }
}