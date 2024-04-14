using UnityEngine;
using System;
using System.Collections.Generic;

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
    
    public Entity CreateEntity(Entity prefab, Vector3 position){
        return CreateEntity(prefab, position, Quaternion.identity, Vector3.one, null);
    }
    
    public Entity CreateEntity(Entity prefab, Vector3 position, Quaternion orientation, Vector3 scale){
        return CreateEntity(prefab, position, orientation, scale, null);
    }
    
    public Entity CreateEntity(Entity prefab, Vector3 position, Quaternion orientation, Vector3 scale, Transform parent){
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
    
    public void DestroyEntity(int id){
        if(EntitiesToRemoveCount == RemoveQueue.Length){
            Array.Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }
        
        try{
            Entities[id].Alive = false;
        }catch{
            Debug.Log("Sfsfsdfsd");
        }
        
        RemoveQueue[EntitiesToRemoveCount++] = id;
    }
    
    public void DestroyEntityImmediate(int id){
        var entity = Entities[id];
        
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
    
    public void Execute(){
        for(var i = 0; i < DynamicEntities.Count; ++i){
            DynamicEntities[i].Execute();
        }
        
        for(var i = 0; i < EntitiesToRemoveCount; ++i){
            DestroyEntityImmediate(RemoveQueue[i]);
        }
        EntitiesToRemoveCount = 0;
    }
}