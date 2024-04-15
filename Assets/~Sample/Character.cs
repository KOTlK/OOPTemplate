using UnityEngine;

public class Character : Entity{
    public float               Speed;
    public int                 Health;
    public CharacterInput      Input;
    public CharacterController CharacterController;

    public bool                IsDead => Health <= 0;
    
    public override void Execute(){
        Move(Input.MoveDirection * Speed);
        Rotate(Quaternion.AngleAxis(Input.LookDirection, Vector3.up));
    }
    
    public virtual void Move(Vector3 move){
        CharacterController.SimpleMove(move);
    }
    
    public virtual void Rotate(Quaternion rotation){
        transform.rotation = rotation;
    }
    
    public void ApplyDamage(int amount){
        Health -= amount;
        if(Health <= 0){
            Em.DestroyEntity(Id);
        }
    }
}
