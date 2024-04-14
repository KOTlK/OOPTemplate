using UnityEngine;

public enum EntityFlags
{
    Static  = 1 << 1,
    Dynamic = 1 << 2
}

public enum EntityType
{
    // types goes here
}

public class Entity : MonoBehaviour{
    public int           Id;
    public bool          Alive;
    public EntityFlags   Flags;
    public EntityType    Type;
    public EntityManager Em;
    
    public virtual void OnCreate(){ }
    public virtual void Execute(){ }
    
    public virtual void Destroy(){
        Destroy(gameObject);
    }
}
