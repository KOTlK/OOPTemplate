using TMPro;
using UnityEngine;

using static Locale;

public class LocalizedText : MonoBehaviour {
    public int      Ident;
    public TMP_Text Text;

    private void Awake() {
        LocalizationLoaded += UpdateText;
    }

    private void Start() {
        UpdateText();
    }

    private void OnDestroy() {
        LocalizationLoaded -= UpdateText;
    }

    private void UpdateText() {
        Text.text = Get(Ident);
    }

    private void OnValidate() {
        if(Has(Ident)) {
            UpdateText();
        }
    }
}