using System;
using System.Runtime.CompilerServices;

using static Assertions;

public class ComponentTable<T> : IComponentTable
where T : struct {
    public T[]    Dense;
    public uint[] Sparse;
    public uint[] EntityByComponentId;
    public uint   Length;
    public uint   Count;
    public uint   ComponentId;
    public Type   ComponentType;

    private const int InitialLength = 256;
    private const uint ResizeStep   = 128;

    public ComponentTable(uint componentId) {
        Dense         = null;
        Dense         = new T[InitialLength];
        Sparse        = null;
        Sparse        = new uint[InitialLength];
        EntityByComponentId = null;
        EntityByComponentId = new uint[InitialLength];
        Length        = InitialLength;
        Count         = 1; //                  0 is reserved
        ComponentId   = componentId;
        ComponentType = typeof(T);
    }

    public uint GetComponentId()     => ComponentId;
    public uint  GetComponentsCount() => Count;
    public Type GetComponentType()   => ComponentType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Add(uint entity, T component = default(T)) {
        if (entity >= Sparse.Length) {
            ResizeEntities(entity + ResizeStep);
        }

        Assert(Has(entity) == false, $"Component {typeof(T)} already attached to entity {entity}!");
        var id = Count++;

        if (Count >= Length) {
            Resize(Count + ResizeStep);
        }

        Dense[id]               = component;
        Sparse[entity]          = id;
        EntityByComponentId[id] = entity;

        return ref Dense[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(uint entity) {
        Assert(Has(entity), $"Component {typeof(T)} is not attached to entity {entity}!");
        var index      = Sparse[entity];
        var last       = Count - 1;
        var lastEntity = EntityByComponentId[last];

        Dense[index]               = Dense[last];
        EntityByComponentId[index] = lastEntity;
        Sparse[lastEntity]         = index;
        EntityByComponentId[last]  = 0;
        Sparse[entity]             = 0;
        Count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(uint h) {
        Assert(Has(h),
               $"Component {typeof(T)} is not attached to entity {h}!");
        return ref Dense[Sparse[h]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(uint h, ref T o) {
        if (Has(h) == false) {
            return false;
        }

        o = Dense[Sparse[h]];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(uint entity) {
        return entity < Sparse.Length && Sparse[entity] != 0;
    }

    private void Resize(uint length) {
        Array.Resize(ref Dense, (int)length);
        Array.Resize(ref EntityByComponentId, (int)length);
        Length = length;
    }

    private void ResizeEntities(uint length) {
        Array.Resize(ref Sparse, (int)length);
    }
}