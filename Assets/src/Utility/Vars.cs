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
    public static Volume Volume;
    public static Player Player;
    ...
}
*/

public static class Vars{
    
    public static void ParseVars(TextAsset asset){
        var text               = asset.text;
        var lines              = text.Split('\n');
        var fields             = typeof(Vars).GetFields();
        var currentSubOption   = "";
        object currentInstance = null;
        FieldInfo[] subOptionFields = null;
        
        foreach(var line in lines){
            if(String.IsNullOrEmpty(line)){
                continue;
            }
            
            line.Trim();
            
            if(line.StartsWith(';'))
                continue;
            
            if(line.StartsWith('[') && line.EndsWith(']')){
                if(currentInstance != null){
                    typeof(Vars).GetField(currentSubOption).SetValue(null, currentInstance);
                }
                var subLine = line.Substring(1, line.Length - 2);
                
                currentSubOption = subLine;
                
                subOptionFields = Type.GetType(currentSubOption).GetFields(BindingFlags.Public | BindingFlags.Instance);
                
                var varsFields = typeof(Vars).GetFields();
                
                foreach(var field in varsFields){
                    if(field.Name == currentSubOption){
                        currentInstance = field.GetValue(null);
                        break;
                    }
                }
                continue;
            }

            var words = line.Split(' ');
            
            foreach(var field in subOptionFields){
                if(field.Name == words[0]){
                    SetValueByType(field, currentInstance, words[1]);
                    break;
                }
            }
        }
        
        if(currentInstance != null){
            typeof(Vars).GetField(currentSubOption).SetValue(null, currentInstance);
        }
    }
    
    private static void SetValueByType(FieldInfo field, object instance, string value){
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