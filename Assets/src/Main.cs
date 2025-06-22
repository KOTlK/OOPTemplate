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
        TaskRunner     = new TaskRunner();
        Events.Init();

        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);

        Assets.InitializeAssets();
        SaveSystem.Init();

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        SaveSystem.Dispose();
        Assets.FreeAssets();
        DestroyContext();
    }

    private void Start() {
        EntityManager.BakeEntities();
    }

    private void Update() {
        SingleFrameArena.Free();
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
