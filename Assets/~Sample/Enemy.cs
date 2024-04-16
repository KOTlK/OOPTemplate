using UnityEngine;

public class Enemy : Character{
    public int       Target;
    public int       Damage;
    public float     AttackRadius;
    public LayerMask TargetsLayer;
    
    private static Collider[] _collisionBuffer = new Collider[32];
    
    public override void Execute(){
        Input.Execute();
        base.Execute();
    }
    
    public bool Attack(){
        var position  = transform.position;
        
        var collCount = Physics.OverlapSphereNonAlloc(position, 
                                                      AttackRadius, 
                                                      _collisionBuffer, 
                                                      TargetsLayer.value);
                                                      
        for(var i = 0; i < collCount; ++i){
            var coll = _collisionBuffer[i];
            
            if(coll.gameObject != gameObject){
                if(coll.TryGetComponent(out Character character)){
                    character.ApplyDamage(Damage);
                    return true;
                }
            }
        }
        
        return false;
    }
}
