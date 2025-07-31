using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using static Assertions;

public struct NativeEntityTable<T> : IDisposable
where T : unmanaged {
    public struct KeyValue {
        public T    Value;
        public int  Key;
        public bool Exist;
    }

    public NativeArray<KeyValue> Items;
    public int        Length;
    public int        Count;

    private Allocator _allocator;

    public NativeEntityTable(Allocator allocator) {
        Items  = new NativeArray<KeyValue>(100, allocator);
        Length = 100;
        Count = 0;
        _allocator = allocator;
    }

    public NativeEntityTable(int startLength, Allocator allocator) {
        Items  = new NativeArray<KeyValue>(startLength, allocator);
        Length = startLength;
        Count = 0;
        _allocator = allocator;
    }

    public T this[int key] {
        get {
            return Get(key);
        }

        set {
            if(key >= Length) {
                Resize(key << 1);
                Items[key] = new KeyValue{
                    Value = value,
                    Key   = key, 
                    Exist = true
                };
                Count++;
            } else {
                if(Items[key].Exist) {
                    Items[key] = new KeyValue{
                        Value = value,
                        Key   = key,
                        Exist = true
                    };
                } else {
                    Items[key] = new KeyValue{
                        Value = value,
                        Key   = key, 
                        Exist = true
                    };
                    Count++;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int key, T value) {
        if(key >= Length) {
            Resize(key << 1);
        }

        if(Items[key].Exist == false) {
            Items[key] = new KeyValue {
                Value = value,
                Key   = key, 
                Exist = true
            };

            Count++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int key) {
        Assert(key < Length);
        if(Items[key].Exist) {
            Items[key] = new KeyValue {
                Value = default(T),
                Key   = -1,
                Exist = false
            };
            Count--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(int key) {
        Assert(key < Length);
        return Items[key].Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(int key) {
        Assert(key < Length);
        return Items[key].Exist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Resize(int newSize) {
        var newArray = new NativeArray<KeyValue>(newSize, _allocator);
        for(var i = 0; i < Length; ++i) {
            newArray[i] = Items[i];
        }
        Items.Dispose();
        Items = newArray;
        Length = newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<(int, T)> Iterate() {
        for(var i = 0; i < Length; ++i) {
            if(Items[i].Exist) {
                yield return (Items[i].Key, Items[i].Value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<int> Keys() {
        for(var i = 0; i < Length; ++i) {
            if(Items[i].Exist) {
                yield return Items[i].Key;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> Values() {
        for(var i = 0; i < Length; ++i) {
            if(Items[i].Exist) {
                yield return Items[i].Value;
            }
        }
    }

    public void Dispose() {
        Items.Dispose();
    }
}