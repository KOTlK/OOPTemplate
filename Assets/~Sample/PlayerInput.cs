using UnityEngine;

public class PlayerInput : CharacterInput{
    public Camera    MainCamera;
    public Transform Target;
    
    public override void Execute(){
        if(Target == null){
            return;
        }
        
        var h         = Input.GetAxis("Horizontal");
        var v         = Input.GetAxis("Vertical");
        var position  = MainCamera.WorldToScreenPoint(Target.position);
        var cursorPos = Input.mousePosition;
        var direction = cursorPos - position;
        var angle     = -(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        
        MoveDirection = new Vector3(h, 0, v);
        LookDirection = angle;
        Shooting      = Input.GetKey(KeyCode.Mouse0);
    }
}
