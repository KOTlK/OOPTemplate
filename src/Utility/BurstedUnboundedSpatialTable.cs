using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Assertions;

public class BurstedUnboundedSpatialTable : IDisposable { //
    public struct EntityReference {
        public Vector3 Position;
        public int Id;
    }
    
    public NativeEntityTable<Vector3> Positions;
    public NativeArray<int> CellCount;
    public NativeArray<EntityReference> EntityTable;
    public int   Size;
    public float Spacing;

    public BurstedUnboundedSpatialTable(int size, float spacing) {
        Size        = size;
        Spacing     = spacing;
        Positions   = new NativeEntityTable<Vector3>(size, Allocator.Persistent);
    }

    public void Dispose() {
        CellCount.Dispose();
        EntityTable.Dispose();
        Positions.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEntity(int entity, Vector3 position) {
        Positions.Add(entity, position);
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
        
        CellCount.Dispose();
        EntityTable.Dispose();
        
        CellCount = new NativeArray<int>(Size + 1, Allocator.TempJob);
        EntityTable = new NativeArray<EntityReference>(Size, Allocator.TempJob);

        var countCells = new CountCells {
            CellCount = CellCount,
            Positions = Positions,
            Spacing   = Spacing,
            Size      = Size
        };

        var countCellsHandle = countCells.Schedule(Positions.Length, default);
        countCellsHandle.Complete();
        
        var calculatePartialSum = new CalculatePartialSum {
            CellCount = CellCount,
            Size      = Size
        };

        calculatePartialSum.Run();

        var fillHashTable = new FillHashTable {
            CellCount = CellCount,
            Positions = Positions,
            EntityTable = EntityTable,
            Spacing     = Spacing,
            Size        = Size
        };

        var fillHashTableHandle = fillHashTable.Schedule(Positions.Length, default);
        fillHashTableHandle.Complete();
    }

    public unsafe int Query(Vector3 position, NativeArray<int> result, float radius) {
        var zero = 0;
        var queryJob = new QueryJob {
            CellCount = CellCount,
            EntityTable = EntityTable,
            Result = result,
            Spacing = Spacing,
            Size = Size,
            Count = &zero,
            Position = position,
            Radius = radius
        };

        queryJob.Run();

        return *queryJob.Count;
    }

    public unsafe void Query(NativeArray<Vector3> positions, NativeArray<float> rads, NativeArray<int> *results, NativeArray<int> counts) {
        Assert(positions.Length == rads.Length);
        Assert(positions.Length == counts.Length);
        var job = new QueryMultipleJob {
            CellCount = CellCount,
            EntityTable = EntityTable,
            Position = positions,
            Radius   = rads,
            Result   = results,
            Count    = counts,
            Spacing  = Spacing,
            Size     = Size
        };

        var handle = job.Schedule(positions.Length, 32);

        handle.Complete();
    }

    [BurstCompile]
    public struct CountCells : IJobFor {
        public NativeArray<int> CellCount;
        [ReadOnly] public NativeEntityTable<Vector3> Positions;
        public float Spacing;
        public int   Size;

        public void Execute(int i) {
            if(Positions.Items[i].Exist) {
                var hash = Hash(Positions.Items[i].Value);
                CellCount[hash]++;
            }
        }

        private int IntCoordinate(float coordinate) {
            return Mathf.FloorToInt(coordinate / Spacing);
        }

        private int Hash(Vector3 position) {
            return Mathf.Abs((IntCoordinate(position.x) * 92837111) ^ 
                            (IntCoordinate(position.y) * 689287499) ^ 
                            (IntCoordinate(position.z) * 283923481)) % Size;
        }
    }

    [BurstCompile]
    public struct CalculatePartialSum : IJob {
        public NativeArray<int> CellCount;
        public int Size;

        public void Execute() {
            for(var i = 1; i < Size; ++i) {
                CellCount[i] += CellCount[i - 1];
            }
        }
    }

    [BurstCompile]
    public struct FillHashTable : IJobFor {
        public NativeArray<int> CellCount;
        public NativeEntityTable<Vector3> Positions;
        public NativeArray<EntityReference> EntityTable;
        public float Spacing;
        public int   Size;

        public void Execute(int i) {
            if(Positions.Items[i].Exist) {
                var hash = Hash(Positions.Items[i].Value);
                CellCount[hash]--;
                EntityTable[CellCount[hash]] = new EntityReference {
                    Position = Positions.Items[i].Value,
                    Id = Positions.Items[i].Key
                };
            }
        }

        private int IntCoordinate(float coordinate) {
            return Mathf.FloorToInt(coordinate / Spacing);
        }

        private int Hash(Vector3 position) {
            return Mathf.Abs((IntCoordinate(position.x) * 92837111) ^ 
                            (IntCoordinate(position.y) * 689287499) ^ 
                            (IntCoordinate(position.z) * 283923481)) % Size;
        }
    }

    [BurstCompile]
    public unsafe struct QueryJob : IJob {
        public NativeArray<int> CellCount;
        public NativeArray<EntityReference> EntityTable;
        public NativeArray<int> Result;
        public Vector3 Position;
        public float   Radius;
        [NativeDisableUnsafePtrRestriction]
        public int *Count;
        public float Spacing;
        public int   Size;
        public void Execute() {
            var count = 0;
            var xmax = IntCoordinateSigned(Position.x + Radius);
            var ymax = IntCoordinateSigned(Position.y + Radius);
            var zmax = IntCoordinateSigned(Position.z + Radius);
            var xmin = IntCoordinateSigned(Position.x - Radius);
            var ymin = IntCoordinateSigned(Position.y - Radius);
            var zmin = IntCoordinateSigned(Position.z - Radius);

            for(var x = xmin; x <= xmax; ++x) {
                for(var y = ymin; y <= ymax; ++y) {
                    for(var z = zmin; z <= zmax; ++z) {
                        var hash  = Hash(x, y, z);
                        var start = CellCount[hash];
                        var end   = CellCount[hash + 1];

                        for(var i = start; i < end; ++i) {
                            if(count == Result.Length) {
                                return;
                            }

                            if(Vector3.Distance(EntityTable[i].Position, Position) <= Radius) {
                                Result[count++] = EntityTable[i].Id;
                            }
                        }
                    }
                }
            }

            *Count = count;
        }
        
        private int IntCoordinateSigned(float coordinate) {
            var sign = Math.Sign(coordinate);

            if(sign > 0) {
                return Mathf.RoundToInt(coordinate / Spacing);
            }
            else {
                return Mathf.FloorToInt(coordinate / Spacing);
            }
        }
        
        private int Hash(int x, int y, int z) {
            return Mathf.Abs((x * 92837111) ^ 
                             (y * 689287499) ^ 
                             (z * 283923481)) % Size;
        }
    }

    [BurstCompile]
    public unsafe struct QueryMultipleJob : IJobParallelFor {
        [ReadOnly] public NativeArray<int> CellCount;
        [ReadOnly] public NativeArray<EntityReference> EntityTable;
        [ReadOnly] public NativeArray<Vector3> Position;
        [ReadOnly] public NativeArray<float>   Radius;
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<int> *Result;
        [WriteOnly] public NativeArray<int>     Count;
        public float Spacing;
        public int   Size;

        public void Execute(int index) {
            var count = 0;
            var xmax = IntCoordinateSigned(Position[index].x + Radius[index]);
            var ymax = IntCoordinateSigned(Position[index].y + Radius[index]);
            var zmax = IntCoordinateSigned(Position[index].z + Radius[index]);
            var xmin = IntCoordinateSigned(Position[index].x - Radius[index]);
            var ymin = IntCoordinateSigned(Position[index].y - Radius[index]);
            var zmin = IntCoordinateSigned(Position[index].z - Radius[index]);

            for(var x = xmin; x <= xmax; ++x) {
                for(var y = ymin; y <= ymax; ++y) {
                    for(var z = zmin; z <= zmax; ++z) {
                        var hash  = Hash(x, y, z);
                        var start = CellCount[hash];
                        var end   = CellCount[hash + 1];

                        for(var i = start; i < end; ++i) {
                            if(count == Result[index].Length) {
                                return;
                            }

                            if(Vector3.Distance(EntityTable[i].Position, 
                                                Position[index]) <= Radius[index]) {
                                Result[index][count++] = EntityTable[i].Id;
                            }
                        }
                    }
                }
            }

            Count[index] = count;
        }
        
        private int IntCoordinateSigned(float coordinate) {
            var sign = Math.Sign(coordinate);

            if(sign > 0) {
                return Mathf.RoundToInt(coordinate / Spacing);
            }
            else {
                return Mathf.FloorToInt(coordinate / Spacing);
            }
        }
        
        private int Hash(int x, int y, int z) {
            return Mathf.Abs((x * 92837111) ^ 
                             (y * 689287499) ^ 
                             (z * 283923481)) % Size;
        }
    }
}