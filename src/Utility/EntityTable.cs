using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using static Assertions;

public class EntityTable<T> : IDisposable {
    public struct KeyValue {
        public T    Value;
        public uint Key;
        public bool Exist;
    }

    public KeyValue[] Items;
    public uint       Length;
    public uint       Count;

    public EntityTable() {
        Items  = new KeyValue[100];
        Length = 100;
        Count = 0;
    }

    public EntityTable(uint startLength) {
        Items  = new KeyValue[startLength];
        Length = startLength;
        Count = 0;
    }

    public void Dispose() {
        for(var i = 0; i < Length; ++i) {
            Items[i] = new KeyValue {
                Value = default(T),
                Key   = 0,
                Exist = false
            };
        }
        Count = 0;
    }

    public T this[uint key] {
        get {
            return Get(key);
        }

        set {
            if(key >= Length) {
                Resize(key << 1);
                Items[key].Key   = key;
                Items[key].Value = value;
                Items[key].Exist = true;
                Count++;
            } else {
                if(Items[key].Exist) {
                    Items[key].Value = value;
                } else {
                    Items[key].Key   = key;
                    Items[key].Value = value;
                    Items[key].Exist = true;
                    Count++;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint key, T value) {
        if(key >= Length) {
            Resize(key << 1);
        }

        Items[key].Key   = key;
        Items[key].Value = value;
        Items[key].Exist = true;
        Count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(uint key) {
        Assert(key < Length);
        if(Items[key].Exist) {
            Items[key].Exist = false;
            Count--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(uint key) {
        Assert(key < Length);
        return Items[key].Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(uint key) {
        Assert(key < Length);
        return Items[key].Exist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Resize(uint newSize) {
        ArrayUtils.Resize(ref Items, newSize);
        Length = newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<(uint, T)> Iterate() {
        for(var i = 0; i < Length; ++i) {
            if(Items[i].Exist) {
                yield return (Items[i].Key, Items[i].Value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<uint> Keys() {
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
}