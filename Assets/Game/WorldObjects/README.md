# Assets:  Game - World Objects

The prefabs within this directory comprise all of the game objects that can be dragged into a zone (scene) to build up the world.  Standard world objects usually comprise a SpriteRenderer, a RigidBody2D and a Collider.

The directories are structured as:

|              Category              | Sprite |       |                                                                     Detail                                                                      |                                                     Attached Component/Object                                                      |
| :--------------------------------: | :----: | :---: | :---------------------------------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------------------: |
|      [Exterior](./Exterior/)       |   √    |       |                   Assets to be placed outdoors, including trees, fences, outdoor furniture/props, graveyard, fall fair, etc.                    |                                          (optional) [World](../../Scripts/World/) scripts                                          |
|      [Interior](./Interior/)       |   √    |       |                   Assets to be placed indoors, including indoor furniture, lighting, office, kitchen, bathroom, bedroom, etc.                   |                                          (optional) [World](../../Scripts/World/) scripts                                          |
|         [Signs](./Signs/)          |   √    |       |                               Any signage/sign posts (agnostic to indoor/outdoor) presenting specific information                               |            [CheckWithMessage](../Checks/CheckWithMessage.prefab) via [Check](../../Scripts/CheckInteractions/Check.cs)             |
|     [Buildings](./Buildings/)      |   √    |       |                                                      Any building with an entry/exit point                                                      | [SimpleNodePersistentSound](./_ZoneNodes/SimpleNodePersistentSound.prefab) (via [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs)) |
|         [Doors](./Doors/)          |   √    |       |                              Any door, elevator, stairs, etc. that can be used for moving from one area to another                              |                  [Door](../../Scripts/Zones/Door.cs) + [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs) scripts                   |
|     [ZoneNodes](./_ZoneNodes/)     |        |       |                                     Standalone prefabs (non-sprite-based) for zone traversal functionality                                      |                                     [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs) scripts                                      |
| [ExteriorWorld](./_ExteriorWorld/) |   ~    |       |                            Parent game object for any exterior world, to be unpacked in a given exterior Scene/Zone                             |                                                                 ~                                                                  |
| [InteriorRooms](./_InteriorRooms/) |   ~    |       | Parent game object for any interior room containing [Doors](./Doors/), may include a [sprite renderer](./_InteriorRooms/TilemapRoomRoot.prefab) |                                             [Room](../../Scripts/Zones/Room.cs) script                                             |
|      [Spawners](./_Spawners/)      |        |       |                                               Standard game object for spawning enemies/monsters                                                |                                [EnemySpawner](../../Scripts/Combat/Spawner/EnemySpawner.cs) script                                 |
|         [Paths](./_Paths/)         |        |       |                         Standard game object for delineating a path for an [NPC](../CharacterObjects/NPCs/) to traverse                         |                                    [PatrolPath](../../Scripts/Control/NPC/PatrolPath.cs) script                                    |

#### Note on Document Scope

*This README largely covers asset configuration of world objects.  For detail on*
* *artwork design/construction requirements:  see the [Style Guide](../../../InfoTools/StyleGuide/README.md#game-object--world-artwork-pixel-art)*
* *Tilemap-specific configuration:  see [Tilemaps](../Tilemaps/)*

## Placement of World Objects

World Objects should be placed in hierarchy under a parent-most GameObject with a [SaveableRoot](../../Scripts/Saving/SaveableRoot.cs) component attached to it.  This allows the Save System to identify any World Objects that are tagged as a [SaveableEntity](../../Scripts/Saving/SaveableEntity.cs) (for more detail on the Save System && Saveable Entity settings, see [Saving](../../Scripts/Saving/)).  In most [Scenes](../../Scenes/):
* This parent-most GameObject is named `World`, and all Rooms/Exterior Maps are childed to it
* World Objects are further placed under their respective Room or Exterior Map

Before placing any objects into the scene, ensure that Snapping is Enabled, with a `Grid Size` set to `0.01` (AKA 1/PPU used for sprite import settings), as below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/WorldSnapping.png" width="300">

If assets have been placed before snapping was enabled, they can be force-snapped to the grid by clicking on the `All Axes` button under `Align Selected`

### Interior Room and Exterior World Map Prefabs

Standard prefabs for both interior rooms and exterior world maps are available in [InteriorRooms](./_InteriorRooms/) and [ExteriorWorld](./_ExteriorWorld/) respectively.  The prefabs include relevant sprite or tilemap renderers to effectively paint all critical layers of a scene while adhering to specific ordering and overlap requirements.  As noted above, World Objects are then placed under their given Room or Exterior Map.

For:
* [TilemapRoomRoot](./_InteriorRooms/TilemapRoomRoot.prefab):  This prefab should be dragged onto a scene and used directly
  * it must be placed under a parent game object with an appropriately configured Grid (see [Tilemaps](../Tilemaps/README.md#parent-gameobject-grid-settings))
* [ExteriorMapReference](./_ExteriorWorld/ExteriorMapReference-UnpackBeforeUse.prefab):  This prefab should be dragged onto a scene and unpacked (first level, not completely)

More detail on the tilemaps used in the prefabs can be found in [Tilemaps](../Tilemaps/README.md#reference-tilemap-prefabs).

## Standard World Objects (Interior, Exterior, Signs)

New world object prefabs should placed in their relevant sub-directory within this folder -- [Interior](./Interior/), [Exterior](./Exterior/) or [Signs](./Signs/), depending on which category they best fit into.  World Object prefabs should be made and setup in such a way that they can be dragged onto a Scene and require minimal configuration/adjustment.

### Configuration:  Tags & Physics Layer

All standard world objects should:
* Set `Tag`:  `Untagged`
* Set (Physics) `Layer`:  `Ignore Raycast`

The latter setting is necessary to ensure that the object does not block any raycasts used for a) mouse/cursor highlighting, b) player-to-check component interactions or c) NPC/player relative position detection.

### Configuration:  Sprite Renderer + Sprite

As detailed in [Rendering](../../Scripts/Rendering/), Frankie employs a custom shader in order to achieve (visually indistinguishable to) pixel-perfect rendering at arbitrary display resolutions/window sizes.  This section describes the steps that **must** be followed in order to avoid visual glitches in rendering.

#### Sprite Import / Setup

Art assets must conform to the rules defined in the [StyleGuide](../../../InfoTools/StyleGuide/).  

The following adjustments must be made to the asset's settings in Unity (otherwise, apply default settings):
* `Pixels Per Unit (PPU)`:  `100`
* `Alpha Is Transparency`:  `Disable`
* `Generate Mipmap`:  `Enable`
* `Filter Mode`:  `Bilinear`
* `Aniso Level`:  `16`
* `Compression`:  `None`

As an example, see below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleSpriteImport.png" width="400">

Note: The above configuration only applies to assets that are desired to be rendered pixel-perfect.  For other assets, such as UI elements or in-battle artwork, the standard Unity sprite shader is sufficient.

#### Slicing && Sprite Anchoring

Beyond those settings detailed above:
1. if the imported sprite is imported as part of a sprite sheet, the sprite sheet should be sliced to isolate the individual sprite
   * in this case, it is critical to ensure the sprite is sliced with at least 1px transparent border around it
   * N.B. UNLESS the sprite sheet comprises a tilemap, see instead [Tilemaps](../Tilemaps/) for tilemap import / setup
2. the sprite anchor should be set, specifically using `Pixels` as the `Unit Pivot Mode`
   * Warning:  if the asset's pixel width is odd, then the default settings (e.g. Bottom (Center)) will result in a non-integer pixel position position for the anchor

For the simple on-axis view assets, the default anchor position should be (as above) set to `Bottom`.  For isometric/angled view assets, the anchor position should be set to the upper-most corner of the asset.  For example, see below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/IsometricCornerAnchoring.png" width="650">

#### Sprite Renderer Settings

In the sprite renderer of the world game object, the following adjustments should be made (otherwise apply default settings):
* `Material`:  [PixelArtShader](../../Scripts/Rendering/Shaders/_PixelArtShaders/PixelArtShader.mat)
* `Sorting Layer`:  `PlayerEnemiesObjects`
* `Order in Layer`:  `0`

As an example, see below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleSpriteRenderer.png" width="400">

#### Alternate Sprite Layer / Sorting Strategies

If a world object contains multiple child game objects with independent sprite renderers, a [SortingGroup](https://docs.unity3d.com/6000.2/Documentation/Manual/sprite/sorting-group/sorting-group-landing.html) should be attached to the parent world object, using the same settings above, or:
* `Sorting Layer`:  `PlayerEnemiesObjects`
* `Order in Layer`:  `0`
* `Sort at Root`:  `Disabled`

Child game objects in this configuration can then be defined relative to each other (e.g. `Order in Layer = -1` for a sub-object in the background, `Order in Layer = 1` for a sub-object in the foreground).  Alternatively, child objects can simply be kept at `Order in Layer = 0`, and rely on simple y-position sorting (i.e. if the sprite's anchor is defined below all sub-objects placed on top of the parent).


If a character should always appear in front of a given world object, the following settings should be used:
* `Material`:  [PixelArtShader](../../Scripts/Rendering/Shaders/_PixelArtShaders/PixelArtShader.mat)
* `Sorting Layer`:  `Background`
* `Order in Layer`:  `15` ~ `20`

### Configuration:  Rigidbody & Physics Colliders

Any world object that should block or obstruct the player (i.e. if sorting layer is set to `PlayerEnemiesObjects` at `Order=0`, the world object necessarily into this category) should include the below components:
* [Rigidbody2D](https://docs.unity3d.com/6000.2/Documentation/Manual/2d-physics/rigidbody/rigidbody-2d-landing.html)
* [Collider2D](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Collider2D.html)

#### Rigidbody for Fixed Objects

Most world objects are fixed and will never move, and can thus be configured accordingly.  These types of objects should use the following settings:
* GameObject, `Static Field`:  `Enabled`
* Rigidbody2D, `Body Type`:  `Static`

For example, see below for a fixed-position grandfather clock:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleStaticPhysicsSettings.png" width="350">


#### Rigidbody for Moving Objects

For world objects that can move, the following settings should be used:
* GameObject, `Static Field`:  `Disabled`
* Rigidbody2D:
  * `Body Type`:  `Dynamic`
  * `Simulated`:  `Enabled`
  * `Mass`:  variable
    * usually set relative to `Player`, which is set to `1000000`
  * `Linear/Angular Damping`:  variable
    * dependent on how much the object should slow after being hit
  * `Gravity Scale`:  `0`
  * `Collision Damping`:  `Continuous`
  * `Interpolate`:  `Interpolate`
  * Constraints, `Freeze Rotation, Z`:  `Enabled`

For example, see below settings for a low/zero friction rolling office chair:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleDynamicPhysicsSettings.png" width="350">

#### On-Axis View Sprites - Box Collider

For simple world objects with bottom-edge (or near-bottom) sprite anchors, a [BoxCollider2D](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/BoxCollider2D.html) can be attached to the world object.  

The collider should have `IsTrigger` set to `False`, and be configured to adequately cover the bottom portion of the sprite that should block player movement.  For example, see below collider outline for the grandfather clock:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleBoxCollider2D.png" width="550">

#### Iso View Sprites - Polygon Collider

For isometric view world objects, a [PolygonCollider2D](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/PolygonCollider2D.html) can be attached to the world object.

The collider should have `IsTrigger` set to `False`, and must be configured such that all portions of the sprite that lie below the sprite's anchor position are appropriately blocked.  For example, see the outer collider outline used for the same building shown in [Slicing & Sprite Anchoring](#slicing--sprite-anchoring):

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExamplePolygonCollider2D.png" width="800">

Note that the collider fully blocks any content below the sprite anchor, which was placed in the back-right corner of the building.

## World Object Interactions

### Checks

The most straightforward way to make a game object interactable is to add a [Check](../Checks/) object, via below:

* attach the relevant prefab from the [Checks](../Checks/) directory as a child to the world object
* adjust the check collider's position -- usually configured to the natural 'interaction edge/side' of the given world object
* adjust the check parameters (e.g. message, UnityEvent, etc.) per [Checks](../Checks/)

In some cases, the default [BoxCollider2D](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/BoxCollider2D.html) on the check prefab is unsuitable for the world object shape -- in this case, it can be replaced with a [PolygonCollider2D](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/PolygonCollider2D.html).  In this case, ensure to set the eplaced Check collider's `IsTrigger`:  `Enabled`.

### Specialized World Scripts && Quest Completion Scripts

It may be necessary to grant additional functionality to the world object that can be triggered via the [Check's](../Checks/) UnityEvents.  

These additional functionalities are typically provided by [World Scripts](../../Scripts/World/), which can be attached as components onto any given world object to provide the requisite public methods.  These scripts and their public methods include:
* [WorldSaver](../../Scripts/World/WorldSaver.cs):  to save the game
* [WorldPointAdjuster](../../Scripts/World/WorldPointAdjuster.cs):  to modify character HP/AP
* [WorldPartyInterface](../../Scripts/World/WorldPartyInterface.cs):  to add/remove characters from the party
* [WorldCashGiverTaker](../../Scripts/World/WorldCashGiverTaker.cs):  to add/remove cash from the wallet
* [WorldItemGiverTaker](../../Scripts/World/WorldItemGiverTaker.cs):  to add/remove items from a character's knapsack

etc.

Another common functionality exercised by world objects is quest giving and quest completion, which are handled by the scripts:
* [QuestGiver](../../Scripts/Quests/QuestGiver.cs):  to assign a quest to the player
* [QuestCompleter](../../Scripts/Quests/QuestCompleters/QuestCompleter.cs):  to directly complete a quest or quest objective
* [CombatParticipantQuestCompleter](../../Scripts/Quests/QuestCompleters/CombatParticipantQuestCompleter.cs):  to complete a quest or quest objective on the state change of a given CombatParticipant (e.g. after destroying a specific enemy)

etc.

## Buildings, Doors and ZoneHandlers

[Buildings](./Buildings/) and [Doors](./Doors/) (and all derivative entities -- stairs, etc.) are special categories of World Objects that can be used for traversal within a zone or from zone-to-zone.  As such, they are all either derivative prefabs of the [SimpleNodePersistentSound](./_ZoneNodes/SimpleNodePersistentSound.prefab) [ZoneNode](./_ZoneNodes/) prefab (which uses a [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs) component), or they have this prefab childed to their world object.  Doors furthermore have a [Door](../../Scripts/Zones/Door.cs) script attached to them, which.

### Making New Buildings and Doors

When making a new [Building](./Buildings/) or [Door](./Doors/), it is highly recommended to simply duplicate an existing prefab from within these directories and modify its attributes accordingly (i.e. [sprite](#configuration--sprite-renderer--sprite), [physics/colliders](#configuration--rigidbody--physics-colliders), etc. -- per above configuration detail).

Once the Building/Door object is placed into the scene, its zone-specific parameters should be setup appropriately.  If the specific instance of the prefab does not lead anywhere, it can be left unconfigured.  Otherwise:
* Configure the [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs) component:
  * set `ZoneNode`:  to its relevant `ZoneNode` in the corresponding [Zone](../OnLoadAssets/Zones/) asset
    * *see [OnLoadAssets/Zones](../OnLoadAssets/Zones/) for more detail on Zone + ZoneNode creation/configuration*
  * if the object is a Door, set:
    * `Room Parent`:  to the room game object that the Door is childed to
      * if the Room is immediately above the Door in the hierarchy, this can be left as `None` and the room will be auto-detected
    * `Disable On Exit`:  to `Enabled` to toggle off the Door's sprite after exiting the room (generally desired)
  * if the `ZoneNode` has multiple exit points, set:
    * `Randomize Choice`:  to `Enabled` if the exit point is determined randomly
    * `Randomize Choice`:  to `Disabled` if a choice menu should appear
      * e.g. in the case of elevators with multiple floors, this parameter is normally set to `Disabled`
      * `Choice Message`:  to the text that should appear over the selection menu if multiple choices exist
* Adjust the `WarpPosition`'s transform to where the player's character should appear upon exit from the Building/Door
  * note:  a purple diamond element is generated in Scene View to show the ZoneHandler's exit position
* Configure the [Sound Effects](../../Scripts/Sound/SoundEffects.cs) component in the `ZoneNodeSoundbox`
  * set the `Audio Clips` parameter to the relevant sound(s) for interacting with the Building/Door

For example, see below configuration for a simple door:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/DoorZoneHandlerConfiguration.png" width="800">

### Door Check Variants

It may be desired to interact with a door via a [Check](../Checks/), such as to knock on the door (but not pass through it).  

For these cases, the [NoZHCheckVariants](./Doors/_NoZHCheckVariants/) can be used instead.  Their childed check components should be configured appropriately.

### Standalone Zone Nodes

It may be desired to trigger a [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs) method (i.e. to traverse within a zone or zone-to-zone) without direct interaction -- for example, via UnityEvents.

For these cases, the [ZoneNode](./_ZoneNodes/) prefabs can be used by placing them onto the scene directly.

## Spawners

Two types of Enemy Spawner prefabs are provided in [Spawners](./_Spawners/):
* [RoomSpawner](./_Spawners/StandardRoomSpawner.prefab):  triggers enemy spawn events on GameObject enablement
  * such that enemies spawn the moment the player passes through a door and the room is set to enabled
* [WorldSpawner](./_Spawners/StandardWorldSpawner.prefab):  triggers enemy spawn events when the spawner GameObject is within the player's camera view

The [EnemySpawner](../../Scripts/Combat/Spawner/EnemySpawner.cs) component on the respective spawner must be configured by setting:
* `Time Between Spawns`:  variable, limiter on the frequency with which an enemy may be spawned
  * generally more relevant for world spawner -- to avoid enemy spam if the player walks in/out of spawner view repeatedly
* `Jitter Distances`:  variable, x/y range in which an enemy may spawn
  * note:  a magenta square / bounding box element is generated in Scene View to show the extent of the spawn range
* `Spawn Configurations`:  variable, includes types of enemies to spawn, number of enemies to spawn, probability/frequency to spawn, etc.

See below for an example of an Office Worker Enemy Spawner:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleEnemySpawner.png" width="800">

This spawner has three possible spawn configurations, where:
* it can spawn a single `GenericOfficeWorkerGlen` with probability `4 / (4 + 1 + 1) = ~67%`
* it can spawn either `GenericOfficeWorkerFran` or `GenericOfficeWorkerStan` with probability `1 / (4 + 1 + 1) = ~17%`
* it can spawn nothing, with probability `1 / (4 + 1 + 1) = ~17%`

## Paths

[Patrol Paths](./_Paths/StandardPatrolPath.prefab) use a series of [Waypoints](./_Paths/Waypoint.prefab) to define a walking route that an [NPC Character](../CharacterObjects/NPCs/) (with an [NPCMover](../../Scripts/) component) can traverse.  

Briefly:
* Drag a new [Patrol Path](./_Paths/StandardPatrolPath.prefab) prefab onto the scene
* Drag 'n' [Waypoint](./_Paths/Waypoint.prefab) prefab children under the patrol path
  * move each waypoint's transform to the desired location for the patrol path
  * note:  a series of blue/green spheres connected by lines are generated in Scene View to show the walking path
* Configure the [Patrol Path Component](../../Scripts/Control/NPC/PatrolPath.cs), by setting:
  * `Waypoints`:  drag the array of waypoint children under the patrol path onto this field
  * `Looping`:  `Enabled` if the NPC should loop through the patrol path
  * `Return to First Waypoint`:  `Enabled` if the NPC should walk back to the first waypoint after reaching the final waypoint

Finally, attach the patrol path to the desired NPC's NPCMover's `Patrol Path` field.

For example, see below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExamplePatrolPath.png" width="800">
