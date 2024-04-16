using UnityEngine;

public class EnemyAi : CharacterInput{
    public Enemy Enemy;
    
    private void Awake(){
        if(Singleton<Player>.Exist){
            Enemy.Target = Singleton<Player>.Instance.Id;
        }
    }
    
    public override void Execute(){
        var target = (Character)Enemy.Em.GetEntity(Enemy.Target);
        
        if(target != null && target.IsDead == false && target.Alive){
            var direction = target.transform.position - Enemy.transform.position;
            var angle     = -(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            
            direction.y   = 0;
            MoveDirection = direction.normalized;
            LookDirection = angle;
            
            if(direction.magnitude < Enemy.AttackRadius){
                if(Enemy.Attack()){
                    Enemy.Em.DestroyEntity(Enemy.Id);
                }
            }
        }
    }
}
