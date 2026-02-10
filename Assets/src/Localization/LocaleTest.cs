using UnityEngine;
using System.Text;
using System.IO;

using static Locale;
using static Locale.TokenType;

public class LocaleTest : MonoBehaviour {
    public NameIdent IdentName;

    public LocalizedString String;

    private void Awake() {
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Q)) {
            LoadLocalization("ru");
        }

        if(Input.GetKeyDown(KeyCode.W)) {
            LoadLocalization("eng");
        }
    }
}