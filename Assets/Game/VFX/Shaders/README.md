# Assets:  Game - VFX, Shaders

Frankie's custom shaders are [Shader Graphs](https://docs.unity3d.com/Manual/shader-graph.html) for UI Toolkit (`UI` target), with the heavier logic written as HLSL functions in [HLSLUtilities](../../../Scripts/Rendering/Shaders/HLSLUtilities/) (see [Shader Utilities](#shader-utilities)).  The one exception is the battle entry swirl, which is a UI Toolkit filter shader applied to a Visual ELement using the `backdrop-filter` attribute.

Since all combat visuals live in UI Toolkit, these shaders are applied per element:
* **Battle backgrounds and battle effects:** the element's `style.unityMaterial`, with a `MaterialDefinition` overriding properties per element
  * Elements sharing one material therefore still animate, colour and shape independently
* **Battle entry swirl:** a `backdrop-filter`, which distorts everything rendered behind it

## Battle Backgrounds

In Frankie, enemies have unique background animations defined on their [Character Objects](../../CharacterObjects/README.md#combat-setup), under `Moving Background Properties`.  These properties define:
* `Tile Sprite Image`:  the tiled background base
* `Shader Material`:  the effects to be applied to background base

The latter shaders are defined here in [BattleBackgrounds](./BattleBackgrounds/).  In general, battle backgrounds overlay two copies of the tiled sprite image, and move them relative to each other in order to give a psychedelic effect.  

For example, see below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleBackgroundExample.gif" width="320">

### Battle Background Shader Properties

The background is drawn by the battle stage's [BattleBackgroundElement](../../../Scripts/UI/Combat/BattleStage/BattleBackgroundElement.cs) as a single quad, textured with the tile sprite.  Its UVs are in **tile units** (i.e. running beyond 0-1, one unit per tile), so the tile texture must be set to Repeat wrap mode.  Shaders read:
* the tile itself as the **element texture** ~ sampled with the **UI > Sample Element Texture** node
* `_UVCenter` (Vector2):  the UV at the centre of the background, set by the element as the pivot for rotating/twirling transformations
* `_PeriodA` (float):  defines the period (impacting the speed) of a first sequence of image transformations
* `_PeriodB` (float):  defines the period of a second sequence of image transformations

As an example, consider [LateralMovementTwirl](./BattleBackgrounds/BattleBackground_LateralMovementTwirl.shadergraph), which was used to generate the above example image:

<img src="../../../../InfoTools/Documentation/Game/VFX/ShaderLateralTwirlGraph.png" width="800">

In this shader, we:
1. convert each period to a running time with the [Period Time](#time-utilities) node
2. use `_PeriodB` to apply a simple time-dependent x-direction (lateral) shift to the UV of the material
3. take `_UVCenter` as the twirl's centre-point
   1. use `_PeriodB` to apply a time-dependent shift to the x-value
   2. use the sine of `_PeriodA` to apply a time-dependent shift to the y-value
4. use this modified center point as the input to a [Twirl Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Twirl-Node.html) with the above UV
5. sample the element texture (the tile) at this modified UV and output it to the shader fragment's base color

## Battle Entry Animations

The [BattleEntryFilter](./BattleEntry/BattleEntryFilter.shader) shader applies the 'swirl' animation when a member of Frankie's party makes contact with an enemy.  It is a UI Toolkit custom filter, applied as a `backdrop-filter`, which means that it twirls the rendered surface behind it (including the world and any open UI boxes alike):
* the [BattleEntryFilter](./BattleEntry/BattleEntryFilter.asset) `Filter Function Definition` binds the shader's material && the twirl strength
* the filter itself is referenced from USS, via `--battle-entry-filter` in [Tokens.uss](../../UI/_UIToolkit/Styles/Tokens.uss) (also setting the strength), and applied by `.battle-entry-overlay` in [Fader.uss](../../Core/CoreDep/Fader/Fader.uss)
* the [Fader](../../Core/README.md#persistent-objects-singleton) shows its full-screen [BattleEntryOverlay](../../../Scripts/Zones/FadeOverlay/BattleEntryOverlay.cs) for the duration of the fade, covering everything on screen as a top-sorted panel

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEntryExample.gif" width="320">

The type of swirl depends on if Frankie is facing the enemy (and vice versa) during contact, such that we have three different types of [TransitionTypes](../../../Scripts/Zones/TransitionType.cs) for battle entry:
* `BattleNeutral`:  when Frankie and the enemy are facing each other (purple swirl)
* `BattleGood`:  when the enemy is facing away from Frankie (green swirl)
* `BattleBad`:  when Frankie is facing away from the enemy (red swirl)

[BattleEntryShaderControl](../../../Scripts/Rendering/Shaders/BattleEntryShaderControl.cs) sets these colours && the fade timings on the filter's material once per fade.  From there, the shader drives its own animation from shader time (i.e. nothing is written per frame).

### Time-Zero / Shader Phase

Since many custom shaders warp the view/image in a continuous forward fashion, they need to have some concept of time-zero as a reference point to subtract from the current time.  This is accomplished with a `_Phase` property, set to `Time.time` when the effect starts:  on the filter's material by [BattleEntryShaderControl](../../../Scripts/Rendering/Shaders/BattleEntryShaderControl.cs), and per element on each [BattleEffectElement](../../../Scripts/UI/Combat/BattleStage/BattleEffectElement.cs)'s `MaterialDefinition` (see [Battle Effects](#battle-effects)).

Notably, time is a simple float with limited precision, which becomes problematic for very large numbers.  In other words, shaders using an input `_Phase` will begin to lack precision, impacting swirl smoothness, for very, very long continuous play-times.

## Battle Effects

Battle effects are visual animations played during combat by [BattleActions](../../Combat/BattleActions/), via the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/Effects/SpawnBattleEffect.cs) effect strategy (in [SpawnedArtwork](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/)).  

Briefly:
* a character will use a [Skill](../../OnLoadAssets/Skills/) or [ActionItem](../../OnLoadAssets/Inventory/ActionItems/), which implements a [BattleAction](../../OnLoadAssets/BattleActions/)
* the battle action includes targeting strategies, filtering strategies and effects strategies, as detailed [here](../../OnLoadAssets/BattleActions/)
  * these effects can be used to apply a change in character state (e.g. adjust HP, apply status effect, etc.)
  * , but they are also used to generate any relevant animations/artwork when the battle action is activated
* a [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/Effects/SpawnBattleEffect.cs) spawns its effect over each target's enemy slide (sized to match it), or over the whole battle stage if set as a global effect

For example, the pink circle pop below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectsCirclePopPink.gif" width="320">

### The Shared BattleEffect Shader

Battle effects generally use one shader graph and material, [BattleEffect](./BattleEffectsEntities/BattleEffect.shadergraph), and are each configured with a unique recipe (a list of up to 8 timed shapes, set on the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/Effects/SpawnBattleEffect.cs) asset).  The recipe is passed through the [BattleStage](../../../Scripts/UI/Combat/BattleStage/BattleStage.cs) to the effect's [BattleEffectElement](../../../Scripts/UI/Combat/BattleStage/BattleEffectElement.cs), which writes it into the element's `MaterialDefinition`.  The graph itself is thin:  the [Battle Effect Composite](#battle-effect-composite) node does the work, then its colour is multiplied by the element tint (the effect colour) and its alpha by the element opacity.

A [recipe](../../../Scripts/UI/Combat/BattleStage/BattleEffects/BattleEffectRecipe.cs) has:
* `Edge Softness`:  anti-aliasing width, in screen pixels
* `Glow Width`:  an extra soft edge, in UV (i.e. to glow > the aliasing)
* `Shapes`:  drawn in order, so later shapes paint over earlier ones

Each [shape](../../../Scripts/UI/Combat/BattleStage/BattleEffects/BattleEffectShape.cs) has a type, a `Start Time` (seconds after the effect spawns), a `Duration` (the shape is only drawn within this window) and a `Colour` (multiplied by the effect colour, so white leaves the effect colour as-is).  Shape settings run linearly from their start to end values over the shape's duration.  Positions and sizes are in UV, i.e. 0-1 over the effect, with v running upwards:

| Shape Type |                                     Settings                                              |                                                                                                                            Detail                                                                                                                             |
|:----------:|:-----------------------------------------------------------------------------------------:|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
|  `Circle`  |            position, filled toggle, start/end radius, start/end ring thickness            |                A ring (or filled disc). Ending the thickness below zero fades the ring out through its glow, e.g. as w/ [CirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnCirclePopPink.asset)                                   |
| `Squiggle` | horizontal toggle, start/end travel, width, frequency, wave range, phase speed, thickness | A sine wave segment that travels along its axis, its phase advancing by phase speed per second and its height pulsing within the wave range, e.g. w/ [VerticalWavyRed](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnVerticalWavyRed.asset)) |

*N.B.  A material other than BattleEffect can also be used as a one-off effect by leaving its recipe empty.  The material is drawn as-is, with `_Phase` and the tint still set.*

### Adding a New Shape Type

Shapes are packed into material properties as three `Vector4`s plus a `Color` per slot:  a header of `(shape type, start time, duration, variant)` and two vectors of type-specific settings.  Adding a shape type means touching, in step:
1. [BattleEffectShapeType](../../../Scripts/UI/Combat/BattleStage/BattleEffects/BattleEffectShapeType.cs):  a new enum value (the value is the shader's shape ID)
2. a new settings class (e.g. [BattleEffectCircle](../../../Scripts/UI/Combat/BattleStage/BattleEffects/BattleEffectCircle.cs)) that packs its settings into the two vectors, plus its field and switch case in [BattleEffectShape](../../../Scripts/UI/Combat/BattleStage/BattleEffects/BattleEffectShape.cs)
3. [BattleEffectShapeDrawer](../../../Scripts/Combat/BattleAction/Editor/BattleEffectShapeDrawer.cs):  show the new settings block for its type
4. [BattleEffectComposite](../../../Scripts/Rendering/Shaders/HLSLUtilities/BattleEffectComposite.hlsl):  a matching shape ID and a branch in `BattleEffectItemDistance` that unpacks the vectors into a signed distance (ideally from a function in [SDFShapes](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFShapes.hlsl))

## Battle Entities

[SpriteWhiteOverride](./BattleEffectsEntities/SpriteWhiteOverride.shadergraph) draws an enemy sprite as a flat white silhouette, used for the shadow behind the currently targeted enemy.  It is applied as the shadow element's `style.unityMaterial`, and samples the sprite as the element texture.

## Shader Utilities

Shader logic beyond simple node chains is written in HLSL in [HLSLUtilities](../../../Scripts/Rendering/Shaders/HLSLUtilities/), exposed to Shader Graph through Unity's [reflection API](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-nodes-hlsl-create-node-by-reflection.html).

### Signed Distance Functions

A [signed distance function](https://en.wikipedia.org/wiki/Signed_distance_function) (SDF) translates an input UV point into a float value.  

[SDFShapes](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFShapes.hlsl) provides:
* `SDF Circle`:  a circle at a `position` (ranging [0,0] to [1,1]) with a `radius`
* `SDF Squiggle`:  a sinusoid wave segment, with tunables of:
  * `isHorizontal` to set if the wave is horizontal or vertical
  * `position` and `width` to define where on the image the wave segment sits, and how far it extends
  * `frequency` and `phaseOffset` to define the shape of the wave
  * `minMax` and `intensityModifier` to define the height of the wave
* `SDF Onion`:  to turn any SDF into an outline of a given `thickness` (e.g. a circle into a ring)

[SDFParser](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFParser.hlsl) converts a single SDF into a colour and alpha for rendering, with a `crispness` (how smoothed the boundary edge should be) and `drawColor` / `backgroundColor` to lerp between.

### Battle Effect Composite

[BattleEffectComposite](../../../Scripts/Rendering/Shaders/HLSLUtilities/BattleEffectComposite.hlsl) is the node behind the [shared BattleEffect shader](#the-shared-battleeffect-shader), and is specific to that graph.  It reads the recipe directly from the graph's blackboard properties (`_ItemCount`, `_EdgeSoftness`, `_GlowWidth`, `_Phase`, and `_Item{0-7}Header` / `ParamsA` / `ParamsB` / `Colour`), rather than through wired inputs.

For each active shape, it:
1. computes the shape's SDF with the functions above
2. converts it to coverage, with an anti-aliased edge (sized in screen pixels via `fwidth`) plus the optional glow
3. composites it over the shapes before it (in colour space)

### Time Utilities

[TimeUtility](../../../Scripts/Rendering/Shaders/HLSLUtilities/TimeUtility.hlsl) provides:
* `Time Since Initialization`:  seconds since a `phase` (see [Time-Zero / Shader Phase](#time-zero--shader-phase)), plus its sine and cosine
* `Period Time`:  a running time of `_Time.y * 2π / period` - i.e. a full sine cycle every `period` seconds (used by the [battle backgrounds](#battle-background-shader-properties))
