using TMPro;
using UnityEngine;

using static Locale;

public class LocalizedText : UIElement {
    public int      Ident;
    public TMP_Text Text;

    public override void OnCreate() {
        LocalizationLoaded += UpdateText;
        UpdateText();
    }

    public override void Destroy() {
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