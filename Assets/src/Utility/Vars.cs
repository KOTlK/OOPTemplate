using System.IO;
using UnityEngine;
using System;
using System.Reflection;

public static class Vars{
    //all the global variables goes here
    //example : public static bool PlayerInvisible
    //asset for this example will be look like this:
    //PlayerInvisible true
    //or
    //PlayerInvisible False
    
    public static void ParseVars(TextAsset asset){
        var text   = asset.text;
        var lines  = text.Split('\n');
        var fields = typeof(Vars).GetFields();
        
        foreach(var line in lines){
            if(String.IsNullOrEmpty(line)){
                continue;
            }

            var words = line.Split(' ');
            
            foreach(var field in fields){
                if(field.Name == words[0]){
                    SetValueByType(field, words[1]);
                    break;
                }
            }
        }
    }
    
    private static void SetValueByType(FieldInfo field, string value){
        //if you need more types, add more cases and implement parsing for it
        switch(field.FieldType.ToString()){
            case "System.Boolean":{
                if(value.ToLower() == "true"){
                    field.SetValue(null, true);
                }else if(value.ToLower() == "false"){
                    field.SetValue(null, false);
                }
            }
            break;
            
            case "System.Single":{
                field.SetValue(null, Single.Parse(value));
            }
            break;
            
            case "System.Int32":{
                field.SetValue(null, Int32.Parse(value));
            }
            break;
            
            default:
                Debug.LogError($"Cannot set field with type: {field.FieldType.ToString()}");
                break;
        }
    }
}