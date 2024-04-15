using UnityEngine;

public class Character : Entity{
    public float               Speed;
    public int                 Health;
    public CharacterInput      Input;
    public CharacterController CharacterController;
    public Weapon              WeaponPrefab;
    public Transform           WeaponSlot;
    
    private Weapon _weapon;
    
    public override void OnCreate(){
        _weapon = (Weapon)Em.CreateEntity(WeaponPrefab, WeaponSlot.position, Quaternion.identity, Vector3.one, WeaponSlot);
        _weapon.Owner = this;
    }
    
    public override void Execute(){
        CharacterController.Move(Input.MoveDirection * Speed * Time.deltaTime);
        
        if(Input.Shooting){
            _weapon.Shoot(new Vector3(Mathf.Sin(Input.LookDirection * Mathf.Deg2Rad), 
                                      0, 
                                      Mathf.Cos(Input.LookDirection * Mathf.Deg2Rad)));
        }
        
        transform.rotation = Quaternion.AngleAxis(Input.LookDirection, Vector3.up);
    }
    
    public void ApplyDamage(int amount){
        Health -= amount;
    }
}
