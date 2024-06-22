using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Assertions;
using static ArrayUtils;

public class UnboundedSpatialTable : IDisposable {
    public struct EntityReference {
        public Vector3 Position;
        public uint Id;
    }
    
    public EntityTable<Vector3> Positions;
    public int[] CellCount;
    public EntityReference[] EntityTable;
    public uint   Size;
    public float Spacing;

    public UnboundedSpatialTable(uint size, float spacing) {
        Size        = size;
        Spacing     = spacing;
        CellCount   = new int[size + 1];
        EntityTable = new EntityReference[size];
        Positions   = new EntityTable<Vector3>(size);
    }

    public void Dispose() {
        Positions.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEntity(uint entity, Vector3 position) {
        Positions[entity] = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveEntity(uint entity) {
        Positions.Remove(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePosition(uint entity, Vector3 newPos) {
        Assert(Positions.ContainsKey(entity));
        if (Positions.ContainsKey(entity) == false) {
            Debug.Log("False");
        }
        Positions[entity] = newPos;
    }

    public void Rehash() {
        if(Positions.Count > Size + 1) {
            Size = Positions.Count;
            Resize(ref CellCount, Size + 1);
            Resize(ref EntityTable, Size);
        }
        Array.Clear(CellCount, 0, CellCount.Length);
        Array.Clear(EntityTable, 0, EntityTable.Length);

        foreach(var (entity, position) in Positions.Iterate()) {
            var hash = Hash(position);
            CellCount[hash]++;
        }

        for(var i = 1; i < CellCount.Length; ++i) {
            CellCount[i] += CellCount[i - 1];
        }

        foreach(var (entity, position) in Positions.Iterate()) {
            var hash = Hash(position);
            CellCount[hash]--;
            EntityTable[CellCount[hash]].Id = entity;
            EntityTable[CellCount[hash]].Position = position;
        }
    }

    public uint Query(Vector3 position, uint[] result, float radius, uint count = 0) {
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
                        if(count == result.Length) {
                            return count;
                        }

                        if(Vector3.Distance(EntityTable[i].Position, position) <= radius) {
                            result[count++] = EntityTable[i].Id;
                        }
                    }
                }
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IntCoordinateSigned(float coordinate) {
        var sign = Math.Sign(coordinate);

        if(sign > 0) {
            return Mathf.RoundToInt(coordinate / Spacing);
        }
        else {
            return Mathf.FloorToInt(coordinate / Spacing);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IntCoordinate(float coordinate) {
        return Mathf.FloorToInt(coordinate / Spacing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IntCoordinateRound(float coordinate) {
        return Mathf.RoundToInt(coordinate / Spacing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Hash(int x, int y, int z) {
        return (int)(Mathf.Abs((x * 92837111) ^ 
                         (y * 689287499) ^ 
                         (z * 283923481)) % Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Hash(Vector3 position) {
        return (int)(Mathf.Abs((IntCoordinate(position.x) * 92837111) ^ 
                         (IntCoordinate(position.y) * 689287499) ^ 
                         (IntCoordinate(position.z) * 283923481)) % Size);
    }
}