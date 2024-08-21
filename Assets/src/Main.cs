using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Main : MonoBehaviour {
    public TextAsset      VarsAsset;
    public EntityManager  EntityManager;
    public TaskRunner     TaskRunner;
    public Events         Events;
    public ResourceSystem ResourceSystem;
    public SaveSystem     SaveSystem;
    
    private void Awake() {
        Vars.ParseVars(VarsAsset);
        TaskRunner     = new TaskRunner();
        Events         = new Events();
        ResourceSystem = new ResourceSystem();
        SaveSystem     = new SaveSystem();

        Singleton<SaveSystem>.Create(SaveSystem);
        Singleton<ResourceSystem>.Create(ResourceSystem);
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Events>.Create(Events);
    }

    private void OnDestroy() {
        SaveSystem.Dispose();
    }
    
    private void Start() {
        EntityManager.BakeEntities();
    }
    
    private void Update() {
        Clock.Update();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
    }
    [ConsoleCommand("quit")]
    public static void Quit() {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
