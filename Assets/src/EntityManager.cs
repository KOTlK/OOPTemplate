using UnityEngine;
using System;
using System.Collections.Generic;

public class EntityManager : MonoBehaviour{
    public Entity[]        Entities        = new Entity[128];
    public List<Entity>    DynamicEntities = new ();
    public int[]           RemoveQueue     = new int[128];
    public int[]           FreeEntities    = new int[128];
    public int             EntitiesCount;
    public int             FreeEntitiesCount;
    public int             EntitiesToRemoveCount;
    
    public Entity CreateEntity(Entity prefab, Vector3 position, Quaternion orientation, Vector3 scale){
        var id = -1;
        
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = EntitiesCount;
        }
        
        var obj = Instantiate(prefab, position, orientation);
        
        if(EntitiesCount == Entities.Length){
            Array.Resize(ref Entities, EntitiesCount << 1);
        }
        
        Entities[EntitiesCount++] = obj;
        
        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
            DynamicEntities.Add(obj);
        }
        
        obj.Id          = id;
        obj.Em          = this;
        obj.Alive       = true;
        
        return obj;
    }
    
    public void DestroyEntity(int id){
        if(EntitiesToRemoveCount == RemoveQueue.Length){
            Array.Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }
        
        Entities[id].Alive = false;
        
        RemoveQueue[EntitiesToRemoveCount++] = id;
    }
    
    public void DestroyEntityImmediate(int id){
        var entity = Entities[id];
        
        if(FreeEntitiesCount == FreeEntities.Length){
            Array.Resize(ref FreeEntities, FreeEntitiesCount << 1);
        }
        
        FreeEntities[FreeEntitiesCount++] = id;
        Entities[id] = null;
        EntitiesCount--;
        
        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic){
            DynamicEntities.Remove(entity);
        }
        
        entity.Destroy();
    }
    
    public void Execute(){
        for(var i = 0; i < DynamicEntities.Count; ++i){
            DynamicEntities[i].Execute();
        }
        
        for(var i = 0; i < EntitiesToRemoveCount; ++i){
            DestroyEntityImmediate(RemoveQueue[i]);
        }
    }
}