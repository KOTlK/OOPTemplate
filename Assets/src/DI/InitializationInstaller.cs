using Reflex.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Reflex.Injectors;
using Reflex.Extensions;

using static Assertions;

public class UIInstaller : MonoBehaviour, IInstaller {
    public AssetReference UI;
	public void InstallBindings(ContainerBuilder builder) {
        builder.RegisterValue("Hello");
    }

    private void Awake() {
        var canvas    = FindAnyObjectByType<Canvas>();
        var container = SceneManager.GetActiveScene().GetSceneContainer();
        var em        = container.Resolve<UIEntityManager>();
        var (_, ui)   = em.CreateEntity<UI>(UI, 
                                            Vector3.zero, 
                                            Quaternion.identity, 
                                            canvas.transform);
    }
}