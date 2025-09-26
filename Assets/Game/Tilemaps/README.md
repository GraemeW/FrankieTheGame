# Assets:  Game - Tilemaps

## Tilemap Setup and Configuration

As detailed in [Rendering](../../Scripts/Rendering/), Frankie employs a custom shader in order to achieve (visually indistinguishable to) pixel-perfect rendering at arbitrary display resolutions/window sizes.  This section describes the steps that **must** be followed in order to avoid visual glitches in rendering.

### Tilemap Art Asset Import

Art assets must conform to the rules defined in the [StyleGuide](../../../InfoTools/StyleGuide/).  

As with standard [WorldObjects](../WorldObjects/README.md#sprite-import--setup), the following adjustments must be made to the tilemap art asset's settings in Unity (otherwise, apply default settings):
* `Pixels Per Unit (PPU)`:  `100`
* `Alpha Is Transparency`:  `Disable`
* `Generate Mipmap`:  `Enable`
* `Filter Mode`:  `Bilinear`
* `Aniso Level`:  `16`
* `Compression`:  `None`

#### Slicing, Pivot and Custom Physics Collider Shapes

Tilemaps need to be sliced into their individual tiles.  Slicing is done right to the edge of the tile borders.

Typical tilemap art assets in Frankie are 50px by 50x, and as discussed in the [StyleGuide](../../../InfoTools/StyleGuide/), are packed with 1px edge border transparency with 2px transparent buffer between tiles.  Given this, the standard slicing parameters are shown below:

<img src="../../../InfoTools/Documentation/Game/Tilemaps/AssetImportSlicing.png" width="300">

The `Pivot` is normally kept at **Center** during the slicing.  The pivot can be modified, for example, if there is a need to have game objects (e.g. the player characters) appear behind the tile as a function of their world position.  This is generally not recommended, however, as it is difficult to accomplish this behaviour in a sensible manner when multiple tiles are painted and stitched together.

For tiles with edge features that need to block the player, custom physics shapes needs to be defined in the sprite editor.  For example, see below shape applied to block the player from traversing over a grassy cliff edge:

<img src="../../../InfoTools/Documentation/Game/Tilemaps/AssetImportCustomPhysicsShapes.png" width="200">

#### Tilemap Sprite Atlas

In order to avoid tile edge stitching artifacts, a [Sprite Atlas](https://docs.unity3d.com/6000.2/Documentation/Manual/sprite/atlas/atlas-landing.html) must be made for and include the tilemap art asset.  

Check if a Sprite Atlas is already made for the tilemap's parent folder (see under each subfolder within this directory -- e.g. [City.spriteatlas](./ExteriorCity/City.spriteatlas), [Landscape.spriteatlas](./ExteriorLandscape/Landscape.spriteatlas), etc.).  If a Sprite Atlas does not exist, you must create a new one using the following settings (otherwise, apply default settings):
* `Allow Rotation`:  `Disable`
* `Tight Packing`:  `Disable`
* `Padding`:  `4`
* `Generate Mip Maps`:  `Enable`
* `Filter Mode`:  `Bilinear`
* `Aniso Level`:  `16`
* `Compression`:  `None`
* `Objects for Packing`:  Attach the parent folder of the tilemap art asset

As an example, see below:

<img src="../../../InfoTools/Documentation/Game/Tilemaps/SpriteAtlasSettings.png" width="400">

### Tilemap Palette / Tile Generation

Check if an existing tilemap palette is suitable for any newly imported tilemap art assets (see under each subfolder within this directory -- e.g. [City.prefab](./ExteriorCity/City.prefab), [Landscape.prefab](./ExteriorLandscape/ExteriorLandscape.prefab), etc.).  If no such viable palette exists, create a new one:
* In this [Tilemaps](./) directory, create a new sub-folder for the new palette
* Right Click:  Create -> 2D -> Tile Palette -> Rectangular

<img src="../../../InfoTools/Documentation/Game/Tilemaps/CreateNewTileMapPalette.png" width="600">

To add the tiles to the palette:
* Open the relevant palette in the Tile Palette editor
* Drag the new tilemap art asset onto an open space in the editor grid
* Select the corresponding palette directory's `Map` sub-folder as the location to save the new tiles
  * e.g. [CityMap](./ExteriorCity/CityMap/), [LandscapeMap](./ExteriorLandscape/LandscapeMap/), etc.
  * if making a new palette, first create a corresponding `PaletteMap` sub-folder

N.B.  When updating an existing tilemap art asset (e.g. appending 4 new tiles on an existing 20-tile map), instead of dragging the .png file from the Unity Project explorer onto the palette, one may instead:
* expand the .png art asset file in the Unity Project explorer to reveal its individual tile slices
* drag **only** the new tile slices onto the palette

### Rule (Smart) Tiles

To create a smart tile:
* Navigate to an existing sub-folder for smart tiles within the relevant tilemap palette directory
  * if a smart tile sub-folder does not yet exist, create it
* Right Click:  Create -> 2D -> Tiles -> Rule Tile

<img src="../../../InfoTools/Documentation/Game/Tilemaps/CreateNewRuleTile.png" width="600">

On the palette itself, smart tiles should be placed 2 rows above (i.e. with 1 row gap) the basic imported tiles.  For example, see below with the City palette:

<img src="../../../InfoTools/Documentation/Game/Tilemaps/CityTileMapPaletteOrganization.png" width="600">

#### Custom Smart (Rule) Tiles

Several custom smart tiles were made to extend the functionality of the basic Unity Rule Tile, and can be created by via Right Click:  Create -> CustomRuleTiles.  

These are:
* [Sibling Rule Tiles](../../Scripts/CustomRuleTiles/RuleTileSibling.cs):  Same functionality as Rule Tiles, but can define 'sibling' tiles that are treated as if they belong to the Sibling Rule Tile from the standpoint of rule execution
  * As an example, see [PathSlantLeft](./ExteriorLandscape/SmartTiles-Paths/PathSlantLeftSibling.asset) + [PathSlantRight](./ExteriorLandscape/SmartTiles-Paths/PathSlantRightSibling.asset)
* [Random Animation Rule Tile](../../Scripts/CustomRuleTiles/RuleTileRandomAnimation.cs):  Same functionality as Rule Tiles, but allows individual rules to be assigned to animated tiles (instead of selection from only static tiles)
  * As an example, see [RandomWaterAnimation](./ExteriorLandscape/SmartTiles-Water/RandomWaterAnimation.asset)
* [Random from Siblings Rule Tile](../../Scripts/CustomRuleTiles/RuleTileRandomFromSiblings.cs):  Instead of painting from the tile itself, selects a random tile from its defined siblings
  * As an example, see [CliffCenterAnimated](./ExteriorLandscape/SmartTiles-Cliffs/CliffCenterAnimated.asset)

## Painting Tiles in the Scene

### Parent GameObject Grid Settings

TileMap GameObjects must be placed under a parent game object with a [Grid](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Grid.html) component attached to it.  The standard grid settings depend on the size of tiles being used relative to the PPU of the artwork.  

For Frankie, the following Grid settings must be used:
* Standard Tiles [50px by 50px @ 100PPU]:
  * `Cell Size X`:  `0.5`
  * `Cell Size Y`:  `0.5`
* [Tree Tiles](./ExteriorTrees/) [200px by 200px @ 100PPU]:
  * `Cell Size X`:  `2`
  * `Cell Size Y`:  `2`

Note that the parent Grid GameObject and all TileMap/TileMapRenderer children GameObjects should also be set to `Static`.

### Tilemap Renderer Settings

A standard TileMap GameObject must include two components:
* [TileMap](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Tilemaps.Tilemap.html)
* [TileMapRenderer](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Tilemaps.Tilemap.html)

The TileMap must be configured with its appropriate `Animation Frame Rate` (if using animated tiles).

The TileMapRenderer must be configured with the below settings:
* `Material`:  [PixelArtShader](../../Scripts/Rendering/Shaders/_PixelArtShaders/PixelArtShader.mat)
  * **Note:  this step is critical and failure to set up the correct material will result in rendering artifacts!**
* `SortingLayer`:  
  * `Background`:  if the TileMap will always appear behind the characters / world objects
  * `PlayerEnemiesObjects`:  if the TileMap can appear in front of the characters / world objects
* `Order in Layer`:  variable, dependent on the desired draw order of the specific TileMap
* `Mode`:  
  * `Chunk`:  Default setting, use most of the time
  * `Individual`:  Use only if:
    * the character / world objects should appear behind the tilemap
    * if that should be dependent on individual tiles instead of the composite tile
  * `Individual` mode is used, for example, for the [Trees](./ExteriorTrees/) tilemap

As an example, see the the settings for the Cliffs used in Prim:

<img src="../../../InfoTools/Documentation/Game/Tilemaps/PrimCliffsTilemapSettings.png" width="300">

### Optional Collider(s)

For TileMaps that need to block the player, the TileMap GameObject must also include the relevant physics/colliders.  

To this end, attach the below components with the following settings:
* [Rigidbody2D](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Rigidbody2D.html)
  * Set `Body Type` to `Static`
* [TilemapCollider2D](https://docs.unity3d.com/6000.2/Documentation/Manual/tilemaps/work-with-tilemaps/tilemap-collider-2d-reference.html)
  * Set `Composite Operation` to `Merge`
* [CompositeCollider2D](https://docs.unity3d.com/6000.2/Documentation/Manual/2d-physics/collider/composite-collider/composite-collider-2d-reference.html)
  * Ensure `IsTrigger` is set to `False`

### Tile Placement Considerations

The creation and placement of Tiles is an exercise in trying to re-use tile art assets (minimizing the number of unique tiles that one needs to maintain), while allowing sufficient flexibility to create a meaningful scene.  Unity allows us to define as many independent TileMaps with independent offsets as desired to avoid the otherwise impractical explosion variant combinations, but this comes with a trade-off in maintaining a complex system of tile sorting layers and overlaps.

#### On Rendering Artifacts

Above stated, the [StyleGuide](../../../InfoTools/StyleGuide/README.md#tilemap-artwork-considerations) provides an overview of requirements with regard to creating TileMaps to avoid rendering artifacts.  The placement of the tiles to avoid tile-edge seams discussed here is equally important to avoid these rendering artifacts.

When drawing **Layered Tiles**:
* avoid terminating the edge of a tiled feature with a tile that fills to sed edge
  * wherever possible, use tiles that terminate with at least 1px transparency (alpha = 0) at the specific feature edge
  * this is shown in the [StyleGuide](../../../InfoTools/StyleGuide/README.md#tilemap-artwork-considerations) as the example for Edge Layered Tiles
* where this cannot be avoided (e.g. when using full-fill basic floor tiles):
  * ensure that the edge of the tiled feature is fully covered by another TileMap
  * this is commonly accomplished by having `X = 0.25` or `Y = 0.25` positional offsets in the Layered TileMaps
* for example, see how the sidewalk is placed to overlap the seam of the concrete floor edge in Prim:

<img src="../../../InfoTools/Documentation/Game/Tilemaps/PrimConcreteSidewalkOverlap.png" width="600">

When drawing **Back-Most Tiles**:
* avoid terminating the edge of a tiled feature that lives above the Camera Background with a transparent tile edge
  * use tiles that terminate with a color matching the Camera Background Color (typically black)
  * this is shown in the [StyleGuide](../../../InfoTools/StyleGuide/README.md#tilemap-artwork-considerations) as the example for Back-Most Tiles
* it is further recommended to place a backing TileMap with full-fill tiles matching the Camera Background Color (i.e. typically 50px by 50px black squares) behind any edge seams
* for example, see the backing layer applied in a PrimInterior room, which:
  * sits behind the edge seams of the left/right walls of the room
  * also serves as a front blocking layer for player movement

<img src="../../../InfoTools/Documentation/Game/Tilemaps/PrimInteriorRoomEdgeUnderlap.png" width="600">

#### On Bounding Player Movement

One or more of the TileMap Layers should include colliders (as detailed [above](#optional-colliders)).  Tiles should be intelligently placed such that they render the scene correctly, and also bound the player characters and world objects from moving off of the painted canvas.

Scenes built using tilemaps should not apply any additional colliders to bound movement, beyond those present on the painted TileMap or those present on standard [WorldObjects](../WorldObjects/) dragged on the scene.

## Reference Tilemap Prefabs

### Interior Rooms

See [TilemapRoomRoot](../WorldObjects/_InteriorRooms/TilemapRoomRoot.prefab) under WorldObjects for an example interior room construction.  This prefab can be dragged onto a parent game object with a [Grid component](#parent-gameobject-grid-settings) and then otherwise used directly.  

It includes four layered TileMaps / TileMapRenderers:
* `FG_LeftRightWall`:  for painting the left/right walls
  * includes a collider to prevent player characters from walking over the side walls
  * offset (X=0.25)
* `FG_FrontBlocking`:  for painting a black border on the bottom of the room
  * includes a collider to prevent player characters from walking below the bottom of the room
  * as noted [above](#on-rendering-artifacts), also used as a back-drop behind tile edge seams 
* `FG_BackWall`:  for painting the back wall
  * includes a collider to prevent characters from walking on the back wall
  * offset (X=0.25, Y=0.25)
* `BG_Floor`:  for painting the floor
  * offset (Y=0.25)

The offsets denoted above are chosen so the edge seams of each layer can be drawn to be incrementally covered by the subsequent layer(s).  Or:
1. `BG_Floor` edges are covered by both `FG_BackWall` and `FG_LeftRightWall`
2. `FG_BackWall` edges are covered by `FG_LeftRightWall`
3. `FG_LeftRightWall` edges are matched to the Camera Background Color, but are otherwise backed by the `FG_FrontBlocking` underlap

### Exterior World

See [ExteriorMapReference](../WorldObjects/_ExteriorWorld/ExteriorMapReference-UnpackBeforeUse.prefab) under WorldObjects for an example exterior TileMap construction.  This prefab can be dragged onto a scene, unpacked (do NOT unpack completely, just first-level unpack) and then otherwise used directly.

It includes many more layered TileMaps / TileMapRenderers:
* LandscapeBase:
  * `Grass`:  Main renderer for grass for the player character to walk on
    * includes collider for blocking player movement off edges to cliffs
  * `GrassLowground`:  Additional renderer to allow multiple walkable layers
  * `Cliffs`:  Main renderer for cliffs
    * animated for cliff/water edge movement 
  * `CliffsBacking`:
    * offset `X=0.25` to cliffs
    * used to place supplemental cliff tiles behind cliff tile edge seams
  * `Paths`:  Walkable flavour
* LandscapeBlocking:  
  * Additional renderers for grass and cliffs that appear in front of player characters / world objects
* Roads:
  * `ConcretePavement`:  Walkable flavour for roads
  * `Sidewalks`
    * offset X=0.25 to `ConcretePavement`
    * walkable sidewalk flavour, also hides road seams
* `BackingWater`:  Back-Most water fill for most outdoor scenes
  * animated for waves / bubbles
* `Trees`:  Main renderer for largescale forest tree fill
  * includes collider for blocking player movement through forests
  * on a separate grid due to larger tree tile size
  * note:  the adjacent SingularTrees Parent GameObject (not a tilemap) is used to place singular [Tree](../WorldObjects/Exterior/TreesShrubs/) prefabs

For more specific details in implementation, open either the [Prim](../../Scenes/Prim.unity) or [Proper](../../Scenes/Proper.unity) scenes to poke around.
