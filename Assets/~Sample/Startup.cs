using UnityEngine;

public class Startup : MonoBehaviour{
    public Player        Player;
    public PlayerInput   PlayerInput;
    public EntityManager Em;
    public TaskRunner    TaskRunner;
    public EnemySpawner  SpawnerTask;

    private void Awake(){
        Singleton<EntityManager>.Create(Em);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Player>.Create(Player);
        
        TaskRunner.StartTask(TaskGroupType.Gameplay, SpawnerTask);
    }
    
    private void Update(){
        PlayerInput.Execute();
        Em.Execute();
        
        TaskRunner.RunTaskGroup(TaskGroupType.Gameplay);
    }
}
