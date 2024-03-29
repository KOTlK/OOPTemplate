using UnityEngine;

public enum EntityFlags
{
    Static,
    Dynamic
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
    
    public virtual void Execute(){ }
    
    public virtual void Destroy(){
        Destroy(gameObject);
    }
}
