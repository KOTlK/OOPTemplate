using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static UnityEngine.Assertions.Assert;

public unsafe class EventData{
    public void*     Data;
    public int       Count;
    public int       Capacity;
    public Type      BindedType;
    public Allocator Allocator;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Create<T>(int initialCapacity, Allocator allocator)
    where T : unmanaged{
        Count      = 0;
        Capacity   = initialCapacity;
        Allocator  = allocator;
        BindedType = typeof(T);
        Data       = MallocTracked(sizeof(T) * initialCapacity, AlignOf<T>(), allocator, 0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free(){
        FreeTracked(Data, Allocator);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(int index)
    where T : unmanaged{
        return ReadArrayElement<T>(Data, index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByRef<T>(int index)
    where T : unmanaged{
        return ref ArrayElementAsRef<T>(Data, index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(){
        Count = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push<T>(T evnt)
    where T : unmanaged{
        IsTrue(typeof(T) == BindedType);
        if(Count >= Capacity){
            Capacity = Capacity << 1;
            var newData = MallocTracked(sizeof(T) * Capacity, AlignOf<T>(), Allocator, 0);
            MemMove(newData, Data, sizeof(T) * Count);
            FreeTracked(Data, Allocator);
            Data = newData;
        }
        
        WriteArrayElement(Data, Count++, evnt);
    }
}

public interface IEventHandler{
    void HandleEvents(EventData events);
}

public class Events : IDisposable{
    public Dictionary<Type, EventData>           AllEvents = new();
    public Dictionary<Type, List<IEventHandler>> Handlers = new();
    
    private readonly int _initialCapacity = 30;
    
    public Events(){
    }
    
    public Events(int initialCapacity){
        _initialCapacity = initialCapacity;
    }
    
    public void RaiseEvent<T>(T evnt)
    where T : unmanaged{
        var type = typeof(T);
        
        if(!AllEvents.ContainsKey(type)){
            var eventData = new EventData();
            eventData.Create<T>(_initialCapacity, Allocator.Persistent);
            AllEvents.Add(type, eventData);
        }
        
        AllEvents[type].Push<T>(evnt);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleEvents<T>()
    where T : unmanaged{
        var type = typeof(T);
        
        IsTrue(Handlers.ContainsKey(type));
        IsTrue(AllEvents.ContainsKey(type));
        
        foreach(var handler in Handlers[type]){
            handler.HandleEvents(AllEvents[type]);
        }
        AllEvents[type].Clear();
    }
    
    public void AddHandler<T>(IEventHandler handler)
    where T : unmanaged{
        var type = typeof(T);
        if(!Handlers.ContainsKey(type)){
            Handlers.Add(type, new List<IEventHandler>());
        }
        
        Handlers[typeof(T)].Add(handler);
    }
    
    public void RemoveHandler<T>(IEventHandler handler)
    where T : unmanaged{
        var type = typeof(T);
        
        IsTrue(Handlers.ContainsKey(type));
        
        Handlers[type].Remove(handler);
    }
    
    public void Dispose(){
        foreach(var (type, events) in AllEvents){
            events.Free();
        }
    }
}