using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using Reflex.Core;
using Reflex.Enums;

public class GameUpdate {};

public static class Main {
    public static string Localization        = "eng";
    public static string UIParentLocation    = "GlobalCanvas";
    public static string EventSystemLocation = "GlobalEventSystem";

    public static EntityManager Em;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize() {
        Config.ParseVars();
        Locale.LoadLocalization(Localization);
        AssetManager.Init();
        Events.Init();
        SaveSystem.Init();
        Em = new();

        // Inject new system
        var defaultLoop = PlayerLoop.GetDefaultPlayerLoop();

        var newSystem = new PlayerLoopSystem {
            subSystemList  = null,
            updateDelegate = UpdateFunc,
            type           = typeof(GameUpdate),
        };

         PlayerLoopSystem newPlayerLoop = new() {
            loopConditionFunction = defaultLoop.loopConditionFunction,
            type                  = defaultLoop.type,
            updateDelegate        = defaultLoop.updateDelegate,
            updateFunction        = defaultLoop.updateFunction
        };

        var newSubSystemList = new List<PlayerLoopSystem>();

        if (defaultLoop.subSystemList != null) {
            for (var i = 0; i < defaultLoop.subSystemList.Length; i++) {
                if (defaultLoop.subSystemList[i].type == typeof(UnityEngine.PlayerLoop.Update)) {
                    newSubSystemList.Add(newSystem);
                }
                newSubSystemList.Add(defaultLoop.subSystemList[i]);
            }
        }

        newPlayerLoop.subSystemList = newSubSystemList.ToArray();
        PlayerLoop.SetPlayerLoop(newPlayerLoop);

        ContainerScope.OnRootContainerBuilding  += InstallBindings;
        ContainerScope.OnSceneContainerBuilding += InstallSceneBindings;
        Application.quitting += OnApplicationQuit;

        var eventSystem = AssetManager.Instantiate<EventSystem>(EventSystemLocation);
        UnityEngine.Object.DontDestroyOnLoad(eventSystem);
    }

    // When domain reload disabled, it should be called to prevent multiple calls of OnApplicationQuit.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Application.quitting -= OnApplicationQuit;
    }

    // Install global bindings here.
    public static void InstallBindings(ContainerBuilder builder) {
        // I need this because the fucking root container is hidden somewhere and I don't want to search for it.
        builder.RegisterType(typeof(ReflexRootContainer), 
                             Lifetime.Singleton,
                             Reflex.Enums.Resolution.Eager);
        builder.RegisterType(typeof(EntityManager), 
                             Lifetime.Singleton, 
                             Reflex.Enums.Resolution.Eager);
        builder.RegisterType(typeof(UIEntityManager), 
                             Lifetime.Singleton, 
                             Reflex.Enums.Resolution.Eager);

        var uiParent = AssetManager.Instantiate<Canvas>(UIParentLocation);
        UnityEngine.Object.DontDestroyOnLoad(uiParent);

        builder.RegisterValue(uiParent);

        // Register all the game systems here.
        builder.RegisterType(typeof(Game), 
                             Lifetime.Singleton, 
                             Reflex.Enums.Resolution.Eager);
        builder.RegisterType(typeof(EntitiesUpdateSystem),
                             Lifetime.Singleton,
                             Reflex.Enums.Resolution.Eager);
        builder.RegisterType(typeof(UIEntitiesUpdateSystem),
                             Lifetime.Singleton,
                             Reflex.Enums.Resolution.Eager);
        builder.RegisterType(typeof(QuitGameSystem),
                             Lifetime.Singleton,
                             Reflex.Enums.Resolution.Eager);
    }

    // Update function
    public static void UpdateFunc() {
        Clock.Update();
        Game game = null;
        EntityManager em = null;
#if UNITY_EDITOR
        try {
            game = ReflexRootContainer.Container.Resolve<Game>();
            em = ReflexRootContainer.Container.Resolve<EntityManager>();
        } catch {
        }
#else
        game = ReflexRootContainer.Container.Resolve<Game>();
        em = ReflexRootContainer.Container.Resolve<EntityManager>();
#endif
        game?.Update();
    }

    // Install additional scene bindings here.
    public static void InstallSceneBindings(Scene scene, ContainerBuilder builder) {

    }

    // Destructor logic here
    public static void OnApplicationQuit() {
        SaveSystem.Dispose();
        Game game = null;
#if UNITY_EDITOR
        try {
            game = ReflexRootContainer.Container.Resolve<Game>();
        } catch {
        }
#else
        game = ReflexRootContainer.Container.Resolve<Game>();
#endif
        game?.Destroy();
    }
}
