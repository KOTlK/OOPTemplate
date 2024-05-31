using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Assertions;

public class UnboundedSpaceTable {
    public Dictionary<int, Vector3> Positions = new();
    public int[] CellCount;
    public int[] EntityTable;
    public int   Size;
    public float Spacing;

    public UnboundedSpaceTable(int size, float spacing) {
        Size               = size;
        Spacing            = spacing;
        CellCount          = new int[size + 1];
        EntityTable        = new int[size];
    }

    public void AddEntity(int entity, Vector3 position) {
        Positions[entity] = position;
    }

    public void RemoveEntity(int entity) {
        Positions.Remove(entity);
    }

    public void UpdatePosition(int entity, Vector3 newPos) {
        Assert(Positions.ContainsKey(entity));
        Positions[entity] = newPos;
    }

    public void Rehash() {
        if(Positions.Count > Size + 1) {
            Size = Positions.Count;
            Array.Resize(ref CellCount, Size + 1);
            Array.Resize(ref EntityTable, Size);
        }
        Array.Clear(CellCount, 0, CellCount.Length);
        Array.Clear(EntityTable, 0, EntityTable.Length);

        foreach(var (entity, position) in Positions) {
            var hash = Hash(position);
            CellCount[hash]++;
        }

        for(var i = 1; i < CellCount.Length; ++i) {
            CellCount[i] += CellCount[i - 1];
        }

        foreach(var (entity, position) in Positions) {
            var hash = Hash(position);
            CellCount[hash]--;
            EntityTable[CellCount[hash]] = entity;
        }
    }

    public int Query(Vector3 position, int[] result) {
        var count = 0;
        var hash  = Hash(position);
        var start = CellCount[hash];
        var end   = CellCount[hash + 1];

        for(var i = start ; i < end; ++i) {
            if(count == result.Length) 
                break;

            result[count++] = EntityTable[i];
        }

        return count;
    }

    public int Query(Vector3 position, int[] result, float radius) {
        var count = 0;
        var xmax = IntCoordinateRound(position.x + radius);
        var ymax = IntCoordinateRound(position.y + radius);
        var zmax = IntCoordinateRound(position.z + radius);
        var xmin = IntCoordinate(position.x - radius);
        var ymin = IntCoordinate(position.y - radius);
        var zmin = IntCoordinate(position.z - radius);


        for(var x = xmin; x <= xmax; ++x) {
            for(var y = ymin; y <= ymax; ++y) {
                for(var z = zmin; z <= zmax; ++z) {
                    var hash  = Hash(x, y, z);
                    var start = CellCount[hash];
                    var end   = CellCount[hash + 1];

                    for(var i = start ; i < end; ++i) {
                        if(count == result.Length) 
                            break;

                        if(Vector3.Distance(Positions[EntityTable[i]], position) <= radius) {
                            result[count++] = EntityTable[i];
                        }
                    }
                }
            }
        }

        return count;
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
        return Mathf.Abs((x * 92837111) ^ 
                         (y * 689287499) ^ 
                         (z * 283923481)) % Size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Hash(Vector3 position) {
        return Mathf.Abs((IntCoordinate(position.x) * 92837111) ^ 
                         (IntCoordinate(position.y) * 689287499) ^ 
                         (IntCoordinate(position.z) * 283923481)) % Size;
    }
}