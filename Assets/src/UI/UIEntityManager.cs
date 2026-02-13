using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Reflex.Injectors;
using Reflex.Extensions;

using static ArrayUtils;
using UnityEngine.SceneManagement;

[Serializable]
public struct PackedUIEntity {
    [DontSave] public UIElement     Entity;
               public uint          Tag; // Slot's generational index
               public bool          Alive;
}

public class UIEntityManager {
    public struct FlagsChange {
        public EntityFlags Old;
        public EntityFlags New;
        public UIElement   Entity; // don't need handle here
    }

    public event Action<FlagsChange> EntityFlagsChanged = delegate {};

    public List<UIElement>  BakedEntities = new();
    public PackedUIEntity[] Entities = new PackedUIEntity[128];
    public List<uint>       DynamicEntities = new();
    public uint[]           RemoveQueue = new uint[128];
    public uint[]           FreeEntities = new uint[128];
    [HideInInspector]
    public uint             MaxEntitiesCount = 1;
    [HideInInspector]
    public uint             FreeEntitiesCount;
    public uint             EntitiesToRemoveCount;

    public UIEntityManager() {
        BakedEntities.Clear();
        DynamicEntities.Clear();
        MaxEntitiesCount = 1;
        FreeEntitiesCount = 0;
        EntitiesToRemoveCount = 0;

        EntityFlagsChanged += CheckDynamicOnFlagsChange;
    }

    public void BakeEntities() {
        for(var i = 0; i < BakedEntities.Count; ++i) {
            BakeEntity(BakedEntities[i]);
        }

        BakedEntities.Clear();
    }

    public void BakeEntity(UIElement entity) {
        uint id;
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        uint tag = Entities[id].Tag;

        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }

        Entities[id].Entity  = entity;
        Entities[id].Alive   = true;
        Entities[id].Tag     = tag;

        entity.Em          = this;
        entity.Id 		   = id;

        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
            entity.OnBecameDynamic();
        }

        entity.OnBaking();
    }

    public T CreateEntity<T>(object     prefab,
                             Vector3    position,
                             Quaternion orientation,
                             Transform  parent = null)
    where T : UIElement {
        uint id;

        if(FreeEntitiesCount > 0) {
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }

        uint tag = Entities[id].Tag;

        var obj = AssetManager.Instantiate<T>(prefab, position, orientation, parent);

        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }

        Entities[id].Entity  = obj;
        Entities[id].Alive   = true;
        Entities[id].Tag     = tag;

        obj.Em          = this;
        obj.Id 			= id;

        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
            obj.OnBecameDynamic();
        }

        var container = SceneManager.GetActiveScene().GetSceneContainer();
        GameObjectInjector.InjectSingle(obj.gameObject, container);
        obj.OnCreate();

        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(uint id) {
    	if (Entities[id].Alive == false) return;

        if(EntitiesToRemoveCount == RemoveQueue.Length) {
            Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }

        Entities[id].Alive = false;
        RemoveQueue[EntitiesToRemoveCount++] = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(uint id) {
        var entity = Entities[id].Entity;

        if(entity != null) {
            if(FreeEntitiesCount == FreeEntities.Length) {
                Resize(ref FreeEntities, FreeEntitiesCount << 1);
            }

            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                DynamicEntities.Remove(id);
            }

            Entities[id].Entity = null;
            entity.Destroy();
            UnityEngine.Object.Destroy(entity.gameObject);
            FreeEntities[FreeEntitiesCount++] = id;
            Entities[id].Tag++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyAllEntities() {
        for(uint i = 0; i < MaxEntitiesCount; ++i) {
            DestroyEntityImmediate(i);
        }

        MaxEntitiesCount      = 1;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
        DynamicEntities.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update() {
        for(var i = 0; i < EntitiesToRemoveCount; ++i) {
            DestroyEntityImmediate(RemoveQueue[i]);
        }
        EntitiesToRemoveCount = 0;


        for(var i = 0; i < DynamicEntities.Count; ++i) {
            Entities[DynamicEntities[i]].Entity.UpdateEntity();
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(UIElement entity, EntityFlags flags) {
        var change = new FlagsChange();

        change.Old    = entity.Flags;
        change.New    = flags;
        change.Entity = entity;

        entity.Flags = flags;

        EntityFlagsChanged(change);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(UIElement entity, EntityFlags flag) {
        if ((entity.Flags & flag) != flag) {
            var change    = new FlagsChange();
            change.Old    = entity.Flags;
            entity.Flags |= flag;
            change.New    = entity.Flags;
            change.Entity = entity;

            EntityFlagsChanged(change);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFlag(UIElement entity, EntityFlags flag) {
        if ((entity.Flags & flag) == flag) {
            var change    = new FlagsChange();
            change.Old    = entity.Flags;
            entity.Flags &= ~flag;
            change.New    = entity.Flags;
            change.Entity = entity;

            EntityFlagsChanged(change);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleFlag(UIElement entity, EntityFlags flag) {
        var change    = new FlagsChange();
        change.Old    = entity.Flags;
        entity.Flags ^= flag;
        change.New    = entity.Flags;
        change.Entity = entity;

        EntityFlagsChanged(change);
    }

    private void CheckDynamicOnFlagsChange(FlagsChange change) {
        // if dynamic flag was removed
        if ((change.Old & EntityFlags.Dynamic) == EntityFlags.Dynamic &&
            (change.New & EntityFlags.Dynamic) != EntityFlags.Dynamic) {
            var id = change.Entity.Id;

            DynamicEntities.Remove(id);
            change.Entity.OnBecameStatic();
        } 
        // if dynamic flag was added
        else if ((change.Old & EntityFlags.Dynamic) != EntityFlags.Dynamic &&
                 (change.New & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            var id = change.Entity.Id;

            DynamicEntities.Add(id);
            change.Entity.OnBecameDynamic();
        }
    }
}