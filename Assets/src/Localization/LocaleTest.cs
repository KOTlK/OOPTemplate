using UnityEngine;
using System.Text;
using System.IO;

using static Locale;
using static Locale.TokenType;

public class LocaleTest : MonoBehaviour {
    public int KeySpacesCount = 20;
    public int StringSpacesCount = 8;

    private void Awake() {
        var err = Locale.LoadLocalization("eng");

        Debug.Log(err);

        Debug.Log(Locale.Get(-5234));

        var sb = new StringBuilder();
        var count = 0;

        foreach (var token in Tokens) {
            if (token.Type == Ident) {
                count++;
            }
        }

        Debug.Log(count);

        // for(var i = 0; i < Tokens.Count; ++i) {
        //     var token = Tokens[i];

        //     switch(token.Type) {
        //         case TokenType.Ident : {
        //             sb.Append(token.Ident.ToString());
        //         } break;
        //         case TokenType.Separator : {
        //             var key = Tokens[i-1].Ident.ToString();
        //             var len = key.Length;
        //             len = KeySpacesCount - len;
        //             sb.Append(' ', len);
        //             sb.Append((char)TokenType.Separator);
        //             sb.Append(' ', StringSpacesCount);
        //         } break;
        //         case TokenType.String : {
        //             sb.Append('"');
        //             for(var j = 0; j < token.String.Length; ++j) {
        //                 if(token.String[j] == '\n') {
        //                     sb.Append('\\');
        //                     sb.Append('n');
        //                     continue;
        //                 }

        //                 sb.Append(token.String[j]);
        //             }
        //             sb.Append('"');
        //             sb.Append('\n');
        //         } break;
        //         case TokenType.SingleCommentary : {
        //             sb.Append("//");
        //             sb.Append(token.String);
        //             sb.Append('\n');
        //         } break;
        //         case TokenType.MultiCommentary : {
        //             sb.Append("/*");
        //             sb.Append(token.String);
        //             sb.Append("*/");
        //             sb.Append('\n');
        //         } break;
        //     }
        // }

        // var path = $"{Application.streamingAssetsPath}/Localization/recreated.loc";

        // if(File.Exists(path)) {
        //     File.Delete(path);
        // }

        // var file = File.CreateText(path);
        // file.Write(sb.ToString());
        // file.Close();
    }
}