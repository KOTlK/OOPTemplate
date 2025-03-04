using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using static Assertions;

public delegate void EventListener(IEvent evnt);

public interface IEvent {
}

public class EventQueue {
    public Queue<IEvent>                   Queue = new();
    public Dictionary<Type, EventListener> Listeners = new();

    public void Clear() {
        Queue.Clear();
        foreach(var (type, listener) in Listeners) {
            Listeners[type] = delegate {};
        }
    }

    public void RaiseEvent<T> (T evnt) 
    where T : IEvent {
        Queue.Enqueue(evnt);
    }

    public void Subscribe<T>(EventListener listener) 
    where T : IEvent {
        var type = typeof(T);
        
        if(Listeners.ContainsKey(type) == false) {
            Listeners[type] = delegate {};
        }

        Listeners[type] += listener;
    }

    public void Unsubscribe<T>(EventListener listener) 
    where T : IEvent {
        var type = typeof(T);
        Listeners[type] -= listener;
    }

    public void Execute() {
        while(Queue.Count > 0) {
            var evnt = Queue.Dequeue();
            var type = evnt.GetType();

            Listeners[type](evnt);
        }
    }
}

public static class Events {
    public static EventQueue GeneralQueue = new();

    public static Dictionary<Type, EventQueue> PrivateQueues = new();

    public static void Init() {
        GeneralQueue.Clear();

        foreach(var (type, queue) in PrivateQueues) {
            queue.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RaiseGeneralEvent<T> (T evnt) 
    where T : IEvent {
        GeneralQueue.RaiseEvent(evnt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SubscribeToGeneral<T>(EventListener listener) 
    where T : IEvent {
        GeneralQueue.Subscribe<T>(listener);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnsubscribeFromGeneral<T>(EventListener listener) 
    where T : IEvent {
        GeneralQueue.Unsubscribe<T>(listener);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteGeneral() {
        GeneralQueue.Execute();
    }

    public static void RaisePrivateEvent<T, U>(U evnt) 
    where U : IEvent {
        var queueType = typeof(T);

        if(PrivateQueues.ContainsKey(queueType) == false) {
            PrivateQueues.Add(queueType, new EventQueue());
        }

        PrivateQueues[queueType].RaiseEvent(evnt);
    }

    public static void SubscribeToPrivate<T, U>(EventListener listener) 
    where U : IEvent {
        var queueType = typeof(T);

        if(PrivateQueues.ContainsKey(queueType) == false) {
            PrivateQueues.Add(queueType, new EventQueue());
        }

        PrivateQueues[queueType].Subscribe<U>(listener);
    }

    public static void UnsubscribeFromPrivate<T, U>(EventListener listener) 
    where U : IEvent {
        var queueType = typeof(T);
        PrivateQueues[queueType].Unsubscribe<U>(listener);
    }

    public static void ExecutePrivateQueue<T>() {
        Assert(PrivateQueues.ContainsKey(typeof(T)));
        PrivateQueues[typeof(T)].Execute();
    }

    public static void ExecuteAll() {
        GeneralQueue.Execute();

        foreach(var (_, queue) in PrivateQueues) {
            queue.Execute();
        }
    }
}