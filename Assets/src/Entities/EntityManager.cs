using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using Reflex.Injectors;
using Reflex.Extensions;

using static Assertions;

[System.Serializable]
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

    public Dictionary<EntityType, IntrusiveList> EntitiesByType = new();
    public List<Entity>  BakedEntities = new();
    public Entity[]      Entities = new Entity[128];
    public EntityType[]  Types = new EntityType[128];
    public EntityFlags[] Flags = new EntityFlags[128];
    public uint[]        Tags = new uint[128];
    public BitSet[]      Archetypes = new BitSet[128];
    public bool[]        Free = new bool[128];
    public uint[]        NextFree = new uint[128];
    public uint[]        NextDynamic = new uint[128];
    public uint[]        PrevDynamic = new uint[128];

    [HideInInspector] public uint MaxEntitiesCount = 1;
    [ReadOnly]        public uint FirstFree;
    [ReadOnly]        public uint FirstDynamic;

    public Ecs Ecs;

    public EntityManager() {
        BakedEntities.Clear();
        EntitiesByType.Clear();
        MaxEntitiesCount = 1;
        FirstFree = 0;
        FirstDynamic = 0;
        var entityTypes = Enum.GetValues(typeof(EntityType));

        foreach(var type in entityTypes) {
            EntitiesByType.Add((EntityType)type, new IntrusiveList(128));
        }

        EntityFlagsChanged += CheckDynamicOnFlagsChange;
        EntityFlagsChanged += OnEcsFlagChange;

        foreach(var bitset in Archetypes) {
            bitset.ClearAll();
        }

        for (var i = 0; i < 128; i++) {
            Free[i] = true;
        }
    }

    public void Save(BinarySaveFile sf) {
        sf.Write(MaxEntitiesCount);
        for(uint i = 1; i < MaxEntitiesCount; ++i) {
            sf.Write(Entities[i].Type);
            sf.Write(Tags[i]);
            sf.Write(Free[i]);
            sf.Write(Flags[i]);

            if (Free[i] == false && Entities[i] != null) {
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
        DestroyAllEntities();

        var entitiesCount = sf.Read<uint>();
        if (Entities.Length < entitiesCount) {
            Resize(entitiesCount + 128);
        }

        for(uint i = 1; i < entitiesCount; ++i) {
            Entities[i] = null;
            Types[i]    = sf.Read<EntityType>();
            Tags[i]     = sf.Read<uint>();
            Free[i]     = sf.Read<bool>();
            Flags[i]    = sf.Read<EntityFlags>();

            if (Free[i] == false) {
                var asset = sf.Read<string>();
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
        uint id = GetNextFree();

        uint tag = Tags[id];

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        if(MaxEntitiesCount == Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }

        Entities[id] = entity;
        Free[id]     = false;
        Types[id]    = entity.Type;
        Tags[id]     = tag;
        Flags[id]    = entity.Flags;

        entity.Handle      = handle;
        entity.Em          = this;

        EntitiesByType[entity.Type].Add(handle.Id);

        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            AddDynamic(id);
            entity.OnBecameDynamic();
        }

        if((entity.Flags & EntityFlags.Ecs) == EntityFlags.Ecs) {
            if (Archetypes[id].Allocated) {
                Archetypes[id].ClearAll();
            } else {
                Archetypes[id] = new (Ecs.ComponentsCount);
            }
        }

        entity.OnBaking();
    }

    public (EntityHandle, T) CreateEntity<T>(object     prefab,
                                             Vector3    position,
                                             Quaternion orientation,
                                             Transform  parent = null)
    where T : Entity {
        uint id = GetNextFree();

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
        Free[id]     = false;
        Types[id]    = obj.Type;
        Tags[id]     = tag;
        Flags[id]    = obj.Flags;

        obj.Handle      = handle;
        obj.Em          = this;

        EntitiesByType[obj.Type].Add(handle.Id);

        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            AddDynamic(id);
            obj.OnBecameDynamic();
        }

        if((obj.Flags & EntityFlags.Ecs) == EntityFlags.Ecs) {
            if (Archetypes[id].Allocated) {
                Archetypes[id].ClearAll();
            } else {
                Archetypes[id] = new (Ecs.ComponentsCount);
            }
        }

        var container = SceneManager.GetActiveScene().GetSceneContainer();
        GameObjectInjector.InjectSingle(obj.gameObject, container);
        obj.OnCreate();

        return (handle, obj);
    }

    public void PushEmptyEntity(uint id) {
        Free[id] = true;
        NextFree[id] = FirstFree;
        FirstFree = id;
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

        if(MaxEntitiesCount >= Entities.Length) {
            Resize(MaxEntitiesCount << 1);
        }
        var handle = new EntityHandle {
            Id = id,
            Tag = Tags[id]
        };

        Entities[id] = e;
        Free[id]     = false;
        Types[id]    = type;
        Flags[id]    = e.Flags;

        e.Handle = handle;
        e.Em     = this;
        e.Type   = type;
        e.Flags  = flags;

        EntitiesByType[e.Type].Add(handle.Id);

        if((e.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            AddDynamic(id);
            e.OnBecameDynamic();
        }

        if((e.Flags & EntityFlags.Ecs) == EntityFlags.Ecs) {
            if (Archetypes[id].Allocated) {
                Archetypes[id].ClearAll();
            } else {
                Archetypes[id] = new (Ecs.ComponentsCount);
            }
        }

        var container = SceneManager.GetActiveScene().GetSceneContainer();
        GameObjectInjector.InjectSingle(e.gameObject, container);
        e.OnCreate();

        return e;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle CreatePureEcsEntity() {
        uint id = GetNextFree();
        uint tag = Tags[id];

        Flags[id] = EntityFlags.EcsOnly;
        Free[id] = false;

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };

        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(EntityHandle handle, bool calledFromEcs = false) {
        if (IsValid(handle)) {
            Free[handle.Id] = true;
            if ((Flags[handle.Id] & EntityFlags.EcsOnly) == EntityFlags.EcsOnly) {
                if (!calledFromEcs) {
                    Ecs.ClearComponents(handle);
                }
                NextFree[handle.Id] = FirstFree;
                FirstFree = handle.Id;
                Tags[handle.Id]++;
                Archetypes[handle.Id].ClearAll();
                return;
            }
            var id = handle.Id;

            if ((Flags[id] & EntityFlags.Ecs) == EntityFlags.Ecs) {
                if (!calledFromEcs) {
                    Ecs.ClearComponents(handle);
                }
                Archetypes[id].ClearAll();
            }

            if ((Flags[id] & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                RemoveDynamic(id);
            }

            var entity = Entities[handle.Id];

            Assert(entity, "Cannot destroy null entity (%)", id);

            EntitiesByType[entity.Type].Remove(handle.Id);
            Entities[id] = null;
            entity.Destroy();
            UnityEngine.Object.Destroy(entity.gameObject);
            NextFree[id] = FirstFree;
            FirstFree = id;
            Tags[id]++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyAllEntities() {
        for (uint i = 0; i < Entities.Length; i++) {
            if (Free[i] == false) {
                DestroyEntity(GetHandle(i));
            }

            Types[i] = EntityType.None;
            Flags[i] = EntityFlags.None;
            Tags[i]  = 0;
            if (Archetypes[i].Allocated) {
                Archetypes[i].ClearAll();
            }
            Free[i]        = true;
            NextFree[i]    = 0;
            NextDynamic[i] = 0;
            PrevDynamic[i] = 0;
        }

        MaxEntitiesCount = 1;
        FirstDynamic     = 0;
        FirstFree        = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update() {
        var next = FirstDynamic;

        while(next > 0) {
            var current = next;
            next = NextDynamic[next];
            Entities[current].UpdateEntity();
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
        return IsValid(handle) && Free[handle.Id] == false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(EntityHandle handle) {
        return handle != EntityHandle.Zero && handle.Tag == Tags[handle.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<Entity> GetAllEntitiesWithType(EntityType type) {
        var list = EntitiesByType[type];

        var next = list.First;

        while (next > 0) {
            var nxt = list.Next[next];
            yield return Entities[next];
            next = nxt;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAllEntitiesWithType<T>(EntityType type)
    where T : Entity {
        var list = EntitiesByType[type];

        var next = list.First;

        while (next > 0) {
            var nxt = list.Next[next];
            yield return (T)Entities[next];
            next = nxt;
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
    public BitSet GetArchetype(uint h) {
        return Archetypes[h];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponentBit(uint h, uint b) {
        Assert(Free[h] == false, "Cannot set component bit for dead entity (%)", h);

        Archetypes[h].SetBit(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearComponentBit(uint h, uint b) {
        Assert(Free[h] == false, "Cannot clear component bit for dead entity (%)", h);

        Archetypes[h].ClearBit(b);
    }

    private void CheckDynamicOnFlagsChange(FlagsChange change) {
        // if dynamic flag was removed
        if ((change.Old & EntityFlags.Dynamic) == EntityFlags.Dynamic &&
            (change.New & EntityFlags.Dynamic) != EntityFlags.Dynamic) {
            var id = change.Entity.Handle.Id;

            RemoveDynamic(id);
            change.Entity.OnBecameStatic();
        } 
        // if dynamic flag was added
        else if ((change.Old & EntityFlags.Dynamic) != EntityFlags.Dynamic &&
                 (change.New & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            var id = change.Entity.Handle.Id;

            AddDynamic(id);
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
        Array.Resize(ref Free, ns);
        Array.Resize(ref NextFree, ns);
        Array.Resize(ref NextDynamic, ns);
        Array.Resize(ref PrevDynamic, ns);
        Array.Resize(ref Archetypes, ns);
        foreach(var (_, list) in EntitiesByType) {
            list.Resize(newSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetNextFree() {
        uint id;
        if (FirstFree > 0) {
            id = FirstFree;
            FirstFree = NextFree[id];
            return id;
        }

        id = MaxEntitiesCount++;
        
        if (id >= Entities.Length) {
            Resize(id + 128);
        }

        return id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddDynamic(uint id) {
        NextDynamic[id] = FirstDynamic;
        PrevDynamic[FirstDynamic] = id;
        FirstDynamic = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveDynamic(uint id) {
        var prev = PrevDynamic[id];
        var next = NextDynamic[id];

        if (FirstDynamic == id) {
            FirstDynamic = next;
        } else {
            NextDynamic[prev] = next;
        }

        if (next > 0) {
            PrevDynamic[next] = prev;
        }

        NextDynamic[id] = 0;
        PrevDynamic[id] = 0;
    }
}