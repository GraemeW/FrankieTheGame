# Frankie Style Guide

**NOTE:  THIS README IS INCOMPLETE AND A WORK IN PROGRESS**

## Nomenclature: Definitions

* **must**:  indicates a requirement -- an absolute contract with no room for discretion
* **should**:  indicates a recommendation -- normative, but not strictly binding
* **may**:  indicates a permission -- technically permissible, but not always normative
* **can**:  indicates a possibility or capability -- typically used in definitions

## Code

### Naming Conventions & Guidance
* Upper:
  * Class names must use `UpperCamelCase`
  * Method names must use `UpperCamelCase`
  * Struct names must use `UpperCamelCase`
  * Enum names must use `UpperCamelCase`
* Lower:
  * Variable names must use `lowerCamelCase`
  * Event names must use `lowerCamelCase`
* Naming Guidance:
  * Names must be chosen to be inherently intuitive and explanatory
  * Names should avoid using broad/vague terminologies, such as `Handler` or `Manager`

### Class Organization

* Includes must appear at the top of the file, in the order of:  1. System/Newtonsoft includes, 2. Unity includes, 3. Frankie includes
* Classes must be placed in their relevant namespace
  * following `Frankie.namespace` formatting, or `Frankie.namespace.UI` for UI components
  * , where namespace refers to the specific relevant namespace (e.g. `Frankie.Control`, `Frankie.Speech`, etc.)
* Class properties must be placed at the top of the class, and should be placed in the following order:
  1. Tunable Parameters -- see [below](#parameters-tunables)
  2. Static Parameters
  3. State Variables
  4. Cached References
  5. Events
* Class methods should be grouped together in a logical order using `#region` and `#endregion` directives
* Unity's built-in MonoBehaviour methods (`Awake()`, `Start()`, `Update()`, etc.) should be placed at the top of the class, just below the class properties

### Parameters (Tunables)

* [Magic Numbers](https://en.wikipedia.org/wiki/Magic_number_(programming)) must not be used in any code in Frankie
* Parameters must be clearly defined at the top of the class
* If the class derives MonoBehaviour:
  * parameters should be set using the `[SerializeField]` attribute
  * parameters may be set without the `[SerializeField]` attribute if they are truly fixed, and must instead be defined as `static`
* If a parameters needs to be accessed externally, the class should define getter/setter methods
  * getter/setter methods should use standard notation `Get<Parameter>`> and `Set<Parameter>`
    * , where `<Parameter>` refers to the specific name of the input parameter
  * auto-implemented properties must NOT be used in lieu of the above paradigm

### MonoBehaviour Dependencies on Same Game Object

* If a MonoBehaviour requires another component on its GameObject for functional operation, the tag `[RequireComponent(typeof(OtherComponent))]` must be used to ensure its existence

### Cached Reference Initialization & Access

* Cached references to other Components (e.g. via `GetComponent<>()`) or GameObjects (e.g. via `GameObject.FindGameObjectWithTag()`) should be initialized during a MonoBehaviour's `Awake()` method
* Cached references and their properties must otherwise only be accessed after `Awake()` (i.e. in `Start()` or later)

### Events 
* Standard Event Subscription:
  * Events should be subscribed and unsubscribed in a MonoBehaviour's `OnEnable()` and `OnDisable()` methods respectively
  * Events must never be subscribed to without having a paired unsubscribe
* Player Input Events:
  * [PlayerInput](../../Assets/UnityConfigurables/InputProfiles/PlayerInput.inputactions) events must only be subscribed to via pre-defined controllers / debug tools, as defined below
  * Standard Controllers:
    * [PlayerController](../../Assets/Scripts/Control/Player/PlayerController.cs):  game/world input
    * [BattleController](../../Assets/Scripts/Control/Controllers/BattleController.cs):  battle input
    * [DialogueController](../../Assets/Scripts/Control/Controllers/DialogueController.cs):  dialogue input
  * Menus and Debuggers:
    * [SplashController](../../Assets/Scripts/Control/Controllers/SplashMenuController.cs)
    * [StartMenuController](../../Assets/Scripts/Control/Controllers/StartMenuController.cs)
    * [FrankieDebugger](../../Assets/Scripts/Core/FrankieDebugger.cs)

## Artwork

### Game Object / World Artwork (Pixel Art)

* Artwork Format:
  * art must be exported as Portable Network Graphic format (.png)
* Artwork Edge Features:
  * art must be drawn with high contrast exterior borders
    * exterior borders should be drawn using black or near-black colours
    * exterior borders may be drawn using alternative colours, such as a low-lightness variant of the feature's edge colour
  * art should be drawn with sufficiently high contrast interior borders
    * interior borders should be drawn using low-lightness variants of the feature's interior colour
    * interior borders should NOT be drawn using black or near-black colours
* Artwork Divisibility:
  * art should be drawn such that the width (horizontal) pixel count is divisible by 2
  * this allows for simple anchor placement in the centre of the object, otherwise anchoring will bias either left or right by 0.5px

*Note:  For sprite import/configuration and placement guidance, see [Game/WorldObjects](../../Assets/Game/WorldObjects/README.md#sprite-import--setup)*

### Additional Rendering Requirements

Frankie uses a custom shader [PixelArtShader](../../Assets/Scripts/Rendering/Shaders/_PixelArtShaders/PixelArtShader.shader), as described in [Rendering](../../Assets/Scripts/Rendering/).  

The [PixelArtShader](../../Assets/Scripts/Rendering/Shaders/_PixelArtShaders/PixelArtShader.shader) allows pixel-perfect game artwork to render properly on any screen resolution, monitor/display pipeline scaling or window resolution.  It, notably, also assumes pre-multiplied alpha for any rendered artwork.  Further, due to the manner in which Unity treats asset edges (i.e. mirroring/extruding at pixel edges) additional considerations must be made in the design/construction of artwork.  Failure to adhere to these requirements will result in visual glitches for certain window resolutions or display configurations (such as flicker, shimmer, jutter, etc.).

#### World Artwork Considerations

* Art must have its content pre-multiplied by alpha
* Art must NOT have any content bordering the edges of its canvas
  * apply ≥1px border of alpha = 0, R/G/B = 0 to the edges of artwork

*For simplicity, see [AlphaPreMultiply.py](../ImagePreImportTool/AlphaPreMultiply.py) python tool, which can be applied to any as-drawn artwork to ensure it adheres to requirements*

#### Tilemap Artwork Considerations

* Tile artwork should be packed using 1px edge border transparency with 2px transparent buffer between tiles
* Tiles should be constructed in such a manner that tile content is continuous across edges
* Tiles must not have high contrast feature discontinuities at their tile edges
  * for example, see below:

![](./Images/TileMap_EdgeHighContrast.png)

* Tiles may have small size or low contrast feature content at their tile edges
  * for example, see below:

![](./Images/TileMap_EdgeLowContrast.png)

* Tiles can be considered in two different categories with additional requirements:
  * **Back-Most Tiles**: which are placed on a TileMap immediately above the Camera Environment Background  (thus have edges that overlap to sed Background)
    * Back-Most Tiles must have content fill that extends to the tile edge
    * Back-Most Tiles edge content fill must match the color of the [Camera Environment Background Color](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Camera-backgroundColor.html)
    * for example, see below:

![](./Images/Tilemap_BackMostLeftEdge.png)

  * **Layered Tiles**: which are placed on a TileMap above another TileMap (e.g. land tiles on water tiles, train track tiles on floor tiles, etc.)
    * Layered Tile features must terminate away from the edge of the tile extent
    * To this end, we can define:
      * **Interior Layered Tile**:  
        * must be wholly encompassed by Edge Layered Tiles
        * can fill to the edge of its tile extent (e.g. full-fill grass, full-fill pavement, etc.)
      * **Edge Layered Tile**: 
        * encompasses or abuts related Interior Layered Tiles (if applicable)
        * can have high contrast edges that indicate termination of feature content (e.g. land/cliff edge, sidewalk edge, etc.)
    * Edge Layered Tiles must NOT have feature content at the edge of their tile extent
      * ensure ≥1px border of alpha = 0, R/G/B = 0 at the tile edge for sed feature content
    * for example, see below:

![](./Images/TileMap_EdgeLayered.png)

*Note:  For TileMap configuration and placement guidance, see [Game/Tilemaps](../../Assets/Game/Tilemaps/)*
