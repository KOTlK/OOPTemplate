using UnityEngine;

public class Weapon : Entity{
    public Entity     Owner;
    public Projectile BulletPrefab;
    public Transform  Muzzle;
    public float      FireRate; //bullets per second;
    public bool       CanShoot;
    
    private float _timePassed;
    
    public override void Execute(){
        if(CanShoot)
            return;
            
        _timePassed += Time.deltaTime;
        
        if(_timePassed >= 1 / FireRate){
            CanShoot = true;   
        }
    }
    
    public void Shoot(Vector3 direction){
        if(CanShoot){
            var bullet = (Projectile)Em.CreateEntity(
            BulletPrefab, 
            Muzzle.position, 
            Quaternion.Euler(direction), 
            BulletPrefab.transform.localScale);
            
            bullet.Shoot(direction, Owner);
        }
    }
}