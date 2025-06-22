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
```
if(GetEntity(handle, out var entity) ...);
```
Using `EntityHandle` instead of simply references, helps with saving the game and with forgetting to check if referenced entity is not null.

## Entity Manager
All your game entities should be created and destroyed by `EntityManager`.  
For this purposes, it has `CreateEntity` and `CreateEntityReturnReference` methods.  
The former returns [EntityHandle](#entityhandle), while the latter [Entity](#entity). For most cases you need only the first one.  
The `EntityManager`, as the name says, manages all the entities. It updates them when they need to, updates their positions inside spatial grid, moves them, etc...

## Saving

## Config

## Events

## Resource Management