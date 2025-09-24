using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using static Context;
using static UIManager;

public class Main : MonoBehaviour {
    public EntityManager  EntityManager;
    public Canvas         UIParent;
    public TaskRunner     TaskRunner;
    public string         Localization = "eng";

    private void Awake() {
        Config.ParseVars();
        Locale.LoadLocalization(Localization);
        InitContext(EntityManager);
        Coroutines.InitCoroutines();
        TaskRunner = new TaskRunner();
        Events.Init();
        ResourceManager.Initialize();
        SaveSystem.Init();

        UIManager.Init(UIParent, UIParent.transform);
        UIManager.RegisterDependencies(EntityManager);

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
        UpdateUI(Clock.Delta);
        Coroutines.RunCoroutines();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
        EntityManager.Execute();
        UpdateLateUI(Clock.Delta);
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
