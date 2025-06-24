using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using static Context;

[Version(19)]
public class Main : MonoBehaviour {
    public EntityManager  EntityManager;
    public TaskRunner     TaskRunner;

    private void Awake() {
        Config.ParseVars();
        InitContext();
        TaskRunner = new TaskRunner();
        Events.Init();
        ResourceManager.Initialize();
        SaveSystem.Init();

        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        SaveSystem.Dispose();
        ResourceManager.Free();
        DestroyContext();
        SaveSystem.Dispose();
    }

    private void Start() {
        EntityManager.BakeEntities();
    }

    private void Update() {
        SingleFrameArena.Free();
        Clock.Update();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
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
