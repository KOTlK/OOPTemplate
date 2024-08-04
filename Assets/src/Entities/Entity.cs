using UnityEngine;
using System;
using System.Runtime.CompilerServices;

[Flags]
public enum EntityFlags
{
    None            = 0,
    Dynamic         = 1 << 1,
    Static          = 1 << 2,
    InsideHashTable = 1 << 3,
}

public enum EntityType
{
    // types goes here
}

public class Entity : MonoBehaviour, ISave  {
    public uint          Id;
    public ResourceLink  PrefabName;
    public EntityFlags   Flags;
    public EntityType    Type;
    public EntityManager Em;
    public World         World;
    public bool          AutoBake;
    
    private void Awake() {
        if(AutoBake) {
            if(Singleton<EntityManager>.Exist) {
                Singleton<EntityManager>.Instance.BakeEntity(this);
            }
        }
    }
    
    public virtual void OnBaking(){ }
    public virtual void OnCreate(){ }
    public virtual void Execute(){ }
    
    public virtual void Destroy() {
        Destroy(gameObject);
    }

    public virtual void Save(SaveFile sf) {
    }

    public virtual void Load(SaveFile sf) {
    }

    public void Move(Vector3 move) {
        transform.position += move;
        Em.MovedEntities.Add(new MovedEntity{
            Id = Id,
            NewPosition = transform.position
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint QueryNearbyEntities(float radius, uint[] buffer, bool includeStatic = true) {
        return World.QueryNearbyEntities(transform.position, buffer, radius, includeStatic);
    }
}