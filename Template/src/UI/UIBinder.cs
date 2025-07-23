using UnityEngine;

[RequireComponent(typeof(UIElement))]
public class UIBinder : MonoBehaviour {
    public string Name;

    private void Start() {
        Bind();
    }

    public void Bind() {
        UIManager.BindUIElement(GetComponent<UIElement>(), Name);
        Destroy(this);
    }
}