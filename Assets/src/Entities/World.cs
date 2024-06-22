using UnityEngine;
using System;
using System.Runtime.CompilerServices;

public class World : MonoBehaviour, IDisposable {
    public int   StartDynamicSize;
    public int   StartStaticSize;
    public float DynamicSpacing;
    public float StaticSpacing;
    public UnboundedSpatialTable DynamicEntities;
    public UnboundedSpatialTable StaticEntities;
    public bool StaticEntitiesDirty = false;

    public void Create() {
        DynamicEntities = new UnboundedSpatialTable(StartDynamicSize, DynamicSpacing);
        StaticEntities = new UnboundedSpatialTable(StartStaticSize, StaticSpacing);
    }

    public void Dispose() {
        DynamicEntities.Dispose();
        StaticEntities.Dispose();
    }

    public void Execute() {
        if(StaticEntitiesDirty) {
            StaticEntities.Rehash();
            StaticEntitiesDirty = false;
        }

        DynamicEntities.Rehash();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDynamicEntity(int id, Vector3 position) {
        DynamicEntities.AddEntity(id, position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveDynamicEntity(int id) {
        DynamicEntities.RemoveEntity(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateDynamicEntityPosition(int id, Vector3 position) {
        DynamicEntities.UpdatePosition(id, position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddStaticEntity(int id, Vector3 position) {
        StaticEntities.AddEntity(id, position);
        StaticEntitiesDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveStaticEntity(int id) {
        StaticEntities.RemoveEntity(id);
        StaticEntitiesDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int QueryNearbyEntities(Vector3 position, int[] buffer, float radius, bool includeStatic = true) {
        var dynamicCount = DynamicEntities.Query(position, buffer, radius);

        if(dynamicCount < buffer.Length && includeStatic) {
            return StaticEntities.Query(position, buffer, radius, dynamicCount);
        }else {
            return dynamicCount;
        }
    }
}