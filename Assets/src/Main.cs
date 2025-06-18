using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Main : MonoBehaviour {
    public TextAsset      VarsAsset;
    public EntityManager  EntityManager;
    public TaskRunner     TaskRunner;
    public SaveSystem     SaveSystem;

    private void Awake() {
        Config.ParseVars();
        TaskRunner     = new TaskRunner();
        SaveSystem     = new SaveSystem();
        Events.Init();

        Singleton<SaveSystem>.Create(SaveSystem);
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);

        Assets.InitializeAssets();
    }

    private void OnDestroy() {
        SaveSystem.Dispose();
        Assets.FreeAssets();
    }

    private void Start() {
        EntityManager.BakeEntities();
    }

    private void Update() {
        Clock.Update();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
        Events.ExecuteAll();
        EntityManager.Execute();
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
