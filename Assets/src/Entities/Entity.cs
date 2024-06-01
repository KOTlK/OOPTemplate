using UnityEngine;
using System;
using System.Runtime.CompilerServices;

[Flags]
public enum EntityFlags
{
    None            = 0,
    Dynamic         = 1 << 1,
    InsideHashTable = 1 << 2,
}

public enum EntityType
{
    Utility,
    // types goes here
}

public struct PackedEntity {
    public Entity        Entity;
    public EntityManager Manager;
    public EntityType    Type;
    public bool          Alive;
}

public class Entity : MonoBehaviour {
    public int           Id;
    public EntityFlags   Flags;
    public EntityType    Type;
    public EntityManager Em;
    public bool          AutoBake;
    
    private void Awake() {
        if(AutoBake) {
            if(Singleton<EntityManager>.Exist) {
                Singleton<EntityManager>.Instance.BakeEntity(this);
            }
        }
    }
    
    public virtual void OnCreate(){ }
    public virtual void Execute(){ }
    
    public virtual void Destroy() {
        Destroy(gameObject);
    }

    public void Move(Vector3 move) {
        transform.position += move;
        Em.MovedEntities.Add(new MovedEntity{
            Id = Id,
            NewPosition = transform.position
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int QueryNearbyEntities(float radius, int[] buffer) {
        return Em.EntitiesTable.Query(transform.position, buffer, radius);
    }
}
