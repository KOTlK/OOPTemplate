using System;
using System.Collections.Generic;

using static Assertions;

// Event handler should return true if event is consumed so the other handlers can't use it.
public delegate bool UIEventHandler<T>(T evnt);

public static class UIEvents {
    public static Dictionary<Type, List<Delegate>> Handlers;

    static UIEvents() {
        Handlers = new Dictionary<Type, List<Delegate>>();
        Handlers.Clear();
    }

    public static void SubUIHandler<T>(UIEventHandler<T> handler) {
        var type = typeof(T);

        if (Handlers.ContainsKey(type) == false) {
            Handlers[type] = new List<Delegate>();
        }

        var list = Handlers[type];

        Assert(list.Contains(handler) == false, "The event list already contains the handler.");

        list.Add(handler);
    }

    public static void UnsubUIHandler<T>(UIEventHandler<T> handler) {
        var type = typeof(T);
        Assert(Handlers.ContainsKey(type), "Event type is not presented in the dictionary.");

        var list = Handlers[type];

        Assert(list.Contains(handler), "Handler is not presented in the list.");

        list.Remove(handler);
    }

    public static void RaiseUIEvent<T>(T evnt) {
        var type = typeof(T);

        if (Handlers.ContainsKey(type)) {
            var list = Handlers[type];

            foreach(var handler in list) {
                if (((UIEventHandler<T>)handler).Invoke(evnt)) break;
            }
        }
    }
}