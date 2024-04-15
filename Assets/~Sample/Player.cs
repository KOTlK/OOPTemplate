using UnityEngine;

public class Player : Character{
    public Weapon    WeaponPrefab;
    public Transform WeaponSlot;
    
    private Weapon _weapon;

    public override void OnCreate(){
        _weapon = (Weapon)Em.CreateEntity(WeaponPrefab, 
                                          WeaponSlot.position, 
                                          Quaternion.identity, 
                                          Vector3.one, 
                                          WeaponSlot);
        _weapon.Owner = this;
    }
    
    public override void Execute(){
        base.Execute();
        if(Input.Shooting){
            _weapon.Shoot(new Vector3(Mathf.Sin(Input.LookDirection * Mathf.Deg2Rad), 
                                      0, 
                                      Mathf.Cos(Input.LookDirection * Mathf.Deg2Rad)));
        }
    }
}
