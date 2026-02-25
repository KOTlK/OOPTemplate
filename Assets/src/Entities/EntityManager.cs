using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using Reflex.Injectors;
using Reflex.Extensions;

using static ArrayUtils;
using static Assertions;

[Serializable]
public struct PackedEntity {
    [DontSave] public Entity        Entity;
               public EntityType    Type;
               public EntityFlags   Flags;
               public uint          Tag; // Slot's generational index
               public bool          Alive;
               // public BitSet        EcsBitset;
}

public struct EntityHandle {
    public uint Id;
    public uint Tag;

    public static readonly EntityHandle Zero = new EntityHandle { Id = 0, Tag = 0};

    public static bool operator==(EntityHandle lhs, EntityHandle rhs) {
        return lhs.Id == rhs.Id && lhs.Tag == rhs.Tag;
    }

    public static bool operator!=(EntityHandle lhs, EntityHandle rhs) {
        return !(lhs == rhs);
    }

    public override bool Equals(object obj) {
        return (EntityHandle)obj == this;
    }

    public override int GetHashCode() {
        return HashCode.Combine(Id, Tag);
    }
}

public class EntityManager {
    public struct FlagsChange {
        public EntityFlags Old;
        public EntityFlags New;
        public Entity      Entity; // don't need handle here
    }

    public event Action<FlagsChange> EntityFlagsChanged = delegate {};

    public List<Entity>                                 BakedEntities = new();
    public Dictionary<EntityType, List<EntityHandle>>   EntitiesByType = new();
    public PackedEntity[]                               Entities = new PackedEntity[128];
    public List<uint>                                   DynamicEntities = new();
    public uint[]                                       RemoveQueue = new uint[128];
    public uint[]                                       FreeEntities = new uint[128];
    public BitSet[]                                     Archetypes = new BitSet[128];
    [HideInInspector]
    public uint                                         MaxEntitiesCount = 1;
    [HideInInspector]
    public uint                                         FreeEntitiesCount;
    public uint                                         EntitiesToRemoveCount;

    public Ecs Ecs;

    public EntityManager() {
        BakedEntities.Clear();
        EntitiesByType.Clear();
        DynamicEntities.Clear();
        MaxEntitiesCount = 1;
        FreeEntitiesCount = 0;
        EntitiesToRemoveCount = 0;
        var entityTypes = Enum.GetValues(typeof(EntityType));

        foreach(var type in entityTypes) {
            EntitiesByType.Add((EntityType)type, new List<EntityHandle>());
        }

        EntityFlagsChanged += CheckDynamicOnFlagsChange;

        foreach(var bitset in Archetypes) {
            bitset.ClearAll();
        }
    }

    // @TODO: Clear and load archetypes
    public void Save(BinarySaveFile sf) {
        sf.Write(MaxEntitiesCount);
        for(uint i = 1; i < MaxEntitiesCount; ++i) {
            sf.Write(Entities[i].Type);
            sf.Write(Entities[i].Tag);
            sf.Write(Entities[i].Alive);

            if (Entities[i].Alive && Entities[i].Entity != null) {
                var entity = Entities[i].Entity;
                sf.Write(entity.AssetAddress);
                sf.Write(entity.Position);
                sf.Write(entity.Rotation);
                sf.Write(entity.Scale);
                sf.Write(entity.Type);
                sf.Write(entity.Flags);
                sf.Write(entity);
            }
        }
    }

    public void Load(BinarySaveFile sf) {
        // Clear everything
        for(uint i = 0; i < MaxEntitiesCount; ++i) {
            DestroyEntityImmediate(i);
        }

        MaxEntitiesCount      = 1;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
        DynamicEntities.Clear();

        var entitiesCount = sf.Read<uint>();
        Entities          = new PackedEntity[entitiesCount];
        for(uint i = 1; i < entitiesCount; ++i) {
            var pe = new PackedEntity();

            pe.Entity = null;
            pe.Type   = sf.Read<EntityType>();
            pe.Tag    = sf.Read<uint>();
            pe.Alive  = sf.Read<bool>();

            if (pe.Alive) {
                pe.Entity = RecreateEntity(sf.Read<string>(),
                                           sf.Read<Vector3>(),
                                           sf.Read<Quaternion>(),
                                           sf.Read<Vector3>(),
                                           sf.Read<EntityType>(),
                                           sf.Read<EntityFlags>());
                sf.Read(pe.Entity);
            } else {
                PushEmptyEntity(i);
            }

            Entities[i] = pe;
        }
    }

    public void BakeEntities() {
        for(var i = 0; i < BakedEntities.Count; ++i) {
            BakeEntity(BakedEntities[i]);
        }

        BakedEntities.Clear();
    }

    public void BakeEntity(Entity entity) {
        uint id;
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        uint tag = Entities[id].Tag;

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        if(MaxEntitiesCount == Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }

        Entities[id].Entity  = entity;
        Entities[id].Alive   = true;
        Entities[id].Type    = entity.Type;
        Entities[id].Tag     = tag;
        Entities[id].Flags   = entity.Flags;

        entity.Handle      = handle;
        entity.Em          = this;

        EntitiesByType[entity.Type].Add(handle);

        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
            entity.OnBecameDynamic();
        }

        entity.OnBaking();
    }

    public (EntityHandle, T) CreateEntity<T>(object     prefab,
                                             Vector3    position,
                                             Quaternion orientation,
                                             Transform  parent = null)
    where T : Entity {
        uint id;

        if(FreeEntitiesCount > 0) {
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }

        uint tag = Entities[id].Tag;

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        var obj = AssetManager.Instantiate<T>(prefab, position, orientation, parent);

        if(MaxEntitiesCount == Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }

        Entities[id].Entity  = obj;
        Entities[id].Alive   = true;
        Entities[id].Type    = obj.Type;
        Entities[id].Tag     = tag;
        Entities[id].Flags   = obj.Flags;


        obj.Handle      = handle;
        obj.Em          = this;

        EntitiesByType[obj.Type].Add(handle);

        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
            obj.OnBecameDynamic();
        }

        var container = SceneManager.GetActiveScene().GetSceneContainer();
        GameObjectInjector.InjectSingle(obj.gameObject, container);
        obj.OnCreate();

        return (handle, obj);
    }

    public void PushEmptyEntity(uint id) {
        FreeEntities[FreeEntitiesCount++] = id;
        Entities[id].Alive   = false;
        MaxEntitiesCount++;
    }

    public Entity RecreateEntity(object      name,
                                 Vector3     position,
                                 Quaternion  orientation,
                                 Vector3     scale,
                                 EntityType  type,
                                 EntityFlags flags) {
        var e = AssetManager.Instantiate<Entity>(name, position, orientation);
        e.transform.localScale = scale;

        uint id = MaxEntitiesCount++;

        if(MaxEntitiesCount == Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }
        var handle = new EntityHandle {
            Id = id,
            Tag = Entities[id].Tag
        };

        Entities[id].Entity  = e;
        Entities[id].Alive   = true;
        Entities[id].Type    = type;
        Entities[id].Flags   = e.Flags;

        e.Handle = handle;
        e.Em     = this;
        e.Type   = type;
        e.Flags  = flags;

        EntitiesByType[e.Type].Add(handle);

        if((e.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
            e.OnBecameDynamic();
        }

        var container = SceneManager.GetActiveScene().GetSceneContainer();
        GameObjectInjector.InjectSingle(e.gameObject, container);
        e.OnCreate();

        return e;
    }

    public EntityHandle CreatePureEcsEntity() {
        uint id;

        if(FreeEntitiesCount > 0) {
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }

        uint tag = Entities[id].Tag;

        Entities[id].Flags = EntityFlags.EcsOnly;
        Entities[id].Alive = true;

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(EntityHandle handle, bool calledFromEcs = false) {
        if (!calledFromEcs) {
            Ecs.ClearComponents(handle);
        }

        if (EntitiesToRemoveCount == RemoveQueue.Length) {
            Array.Resize(ref RemoveQueue, (int)EntitiesToRemoveCount << 1);
        }

        if (IsValid(handle)) {
            Entities[handle.Id].Alive = false;
            if ((Entities[handle.Id].Flags & EntityFlags.EcsOnly) == EntityFlags.EcsOnly) {

                FreeEntities[FreeEntitiesCount++] = handle.Id;
                Entities[handle.Id].Tag++;
                return;
            }

            RemoveQueue[EntitiesToRemoveCount++] = handle.Id;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(uint id) {
        var entity = Entities[id].Entity;

        if(entity != null) {
            if(FreeEntitiesCount == FreeEntities.Length) {
                Array.Resize(ref FreeEntities, (int)FreeEntitiesCount << 1);
            }

            EntitiesByType[entity.Type].Remove(GetHandle(id));

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
    public bool GetEntity(EntityHandle handle, out Entity e) {
        if(IsValid(handle)) {
            e = Entities[handle.Id].Entity;
            return true;
        } else {
            e = null;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntity<T>(EntityHandle handle, out T e)
    where T : Entity {
        if(IsValid(handle)) {
            e = (T)Entities[handle.Id].Entity;
            return true;
        } else {
            e = null;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle GetHandle(uint id) {
        return new EntityHandle {
            Id = id,
            Tag = Entities[id].Tag
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(EntityHandle handle) {
        return IsValid(handle) && Entities[handle.Id].Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(EntityHandle handle) {
        return handle != EntityHandle.Zero && handle.Tag == Entities[handle.Id].Tag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<Entity> GetAllEntitiesWithType(EntityType type) {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            if(Entities[i].Type == type && Entities[i].Alive) {
                yield return Entities[i].Entity;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAllEntitiesWithType<T>(EntityType type)
    where T : Entity {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            if(Entities[i].Type == type && Entities[i].Alive) {
                yield return (T)Entities[i].Entity;
            }
        }
    }

    // @TODO: change it to use Entities[i].Flags instead of the entity itself
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(EntityHandle handle, EntityFlags flags) {
        Assert((flags & EntityFlags.EcsOnly) != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        var change = new FlagsChange();

        change.Old    = entity.Flags;
        change.New    = flags;
        change.Entity = entity;

        entity.Flags = flags;
        Entities[handle.Id].Flags = flags;

        EntityFlagsChanged(change);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(EntityHandle handle, EntityFlags flag) {
        Assert(flag != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        if ((entity.Flags & flag) != flag) {
            var change    = new FlagsChange();
            change.Old    = entity.Flags;
            entity.Flags |= flag;
            Entities[handle.Id].Flags |= flag;
            change.New    = entity.Flags;
            change.Entity = entity;

            EntityFlagsChanged(change);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFlag(EntityHandle handle, EntityFlags flag) {
        Assert(flag != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        if ((entity.Flags & flag) == flag) {
            var change    = new FlagsChange();
            change.Old    = entity.Flags;
            entity.Flags &= ~flag;
            Entities[handle.Id].Flags &= ~flag;
            change.New    = entity.Flags;
            change.Entity = entity;

            EntityFlagsChanged(change);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleFlag(EntityHandle handle, EntityFlags flag) {
        Assert(flag != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        var change    = new FlagsChange();
        change.Old    = entity.Flags;
        entity.Flags ^= flag;
        Entities[handle.Id].Flags ^= flag;
        change.New    = entity.Flags;
        change.Entity = entity;

        EntityFlagsChanged(change);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEcsEntity(EntityHandle h) {
        if (!IsValid(h)) return false;

        return ((Entities[h.Id].Flags & EntityFlags.Ecs) == EntityFlags.Ecs) ||
               ((Entities[h.Id].Flags & EntityFlags.EcsOnly) == EntityFlags.EcsOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakeEmptyBitset(EntityHandle h, uint componentsCount) {
        if (IsValid(h)) {
            Archetypes[h.Id] = new(componentsCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitSet GetArchetype(EntityHandle h) {
        Assert(IsValid(h), "Cannot get archetype for invalid entity (%)", h.Id);

        return Archetypes[h.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponentBit(uint h, uint b) {
        Assert(Entities[h].Alive, "Cannot set component bit for dead entity (%)", h);

        Archetypes[h].SetBit(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearComponentBit(uint h, uint b) {
        Assert(Entities[h].Alive, "Cannot clear component bit for dead entity (%)", h);

        Archetypes[h].ClearBit(b);
    }

    private void CheckDynamicOnFlagsChange(FlagsChange change) {
        // if dynamic flag was removed
        if ((change.Old & EntityFlags.Dynamic) == EntityFlags.Dynamic &&
            (change.New & EntityFlags.Dynamic) != EntityFlags.Dynamic) {
            var id = change.Entity.Handle.Id;

            DynamicEntities.Remove(id);
            change.Entity.OnBecameStatic();
        } 
        // if dynamic flag was added
        else if ((change.Old & EntityFlags.Dynamic) != EntityFlags.Dynamic &&
                 (change.New & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            var id = change.Entity.Handle.Id;

            DynamicEntities.Add(id);
            change.Entity.OnBecameDynamic();
        }
    }

    private void Resize(uint newSize) {
        Array.Resize(ref Entities, (int)newSize);
        Array.Resize(ref Archetypes, (int)newSize);
    }
}