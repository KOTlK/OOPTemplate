using System;
using System.Collections.Generic;

using static Assertions;

public delegate void EventListener<T>(T evnt);

public unsafe class EventQueue {
    // C# dynamic dispatch is awful
    public Dictionary<Type, Delegate> Listeners = new();

    public void Clear() {
        Listeners.Clear();
    }

    public void RaiseEvent<T> (T evnt) {
        var type = typeof(T);

        if(Listeners.TryGetValue(type, out var list)) {
            (list as EventListener<T>)?.Invoke(evnt);
        }
    }

    public void Subscribe<T>(EventListener<T> listener) {
        var type = typeof(T);

        if(Listeners.ContainsKey(type) == false) {
            Listeners[type] = listener;
        } else {
            Listeners[type] = (EventListener<T>)Listeners[type] + listener;
        }
    }

    public void Unsubscribe<T>(EventListener<T> listener) {
        var type = typeof(T);

        if(Listeners.ContainsKey(type)) {
            Listeners[type] = (EventListener<T>)Listeners[type] - listener;
        }
    }
}

public static class Events {
    public static EventQueue                     GeneralQueue;
    public static Dictionary<string, EventQueue> PrivateQueues;

    public static void Init() {
        GeneralQueue = new();
        GeneralQueue.Clear();

        PrivateQueues = new();
        foreach(var (type, queue) in PrivateQueues) {
            queue.Clear();
        }
    }

    public static void RaiseGeneral<T>(T evnt) {
        GeneralQueue.RaiseEvent(evnt);
    }

    public static void SubGeneral<T>(EventListener<T> listener) {
        GeneralQueue.Subscribe<T>(listener);
    }

    public static void UnsubGeneral<T>(EventListener<T> listener) {
        GeneralQueue.Unsubscribe<T>(listener);
    }

    public static void RaisePrivate<T>(string name, T evnt) {
        if(PrivateQueues.ContainsKey(name) == false) {
            PrivateQueues.Add(name, new EventQueue());
        }

        PrivateQueues[name].RaiseEvent(evnt);
    }

    public static void SubPrivate<T>(string name, EventListener<T> listener) {
        if(PrivateQueues.ContainsKey(name) == false) {
            PrivateQueues.Add(name, new EventQueue());
        }

        PrivateQueues[name].Subscribe<T>(listener);
    }

    public static void UnsubPrivate<T>(string name, EventListener<T> listener) {
        PrivateQueues[name].Unsubscribe<T>(listener);
    }
}