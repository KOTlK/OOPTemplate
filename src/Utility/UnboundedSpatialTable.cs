using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using Unity.Profiling;
using static Assertions;
using static ArrayUtils;

public class UnboundedSpatialTable : IDisposable {
    public struct EntityReference {
        public float3 Position;
        public uint Id;
    }

    public struct Entity {
        public float3 Position;
        public long     Hash;
    }

    public EntityTable<Entity> Positions;
    public int[] CellCount;
    public EntityReference[] EntityTable;
    public uint   Size;
    public float Spacing;

    public UnboundedSpatialTable(uint size, float spacing) {
        Size        = size;
        Spacing     = spacing;
        CellCount   = new int[size + 1];
        EntityTable = new EntityReference[size];
        Positions   = new EntityTable<Entity>(size);
    }

    public void Dispose() {
        Positions.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEntity(uint entity, float3 position) {
        var e = new Entity();
        e.Position = position;
        e.Hash     = Hash(position);
        Positions[entity] = e;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveEntity(uint entity) {
        Positions.Remove(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePosition(uint entity, float3 newPos) {
        Assert(Positions.ContainsKey(entity), $"Table does not contain entity {entity}");
        var e = new Entity();
        e.Position = newPos;
        e.Hash     = Hash(newPos);
        Positions[entity] = e;
    }

    public void Rehash() {
        if(Positions.Count > Size + 1) {
            Size = Positions.Count;
            Resize(ref CellCount, Size + 1);
            Resize(ref EntityTable, Size);
        }

        Array.Clear(CellCount, 0, CellCount.Length);
        Array.Clear(EntityTable, 0, EntityTable.Length);

        foreach(var (id, entity) in Positions.Iterate()) {
            CellCount[entity.Hash]++;
        }

        for(var i = 1; i < CellCount.Length; ++i) {
            CellCount[i] += CellCount[i - 1];
        }

        foreach(var (id, entity) in Positions.Iterate()) {
            var hash = entity.Hash;
            CellCount[hash]--;
            EntityTable[CellCount[hash]].Id = id;
            EntityTable[CellCount[hash]].Position = entity.Position;
        }
    }

    public uint Query(float3 position, uint[] result, float radius, uint count = 0) {
        var len  = result.Length;
        var xmax = IntCoordinateSigned(position.x + radius);
        var ymax = IntCoordinateSigned(position.y + radius);
        var zmax = IntCoordinateSigned(position.z + radius);
        var xmin = IntCoordinateSigned(position.x - radius);
        var ymin = IntCoordinateSigned(position.y - radius);
        var zmin = IntCoordinateSigned(position.z - radius);

        for(var x = xmin; x <= xmax; ++x) {
            for(var y = ymin; y <= ymax; ++y) {
                for(var z = zmin; z <= zmax; ++z) {
                    var hash  = Hash(x, y, z);
                    var start = CellCount[hash];
                    var end   = CellCount[hash + 1];

                    for(var i = start; i < end; ++i) {
                        var e = EntityTable[i];
                        if(math.distance(e.Position, position) <= radius) {
                            result[count++] = e.Id;

                            if(count == len) {
                                return count;
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IntCoordinateSigned(float coordinate) {
        var sign = math.sign(coordinate);

        if(sign > 0) {
            return (int)math.round(coordinate / Spacing);
        }
        else {
            return (int)math.floor(coordinate / Spacing);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IntCoordinate(float coordinate) {
        return (int)math.floor(coordinate / Spacing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IntCoordinateRound(float coordinate) {
        return (int)math.round(coordinate / Spacing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long Hash(int x, int y, int z) {
        return math.abs((x * 9283) +
                        (y * 6892) +
                        (z * 2839)) % Size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long Hash(float3 position) {
        return math.abs((IntCoordinate(position.x) * 9283) +
                        (IntCoordinate(position.y) * 6892) +
                        (IntCoordinate(position.z) * 2839)) % Size;
    }
}