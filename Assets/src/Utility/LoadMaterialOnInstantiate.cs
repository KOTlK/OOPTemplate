using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(Renderer))]
public class LoadMaterialOnInstantiate : MonoBehaviour {
	public AssetReference Material;
	public Renderer 	  Renderer;

	private void Awake() {
		Renderer = GetComponent<Renderer>();
	}
}