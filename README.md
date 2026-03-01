# OOP Template
**Template for Unity, that focus on simplicity.**

# ⚠⚠⚠ Epilepsy Warning ⚠⚠⚠
**If you think incapsulation (hide everything) is the best thing in the world, close the page and contact your doctor.**  
**If you think SOLID is a great thing and everyone should follow it (especially S part), close the page.**  
**If you are clean code guru, close the page.**  
**If you prefere complexity over simplicity, close the page.**  
**There is no MVC/MVP/MVVM/(put your thing, that's being pushed right now as better mvc, even though is't the same fucking concept every fucking time, for sure, this time it will work out, here.). If you like it, I pity you.**  
**Entity System is NOT ECS, but ECS is a part of this template.**  
**If something above triggers you, close the page, don't waste your time.**

# Limitations
 - **Unity version: 6.3 LTS(6000.3.4f1)**  
 - Medium and hard IL2CPP stripping levels kills everything. It can be di container issue (as it affects only the things that are instantiated by it), but, thanks to Unity, I don't even know what exactly getting stripped out.

<details>
<summary> Table of content </summary>

- [OOP Template](#oop-template)
- [⚠⚠⚠ Epilepsy Warning ⚠⚠⚠](#-epilepsy-warning-)
- [Limitations](#limitations)
- [Installation](#installation)
- [Dependency Injection](#dependency-injection)
- [Entry Point](#entry-point)
- [Game Systems](#game-systems)
- [Entity System](#entity-system)
  - [Entity](#entity)
  - [EntityHandle](#entityhandle)
  - [Entity Manager](#entity-manager)
- [Saving](#saving)
- [Config](#config)
    - [How to use:](#how-to-use)
- [Events](#events)
- [Asset Management](#asset-management)
- [Localization](#localization)
    - [Editor overview](#editor-overview)
    - [Locale](#locale)
- [UI](#ui)
- [Ecs](#ecs)
</details>

# Installation
    Press big green "Use this template" button on github or just clone the repository.

# Dependency Injection
[Reflex](https://github.com/gustavopsantos/Reflex) used here as di container.  
Use `Reflex.Attributes.Inject` attribute to inject dependencies.  
Dependencies will be injected into entities created with `EntityManager` automatically.  
For entities use only properties/field injection.  
You should always have game object with `ContainerScope` component on it in your scene, or the dependencies will not be injected into entitie's or MonoBehaviour's.  
Install global bindigs in `InstallBindings` method at [Main](Assets/src/Main.cs).  
To use scene installers, attach installer components to a gameobject with `ContainerScope` component on it.  

# Entry Point
[Main](Assets/src/Main.cs) is a game's entry point.  
It initializes after the first scene loaded and before any `Awake` methods called.  
Here you can create and initialize your game systems, bind them to di container, etc.  
`UpdateFunc` is main update function. It is called before any `MonoBehaviour.Update()` function called. You can add anything you want to it.  
[Initialization](Assets/src/Initialization.cs) is script, that should always be on the initialization scene. It should be used to initialize anything on the first `Awake` call. For now it instantiates ui on canvas. After initialization completed, it raises `InitializationCompleted` event.

# Game Systems
To create custom game system, inherit from [GameSystem](Assets/src/Game/GameSystem.cs) class and override it's methods for your need.  
Register your system in [Main.cs](Assets/src/Main.cs).  
To register system, bind it to the container after `Game` binding.  
To obtain any system, call `GetSystem<T>()` on `Game` instance. It's better not to inject system into other system via di container as it handles cross references badly. Game instance will always be injected into the system automatically as long as it stays in bindings before systems.  

# Entity System

## Entity
Game entities should inherit base `Entity` class. 
Each entity contains `EntityManager` it was created with inside `Em` field.  
Eentities does not use Unity's Update method, they are updated via `EntityManager`. You can use any Unity method if you need it.  
Override `OnCreate` method to do something, when object gets created.  
Override `OnBaking` method if your entity placed as scene object and needs `OnCreate` functionality.  
Override `Destroy` method to execute code when the entity gets destroyed.  
To update entity each frame, override `Execute` method. It is the same as writing Unity's `Update` function.  
`Entity` also contains some helper functions and properties:
- `Position, Rotation, Scale, ObjectToWorld` to access it's position, rotation, localScale and local to world matrix.
- `GetEntity` and `GetEntity<T>` to get entity from the [EntityManager](#entity-manager) your entity was created with.
- `DestroyThisEntity` and `DestroyEntity` to destroy your entity or some other entity.
- `Save` and `Load` methods to save/load your entity (more about it in [Saving](#saving) topic).

## EntityHandle
`EntityHandle` is a struct to get access to entities instead of doing it standard way (references).  
To get entity instance, use Entity's helper functions or `EntityManager.GetEntity` methods.  
Always check if entity is alive:
``` C#
if(GetEntity(handle, out var entity) ...);
```

## Entity Manager
All your game entities should be created and destroyed with `EntityManager`.
For this purposes, it has `CreateEntity<T>` method. You should pass `AssetReference`, name of the Addressable asset or addressable guid of the asset to the method.  
Ireturns [EntityHandle](#entityhandle) and the reference to the entity.

# Saving
The template comes with build-in saving support.  
Before using `SaveSystem`, you should initialize it with `SaveSystem.Init()` method.  
To initiate saving, call `SaveSystem.BeginSave()` function. It will return you an instance of [BinarySaveFile](Assets/src/Saving/BinarySaveFile.cs).  
Save your objects via `BinarySaveFile.Write(obj)` method.  
End saving, calling `SaveSystem.EndSave(string saveFileName)` function.  

Each field of an object will be saved automatically.  
If you don't want field to be saved, mark it with [DontSave] attribute.  
You can mark field with [`[V(uint version)]`](Assets/src/Saving/SaveAttributes.cs) attribute to provide field version. And you can mark type with [`[Version(uint version)]`](Assets/src/Saving/TypeVersion.cs) attribute to provide type's version.  
If type's version is lower than the field's version, that means, that the field does not exist in the save file, thus it won't be loaded.  
If you want manually save/load the type's data, you can create method with the name `Save`/`Load` that accepts [BinarySaveFile](Assets/src/Saving/BinarySaveFile.cs) as a parameter and write your saving/loading logic there. You don't need an interface or something else. You can see [EntityManager](Assets/src/Entities/EntityManager.cs) for an example.  

If you want to execute some code after loading is complete, subscribe to `Action<ISaveFile> LoadingOver` event.

You can use `SaveSystem.GetAllSaves()` method to get all save files.
You can always change the directory, where your save files stored, in [SaveSystem](Assets/src/Saving/SaveSystem.cs).

# Config
[Config](Assets/src/Utility/Config.cs) is just a simple way of storing global config data in a single place.
It loads the data from a [config file](Assets/StreamingAssets/Config.cfg).
### How to use:
Add the data you need into a [Config](Assets/src/Utility/Config.cs) class.
``` C#
public struct SomeData {
    public int   Int;
    public float Float;
}

public static class Config {
    public Vector3  Vector;
    public string   Text;
    public SomeData Data;
    ... the rest of the file
}
```

Change your [config file](StreamingAssets/Config.cfg)  
``` C#
Vector (12.4, 55.0, 90.2)
Text   "Hello World!"

[SomeData]
Int   4221
Float 13.37
```

Call `ParseVars()` function (called automatically in [Main](Assets/src/Main.cs). All the additional information can be found in [Config.cs](Assets/src/Utility/Config.cs) and [Config.cfg](Assets/StreamingAssets/Config.cfg) files.  
Use [ConfigLiveReloadSystem](Assets/src/Game/ConfigLiveReloadSystem.cs) to update config during play mode.  


# Events
[Events](Assets/src/Events/Events.cs) is an event system.  
It contains general events and private events.  
An example of private events can be Player events or Market events.  
First, you need to create an event type. It can be either struct or a class.  
``` C#
public struct SomeEvent {
    public int a;
}

public class SomeEvent2 {
    public int b;
}
```
You can inherit one event from the other, but only the events you subscribe to will be processed. For example.
``` C#
public class SomeEvent3 : SomeEvent2 {
    public int c;
}

public void ProcessEvent2(SomeEvent2 evnt);
public void ProcessEvent3(SomeEvent3 evnt);

Events.SubGeneral<SomeEvent2>(ProcessEvent2); // ok
Events.SubGeneral<SomeEvent3>(ProcessEvent3); // ok
Events.SubGeneral<SomeEvent2>(ProcessEvent3); // can't do this

Events.RaiseGeneral(new SomeEvent2()); // Only ProcessEvent2 will be called.
```

As you can see in the code above, you can subscribe to event using `Events.SubGeneral<EventType>(method)` or `Events.SubPrivate<EventType>(name, method)` and unsubscribe, using `Unsub...` functions.  
To raise event, call `Events.Raise(event)` and pass instance of the event.  

# Asset Management
Using the [AssetManager](Assets/src/Resource/AssetManager.cs) you can load/unload/instantiate game assets.  
It's a simple wrapper of Addressables that makes loading synchronous.  
The `EntityManager` will use AssetManager to load and instantiate entities.  
All entities instantiated synchronous, but you can instantiate their meshes or materials async, using [LoadMeshOnInstantiate](Assets/src/Utility/LoadMeshOnInstantiate.cs) or [LoadMeshOnInstantiate](Assets/src/Utility/LoadMeshOnInstantiate.cs). Just add this components to the object, that contains `MeshFilter` or `Renderer` component and assign reference to mesh/material.  

# Localization
Use `Localization/Editor` button to open localization editor.
Inside this editor you can load/save/make new localization entries.
The localization entry consist of identifier, tag and text.
You can add new tags, by modifying `LocalizationTag` enum inside [Locale](Assets/src/Localization/Locale.cs) file.
### Editor overview
Pressing `New` button will open popup window.
In this window, you describe localization entry.
- Randomize identifier.
- Choose tag.
- Write text.
- Make comment if needed.
- Press Save.

New entry will be displayed in Entries list.
There you can modify it aswell.
Copy button will copy the identifier into the clipboard, you will need it later.
In Advanced options, you can change the location of your localization files, name of the current file, save and load the file.
The default path of localizations is: `StreamingAssets` directory.
Using the search field, you can filter entries by identifier, tag or text.

### Locale
Using [Locale](Assets/src/Localization/Locale.cs) you can load the localization file, by calling `Locale.LoadLocalization(name)`. Name is just the name of file, without the extension.  
And get string by it's identifier. Use `Locale.Get(ident)` to do it.  
To help with identifiers, you have [LocalizedString](Assets/src/Localization/LocalizedString.cs).  
It has custom property drawer. Add it to your class, copy the identifier of your string from editor, by clicking copy button, paste it, and the inspector will show you the string or an error if you made a mistake somewhere. Make sure to load the localization, using `Localization/Load English`.  
Use `LocalizedString.Get()` and `LocalizedString.Get(int id)` methods to get a string. The first method used, when you need only one string and the second one, when you need multiple strings.
You can also subscribe to `Locale.LocalizationLoaded` event to update your text if localization changed.  
Localization files should always be in `StreamingAssets` directory (engine and build).  

Here is code sample:
``` C#
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour {
    public LocalizedString String;
    public TMP_Text        Text;
    public int             Stage = 0;
    public int             MaxStage = 5;

    private void Start() {
        Locale.LocalizationLoaded += UpdateText;
        UpdateText();
    }

    private void OnDestroy() {
        Locale.LocalizationLoaded -= UpdateText;
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Space)) {
            Stage = Mathf.Clamp(Stage + 1, 0, MaxStage - 1);
            if(Stage < MaxStage) {
                UpdateText();
            }
        }

        if(Input.GetKeyDown(KeyCode.Alpha1)) {
            Locale.LoadLocalization("eng");
        }

        if(Input.GetKeyDown(KeyCode.Alpha2)) {
            Locale.LoadLocalization("ru");
        }
    }

    private void UpdateText() {
        Text.text = String.Get(Stage);
    }
}
```

Inside [Prefabs](Prefabs/UI) directory located `LocalizedText` file, simple wrapper on TMP_Text, use it if you need.

You can see localization file examples inside `Assets/StreamingAssets/Localization` directory.

# UI
[UIEntityManager](Assets/src/UI/UIEntityManager.cs) is entity manager that you should use to instantiate [UIElement](Assets/src/UI/UIelement.cs)'s.  
It's almost the same as [EntityManager](#entity-manager), but it doesn't use handles as ui elements are more static then entities.  
To connect game logic and ui, use [Events](#events). Don't use fucking mvc.  
Here is an example of how simple it can be:
``` c#
public class Unit : Entity {
    public Side Side;

    public void Die(CauseOfDeath causeOfDeath) {
        var evnt = new UnitDiedEvent();
        evnt.CauseOfDeath = causeOfDeath;
        evnt.Entity = Handle;
        evnt.Side = Side;
        Events.RaisePrivate<UnitDiedEvent>("units", evnt);
    }
}

public class PlayerUnitScreen : UIElement {
    public override void OnBecameDynamic() {
        Events.SubPrivate<UnitDiedEvent>("units", OnUnitDeath);
    }

    public override void OnBecameStatic() {
        Events.UnsubPrivate<UnitDiedEvent>("units", OnUnitDeath);
    }

    private void OnUnitDeath(UnitDiedEvent evnt) {
        if (evnt.Side != Side.Player) return;

        RemoveUnitCardFromList(evnt.Handle);
    }
}
```

# Ecs
[Ecs](Assets/src/Entities/Ecs/Ecs.cs) is an additional entity system, that you can use along with the default entity system.  
Ecs can be injected via di container in any game system or entity, created by EntityManager.  
To be able to use ecs on entity, you should set `EntityFlags.Ecs` or `EntityFlags.EcsOnly` on the entity.  
Set the first flag if you want to use entity, created by EntityManager(inherited from `Entity`) with ecs.  
Set the second flag if you want to use only ecs for this entity.  
You can create ecs only entity with `Ecs.CreateEntity()`.  
Mark your struct with `[Component]` attribute before using it.  
Add component with `Ecs.AddComponent<T>(handle)`.  
Remove component with `Ecs.RemoveComponent<T>(handle)`.  
Check if entity has component with `Ecs.HasComponent<T>(handle)`.  
Iterate over entities with `Ecs.ForEach<C1, C2>(func)`.  
There is no functionality to exclude components from query as it creates more complexity in gameplay code.  
Here is how you can iterate over entities:
```c#
[Component]
public struct Health {
    public uint Max;
    public uint Current;
}

[Component]
public struct Damage {
    public uint Amount;
}

Ecs.ForEach((uint entity, ref Monster monster, ref Health hp, ref Damage damage) => {
    hp.Current = clamp(hp.Current - damage.Amount, 0, hp.Max);

    if (hp.Current == 0) {
        Ecs.AddComponent<Dead>(entity);
    }
});
```