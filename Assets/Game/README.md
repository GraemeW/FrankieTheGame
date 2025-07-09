# Assets: Game

This directory houses all the physical Frankie game files -- including game object prefabs, scriptable objects, shaders, cinematics, configuration files, etc.  

Notably, this directory also includes all [OnLoadAssets](./OnLoadAssets/), which refers to files loaded during run-time via the Unity Addressables system.  These are thus files that are not necessarily present in a given scene, but are nonetheless critical for game functionality: character properties, inventory items, battle actions/skills, quests and zone properties.

## Summary of Game Data Categories

A brief summary of each sub-directory within `Assets: Game` is provided below.  Further detail is provided in the corresponding/linked folder (if requiring further detail).

|                Directory                |                                                                                              Detail                                                                                              |
| :-------------------------------------: | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: |
|             [Core](./Core/)             |                        Prefabs for various core Game Objects: e.g. Player, Core (incl. persistent object loader), AddressablesLoader, Saving, SceneLoader, Cameras, etc.                         |
|           [Camera](./Camera/)           |                                                                           Scriptable objects for camera configuration                                                                            |
|      [Controllers](./Controllers/)      | Prefabs for key control game objects: [BattleController](./Controllers/Battle%20Controller.prefab), [DialogueController](./Controllers/DialogueController.prefab) && Splash/StartMenuControllers |
|     [OnLoadAssets](./OnLoadAssets/)     |            Scriptable objects loaded via the AddressablesLoader (game memory outside of objects stored in scenes) - *See [OnLoadAssets](./OnLoadAssets/) directory for more detail)*             |
| [CharacterObjects](./CharacterObjects/) |                    Prefabs for individual characters (both PCs, NPCs) w/ associated animations, scriptable objects for character stat progression, and prefabs for wearables                     |
|           [Speech](./Speech/)           |                          Scriptable objects for any game dialogue, as employed by NPCs via AIConversant, with custom UnityUI editor for linking together complex speech                          |
|           [Combat](./Combat/)           |                                       Scriptable objects for character skill trees, for battle action targeting/effects/filters, and for NPC battle logic                                        |
|           [Checks](./Checks/)           |                                    Standard player-world interaction prefabs (e.g. to child to game objects to prompt arbitrary Unity Events & simple menus)                                     |
|       [Predicates](./Predicates/)       |                      Scriptable objects for any conditional / predicate logic associated with e.g. player progression, game state, character configuration, key items, etc.                      |
|     [WorldObjects](./WorldObjects/)     |                Prefabs for anything placed in a scene: buildings, trees, lights, desks, posters, crates, beds, ice cream cones, ATMs, vehicles, signs, windows, bumper cars, etc.                |
|               [UI](./UI/)               |               Prefabs for all standard UI elements: e.g. windows, menus (stats, abilities, knapsack, equipment, etc.), dialogue boxes, canvases, battle elements/frames, and so on               |
|            [Sound](./Sound/)            |                                      Music mixer & soundbox prefabs -- background music, music overrides, and sound effects to child to other game objects                                       |
|              [VFX](./VFX/)              |                                              Particle system prefabs, and shaders (e.g. for battle background, combat swirl, battle effects, etc.)                                               |
|       [Cinematics](./Cinematics/)       |                                                                                    Cinematic cut-scene files                                                                                     |
|         [Tilemaps](./Tilemaps/)         |                                                                      Tilemap prefabs and scriptable objects for rule tiles                                                                       |
|              [Map](./Map/)              |                                                                            Prefabs and configurables for the mini-map                                                                            |
|             [Misc](./Misc/)             |                                                                  Prefabs for standard rooms and standard patrol paths/waypoints                                                                  |
