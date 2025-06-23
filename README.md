# OOP Template
Easy-to-use object oriented template for Unity.

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

## Saving
The template comes with build-in saving support.  
To save/load your type's data, implement [ISave](Assets/src/Saving/ISave.cs) interface.  
Then, use one of the `SaveSystem`'s methods of saving.  
Depending on the method you choose `SaveSystem` will give you the [ISaveFile](Assets/src/Saving/ISaveFile.cs) instance.  
[ISaveFile](Assets/src/Saving/ISaveFile.cs) is an interface that provides write/read methods to store/load your data.  
The implementation depends on the `SaveType` you pass to `SaveSystem`'s Init function.  

Before using `SaveSystem`, you should initialize it with `void Init(SaveType type = SaveType.Binary)` method, passing save type you want (either text of binary). There are no switching from one save type to another, use only one of them at a time.  
There are two ways of saving your data:
1. Single `void Save(SaveFunc func, string name)` function.
2. Two functions `ISaveFile BeginSave()` and `void EndSave(string name)`.

For the first one you should provide the function that will save the data.  
Here is the signature of this function:
``` C#
delegate void SaveFunc(ISaveFile sf);
```
For the second way you don't need any function. Just get save file by calling `BeginSave()`, write your data into it, and call `EndSave(name)`.    

Same deal with loading data.
```
delegate void LoadFunc(ISaveFile sf);
void Load(LoadFunc func, string fileName);
ISaveFile BeginLoading(string fileName);
void EndLoading();
```

If you want to execute some code after loading is complete, subscribe to `Action<ISaveFile> LoadingOver` event.

### ⚠⚠⚠  
Be careful with loading binary data as it is implemented as simple array of bytes. So the order you save and load data matter. The load order should be the same as save order.  
If you make changes to your data and want to support previous save files, use [Version](Assets/src/Saving/TypeVersion.cs) attribute and save/load your data, based on the version. Example:
``` C#
[Version(5)]
public struct Data : ISave {
    public int    Int;
    public float  Float;
    public double Double = 2323.2323d;

    public void Save(ISaveFile sf) {
        sf.Write(Int);
        sf.Write(Float);
        sf.Write(Double);
    }

    public void Load(ISaveFile sf) {
        Int   = sf.Read<int>();
        Float = sf.Read<float>();

        if(this.GetVersion() > 3) {
            Double = sf.Read<double>();
        }
    }
}
```

Also, you can use `SaveFile[] GetAllSaves()` method to get all save files.  
You can always change the directory, where your save files stored, in [SaveSystem](Assets/src/Saving/SaveSystem.cs).

## Config
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
    public SomeData Data;
    public Vector3  Vector;
    public string   Text;
}
```

Change your [config file](Assets/StreamingAssets/Config.cfg)
``` C#
Vector (12.4, 55, 90.2)
Text   "Hello World!"

[SomeData]
Int   4221
Float 13.37
```

Call `ParseVars()` function. All the additional information can be found in [Config.cs](Assets/src/Utility/Config.cs) and [Config.cfg](Assets/StreamingAssets/Config.cfg) files.


## Events

## Resource Management