using UnityEngine;

public class Main : MonoBehaviour {
    public TextAsset     VarsAsset;
    public EntityManager EntityManager;
    public TaskRunner    TaskRunner;
    public Events        Events;
    
    private void Awake() {
        Vars.ParseVars(VarsAsset);
        TaskRunner = new TaskRunner();
        Events     = new Events();
        
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Events>.Create(Events);
    }
    
    private void Start() {
        EntityManager.BakeEntities();
    }
    
    private void Update() {
        Clock.Update();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
    }
}
