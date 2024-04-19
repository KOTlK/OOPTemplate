using UnityEngine;

public class Main : MonoBehaviour{
    public EntityManager EntityManager;
    public TaskRunner    TaskRunner;
    
    private void Awake(){
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
    }
    
    private void Start(){
        EntityManager.BakeEntities();
    }
    
    private void Update(){
        
    }
}
