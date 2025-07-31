using UnityEngine;

public class UIBaker : MonoBehaviour {
    private void Start() {
        UIManager.BakeUIElement(GetComponent<UIElement>());
        Destroy(this);
    }
}