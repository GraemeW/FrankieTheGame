# Assets: Scenes

## Quick Start:  Setting Up a New Scene

### Scene Templates

Two baseline/template scenes are located in [_Templates](./_Templates/):
* [BASE_Exterior](./_Templates/BASE_Exterior.unity): For outdoor-type scenes (broad area, entry into buildings)
  * e.g. used for [Prim](./Prim.unity)
* [BASE_Interior](./_Templates/BASE_Interior.unity): For indoor-type scenes (many rooms, travel between rooms)
  * e.g. used for [OfficeInterior](./OfficeInterior.unity)

To easily spin up new scene, duplicate one of these scene files as the starting point.

*A Brief Note on Rooms*

The key differentiator in interior vs. exterior is the use of the [Room](../Scripts/Zones/Room.cs) script.  Generally, a new game object is created for each room in the scene, and all world objects within that room (including NPCs) are childed to that room.  We can use this property to toggle rooms as Frankie travels between them using the standard [ZoneNode](../Scripts/Zones/ZoneNode.cs) functionality -- see:  `ToggleParentGameObjects()`

### Scene-Unity Hook-Up

As with any new scene in a Unity project, the scene must be included in the Build List under Unity's Build Profiles, as below:

<img src="../../InfoTools/Documentation/Scenes/BuildProfilesSceneList.png" width="600">

### Scene-Zone Hook-Up

In order to make use of the [Zones](../Scripts/Zones/) functionality, which enables us to:
* allow the player to move into/out of the scene
* enable background music
, a corresponding Zone must be made for each scene.

This can be accomplished with the following steps:
1. Navigate to [OnLoadAssets/Zones/](../Game/OnLoadAssets/Zones/) in the Unity Project explorer
2. Right click : `Create->Zone->New Zone` , as below:

<img src="../../InfoTools/Documentation/Scenes/CreateNewZone.png" width="400">

1. Adjust the ZoneParameters in the Unity Inspector with:
   * Scene Reference - Link to the newly created Scene file
   * Update Map - Enable if mini-map should be updated (usually true for exterior scenes)
   * Zone Audio - Link to the scene background music file

<img src="../../InfoTools/Documentation/Scenes/ZoneParameters.png" width="250">

1. Use the Zone Editor to link this zone to another zone (i.e. to enable movement across scenes)
   * See [Game/Zones](../Game/OnLoadAssets/Zones/) for more detail on constructing / linking zones, or [Scripts/Zones](../Scripts/Zones/) for specific implementation

## Scene Key Elements

All scenes need to have three key PreFabs to function:
1. [Core](../Game/Core/Core.prefab), which:
   * includes the [PersistentObjectSpawner](../Scripts/Core/PersistentObjectSpawner.cs)
     * , to spawn the prefab [PersistenObjects](../Game/Core/PersistentObjects.prefab)
     * , which are any singleton objects that must persisten from scene to scene (e.g. background music, fader, saver, scene loader, etc.)
   * has childed objects:
     * UICanvas -- for rendering UI elements over the scene
     * BackingCanvas -- black backing for the game
     * AddressablesLoader -- for loading all addressables (key game memory/elements), located in [OnLoadAssets](../Game/OnLoadAssets/), per [Addressables](../Scripts/Core/AddressablesHandling/AddressablesLoader.cs)
2. [Cameras](../Game/Core/Cameras.prefab): Includes the main camera, as well as a state-driven camera based on Frankie's state (idle vs. active)
3. [Player](../Game/Core/Player.prefab), which:
   * includes all relevant player control scripts (e.g. [Player](../Scripts/Core/Player.cs), [PlayerStateMachine](../Scripts/Control/Player/PlayerStateMachine.cs), [PlayerController](../Scripts/Control/Player/PlayerController.cs), [PlayerMover](../Scripts/Control/Player/PlayerMover.cs), etc. )
     * , as well as all other core scripts (e.g. [Party](../Scripts/Stats/Party/Party.cs) + affiliated conduits, [QuestList](../Scripts/Quests/QuestList.cs)), etc.
   * has childed objects -- interaction center point, and Party + PartyAssist containers
     * , where Party container includes Frankie as well as any active party members

Beyond the above core elements, a typical scene in Frankie will contain a World or Building root object, which includes:

* All relevant world elements -- such as tilemaps, characters, buildings, etc. 
* ZoneNodes, for movement into/out of, as well as throughout the scene -- such as into buildings/rooms

The scene may also include enemy spawners, as well as an an optional CameraBounds (polygon collider) to limit camera movement within the scene.

### Example Scene Structure:  Prim

![Prim](../../InfoTools/Documentation/Scenes/SceneStructure.png)

## Overview of Existing Scenes

*last updated:  2024-12-11*

**Intro + Administrative** 

*in order of appearance*

* [SplashScreen](./SplashScreen.unity): Simple timer-based scene on program launch, queues up [StartScreen](./StartScreen.unity)
* [StartScreen](./StartScreen.unity): Landing screen to continue, load save & quit
* [GameOverScreen](./GameOverScreen.unity): Handling for player death
* [GameWinScreen](./GameWinScreen.unity): Handling for game-win criteria met

**Game Scenes**

*in order of appearance*

* [OfficeExterior](./OfficeExterior.unity): Intro, outside Frankie's workplace
* [OfficeInterior](./OfficeInterior.unity): Intro, Frankie's workplace
* [PortaBank](./PortaBank.unity): Recurring scene used for banking
* [Subway](./Subway.unity): Subway connecting various regions
* [ApartmentInterior](./ApartmentInterior.unity): Frankie and Lucy's apartment
* [Prim](./Prim.unity): Frankie's home town
* [PrimInterior](./PrimInterior.unity): Interior of buildings in Prim
* [Tunnels](./Tunnels.unity): Highway tunnels connecting various regions
* [Proper](./Proper.unity): A livelier town a little while off from Prim

