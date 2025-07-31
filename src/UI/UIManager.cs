/*
    Before using the module, initialize it with Init(canvas, parent)
    by default parent is canvas.
    Configure UI update frequency by changing UIFps field.
    Register dependencies with `RegisterDependencies` or `RegisterDependency` functions.

    To create ui elements, use MakeUIElement functions.
    Functions with string as first argument are loading asset from ResourceManager
    before making an element.

    To bake already existing element, use BakeUIElement function or
    attach UIBaker component to the element and it will automatically bake
    the element on Start.

    To bind already existing element with a name, use BindUIElement function or
    attach UIBinder component to the element and it will automatically bind
    the element on Start.

    To make unique ui element, use MakeUniqueUI<T>.
    Access unique element with GetUniqueUI<T>.

    Update ui by calling UpdateUI and UpdateLateUI.
*/

using System.Collections.Generic;
using UnityEngine;

using static Assertions;

public class Container {
    public Dictionary<string, object> Dependencies;

    public Container() {
        Dependencies = new Dictionary<string, object>();
        Dependencies.Clear();
    }

    public void Add(object dep) {
        var name = dep.GetType().FullName;
        Dependencies.Add(name, dep);
    }

    public void Add(string name, object dep) {
        Dependencies.Add(name, dep);
    }

    public T Resolve<T>() {
        var name = typeof(T).FullName;
        Assert(Dependencies.ContainsKey(name), $"Container does not contains the item {name}");
        return (T)Dependencies[name];
    }

    public T Resolve<T>(string name) {
        Assert(Dependencies.ContainsKey(name), $"Container does not contains the item {name}");
        return (T)Dependencies[name];
    }
}

public static class UIManager {
    public static uint        UIFps = 60;
    public static uint        Length;
    public static uint        MaxElement;
    public static UIElement[] Elements;
    public static List<uint>  DynamicElements;
    public static List<uint>  LateElements;
    public static Transform   Parent;
    public static Stack<uint> FreeSlots;
    public static Stack<uint> RemoveQueue;

    public static Canvas      ActiveCanvas;
    public static Dictionary<string, UIElement> BindedElements;
    public static Container Container;

    private static float Time;
    private static float LateTime;
    private static uint  InitialLength = 200;

    public static void Init(Canvas canvas, Transform parent = null) {
        Length          = InitialLength;
        MaxElement      = 1;
        Elements        = new UIElement[Length];
        DynamicElements = new List<uint>();
        LateElements    = new List<uint>();
        RemoveQueue     = new Stack<uint>();
        FreeSlots       = new Stack<uint>();
        BindedElements  = new Dictionary<string, UIElement>();
        Container       = new Container();
        Parent          = parent;
        ActiveCanvas    = canvas;
        DynamicElements.Clear();
        LateElements.Clear();
        RemoveQueue.Clear();
        FreeSlots.Clear();
        BindedElements.Clear();
        Object.DontDestroyOnLoad(ActiveCanvas);
    }

    public static void RegisterDependencies(params object[] dependencies) {
        foreach(var dep in dependencies) {
            Container.Add(dep);
        }
    }

    public static void RegisterDependency(object dep) {
        Container.Add(dep);
    }

    public static UIElement MakeUIElement(string asset) {
        return MakeUIElement(asset, Vector2.zero, Parent);
    }

    public static UIElement MakeUIElement(string asset, Vector2 pixelPos) {
        return MakeUIElement(asset, pixelPos, Parent);
    }

    public static UIElement MakeUIElement(string asset, Vector2 pixelPos, Transform parent) {
        var obj = ResourceManager.Instantiate<UIElement>(asset);
        obj.transform.SetParent(parent);
        obj.PixelPosition = pixelPos;
        BakeUIElement(obj);
        return obj;
    }

    public static UIElement MakeUIElement(UIElement prefab) {
        return MakeUIElement(prefab, Vector2.zero, Parent);
    }

    public static UIElement MakeUIElement(UIElement prefab, Vector2 pixelPos) {
        return MakeUIElement(prefab, pixelPos, Parent);
    }

    public static UIElement MakeUIElement(UIElement prefab, Vector2 pixelPos, Transform parent) {
        uint id = 0;

        if (FreeSlots.Count > 0) {
            id = FreeSlots.Pop();
        } else {
            id = MaxElement++;
        }

        var obj = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);

        obj.Id            = id;
        obj.Canvas        = ActiveCanvas;
        obj.Transform     = obj.GetComponent<RectTransform>();
        obj.PixelPosition = pixelPos;

        if ((obj.Flags & UIElementFlags.Dynamic) == UIElementFlags.Dynamic) {
            DynamicElements.Add(id);
        }

        if ((obj.Flags & UIElementFlags.UpdateLately) == UIElementFlags.UpdateLately) {
            LateElements.Add(id);
        }

        Elements[id] = obj;

        obj.ResolveDependencies(Container);
        obj.OnElementCreate();

        return obj;
    }

    public static uint BakeUIElement(UIElement element) {
        uint id = 0;

        if (FreeSlots.Count > 0) {
            id = FreeSlots.Pop();
        } else {
            id = MaxElement++;
        }

        element.Id        = id;
        element.Canvas    = ActiveCanvas;
        element.Transform = element.GetComponent<RectTransform>();

        if ((element.Flags & UIElementFlags.Dynamic) == UIElementFlags.Dynamic) {
            DynamicElements.Add(id);
        }

        if ((element.Flags & UIElementFlags.UpdateLately) == UIElementFlags.UpdateLately) {
            LateElements.Add(id);
        }

        Elements[id] = element;
        element.ResolveDependencies(Container);
        element.OnElementCreate();

        return id;
    }

    public static uint BindUIElement(UIElement element, string name) {
        uint id = BakeUIElement(element);
        BindedElements[name] = element;
        return id;
    }

    public static uint MakeUniqueUI<T>(T element)
    where T : UIElement {
        return BindUIElement(element, typeof(T).Name);
    }

    public static T GetUniqueUI<T>()
    where T : UIElement {
        var name = typeof(T).Name;
        Assert(BindedElements.ContainsKey(name));
        var element = BindedElements[name];
        return (T)element;
    }

    public static UIElement GetUIElement(string name) {
        Assert(BindedElements.ContainsKey(name));
        return BindedElements[name];
    }

    public static UIElement GetUIElement(uint id) {
        Assert(Elements[id] != null);
        return Elements[id];
    }

    public static void DestroyElement(uint id) {
        if (Elements[id] == null) return;

        RemoveQueue.Push(id);
    }

    public static void DestroyElement(UIElement element) {
        if (Elements[element.Id] == null) return;

        RemoveQueue.Push(element.Id);
    }

    public static void UpdateUI(float dt) {
        Time += dt;
        var delta = 1f / UIFps;

        while (Time >= delta) {
            var c = DynamicElements.Count;
            for (var i = 0; i < c; ++i) {
                Elements[DynamicElements[i]].UpdateElement(delta);
            }

            Time -= delta;
        }

        DestroyElements();
    }

    public static void UpdateLateUI(float dt) {
        LateTime += dt;
        var delta = 1f / UIFps;

        while (LateTime >= delta) {
            var c = LateElements.Count;
            for (var i = 0; i < c; ++i) {
                Elements[LateElements[i]].UpdateLate(delta);
            }

            LateTime -= delta;
        }
    }

    private static void DestroyElements() {
        while (RemoveQueue.Count > 0) {
            var id   = RemoveQueue.Pop();
            var elem = Elements[id];

            if ((elem.Flags & UIElementFlags.Dynamic) == UIElementFlags.Dynamic) {
                DynamicElements.Remove(id);
            }

            if ((elem.Flags & UIElementFlags.UpdateLately) == UIElementFlags.UpdateLately) {
                LateElements.Remove(id);
            }

            elem.OnElementDestroy();
            Object.Destroy(elem.gameObject);
            FreeSlots.Push(id);
            Elements[id] = null;
        }
    }
}