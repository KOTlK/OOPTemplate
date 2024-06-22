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
    Utility,
    // types goes here
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
    public int QueryNearbyEntities(float radius, int[] buffer, bool includeStatic = true) {
        return Em.World.QueryNearbyEntities(transform.position, buffer, radius, includeStatic);
    }
}