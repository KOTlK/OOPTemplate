using UnityEngine;

public abstract class CharacterInput : MonoBehaviour{
    public Vector3 MoveDirection;
    public float   LookDirection;
    public bool    Shooting;
    
    public abstract void Execute();
}
