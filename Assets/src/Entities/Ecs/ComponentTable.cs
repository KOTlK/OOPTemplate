using System;
using System.Runtime.CompilerServices;

using static Assertions;

public class ComponentTable<T> : IComponentTable
where T : struct {
    public T[]        Components;
    public int        Length;
    public int        Count;
    public uint       ComponentId;
    public Type       ComponentType;

    public int[]          ComponentIdByHandleId;
    public EntityHandle[] HandleByComponentId;

    private const int InitialLength = 256;

    public ComponentTable(uint componentId) {
        Components  = null;
        Components  = new T[InitialLength];
        Length      = InitialLength;
        Count       = 1; // 0 is reserved
        ComponentIdByHandleId = null;
        ComponentIdByHandleId = new int[InitialLength];
        HandleByComponentId   = null;
        HandleByComponentId   = new EntityHandle[InitialLength];
        ComponentId           = componentId;
        ComponentType         = typeof(T);
    }

    public uint GetComponentId() => ComponentId;
    public int  GetComponentsCount() => Count;
    public Type GetComponentType() => ComponentType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(EntityHandle h, T component = default(T)) {
        if (h.Id >= ComponentIdByHandleId.Length) {
            ResizeEntities(h.Id + 1);
        }

        Assert(Has(h) == false, $"Component {typeof(T)} already attached to entity {h.Id}!");
        var cid = Count++;

        if (Count >= Length) {
            Resize((Count + 1) << 1);
        }

        Components[cid]             = component;
        ComponentIdByHandleId[h.Id] = cid;
        HandleByComponentId[cid]    = h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(EntityHandle h) {
        Assert(Has(h),
               $"Component {typeof(T)} is not attached to entity {h.Id}!");
        return ref Components[ComponentIdByHandleId[h.Id]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(uint h) {
        Assert(Has(h),
               $"Component {typeof(T)} is not attached to entity {h}!");
        return ref Components[ComponentIdByHandleId[h]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(EntityHandle h, ref T o) {
        if (Has(h) == false) {
            return false;
        }

        o = Components[ComponentIdByHandleId[h.Id]];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(uint h, ref T o) {
        if (Has(h) == false) {
            return false;
        }

        o = Components[ComponentIdByHandleId[h]];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(EntityHandle h) {
        return h.Id < ComponentIdByHandleId.Length && ComponentIdByHandleId[h.Id] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(uint h) {
        return h < ComponentIdByHandleId.Length && ComponentIdByHandleId[h] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(EntityHandle h) {
        Assert(Has(h), $"Component {typeof(T)} is not attached to entity {h.Id}!");
        var cid     = ComponentIdByHandleId[h.Id];
        var lastcid = Count - 1;
        var lastH   = HandleByComponentId[lastcid];

        Components[cid]                 = Components[lastcid];
        ComponentIdByHandleId[lastH.Id] = cid;
        ComponentIdByHandleId[h.Id]     = 0;
        HandleByComponentId[cid]        = lastH;
        HandleByComponentId[lastcid]    = default;
        Count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int id) {
        var h = HandleByComponentId[id];
        Remove(h);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle GetHandle(int id) {
        return HandleByComponentId[id];
    }

    private void Resize(int length) {
        Array.Resize(ref Components, length);
        Array.Resize(ref HandleByComponentId, length);
        Length = length;
    }

    private void ResizeEntities(uint length) {
        Array.Resize(ref ComponentIdByHandleId, (int)length);
    }
}