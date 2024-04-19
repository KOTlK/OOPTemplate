using UnityEngine;
using System;

[Flags]
public enum EntityFlags
{
    None    = 0,
    Static  = 1 << 1,
    Dynamic = 1 << 2
}

public enum EntityType
{
    Utility,
    // types goes here
}

public class Entity : MonoBehaviour{
    public int           Id;
    public bool          Alive;
    public EntityFlags   Flags;
    public EntityType    Type;
    public EntityManager Em;
    public bool          AutoBake;
    
    private void Awake(){
        if(AutoBake){
            if(Singleton<EntityManager>.Exist){
                Singleton<EntityManager>.Instance.BakeEntity(this);
            }
        }
    }
    
    public virtual void OnCreate(){ }
    public virtual void Execute(){ }
    
    public virtual void Destroy(){
        Destroy(gameObject);
    }
}
