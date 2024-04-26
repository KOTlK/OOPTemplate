using UnityEngine;

public class Main : MonoBehaviour{
    public EntityManager EntityManager;
    public TaskRunner    TaskRunner;
    public Events        Events;
    
    private void Awake(){
        TaskRunner = new TaskRunner();
        Events     = new Events();
        
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Events>.Create(Events);
    }
    
    private void Start(){
        EntityManager.BakeEntities();
    }
    
    private void Update(){
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
    }
}
