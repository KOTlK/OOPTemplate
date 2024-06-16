using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Assertions;

public class BoundedSpatialTable {
    public struct EntityReference {
        public Vector3 Position;
        public int     Id;
    }

    public Vector3Int Size;
    public EntityTable<Vector3> Positions;
    public EntityReference[]    EntityTable;
    public int[]   CellCount;
    public int     TableSize;
    public float   Spacing;

    public BoundedSpatialTable(Vector3Int size, float spacing) {
        Size        = size; 
        TableSize   = size.x * size.y * size.z;
        Spacing     = spacing;
        CellCount   = new int[TableSize + 1];
        EntityTable = new EntityReference[TableSize];
        Positions   = new EntityTable<Vector3>(TableSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEntity(int entity, Vector3 position) {
        Positions[entity] = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveEntity(int entity) {
        Positions.Remove(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePosition(int entity, Vector3 newPos) {
        Assert(Positions.ContainsKey(entity));
        Positions[entity] = newPos;
    }

    public void Rehash() {
        if(Positions.Count > TableSize + 1) {
            TableSize = Positions.Count;
            Array.Resize(ref CellCount, TableSize + 1);
            Array.Resize(ref EntityTable, TableSize);
        }
        Array.Clear(CellCount, 0, CellCount.Length);
        Array.Clear(EntityTable, 0, EntityTable.Length);

        foreach(var (entity, position) in Positions.Iterate()) {
            var hash = Hash(position);
            CellCount[hash]++;
        }

        for(var i = 1; i < TableSize + 1; ++i) {
            CellCount[i] += CellCount[i - 1];
        }

        foreach(var (entity, position) in Positions.Iterate()) {
            var hash = Hash(position);
            CellCount[hash]--;
            EntityTable[CellCount[hash]].Id       = entity;
            EntityTable[CellCount[hash]].Position = position;
        }
    }

    public int Query(Vector3 position, int[] result, float radius) {
        var count = 0;
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

                    for(var i = start ; i < end; ++i) {
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
    private int Hash(Vector3 pos) {
        return Math.Abs((Mathf.FloorToInt(pos.x / Spacing * Size.y + Mathf.FloorToInt(pos.y / Spacing))) * Size.z + Mathf.FloorToInt(pos.z / Spacing));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Hash(float x, float y, float z) {
        return Math.Abs((Mathf.FloorToInt(x / Spacing * Size.y + Mathf.FloorToInt(y / Spacing))) * Size.z + Mathf.FloorToInt(z / Spacing));
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
}