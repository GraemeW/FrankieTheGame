# Assets:  Game - Core

This folder contains the key game prefabs that **must** be present in a scene in order for the game to funciton (as described in [Scenes](../../Scenes/)).  To re-iterate, a scene in Frankie must include:
* [Core](./Core.prefab)
* [Cameras](./Cameras.prefab)
* [Player](./Player.prefab)

## Core Prefab

The Core game object serves three primary functions:
1. Owner of the [PersistentObjectSpawner](../../Scripts/Core/PersistentObjectSpawner.cs) script
2. Parent of the [AddressablesLoader](./CoreDep/AddressablesLoader.prefab)
3. Parent of the canvases used for rendering UI elements

### Persistent Objects (Singleton)

The [Core](./Core.prefab) prefab ensures [PersistentObjects](./CoreDep/PersistentObjects.prefab) is generated in the scene only once (i.e. created as a singleton).  [PersistentObjects](./CoreDep/PersistentObjects.prefab) is furthermore tagged with `DontDestroyOnLoad()`, such that it will persistent during transitions from scene (zone)-to-scene (zone).

Thus, any singleton objects that must always remain present in the game should be included under [PersistentObjects](./CoreDep/PersistentObjects.prefab).  Of course:  the singleton pattern should be used sparingly, so any new inclusions should be considered carefully (i.e. **seriously** consider alternate approaches instead).

The key scripts attached to [PersistentObjects] include:
* [InputSystemUIInputModule](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/UISupport.html):  for use with Unity's new input system + UI elements

The key objects childed to [PersistentObjects](./CoreDep/PersistentObjects.prefab) include:
* [Saver](./CoreDep/Saver.prefab):  enabling the save system, with scripts:
  * [SavingSystem](../../Scripts/Saving/SavingSystem.cs):  build up save file (find [SaveableEntities](../../Scripts/Saving/SaveableEntity.cs)) and read/write to save file
  * [SavingWrapper](../../Scripts/Core/SavingWrapper.cs):  interfacing the above ^, incorporating playerprefs (save names) && scene loading/transitions
* [SceneLoader](./CoreDep/SceneLoader.prefab):  employs [SceneLoader](../../Scripts/Zones/SceneLoader.cs) script to transition across scenes (zones)
* [Fader](./CoreDep/Fader.prefab):  employs [Fader](../../Scripts/Zones/Fader.cs) script to add fading screen/transition graphics when entering/exiting both scenes (zones) and combat battles
* [BackgroundMusic](../Sound/BackgroundMusic.prefab):  employs [BackgroundMusic](../../Scripts/Sound/BackgroundMusic.cs) script to add background music to the scene (zone)
* [MapCamera](../Map/MapCamera.prefab):  includes a childed SubCamera and employs [MapCamera](../../Scripts/Zones/Map/MapCamera.cs) to generate the mini-map
* [Debugger](./CoreDep/Debugger.prefab):  employs [FrankieDebugger](../../Scripts/Core/FrankieDebugger.cs) to enable debug functionality (not for release)


### Addressables Loader (Singleton)

## Cameras Prefab

## Player Prefab (Singleton)
