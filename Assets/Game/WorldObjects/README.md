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

## Placement of World Objects

Before placing any objects into the scene, ensure that Snapping is Enabled, with a `Grid Size` set to `0.01` (AKA 1/PPU used for sprite import settings), as below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/WorldSnapping.png" width="300">

If assets have been placed before snapping was enabled, they can be force-snapped to the grid by clicking on the `All Axes` button under `Align Selected`

## Standard World Objects (Interior, Exterior, Signs)

* *See the [StyleGuide](../../../InfoTools/StyleGuide/README.md#game-object--world-artwork-pixel-art) for futher detail on artwork design/construction requirements*
* *See [Tilemaps](../Tilemaps/) for further detail on Tilemap-specific configuration*

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

*Note that the above configuration only applies to assets that are desired to be rendered pixel-perfect.  For other assets, such as UI elements or in-battle artwork, the standard Unity sprite shader is sufficient.*

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
* `Sorting Layer`:  `PlayerEnemiesObjects`
* `Order in Layer`:  `0`

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

### Check Interactions

*TODO:  Add Detail*

### Specialized World Interactions

*TODO:  Add Detail*

## Buildings, Doors && ZoneHandlers

*TODO:  Add Detail*

## Standalone Zone Nodes

*TODO:  Add Detail*

## Rooms

*TODO:  Add Detail*

## Spawners

*TODO:  Add Detail*

## Paths

*TODO:  Add Detail*
