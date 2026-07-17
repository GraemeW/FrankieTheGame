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

#### Singleton Generation

The [Core](./Core.prefab) prefab ensures [PersistentObjects](./CoreDep/PersistentObjects.prefab) is generated in the scene only once (i.e. created as a singleton).  [PersistentObjects](./CoreDep/PersistentObjects.prefab) is furthermore tagged with `DontDestroyOnLoad()` ([ref](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.DontDestroyOnLoad.html)), such that it will persistent during transitions from scene (zone)-to-scene (zone).

Thus, any singleton objects that must always remain present in the game should be included under [PersistentObjects](./CoreDep/PersistentObjects.prefab).  Of course:  the singleton pattern should be used sparingly, so any new inclusions should be considered carefully (i.e. **seriously** look into alternate approaches).

#### Scripts & Children

The key scripts attached to [PersistentObjects](./CoreDep/PersistentObjects.prefab) include:
* [InputSystemUIInputModule](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/UISupport.html):  for use with Unity's new input system + UI elements

The key objects childed to [PersistentObjects](./CoreDep/PersistentObjects.prefab) include:
* [SceneLoader](./CoreDep/SceneLoader.prefab):  employs [SceneLoader](../../Scripts/Zones/Transitions/SceneLoader.cs) script to transition across scenes (zones)
* [Fader](./CoreDep/Fader.prefab):  employs [Fader](../../Scripts/Zones/Transitions/Fader.cs) script to add fading screen/transition graphics when entering/exiting both scenes (zones) and combat battles
* [BackgroundMusic](../Sound/BackgroundMusic.prefab):  employs [BackgroundMusic](../../Scripts/Sound/BackgroundMusic.cs) script to add background music to the scene (zone)
* [MapCamera](../Map/MapCamera.prefab):  includes a childed SubCamera and employs [MapCamera](../../Scripts/Rendering/Camera/MapCamera.cs) to generate the mini-map
* [Debugger](./CoreDep/Debugger.prefab):  employs [FrankieDebugger](../../Scripts/Utils/FrankieDebugger.cs) for debug functionality (not for release)

### Addressables Loader (Singleton)

[AddressablesLoader](./CoreDep/AddressablesLoader.prefab) is a singleton tagged with `DontDestroyOnLoad()` ([ref](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.DontDestroyOnLoad.html)), such that it will persistent during transitions from scene (zone)-to-scene (zone).  Note that it is separate from the [PersistentObjects](#persistent-objects-singleton) prefab due to its strict load order requirements.

[AddressablesLoader](./CoreDep/AddressablesLoader.prefab) employs the [AddressablesLoader](../../Scripts/Utils/Addressables/AddressablesLoader.cs) script, which is used to build all the caches for the scriptable objects in [OnLoadAssets](../OnLoadAssets/).  In other words, [AddressablesLoader](./CoreDep/AddressablesLoader.prefab) serves to load into memory & establish references to any data that is **not** present in the active scene.

Thus, [AddressablesLoader](./CoreDep/AddressablesLoader.prefab) creates the caches to allow us to **dynamically** (i.e. during run-time):
* [BattleActions](../OnLoadAssets/BattleActions/):  use arbitrary actions
* [CharacterProperties](../OnLoadAssets/CharacterProperties/):  load any character/NPC into scenes
* [InventoryItems](../OnLoadAssets/Inventory/):  use arbitrary items
* [Quests](../OnLoadAssets/Quests/):  add/complete/delete any quests
* [Skills](../OnLoadAssets/Skills/):  use arbitrary skills
* [Zones](../OnLoadAssets/Zones):  transition to any scene (zone)

## Cameras Prefab

[Cameras](./Cameras.prefab) employs the [CameraController](../../Scripts/Rendering/Camera/CameraController.cs) script, which interfaces with the Main Camera child game object and the State Driven Camera game object.  The Main Camera child object simply contains the main Unity [Camera](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.html), as well as the [Cinemachine Brain](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineBrain.html).  

The State Driven Camera child object employs a [Cinemachine State-Driven Camera](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineStateDrivenCamera.html), which allows us to:
* follow the player as they move around the map
* modify the camera zoom as a function of the player's lead character's animator state
  * *so we can add a neat zoom out effect when the player is idle*

The latter functionality is established using two separate virtual cameras (`VCam Active` and `VCam Idle`), which are childed to the State Driven Camera.  The [CameraController](../../Scripts/Rendering/Camera/CameraController.cs) script then ensures that the state-driven camera is correctly following the player's lead party member.

### On Pixel-Perfect Rendering

See [here](../../Scripts/Rendering/README.md#pixel-perfect-rendering) for more discussion on this topic.

## Player Prefab (Singleton)

[Player](./Player.prefab) is a singleton tagged with `DontDestroyOnLoad()` ([ref](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.DontDestroyOnLoad.html)), such that it will persistent during transitions from scene (zone)-to-scene (zone).  Note that it is separate from the [PersistentObjects](#persistent-objects-singleton) prefab due to its strict load order requirements.

### Key Components

The [Player](./Player.prefab) includes a number of important game/control components:
* [Player](../../Scripts/Core/Player.cs):  ensure singleton and handle game loss criteria
* [PlayerController](../../Scripts/Control/Controllers/PlayerController.cs):  standard user input translation script (i.e. for [PlayerStateType](../../Scripts/Core/PlayerStateMachine/PlayerStateType.cs) : inWorld)
* [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs):  primary game state machine for different [IPlayerState](../../Scripts/Core/PlayerStateMachine/PlayerStates/IPlayerState.cs)
  * e.g. including hand-off from the [PlayerController](../../Scripts/Control/Controllers/PlayerController.cs) to alternate [Controllers](../Controllers/)
* [PlayerMover](../../Scripts/Control/Movement/PlayerMover.cs):  character movement through the world (based on input from [PlayerController](../../Scripts/Control/Controllers/PlayerController.cs))
  * [PathFinder](../../Scripts/Control/Movement/PathFinding/PathFinder.cs):  interfaces with PlayerMover to allow for A* pathfinding (as-needed for cutscenes, where player control is handled by the game engine instead of player input)
* [Party](../../Scripts/Stats/Party/Party.cs):  add/remove characters to active party & queries for associated party state
  * [InactiveParty](../../Scripts/Stats/Party/InactiveParty.cs):  manages save state for characters not currently in party
  * [PartyAssist](../../Scripts/Stats/Party/PartyAssist.cs):  handles add/remove for 'assisting' characters (i.e. not official party members)
  * [PartyKnapsackConduit](../../Scripts/Stats/Party/PartyKnapsackConduit.cs):  single entry point for caching and accessing party knapsack data
  * [PartyCombatConduit](../../Scripts/Stats/Party/PartyCombatConduit.cs):  single entry point for caching and accessing party combat data
* [Wallet](../../Scripts/Inventory/Wallet.cs):  add/remove funds to the player & queries for associated wallet state
* [Shopper](../../Scripts/Inventory/Shopper.cs):  interfacing with [Shops](../../Scripts/Inventory/Shop.cs) to purchase/sell [items](../OnLoadAssets/Inventory/)
* [QuestList](../../Scripts/Quests/QuestList.cs):  add/remove quests, complete quest objectives/disburse rewards & queries for associated quest state
* [SaveableEntity](../../Scripts/Saving/SaveableEntity.cs):  tags [Player](./Player.prefab) for saving with the [SaveSystem](../../Scripts/Saving/)

Notes:
1. Further detail on input/control and interfacing with the [PlayerStateMachine](../../Scripts/Core/PlayerStateMachine.cs) is provided in [Controllers](../Controllers/)
2. Rigidbodies and collider physics are handled on the individual character objects present in the party / party assist

### Key Children

The [Player](./Player.prefab) also has several child game objects for interfacing with above components, including:
* `InteractionCenterPoint`:  used by the [PlayerController](../../Scripts/Control/Controllers/PlayerController.cs) as the source point for raycasting
* `PartyContainer`:  used by the [Party](../../Scripts/Stats/Party/Party.cs) as the parent object for placing [character](../CharacterObjects/PCs/) prefabs
  * by default [Frankie](../CharacterObjects/PCs/Frankie/Frankie.prefab) is placed in the party and is default the `PartyLeader`
  * , but in gameplay he can be removed/replaced with other characters
* `PartyAssistContainer`:  used by the [PartyAssist](../../Scripts/Stats/Party/PartyAssist.cs) as the parent object for placing [assist](../CharacterObjects/PCs/Assist/) prefabs
