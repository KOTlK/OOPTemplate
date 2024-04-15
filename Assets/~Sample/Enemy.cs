using UnityEngine;

public class Enemy : Character{
    public int       Target;
    public int       Damage;
    public float     AttackRadius;
    public float     AttackDistance;
    public float     AttacksPerSecond;
    public float     AttackDelay;
    public LayerMask TargetsLayer;
    
    private float _delay;
    
    public bool CanAttack => _delay <= 0;
    
    private static Collider[] _collisionBuffer = new Collider[32];
    
    public override void Execute(){
        Input.Execute();
        base.Execute();
        
        _delay -= Time.deltaTime;
    }
    
    public void Attack(){
        var direction = new Vector3(Mathf.Sin(Input.LookDirection * Mathf.Deg2Rad), 
                                    0, 
                                    Mathf.Cos(Input.LookDirection * Mathf.Deg2Rad));
        var position  = transform.position + direction.normalized * AttackDistance;
        Debug.DrawRay(position, Vector3.up * 3f, Color.red);
        
        var collCount = Physics.OverlapSphereNonAlloc(position, 
                                                      AttackRadius, 
                                                      _collisionBuffer, 
                                                      TargetsLayer.value);
                                                      
        for(var i = 0; i < collCount; ++i){
            var coll = _collisionBuffer[i];
            
            if(coll.gameObject != gameObject){
                if(coll.TryGetComponent(out Character character)){
                    character.ApplyDamage(Damage);
                    break;
                }
            }
        }
        
        _delay = AttackDelay;
    }
}
