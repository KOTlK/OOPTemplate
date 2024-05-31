using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public struct MovedEntity {
    public int     Id;
    public Vector3 NewPosition;
}

public class EntityManager : MonoBehaviour {
    public UnboundedSpaceTable EntitiesTable;
    public int                 StartEntityTableSize = 100;
    public float               EntityTableSpacing   = 1f;
    public List<Entity>      BakedEntities;
    public PackedEntity[]    Entities        = new PackedEntity[128];
    public List<int>         DynamicEntities = new ();
    public List<MovedEntity> MovedEntities   = new ();
    public int[]             RemoveQueue     = new int[128];
    public int[]             FreeEntities    = new int[128];
    public int               MaxEntitiesCount;
    public int               FreeEntitiesCount;
    public int               EntitiesToRemoveCount;

    private void Awake() {
        EntitiesTable = new UnboundedSpaceTable(StartEntityTableSize, EntityTableSpacing);
    }
        
    public void BakeEntities() {
        for(var i = 0; i < BakedEntities.Count; ++i) {
            BakeEntity(BakedEntities[i]);
        }
        
        BakedEntities.Clear();
    }
    
    public void BakeEntity(Entity entity) {
        var id = -1;
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        
        if(MaxEntitiesCount == Entities.Length) {
            Array.Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id].Entity  = entity;
        Entities[id].Alive   = true;
        Entities[id].Type    = entity.Type;
        Entities[id].Manager = this;
        
        entity.Id          = id;
        entity.Em          = this;
        
        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
        }

        if((entity.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
            EntitiesTable.AddEntity(id, entity.transform.position);
            MovedEntities.Add(new MovedEntity{
                Id = id,
                NewPosition = entity.transform.position
            });
        }
        
        
        entity.OnCreate();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(Entity prefab, Vector3 position) {
        return CreateEntity(prefab, position, Quaternion.identity, Vector3.one, null);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T CreateEntity<T>(Entity prefab, Vector3 position)
    where T : Entity {
        return (T)CreateEntity(prefab, position);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(Entity prefab, 
                               Vector3 position, 
                               Quaternion orientation, 
                               Vector3 scale) {
        return CreateEntity(prefab, position, orientation, scale, null);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T CreateEntity<T>(Entity prefab, 
                             Vector3 position, 
                             Quaternion orientation, 
                             Vector3 scale)
    where T : Entity {
        return (T)CreateEntity(prefab, position, orientation, scale);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(Entity prefab, 
                               Vector3 position, 
                               Quaternion orientation, 
                               Vector3 scale, 
                               Transform parent) {
        var id = -1;
        
        if(FreeEntitiesCount > 0) {
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        
        var obj = Instantiate(prefab, position, orientation, parent);
        
        if(MaxEntitiesCount == Entities.Length) {
            Array.Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id].Entity = obj;
        Entities[id].Alive  = true;
        Entities[id].Type   = obj.Type;
        Entities[id].Manager = this;
        
        obj.Id          = id;
        obj.Em          = this;
        
        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
        }

        if((obj.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
            EntitiesTable.AddEntity(id, obj.transform.position);
            MovedEntities.Add(new MovedEntity{
                Id = id,
                NewPosition = obj.transform.position
            });
        }
        
        
        obj.OnCreate();
        
        return obj;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T CreateEntity<T>(Entity prefab,
                             Vector3 position,
                             Quaternion orientation,
                             Vector3 scale,
                             Transform parent)
    where T : Entity {
        return (T)CreateEntity(prefab, position, orientation, scale, parent);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(int id) {
        if(EntitiesToRemoveCount == RemoveQueue.Length) {
            Array.Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }
        
        Entities[id].Alive = false;
        RemoveQueue[EntitiesToRemoveCount++] = id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(int id) {
        var entity = Entities[id].Entity;
        
        if(entity != null){
            if(FreeEntitiesCount == FreeEntities.Length){
                Array.Resize(ref FreeEntities, FreeEntitiesCount << 1);
            }
            
            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
                DynamicEntities.Remove(id);
            }
            
            Entities[id].Entity = null;
            entity.Destroy();
            FreeEntities[FreeEntitiesCount++] = id;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyAllEntities() {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            var entity = Entities[i].Entity;
            if(entity != null){
                if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                    DynamicEntities.Remove(entity.Id);
                }
                
                Entities[i].Entity = null;
                entity.Destroy();
            }
        }
        
        MaxEntitiesCount      = 0;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute() {
        for(var i = 0; i < MovedEntities.Count; ++i) {
            EntitiesTable.UpdatePosition(MovedEntities[i].Id, MovedEntities[i].NewPosition);
        }

        EntitiesTable.Rehash();

        MovedEntities.Clear();

        for(var i = 0; i < EntitiesToRemoveCount; ++i) {
            DestroyEntityImmediate(RemoveQueue[i]);
        }
        EntitiesToRemoveCount = 0;
        
        for(var i = 0; i < DynamicEntities.Count; ++i) {
            Entities[DynamicEntities[i]].Entity.Execute();
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (bool alive, Entity entity) GetEntity(int id) {
        return (Entities[id].Alive, Entities[id].Entity);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (bool alive, T entity) GetEntity<T>(int id)
    where T : Entity {
        return (Entities[id].Alive, (T)Entities[id].Entity);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityType GetType(int id) {
        return Entities[id].Type;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(int id) {
        return Entities[id].Alive;
    }
}