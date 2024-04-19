using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class EntityManager : MonoBehaviour{
    public List<Entity>    BakedEntities;
    public Entity[]        Entities        = new Entity[128];
    public List<Entity>    DynamicEntities = new ();
    public int[]           RemoveQueue     = new int[128];
    public int[]           FreeEntities    = new int[128];
    public int             MaxEntitiesCount;
    public int             FreeEntitiesCount;
    public int             EntitiesToRemoveCount;
        
    public void BakeEntities(){
        for(var i = 0; i < BakedEntities.Count; ++i){
            BakeEntity(BakedEntities[i]);
        }
        
        BakedEntities.Clear();
    }
    
    public void BakeEntity(Entity entity){
        var id = -1;
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        
        if(MaxEntitiesCount == Entities.Length){
            Array.Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id] = entity;
        
        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
            DynamicEntities.Add(entity);
        }
        
        entity.Id          = id;
        entity.Em          = this;
        entity.Alive       = true;
        
        entity.OnCreate();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(Entity prefab, Vector3 position){
        return CreateEntity(prefab, position, Quaternion.identity, Vector3.one, null);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(Entity prefab, 
                               Vector3 position, 
                               Quaternion orientation, 
                               Vector3 scale){
        return CreateEntity(prefab, position, orientation, scale, null);
    }
    
    public Entity CreateEntity(Entity prefab, 
                               Vector3 position, 
                               Quaternion orientation, 
                               Vector3 scale, 
                               Transform parent){
        var id = -1;
        
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        
        var obj = Instantiate(prefab, position, orientation, parent);
        
        if(MaxEntitiesCount == Entities.Length){
            Array.Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id] = obj;
        
        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
            DynamicEntities.Add(obj);
        }
        
        obj.Id          = id;
        obj.Em          = this;
        obj.Alive       = true;
        
        obj.OnCreate();
        
        return obj;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(int id){
        if(EntitiesToRemoveCount == RemoveQueue.Length){
            Array.Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }
        
        Entities[id].Alive = false;
        RemoveQueue[EntitiesToRemoveCount++] = id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(int id){
        var entity = Entities[id];
        
        if(entity != null){
            if(FreeEntitiesCount == FreeEntities.Length){
                Array.Resize(ref FreeEntities, FreeEntitiesCount << 1);
            }
            
            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
                DynamicEntities.Remove(entity);
            }
            
            Entities[id] = null;
            entity.Destroy();
            FreeEntities[FreeEntitiesCount++] = id;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyAllEntities(){
        for(var i = 0; i < MaxEntitiesCount; ++i){
            var entity = Entities[i];
            if(entity != null){
                if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
                    DynamicEntities.Remove(entity);
                }
                
                Entities[i] = null;
                entity.Destroy();
            }
        }
        
        MaxEntitiesCount      = 0;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(){
        for(var i = 0; i < DynamicEntities.Count; ++i){
            DynamicEntities[i].Execute();
        }
        
        for(var i = 0; i < EntitiesToRemoveCount; ++i){
            DestroyEntityImmediate(RemoveQueue[i]);
        }
        EntitiesToRemoveCount = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int id){
        return Entities[id];
    }
}