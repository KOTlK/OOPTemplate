using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LoadMeshOnInstantiate : MonoBehaviour {
	public AssetReference Mesh;
	public MeshFilter     Filter;

	private void Awake() {
		Filter = GetComponent<MeshFilter>();
	}
}