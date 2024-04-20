using UnityEngine;

public class WaitForSecondsTask : Task{
    public float Start;
    
    public WaitForSecondsTask(float start){
        Start = start;
    }
    
    public override void Run(){
        Start -= Time.deltaTime;
        
        if(Start <= 0){
            Stop();
        }
    }
}
