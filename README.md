# OOP Template
Easy-to-use object oriented template for Unity.

# Table of content
- [OOP Template](#oop-template)
- [Table of content](#table-of-content)
- [Installation](#installation)
- [Entity System](#entity-system)
  - [Entity](#entity)
  - [EntityHandle](#entityhandle)
  - [Entity Manager](#entity-manager)
- [Saving](#saving)
- [Config](#config)
    - [How to use:](#how-to-use)
- [Events](#events)
- [Resource Management](#resource-management)
- [Localization](#localization)
    - [Editor overview](#editor-overview)
    - [Locale](#locale)
- [Coroutines](#coroutines)
- [UIManager](#uimanager)
- [ComponentSystem](#componentsystem)


# Installation
    Just copy everything into Assets directory.

# Entity System

## Entity
Game entities should inherit base `Entity` class.
Each entity contains `EntityManager` it was created with inside `Em` field.
The entities does not use Unity's Update, FixedUpdate, etc. methods.
Override `OnCreate` method to do something, when object gets created.
Override `OnBaking` method if your entity placed as scene object and needs `OnCreate` functionality.
Override `Destroy` method to execute code when the entity gets destroyed.
To update entity each frame, override `Execute` method. It is the same as writing Unity's `Update` function.
To get `FixedUpdate` functionality, override `UpdatePhysics` method.
To move entity, use `MoveEntity` method and pass the new position. You need to do it if you want your entity to be correctly updated inside spatial grid.
`Entity` also contains some helper functions and properties:
- `Position, Rotation, Scale, ObjectToWorld` to access it's position, rotation, localScale and local to world matrix.
- `QueryNearbyEntities` to query entities in certain radius from the entity. Spatial grid, mentioned above, used for it.
- `GetEntity` and `GetEntity<T>` to get entity from the [EntityManager](#entity-manager) your entity was created with.
- `DestroyThisEntity` and `DestroyEntity` to destroy your entity or some other entity.
- `Save` and `Load` methods to save/load your entity (more about it in [Saving](#saving) topic).

## EntityHandle
`EntityHandle` is a struct to get access to entities instead of doing it standard way (using references).
To get instance of the entity, use Entity's helper functions or `EntityManager.GetEntity` methods.
Neither of this methods return only reference. Instead they return boolean value, indicating if entity is alive and a reference.
Always check if entity is alive:
``` C#
if(GetEntity(handle, out var entity) ...);
```
Using `EntityHandle` instead of simply references, helps with saving the game and with forgetting to check if referenced entity is not null.

## Entity Manager
All your game entities should be created and destroyed with `EntityManager`.
For this purposes, it has `CreateEntity` and `CreateEntityReturnReference` methods.
The former returns [EntityHandle](#entityhandle), while the latter [Entity](#entity). For most cases you need only the first one.
The `EntityManager`, as the name says, manages all the entities. It updates them when they need to, updates their positions inside spatial grid, moves them, etc...

# Saving
The template comes with build-in saving support.  
Before using `SaveSystem`, you should initialize it with `SaveSystem.Init()` method.  
To initiate saving, call `SaveSystem.BeginSave()` function. It will return you an instance of [BinarySaveFile](src/Saving/BinarySaveFile.cs).  
Save your objects via `BinarySaveFile.Write(obj)` method.  
End saving, calling `SaveSystem.EndSave(string saveFileName)` function.  

Each field of an object will be saved automatically.  
If you don't want field to be saved, mark it with [DontSave] attribute.  
You can mark field with [`[V(uint version)]`](src/Saving/SaveAttributes.cs) attribute to provide field version. And you can mark type with [`[Version(uint version)]`](src/Saving/TypeVersion.cs) attribute to provide type's version.  
If type's version is lower than the field's version, that means, that the field does not exist in the save file, thus it won't be loaded.  
If you want manually save/load the type's data, you can create method with the name `Save`/`Load` that accepts [BinarySaveFile](src/Saving/BinarySaveFile.cs) as a parameter and write your saving/loading logic there. You don't need an interface or something else. You can see [EntityManager](src/Entities/EntityManager.cs) for an example.  

If you want to execute some code after loading is complete, subscribe to `Action<ISaveFile> LoadingOver` event.

You can use `SaveSystem.GetAllSaves()` method to get all save files.
You can always change the directory, where your save files stored, in [SaveSystem](src/Saving/SaveSystem.cs).

# Config
[Config](src/Utility/Config.cs) is just a simple way of storing global config data in a single place.
It loads the data from a [config file](StreamingAssets/Config.cfg).
### How to use:
Add the data you need into a [Config](src/Utility/Config.cs) class.
``` C#
public struct SomeData {
    public int   Int;
    public float Float;
}

public static class Config {
    public SomeData Data;
    public Vector3  Vector;
    public string   Text;
}
```

Change your [config file](StreamingAssets/Config.cfg)
``` C#
Vector (12.4, 55, 90.2)
Text   "Hello World!"

[SomeData]
Int   4221
Float 13.37
```

Call `ParseVars()` function. All the additional information can be found in [Config.cs](src/Utility/Config.cs) and [Config.cfg](StreamingAssets/Config.cfg) files.


# Events
[Events](src/Events/Events.cs) contains general events and private events.
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
Do not use it for processing input events, it's not made for it. Also it is <span style="color:rgb(255, 0, 0)">NOT</span> a replacement for C# events.

# Resource Management
Using the [ResourceManager](src/Resource/ResourceManager.cs) you can load/unload/instantiate game resource.
The template forces you to use AssetBundles for your assets pipeline.
To build the bundles, use `Asset Bundles/Build Bundles` in Unity's menu.
Unfortunately, you always have to rebuild your bundles, when any of the assets changes. Maybe later I will make partial assets rebuild.
When you call `ResourceManager.LoadAsset(name)`/`ResourceManager.LoadBundle(name)`, or it's async versions, it will return you the handle to an asset. Using this handle, you can:
 - Instantiate the object with `Instantiate<T>(...)` methods.
 - Get the loading progress of the asset/bundle with `float GetLoadingProgress(AssetHandle handle)` method. The progress is in [0-1] range. Note, that, if loading operation progress equals to 1, but the onLoad action is not yet performed, you should wait for that action. Unity doing it's dirty work.

Make sure you always load bundle, containing the asset, before loading an asset.
Typical use case:
``` C#
class Main :

void Start() :
    ResourceManager.Initialize();
    ResourceManager.LoadBundle("playables");
    ResourceManager.LoadBundle("music");
    ResourceManager.LoadBundle("sfx");

    var handle = ResourceManager.LoadAsset("player");

    var player = ResourceManager.Instantiate<Player>(handle, Vector3.zero, Quaternion.identity);
    player.DoStuff();
```

or you can use [EntityManager](#entity-manager) instead of managing assets:
``` C#
class Main :
EntityManager Em;

void Start() :
    ResourceManager.Initialize();
    ResourceManager.LoadBundle("playables");
    ResourceManager.LoadBundle("music");
    ResourceManager.LoadBundle("sfx");

    player = Em.CreateEntity<Player>("player", Vector3.zero, Quaternion.identity);
    player.DoStuff();
```

Multiple Assets loading:
``` C#
using static ResourceManager;

Handle = LoadBundles(OnBundleLoad, "playables", "music", "environment")

void OnBundlesLoad() :
    Handle = LoadAssets(OnAssetsLoad, "player", "battle_music", "chair");

void OnAssetsLoad() :
    instantiate assets

```

# Localization
Use `Localization/Editor` button to open localization editor.
Inside this editor you can load/save/make new localization entries.
The localization entry consist of identifier, tag and text.
You can add new tags, by modifying `LocalizationTag` enum inside [Locale](src/Localization/Locale.cs) file.
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
Using [Locale](src/Localization/Locale.cs) you can load the localization file, by calling `Locale.LoadLocalization(name)`. Name is just the name of file, without the extension.
And get string by it's identifier. Use `Locale.Get(ident)` to do it.
To help with identifiers, you have [LocalizedString](src/Localization/LocalizedString.cs).
It has custom property drawer. Add it to your class, copy the identifier of your string from editor, by clicking copy button, paste it, and the inspector will show you the string or an error if you made a mistake somewhere. Make sure to load the localization, using `Localization/Load English`.
Use `LocalizedString.Get()` and `LocalizedString.Get(int id)` methods to get a string. The first method used, when you need only one string and the second one, when you need multiple strings.
You can also subscribe to `Locale.LocalizationLoaded` event to update your text if localization changed.

Here is working code sample:
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

You can see localization file examples inside `StreamingAssets/Localization` directory.

# Coroutines
Works like Unity's coroutines, but don't need GameObject to run.
Call `Coroutines.InitCoroutines()` in the initialization code to init the module. And `Coroutines.RunCoroutines()` somewhere in your update code ([here](src/Main.cs)) to update coroutines. Don't forget to call it, if you use `ResourceManager`, it depends on coroutines.
Unity's build-in coroutines are not supported (WaitForSeconds etc.).
Start coroutine with `Coroutines.BeginCoroutine()`. This function will return you `CoroutineHandle`. Which you can use to stop coroutine, using `Coroutines.EndCoroutine(handle)` and get coroutine's status with `Coroutines.GetCoroutineStatus(handle)`.
The naming differs from Unity's, because c# static dispatch can't handle it 🙂.

``` C#
using static Coroutines;

public CoroutineHandle Handle;

public void Start() {
    Handle = BeginCoroutine(CustomCoroutine(10f, 20));
}

public void Update() {
    if (Input.GetKeyDown(KeyCode.Space)) {
        EndCoroutine(Handle);
    }
}

private IEnumerator CustomCoroutine(float wait, int countTo) {
    Debug.Log("Enter");

    var i = 0;

    while(i < countTo) {
        Debug.Log("Counting");
        i++;
    }

    Debug.Log("Counted");

    Debug.Log("Waiting");

    yield return Wait(wait);

    Debug.Log("Done");
}
```

# UIManager
Before using the module, initialize it with `Init(canvas, parent)`
by default parent is canvas.  
Configure UI update frequency by changing `UIFps` field.  
Register dependencies with `RegisterDependencies` or `RegisterDependency` functions.

To create ui elements, use `MakeUIElement` functions.
Functions with string as first argument are loading asset from `ResourceManager`
before making an element.

To bake already existing element, use `BakeUIElement` function or
attach [UIBaker](src/ui/UIBaker.cs) component to the element and it will automatically bake
the element on Start.

To bind already existing element with a name, use `BindUIElement` function or
attach [UIBinder](src/ui/UIBinder.cs) component to the element and it will automatically bind
the element on Start.

To make unique ui element, use `MakeUniqueUI<T>`.
Access unique element with `GetUniqueUI<T>`.

Update ui by calling UpdateUI and UpdateLateUI.

Here some ways of how you can display score, using the `UIManager`

Use dependency container:
``` C#
public class ScoreUI : UIElement {
    public TMP_Text Text;
    public Score    Score;

    public override void ResolveDependencies(Container c) {
        Score = c.Resolve<Score>();
    }

    public override void UpdateElement(float dt) {
        Text.text = Score.Total.ToString();
    }
}
```

Access unique ui element:
``` C#
class Score {
    uint total_score;

    void IncreaseScore(uint count) {
        total_score += count;
        UIManager.GetUnique<ScoreUI>().UpdateTotalScore(total_score);
    }
}
```

Update element by accessing score directly
``` C#
class Score {
    uint total_score;

    void IncreaseScore(uint count) {
        total_score += count;
    }
}

class ScoreUI : UIElement {
    TMP_Text text;

    override void UpdateElement(float dt) {
        var score = Singleton<Score>.Instance;
        text.text = score.total_score;
    }
}
```

You don't need mvc if you use your brain instead of reading articles on hackernews.  

`UIElements` can be separated by update time.  
`UpdateUI` should be called before gameplay code and `UpdateLateUI` after.  
Don't forget to configure flags on `UIElement`.  
If it has `Dynamic` flag, it will be updated during `UpdateUI`.
If it has `UpdateLately` flag, it will be updated during `UpdateLateUI`.  
If neither of flags are set it won't be updated at all.

# ComponentSystem
The example project using it, can be found [HERE](https://github.com/KOTlK/FireSpread).  
With component system you can add component to an Entity.  
This is not ECS, you can't filter entities, there is no archetypes, etc.  
Prefere to use fat struct for your component instead of having more components.  
Before using each component system, you should initialize it with  
`ComponentSystem<T>.Make(UpdateFunc)`.  
`UpdateFunc` is a method with signature: `void UpdateFunc(T[] components, int count)`.  
In the update function iterate through components and do whatever you want to.  
⚠ Always start iteration from 1. `for(int i = 1; i < count; i++)`.  
Or you can use `ComponentSystem<T>.Iterate()` to iterate through components, but it's slower.  
`foreach(var component in ComponentSystem<Component>.Iterate()) ...`  
Update system directly with `ComponentSystem<T>.Update()`.  
Add, remove, get, check component, using `ComponentSystem<T>.Add/Remove/Get/Has()` or  
Use extension methods for `EntityHandle` and `Entity`:  
`handle.AddComponent<Component>(new Component())`,  
`entity.AddComponent<Component>()`.  