using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using Reflex.Injectors;
using Reflex.Extensions;

using static Assertions;

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
    public Entity[]                                     Entities = new Entity[128];
    public EntityType[]                                 Types    = new EntityType[128];
    public EntityFlags[]                                Flags    = new EntityFlags[128];
    public uint[]                                       Tags     = new uint[128];
    public bool[]                                       Alive    = new bool[128];
    public BitSet[]                                     Archetypes = new BitSet[128];
    public List<uint>                                   DynamicEntities = new();
    public uint[]                                       RemoveQueue = new uint[128];
    public uint[]                                       FreeEntities = new uint[128];
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
        EntityFlagsChanged += OnEcsFlagChange;

        foreach(var bitset in Archetypes) {
            bitset.ClearAll();
        }
    }

    public void Save(BinarySaveFile sf) {
        sf.Write(MaxEntitiesCount);
        for(uint i = 1; i < MaxEntitiesCount; ++i) {
            sf.Write(Entities[i].Type);
            sf.Write(Tags[i]);
            sf.Write(Alive[i]);
            sf.Write(Flags[i]);

            if (Alive[i] && Entities[i] != null) {
                var entity = Entities[i];
                sf.Write(entity.AssetAddress);
                sf.Write(entity.Position);
                sf.Write(entity.Rotation);
                sf.Write(entity.Scale);
                sf.Write(entity.Type);
                if ((entity.Flags & EntityFlags.Ecs) == EntityFlags.Ecs ||
                    (entity.Flags & EntityFlags.EcsOnly) == EntityFlags.EcsOnly) {
                    sf.Write(Archetypes[i]);
                }
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
        // Entities          = new PackedEntity[entitiesCount];
        if (Entities.Length < entitiesCount) {
            Resize(entitiesCount + 128);
        }

        for(uint i = 1; i < entitiesCount; ++i) {
            // var pe = new PackedEntity();

            Entities[i] = null;
            Types[i]    = sf.Read<EntityType>();
            Tags[i]     = sf.Read<uint>();
            Alive[i]    = sf.Read<bool>();
            Flags[i]    = sf.Read<EntityFlags>();

            if (Alive[i]) {
                var asset = sf.Read<UnityEngine.AddressableAssets.AssetReference>();
                var pos   = sf.Read<Vector3>();
                var rot   = sf.Read<Quaternion>();
                var scale = sf.Read<Vector3>();
                var type  = sf.Read<EntityType>();

                Entities[i] = RecreateEntity(asset,
                                           pos,
                                           rot,
                                           scale,
                                           type,
                                           Flags[i]);

                if ((Flags[i] & EntityFlags.Ecs) == EntityFlags.Ecs ||
                    (Flags[i] & EntityFlags.EcsOnly) == EntityFlags.EcsOnly) {
                    Archetypes[i] = sf.Read<BitSet>();
                }

                sf.Read(Entities[i]);
                Entities[i].Flags = Flags[i];
            } else {
                PushEmptyEntity(i);
            }
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
        uint tag = Tags[id];

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        if(MaxEntitiesCount == Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }

        Entities[id] = entity;
        Alive[id]    = true;
        Types[id]    = entity.Type;
        Tags[id]     = tag;
        Flags[id]    = entity.Flags;

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

        uint tag = Tags[id];

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        var obj = AssetManager.Instantiate<T>(prefab, position, orientation, parent);

        if(MaxEntitiesCount == Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }

        Entities[id] = obj;
        Alive[id]    = true;
        Types[id]    = obj.Type;
        Tags[id]     = tag;
        Flags[id]    = obj.Flags;

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
        Alive[id]   = false;
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
            Tag = Tags[id]
        };

        Entities[id] = e;
        Alive[id]    = true;
        Types[id]    = type;
        Flags[id]    = e.Flags;

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

        if (id >= Entities.Length) {
            Resize(id << 1);
        }

        uint tag = Tags[id];

        Flags[id] = EntityFlags.EcsOnly;
        Alive[id] = true;

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        return handle;
    }

    // @TODO: Merge DestroyEntity and DestroyEntityImmediate, so there is no 1 frame waiting to destroy entity.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(EntityHandle handle, bool calledFromEcs = false) {
        if (!calledFromEcs) {
            Ecs.ClearComponents(handle);
        }

        if (EntitiesToRemoveCount == RemoveQueue.Length) {
            Array.Resize(ref RemoveQueue, (int)EntitiesToRemoveCount << 1);
        }

        if (IsValid(handle)) {
            Alive[handle.Id] = false;
            if ((Flags[handle.Id] & EntityFlags.EcsOnly) == EntityFlags.EcsOnly) {
                if(FreeEntitiesCount == FreeEntities.Length) {
                    Array.Resize(ref FreeEntities, (int)FreeEntitiesCount << 1);
                }
                FreeEntities[FreeEntitiesCount++] = handle.Id;
                Tags[handle.Id]++;
                Archetypes[handle.Id].ClearAll();
                return;
            }

            RemoveQueue[EntitiesToRemoveCount++] = handle.Id;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(uint id) {
        var entity = Entities[id];

        if(entity != null) {
            if(FreeEntitiesCount == FreeEntities.Length) {
                Array.Resize(ref FreeEntities, (int)FreeEntitiesCount << 1);
            }

            EntitiesByType[entity.Type].Remove(GetHandle(id));

            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                DynamicEntities.Remove(id);
            }

            if((entity.Flags & EntityFlags.Ecs) == EntityFlags.Ecs) {
                Archetypes[id].ClearAll();
            }

            if((entity.Flags & EntityFlags.EcsOnly) == EntityFlags.EcsOnly) {
                Archetypes[id].ClearAll();
            }

            Entities[id] = null;
            entity.Destroy();
            UnityEngine.Object.Destroy(entity.gameObject);
            FreeEntities[FreeEntitiesCount++] = id;
            Tags[id]++;
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
            Entities[DynamicEntities[i]].UpdateEntity();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntity(EntityHandle handle, out Entity e) {
        if(IsValid(handle)) {
            e = Entities[handle.Id];
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
            e = (T)Entities[handle.Id];
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
            Tag = Tags[id]
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(EntityHandle handle) {
        return IsValid(handle) && Alive[handle.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(EntityHandle handle) {
        return handle != EntityHandle.Zero && handle.Tag == Tags[handle.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<Entity> GetAllEntitiesWithType(EntityType type) {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            if(Entities[i].Type == type && Alive[i]) {
                yield return Entities[i];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAllEntitiesWithType<T>(EntityType type)
    where T : Entity {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            if(Entities[i].Type == type && Alive[i]) {
                yield return (T)Entities[i];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(EntityHandle handle, EntityFlags flags) {
        Assert((flags & EntityFlags.EcsOnly) != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        var change = new FlagsChange();

        change.Old       = Flags[handle.Id];
        change.New       = flags;
        change.Entity    = entity;
        entity.Flags     = flags;
        Flags[handle.Id] = flags;

        EntityFlagsChanged(change);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(EntityHandle handle, EntityFlags flag) {
        Assert(flag != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        if ((Flags[handle.Id] & flag) != flag) {
            var change        = new FlagsChange();
            change.Old        = Flags[handle.Id];
            entity.Flags     |= flag;
            Flags[handle.Id] |= flag;
            change.New        = Flags[handle.Id];
            change.Entity     = entity;

            EntityFlagsChanged(change);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFlag(EntityHandle handle, EntityFlags flag) {
        Assert(flag != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        if ((Flags[handle.Id] & flag) == flag) {
            var change        = new FlagsChange();
            change.Old        = Flags[handle.Id];
            entity.Flags     &= ~flag;
            Flags[handle.Id] &= ~flag;
            change.New        = Flags[handle.Id];
            change.Entity     = entity;

            EntityFlagsChanged(change);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleFlag(EntityHandle handle, EntityFlags flag) {
        Assert(flag != EntityFlags.EcsOnly, "Cannot change EcsOnly flag on already created entity, use Ecs flag instead.");
        if (!GetEntity(handle, out var entity)) return;

        var change    = new FlagsChange();
        change.Old        = Flags[handle.Id];
        entity.Flags     ^= flag;
        Flags[handle.Id] ^= flag;
        change.New        = Flags[handle.Id];
        change.Entity     = entity;

        EntityFlagsChanged(change);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEcsEntity(EntityHandle h) {
        if (!IsValid(h)) return false;

        return ((Flags[h.Id] & EntityFlags.Ecs) == EntityFlags.Ecs) ||
               ((Flags[h.Id] & EntityFlags.EcsOnly) == EntityFlags.EcsOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakeEmptyBitset(EntityHandle h, uint componentsCount) {
        if (IsValid(h)) {
            if (Archetypes[h.Id].Allocated) {
                Archetypes[h.Id].ClearAll();
            } else {
                Archetypes[h.Id] = new(componentsCount);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitSet GetArchetype(EntityHandle h) {
        Assert(IsValid(h), "Cannot get archetype for invalid entity (%)", h.Id);

        return Archetypes[h.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponentBit(uint h, uint b) {
        Assert(Alive[h], "Cannot set component bit for dead entity (%)", h);

        Archetypes[h].SetBit(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearComponentBit(uint h, uint b) {
        Assert(Alive[h], "Cannot clear component bit for dead entity (%)", h);

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

    private void OnEcsFlagChange(FlagsChange change) {
        // if ecs flag was removed
        if ((change.Old & EntityFlags.Ecs) == EntityFlags.Ecs &&
            (change.New & EntityFlags.Ecs) != EntityFlags.Ecs) {
            var id = change.Entity.Handle.Id;

            Ecs.ClearComponents(change.Entity.Handle);
            Archetypes[id].ClearAll();
        } 
        // if ecs flag was added
        else if ((change.Old & EntityFlags.Ecs) != EntityFlags.Ecs &&
                 (change.New & EntityFlags.Ecs) == EntityFlags.Ecs) {
            MakeEmptyBitset(change.Entity.Handle, Ecs.ComponentsCount);
        }
    }

    private void Resize(uint newSize) {
        var ns = (int)newSize; // fuck you c#;
        Array.Resize(ref Entities, ns);
        Array.Resize(ref Types, ns);
        Array.Resize(ref Flags, ns);
        Array.Resize(ref Tags, ns);
        Array.Resize(ref Alive, ns);
        Array.Resize(ref Archetypes, ns);
    }
}