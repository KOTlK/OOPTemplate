using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommandAttribute : Attribute {
    public string[] Variants;

    public ConsoleCommandAttribute() {
        Variants = null;
    }

    public ConsoleCommandAttribute(params string[] variants) {
        Variants = variants;
    }
}

public class Console : MonoBehaviour {
    public enum CommandType {
        AutoRegister,
        UserRegister
    }

    public struct Command {
        public CommandType Type;
        public MethodInfo  Method;
        public object      Obj;

        public Command(MethodInfo method, object obj) {
            Type = CommandType.UserRegister;
            Method = method;
            Obj = obj;
        }
    }

    public KeyCode OpenKey = KeyCode.BackQuote;
    public KeyCode Submit  = KeyCode.Return;
    public float OpenSpeed = 2f;
    public float ConsoleHeight = 0.5f;
    public float InputHeight = 0.05f;
    public float HistoryLineHeight = 0.05f;
    public float LineOffsetFromInput = 0.01f;

    public bool  Active = false;
    public bool  InputActive = true;
    public float OpenProgress = 0f;

    public string InputText = "";

    public string[] InputStack = new string[128];
    public int      InputStackCount = 0;
    public int      StartLineToDraw;

    public uint SelectedEntity = 0;
    public Rect InputRect = new();

    public static Dictionary<string, Command> Commands;

    private void Awake() {
        Commands = new Dictionary<string, Command>();
        Commands.Clear();
        var types = typeof(ConsoleCommandAttribute).Assembly.GetTypes();

        foreach(var type in types) {
            var methods = type.GetMethods();

            foreach(var method in methods) {
                if(method.GetCustomAttributes<ConsoleCommandAttribute>().Any()) {
                    var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();

                    Commands.Add(method.Name, new Command {
                        Type = CommandType.AutoRegister,
                        Method = method
                    });

                    if(attribute.Variants != null) {
                        for(var i = 0; i < attribute.Variants.Length; ++i) {
                            Commands.Add(attribute.Variants[i], new Command {
                                Type = CommandType.AutoRegister,
                                Method = method
                            });
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy() {
        Commands.Clear();
        Commands = null;
    }

    public static void RegisterCommand(string name, 
                                       MethodInfo method, 
                                       object obj,
                                       params string[] nameVariants) {
        if(Commands.ContainsKey(name)) {
            throw new Exception($"Command with name: {name} already exist");
        }
        Commands.Add(name, new Command(method, obj));

        if (nameVariants != null) {
            for(var i = 0; i < nameVariants.Length; ++i) {
                if(Commands.ContainsKey(nameVariants[i])) {
                    throw new Exception($"Command with name: {nameVariants[i]} already exist");
                }
                Commands.Add(nameVariants[i], new Command(method, obj));
            }
        }
    }

    public static void RegisterCommand<T>(string name, object reference, params string[] nameVariants) {
        RegisterCommand(name, typeof(T).GetMethod(name), reference, nameVariants);
    }

    public void PushToConsole(string str) {
        if(InputStackCount == InputStack.Length) {
            Array.Resize(ref InputStack, InputStackCount << 1);
        }

        StartLineToDraw = InputStackCount;

        InputStack[InputStackCount++] = str;
    }

    private void Update() {
        if(Input.GetKeyDown(OpenKey)) {
            if(Active) {
                Active = false;
            } else {
                Active = true;
                InputActive = true;
                if(OpenProgress <= 0f) {
                    OpenProgress += Time.deltaTime;
                }
            }
            InputText = "";
        }
        var mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y;

        if(Active && Input.GetKeyDown(KeyCode.Mouse0) && InputRect.Contains(mousePos) == false) {
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, float.MaxValue)) {
                if(hit.collider.gameObject.TryGetComponent<Entity>(out var e)) {
                    SelectedEntity = e.Handle.Id;
                } else {
                    SelectedEntity = 0;
                }
            }
        }
    }

    
    private void OnGUI() {
        var screenWidth  = Screen.width;
        var screenHeight = Screen.height;

        if(OpenProgress > 0f) {
            if(Active) {
                if(OpenProgress < 1f) {
                    OpenProgress += Time.deltaTime * OpenSpeed;
                    if(OpenProgress >= 1f) {
                        OpenProgress = 1f;
                    }    
                }
            } else {
                OpenProgress -= Time.deltaTime * OpenSpeed;
                if(OpenProgress <= 0f) {
                    OpenProgress = 0f;
                }
            }
            
            var height = ConsoleHeight * OpenProgress * screenHeight;
            var consoleRect = new Rect(0, 0, screenWidth, height);
            var inputHeight = screenHeight * InputHeight;
            var historyHeight = height - inputHeight;
            var historyLineHeight = screenHeight * HistoryLineHeight;
            var lineOffset = screenHeight * LineOffsetFromInput;
            GUI.BeginGroup(consoleRect); // whole console
            GUI.Box(consoleRect, GUIContent.none);
            var historyRect = new Rect(0, 0, screenWidth, historyHeight);
            GUI.BeginGroup(historyRect); // history
            //Draw history
            var currentHeight = historyRect.height - historyLineHeight - lineOffset;

            if(Input.mouseScrollDelta.y != 0) {
                if(consoleRect.Contains(Event.current.mousePosition)) {
                    var sign = Mathf.Sign(Input.mouseScrollDelta.y);
                    Debug.Log(sign);

                    StartLineToDraw -= (int)sign;

                    StartLineToDraw = Mathf.Clamp(StartLineToDraw, 0, InputStackCount - 1);
                }
            }

            for(var i = StartLineToDraw; i >= 0; --i) {
                GUI.Label(new Rect(0, currentHeight, screenWidth, historyLineHeight), InputStack[i]);
                currentHeight -= historyLineHeight;

                if(currentHeight <= 0) {
                    break;
                }
            }

            GUI.EndGroup(); // history
            GUI.SetNextControlName(nameof(InputText));
            InputRect = new Rect(0, height - inputHeight, screenWidth, inputHeight);
            InputText = GUI.TextField(InputRect, InputText);

            if(Input.GetKeyDown(KeyCode.Mouse0)) {
                if(InputRect.Contains(Event.current.mousePosition) == false) {
                    InputActive = false;
                }
            }

            if(InputActive) {
                GUI.FocusControl(nameof(InputText));
            }

            GUI.EndGroup(); // whole console

            if(SelectedEntity != 0) {
                GUI.Label(new Rect(screenWidth / 2, screenHeight - 50, screenWidth / 2, 50), SelectedEntity.ToString());
            }

            if(Active) {
                var e = Event.current;

                if(e.keyCode == Submit && string.IsNullOrEmpty(InputText) == false) {
                    ProcessCommand(InputText);
                    InputText = "";
                }

                if(InputActive == false && e.keyCode == KeyCode.BackQuote) {
                    Active = false;
                    InputText = "";
                }

                if(e.keyCode == KeyCode.Escape) {
                    InputActive = false;
                    Active = false;
                    InputText = "";
                }
            }
        }
    }

    private void ProcessCommand(string command) {
        var lines = command.Split(' ');

        var paramsCount = lines.Length - 1;

        if(Commands.ContainsKey(lines[0])) {
            var cmd = Commands[lines[0]];
            var parameters = cmd.Method.GetParameters();
            
            if(paramsCount != parameters.Length) {
                PushToConsole("Wrong parameters count");
                return;
            }

            object[] p = null;

            if(parameters.Length > 0) {
                p = new object[parameters.Length];
            }

            for(var i = 0; i < parameters.Length; ++i) {
                p[i] = ParseParameter(lines[i + 1], parameters[i]);
            }

            if(cmd.Type == CommandType.AutoRegister) {
                cmd.Method.Invoke(null, p);
            } else {
                cmd.Method.Invoke(cmd.Obj, p);
            }
        } else {
            PushToConsole($"There is no such command: {lines[0]}");
        }
    }

    private object ParseParameter(string value, ParameterInfo param) {
        switch(param.ParameterType.ToString()) {
            case "System.String" : {
                return value;
            }
            case "System.Int32" : {
                if(int.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.UInt32" : {
                if(uint.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.Int64" : {
                if(long.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.UInt64" : {
                if(ulong.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.Int16" : {
                if(short.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.UInt16" : {
                if(ushort.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.Byte" : {
                if(byte.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.SByte" : {
                if(sbyte.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.Boolean" : {
                if(bool.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.Single" : {
                if(float.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
            case "System.Double" : {
                if(double.TryParse(value, out var v)) {
                    return v;
                } else {
                    return null;
                }
            }
        }
        return null;
    }
}