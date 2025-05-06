using System.IO;
using UnityEngine;
using System;
using System.Reflection;

/* Vars example, config file example: Assets/Text/Vars.txt
public struct Volume{
    public float Music;
    public float Sound;
    public float Voice;
}

public struct Player{
    public bool IsInvinsible;
}

public static class Vars{
    public static float  SomeFloatData;
    public static int    SomeInt32Data;
    public static Volume Volume;
    public static Player Player;
    ...
}
*/

public static class Vars {

    public static void ParseVars(TextAsset asset){
        var text               = asset.text;
        var lines              = text.Split('\n');
        var fields             = typeof(Vars).GetFields();
        var currentSubOption   = "";
        object currentInstance = null;
        FieldInfo[] subOptionFields = null;
        var charBuffer              = new char[512];
        var length                  = 0;

        void push(char c) {
            charBuffer[length++] = c;
        }

        string flush() {
            var str = new string(charBuffer, 0, length);
            length  = 0;
            return str;
        }

        (string, int) parseName(string l, int len) {
            for(var i = 0; i < len; ++i) {
                if(l[i] == ' ') {
                    return (flush(), i);
                } else {
                    push(l[i]);
                }
            }

            return (flush(), len);
        }

        string parseValue(string l, int start, int len) {
            for(var i = start; i < len; ++i) {
                if(l[i] != ' ') {
                    push(l[i]);
                }
            }

            return flush();
        }

        foreach(var line in lines) {
            if(String.IsNullOrEmpty(line)){
                continue;
            }

            line.Trim();

            if(line.StartsWith(';'))
                continue;

            if(line.StartsWith('[') && line.EndsWith(']')) {
                if(currentInstance != null){
                    typeof(Vars).GetField(currentSubOption).SetValue(null, currentInstance);
                }
                var subLine = line.Substring(1, line.Length - 2);

                currentSubOption = subLine;

                subOptionFields = Type.GetType(currentSubOption).GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach(var field in fields) {
                    if(field.FieldType.ToString() == currentSubOption) {
                        currentInstance = field.GetValue(null);
                        break;
                    }
                }
                continue;
            }

            var (name, index) = parseName(line, line.Length);
            var value         = parseValue(line, index, line.Length);

            if(subOptionFields == null) {
                foreach(var field in fields) {
                    if(field.Name == name) {
                        SetValueByType(field, null, value);
                    }
                }
            }else {
                foreach(var field in subOptionFields) {
                    if(field.Name == name) {
                        SetValueByType(field, currentInstance, value);
                        break;
                    }
                }
            }

        }

        if(currentInstance != null) {
            foreach(var field in fields) {
                if(field.FieldType.ToString() == currentSubOption) {
                    field.SetValue(null, currentInstance);
                    break;
                }
            }
        }
    }

    private static void SetValueByType(FieldInfo field, object instance, string value) {
        Debug.Log($"set {field.Name} {value}");
        //if you need more types, add more cases and implement parsing for it
        switch(field.FieldType.ToString()){
            case "System.Boolean":{
                if(value.ToLower() == "true"){
                    field.SetValue(instance, true);
                }else if(value.ToLower() == "false"){
                    field.SetValue(instance, false);
                }
            }
            break;

            case "System.Single":{
                field.SetValue(instance, Single.Parse(value));
            }
            break;

            case "System.Int32":{
                field.SetValue(instance, Int32.Parse(value));
            }
            break;

            default:
                Debug.LogError($"Cannot set field with type: {field.FieldType.ToString()}");
                break;
        }
    }
}