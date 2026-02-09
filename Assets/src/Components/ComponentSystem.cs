using System.Collections.Generic;
using System;

using static Assertions;

public static class ComponentSystem<T>
where T : struct {
    public delegate void UpdateFunc(T[] components, int count);

    public static T[]        Components;
    public static int        Length;
    public static int        Count;
    public static UpdateFunc Func;

    public static int[]          ComponentIdByHandleId;
    public static EntityHandle[] HandleByComponentId;

    private const int InitialLength = 256;

    public static void Make(UpdateFunc func = null) {
        Func        = func;
        Components  = null;
        Components  = new T[InitialLength];
        Length      = InitialLength;
        Count       = 1;
        ComponentIdByHandleId = null;
        ComponentIdByHandleId = new int[InitialLength];
        HandleByComponentId   = null;
        HandleByComponentId   = new EntityHandle[InitialLength];
    }

    public static void Update() {
        Func(Components, Count);
    }

    public static void Add(EntityHandle h, T component = default(T)) {
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

    public static ref T Get(EntityHandle h) {
        Assert(Has(h),
               $"Component {typeof(T)} is not attached to entity {h.Id}!");
        return ref Components[ComponentIdByHandleId[h.Id]];
    }

    public static bool TryGet(EntityHandle h, ref T o) {
        if (Has(h) == false) {
            return false;
        }

        o = Components[ComponentIdByHandleId[h.Id]];
        return true;
    }

    public static bool Has(EntityHandle h) {
        return h.Id < ComponentIdByHandleId.Length && ComponentIdByHandleId[h.Id] != 0;
    }

    public static void Remove(EntityHandle h) {
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

    public static void Remove(int id) {
        var h = HandleByComponentId[id];
        Remove(h);
    }

    public static EntityHandle GetHandle(int id) {
        return HandleByComponentId[id];
    }

    public static IEnumerable<T> Iterate() {
        for (var i = 1; i < Count; ++i) {
            yield return Components[i];
        }
    }

    private static void Resize(int length) {
        Array.Resize(ref Components, length);
        Array.Resize(ref HandleByComponentId, length);
        Length = length;
    }

    private static void ResizeEntities(uint length) {
        Array.Resize(ref ComponentIdByHandleId, (int)length);
    }
}