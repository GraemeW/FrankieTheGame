# Assets:  Game - World Objects

**NOTE:  THIS README IS INCOMPLETE AND A WORK IN PROGRESS**

The prefabs within this directory comprise all of the game objects that can be dragged into a zone (scene) to build up the world.  Standard world objects usually comprise a SpriteRenderer, a RigidBody2D and a Collider.

The directories are structured as:

|          Category          | Sprite |       |                                                                 Detail                                                                  |                                                     Attached Component/Object                                                      |
| :------------------------: | :----: | :---: | :-------------------------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------------------: |
|  [Exterior](./Exterior/)   |   √    |       |               Assets to be placed outdoors, including trees, fences, outdoor furniture/props, graveyard, fall fair, etc.                |                                          (optional) [World](../../Scripts/World/) scripts                                          |
|  [Interior](./Interior/)   |   √    |       |               Assets to be placed indoors, including indoor furniture, lighting, office, kitchen, bathroom, bedroom, etc.               |                                          (optional) [World](../../Scripts/World/) scripts                                          |
|     [Signs](./Signs/)      |   √    |       |                           Any signage/sign posts (agnostic to indoor/outdoor) presenting specific information                           |            [CheckWithMessage](../Checks/CheckWithMessage.prefab) via [Check](../../Scripts/CheckInteractions/Check.cs)             |
| [Buildings](./Buildings/)  |   √    |       |                                                  Any building with an entry/exit point                                                  | [SimpleNodePersistentSound](./_ZoneNodes/SimpleNodePersistentSound.prefab) (via [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs)) |
|     [Doors](./Doors/)      |   √    |       |                          Any door, elevator, stairs, etc. that can be used for moving from one area to another                          |       [SimpleNodePersistentSound](./_ZoneNodes/SimpleNodePersistentSound.prefab), [Door](../../Scripts/Zones/Door.cs) script       |
| [ZoneNodes](./_ZoneNodes/) |        |       |                            Standalone prefabs to attach to any game object for zone traversal functionality                             |                                     [ZoneHandler](../../Scripts/Zones/ZoneHandler.cs) scripts                                      |
|     [Rooms](./_Rooms/)     |   ~    |       | Parent game object for any interior room containing [Doors](./Doors/), may include a [sprite renderer](./_Rooms/TilemapRoomRoot.prefab) |                                             [Room](../../Scripts/Zones/Room.cs) script                                             |
|  [Spawners](./_Spawners/)  |        |       |                                           Standard game object for spawning enemies/monsters                                            |                                [EnemySpawner](../../Scripts/Combat/Spawner/EnemySpawner.cs) script                                 |
|     [Paths](./_Paths/)     |        |       |                     Standard game object for delineating a path for an [NPC](../CharacterObjects/NPCs/) to traverse                     |                                    [PatrolPath](../../Scripts/Control/NPC/PatrolPath.cs) script                                    |

## Placement of World Objects

Before placing any objects into the scene, ensure that Snapping is Enabled, with a `Grid Size` set to `0.01` (AKA 1/PPU used for sprite import settings), as below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/WorldSnapping.png" width="300">

If assets have been placed before snapping was enabled, they can be force-snapped to the grid by clicking on the `All Axes` button under `Align Selected`

## Standard World Objects (Interior, Exterior, Signs)

### Configuration:  Sprite Renderer + Sprite

As detailed in [Rendering](../../Scripts/Rendering/), Frankie employs a custom shader in order to achieve (visually indistinguishable to) pixel-perfect rendering at arbitrary display resolutions/window sizes.  This section describes the steps that **must** be followed in order to avoid visual glitches in rendering.

*Note:*
* *See the [StyleGuide](../../../InfoTools/StyleGuide/README.md#game-object--world-artwork-pixel-art) for futher detail on artwork design/construction requirements*
* *See [Tilemaps](../Tilemaps/) for further detail on Tilemap-specific configuration*

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

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleSpriteImport.png" width="500">

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

In the sprite renderer of the world game object, the following adjustments must be made (otherwise apply default settings):
* `Material`:  [PixelArtShader](../../Scripts/Rendering/Shaders/_PixelArtShaders/PixelArtShader.mat)
* `Sorting Layer`:  `PlayerEnemiesObjects`

As an example, see below:

<img src="../../../InfoTools/Documentation/Game/WorldObjects/ExampleSpriteRenderer.png" width="500">

### Configuration:  Collider

*TODO:  Add Detail*

### World Interaction Scripts

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
