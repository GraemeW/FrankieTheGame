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
* Underscore+Lower:
  * Private Const names must use `_underscoreLowerCamelCase`
  * Private Static names must use `_underscoreLowerCamelCase`
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
    * [PlayerController](../../Assets/Scripts/Control/Controllers/PlayerController.cs):  game/world input
    * [BattleController](../../Assets/Scripts/Control/Controllers/BattleController.cs):  battle input
    * [DialogueController](../../Assets/Scripts/Control/Controllers/DialogueController.cs):  dialogue input
  * Menus and Debuggers:
    * [SplashController](../../Assets/Scripts/Control/Controllers/SplashMenuController.cs)
    * [MainMenuController](../../Assets/Scripts/Control/Controllers/MainMenuController.cs)
    * [FrankieDebugger](../../Assets/Scripts/Utils/Debugger/FrankieDebugger.cs)

### Logging

*Note:  For the logging API and settings, see [LowDefMustard Core](../../Packages/com.lowdefmustard.core/README.md)*

* Logs must go through `LDMLog` (`LowDefMustard.Core`) ~ `UnityEngine.Debug.Log*` must not be called directly outside of `LowDefMustard.Core`
* Severity must be chosen by the state the game is left in:
  * `Error`:  the game/tool is no longer viable, or data is unrecoverably broken (e.g. a cache failed to load, a battle cannot start)
  * `Warning`:  something is wrong or misconfigured, but play continues (default for most problems)
  * `Important`:  a milestone worth reading in a shipped player log (e.g. zone loaded, game saved, battle ended, quest completed)
  * `Info`:  diagnostics and tracing, intentionally ignored requests, routine automatic actions, normal gameplay limits
    * Note:  editor-only code should use `Info` rather than `Important`
* Category must be the owning subsystem's `LDMLogCategory` (e.g. `Combat`, `Saving`, `Zones`)
  * a second flag (e.g. `Zones | Localization`) should be added when the message spans multiple systems
  * debug/editor tools should tag the system they act on, not `Utilities`
* Messages must follow the standard format:
  * no source or severity prefixes in the text (no `Warning!`, `Error:`, `ClassName:`, `[ToolName]`)
  * sentence case, full sentences ending in a period, no exclamation marks
  * shape:  `<what happened> [for <subject>]. <consequence or fix>.`
    * warnings/errors should state what the code does next (`Skipping ...`, `Ignoring the request.`) and the fix if known
  * names, IDs, paths and enum values should be wrapped in single quotes (e.g. `zone 'Forest01'`)
  * supporting data should be given in parentheses as `key: value` pairs (e.g. `(path: 'Assets/...', count: 3)`)
  
  Example Message:
  
  ```LDMLog.Warning(LDMLogCategory.Zones, $"Scene path for '{sceneName}' is not configured. Re-link the scene reference in the editor.");```

* A `context` object should be passed when a Unity object is the subject of the message to enable click-to-ping
* Logs should be added on paths that silently drop expected work or return an unintuitive fallback
* Logs should not be added for:
  * optional features that are deliberately unused (e.g. pathfinding, which is not strictly required for all NPCs/situations)
  * per-frame / per-tick traces ~ remove any debugging breadcrumbs before committing
  * events already logged one layer down
* The message must always be the plain `string` parameter directly after the category ~ `params object[]` / format-string overloads must not be added (keeps a future C# 10+ interpolated string handler drop-in)

## Artwork

### Rendering Notes

Frankie uses a custom shader [PixelArtShader](../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/_PixelArtShaders/PixelArtShader.shader), as described in [Rendering](../../Assets/Scripts/Rendering/).

The [PixelArtShader](../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/_PixelArtShaders/PixelArtShader.shader) allows pixel-perfect game artwork to render properly on any screen resolution, monitor/display pipeline scaling or window resolution.  It, notably, also assumes pre-multiplied alpha for any rendered artwork, as well as specific artwork construction + alpha/transparency padding (as described below).  Failure to adhere to the requirements described below **will** result in visual glitches for certain window resolutions or display configurations (such as flicker, shimmer, jutter, etc.).

*Note:  For sprite import/configuration and placement guidance, see [Game/WorldObjects](../../Assets/Game/WorldObjects/README.md#sprite-import--setup)*

### Artwork Format:

* art must be exported as Portable Network Graphic format (.png)
* individual art PNG files must map to individual sprite objects (except for tilemaps) - i.e.
  * all art must be pre-sliced before importing to Unity
  * multiple standalone objects must never be packaged together inside a single PNG file

### Light Source

* Unless a scene has a specific/explicit requirement, all artwork should be drawn as though a single point light source exists in the west/north-west of the art canvas

### Sprite Edge Features:

* **Exterior Sprite Edges/Borders** should be drawn with darker (non-black) colours
  * this is usually accomplished by taking a a low-lightness variant of the feature's edge colour (e.g. red shirt -> dark red edge, pale skin -> slightly tanned edge, etc.)
  * exceptions to this rule include:
    * buildings + large furniture - generally use black or near-black borders to better guide player pathing
    * low readability - if the typical approach leads to visual muddiness, edges may be drawn with much lower lightness or higher lightness
      * e.g. white shirts - occasionally use black borders (for dress shirts) or bright boarders (for party shirts) instead of a dark gray colour
      * e.g. skin tones - occasionally needs higher contrast, especially if adjacent clothing colour would muddy 
* **Interior Sprite Edges/Borders** must be drawn with sufficiently high contrast interior borders to avoid muddiness
  * interior borders should be drawn using low-lightness variants of the feature's interior colour
  * interior borders should NOT be drawn using black or near-black colours

### Artwork Divisibility:

* art should be drawn such that the width (horizontal) pixel count is divisible by 2
* this allows for simple anchor placement in the centre of the object, otherwise anchoring will bias either left or right by 0.5px

### Alpha and Padding

* Art must have its content pre-multiplied by alpha
* Art must NOT have any content bordering the edges of its canvas
  * apply ≥1px border of alpha = 0, R/G/B = 0 to the edges of artwork

*For simplicity, see [AlphaPreMultiply.py](../PyTools/ImagePreImportTool/AlphaPreMultiply.py) python tool, which can be applied to any as-drawn artwork to ensure it adheres to requirements*

### Character Animation Pixel Offsetting

* When animating character movement, 1px offsetting in the artwork should be applied as follows:
  * Front/Back Motion:  Wobble 1px left/right
  * Left/Right Motion:  Wobble 1px up/down
  * BackLeft/Right && FrontLeft/Right Motion:  Wobble 1px up/down

### Tilemap Artwork - Additional Considerations

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
