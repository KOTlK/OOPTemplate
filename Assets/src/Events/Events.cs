using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
#if !UNITY_EDITOR
using Unity.Collections.LowLevel.Unsafe;
#endif
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
#if UNITY_EDITOR
        Data       = MallocTracked(sizeof(T) * initialCapacity, AlignOf<T>(), allocator, 0);
#else
        Data       = Malloc(sizeof(T) * initialCapacity, AlignOf<T>(), allocator);
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free() {
#if UNITY_EDITOR
        FreeTracked(Data, Allocator);
#else
        UnsafeUtility.Free(Data, Allocator);
#endif
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
#if UNITY_EDITOR
            var newData = MallocTracked(sizeof(T) * Capacity, AlignOf<T>(), Allocator, 0);
#else
            var newData = Malloc(sizeof(T) * Capacity, AlignOf<T>(), Allocator);
#endif
            MemMove(newData, Data, sizeof(T) * Count);
            FreeTracked(Data, Allocator);
            Data = newData;
        }
        
        WriteArrayElement(Data, Count++, evnt);
    }
}

public class Events : IDisposable{
    public delegate void EventHandler(EventData events);
    public Dictionary<Type, EventData>           AllEvents = new();
    public Dictionary<Type, EventHandler>        Handlers = new();
    
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
        
        if(AllEvents.ContainsKey(type)){
            IsTrue(Handlers.ContainsKey(type));
            Handlers[type].Invoke(AllEvents[type]);
            AllEvents[type].Clear();
        }
    }
    
    public void AddHandler<T>(EventHandler handler)
    where T : unmanaged{
        var type = typeof(T);
        if(!Handlers.ContainsKey(type)){
            Handlers.Add(type, delegate{});
        }
        
        Handlers[typeof(T)] += handler;
    }
    
    public void RemoveHandler<T>(EventHandler handler)
    where T : unmanaged{
        var type = typeof(T);
        
        IsTrue(Handlers.ContainsKey(type));
        
        Handlers[type] -= handler;
    }
    
    public void Dispose(){
        foreach(var (type, events) in AllEvents){
            events.Free();
        }
    }
}