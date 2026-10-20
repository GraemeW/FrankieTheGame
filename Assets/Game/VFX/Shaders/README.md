# Assets:  Game - VFX, Shaders

Frankie's custom shaders are [Shader Graphs](https://docs.unity3d.com/Manual/shader-graph.html) for UI Toolkit (`UI` target), with the most of the heavy logic written as HLSL functions in [HLSLUtilities](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/) (see [Shader Utilities](#shader-utilities)).  The one exception is the battle entry swirl, which is a UI Toolkit filter shader applied to a Visual Element using the `backdrop-filter` attribute.

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
1. convert each period to a running time with the [Period Time](#shader-utilities) node
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

[Fader](../../../Scripts/Zones/Fader.cs) holds these colours (one per `TransitionType`), and [BattleEntryShaderControl](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/BattleEntryShaderControl.cs) sets the chosen colour && the fade timings on the filter's material once per fade.  From there, the shader drives its own animation from shader time (or:  nothing is written per frame).

### Time-Zero / Shader Phase

Since many custom shaders warp the view/image in a continuous forward fashion, they need to have some concept of time-zero as a reference point to subtract from the current time.  This is accomplished with a `_Phase` property, set to `Time.time` when the effect starts.  Phase is set on the filter's material by [BattleEntryShaderControl](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/BattleEntryShaderControl.cs), and per element on each [BattleEffectElement](../../../Scripts/UI/Combat/BattleStage/BattleEffectElement.cs)'s `MaterialDefinition` (see [Battle Effects](#battle-effects)).

Notably, time is a simple float with limited precision, which becomes problematic for very large numbers.  In other words, shaders using an input `_Phase` will begin to lack precision, impacting swirl smoothness, for very, very long continuous play-times.

## Battle Effects

Battle effects are visual animations played during combat by [BattleActions](../../Combat/BattleActions/), via the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) effect strategy (in [SpawnedArtwork](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/)).  

Briefly:
* a character will use a [Skill](../../OnLoadAssets/Skills/) or [ActionItem](../../OnLoadAssets/Inventory/ActionItems/), which implements a [BattleAction](../../OnLoadAssets/BattleActions/)
* the battle action includes targeting strategies, filtering strategies and effects strategies, as detailed [here](../../OnLoadAssets/BattleActions/)
  * these effects can be used to apply a change in character state (e.g. adjust HP, apply status effect, etc.)
  * , but they are also used to generate any relevant animations/artwork when the battle action is activated
* a [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) spawns its effect over each target's enemy slide (sized to match it), or over the whole battle stage if set as a global effect
  *  note that global effects stretch to fill the stage (see `BattleEffectFit`), while slide effects keep their proportions

The framework itself (incl. recipe library, items, shapes, tints, HLSL library, preview window + the dispatch generator) lives in the [com.lowdefmustard.rendering](../../../../Packages/com.lowdefmustard.rendering/README.md) package.  Frankie implements the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) strategy, the battle stage elements that draw it, the shader graphs + materials, and the recipe asset files.

For example, see the pink circle pop below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectsCirclePopPink.gif" width="320">

### Recipes

Animated battle effects are comprised of a list of timed, animated shape items, with each effect using a shared [BattleEffect](./BattleEffectsEntities/BattleEffect.shadergraph) shader, and its own distinct recipe ([BattleEffectRecipe](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectRecipe.cs) asset).  [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) references a recipe, and then adds the effect's tint, so one recipe serves many colour variants (e.g. [SpawnMultiPopBlue](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnMultiPopBlue.asset) and [SpawnMultiPopYellow](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnMultiPopYellow.asset)).

A recipe has:
* `Frame Rate`:  steps the whole animation at this many frames per second for a retro, held-frame look (0 = smooth)
* `Scale`:  grows / shrinks the whole effect about its centre (positions, sizes, outlines, jitter + glow)
* `Edge Softness`:  anti-aliasing, in screen pixels (so edges are equally crisp at any size)
* `Glow Width`:  an extra soft edge, in effect units
* `Items`:  drawn in order, so later items paint over earlier ones

Important Notes:
1. In the standard `fit=aspectFit`  configuration, positions and sizes are in effect units:  
   - 0-1 spans the effect's shorter side, centred, with v running upwards
   - In this manner, shapes keep their proportions on any slide size (the longer side simply reaches beyond 0-1).
2. In the `fit=stretch` configuration, e.g. with global (full-screen) effects, the effect stretches to fill the element
   - 0-1 spans the whole element on each axis for this case
3. A material other than BattleEffect can still be used for a one-off effect
    - simply leave the SpawnBattleEffect's recipe empty, and the material is drawn as-is (w/ `_Phase` set, and the tint's colour A as its vertex tint)

#### Recipe Items

Most item settings are ranges, running from their start to end value over the item's eased duration.  

Each [item](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectItem.cs) has:

|   Group    |                                    Settings                                    |                                                                                                Detail                                                                                                 |
|:----------:|:------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
|  `Shape`   |                         the shape type + its own settings                         |                                                                                     see [Shapes](#shapes) below                                                                                      |
|  `Timing`  |                            start time, duration, easing                            |                        seconds after the effect spawns;  the item only draws within [start, start + duration);  easing = linear / ease in / ease out / ease in-out                         |
| `Transform` |                       position, scale, rotation, elongation                        |                    a unit shape's size is its scale;  elongation stretches a shape's middle (e.g. diamond -> long hexagon, circle -> capsule), in the shape's own units                     |
|  `Style`   |                          fill / outline, outline thickness, colour                          |                          thickness may end below zero to fade an outline out through the glow;  colour is multiplied by the effect's tint                           |
| `Repetition` |                        none / line / cross / grid, count, spacing, mirror x / y                        | copies of the shape in its own space (spacing in shape units, so patterns scale + rotate w/ the item);  e.g. a plus of five = cross w/ count 3;  mirror folds one half onto the other |
| `Flicker`  |                                 rate, depth, phase                                 |                                            a square-wave bright / dim on the effect's clock (phase 0.5 = dim half first)                                             |
|  `Jitter`  |                       rate, random / orbit, amount (x / y), seed, phase                       |              held offsets:  random re-rolls each step (same seed + rate = same jumps);  orbit steps clockwise around the corners of the ± amount box              |
|  `Pulse`   |                                 rate, amount, phase                                 |                                                     a square-wave size change on the effect's clock                                                      |

*N.B.  Flicker, jitter + pulse run on the effect's clock (not the item's), so items w/ the same settings stay in sync -- e.g. a shape's fill + outline items move as one.*

#### Shapes

Each [shape](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/Shapes/BattleEffectShape.cs) is drawn centred on the item's position, sized by its scale:

|  Shape  |                          Settings                          |                                    Detail                                     |
|:-------:|:----------------------------------------------------------:|:-----------------------------------------------------------------------------:|
| `Circle` |                             -                              |                        a unit circle (radius = scale)                        |
| `Diamond` |                         aspect                          |                    points at x = ±1;  aspect = height / width                    |
| `Segment` |                             -                              |                  a line from x = -1 to 1 (use the outline style)                  |
| `Chevron` |                          depth                           |    a '^' from x = -1 to 1, its arms sweeping back by depth (use the outline style)    |
| `Squiggle` | frequency, wave base / peak, phase speed, width | a sine wave segment along local x, its phase advancing by phase speed per second (rotate the item 90° for vertical) |

#### Tint

The [tint](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/Properties/BattleEffectTint.cs) (on the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs)) colours the whole effect (multiplied over its items' own colours):
* `Solid`:  colour A
* `Linear`:  A -> B along an angle
* `Wave`:  bands along an angle, A at the peaks + B in the troughs, scrolling at speed
* `Radial`:  rings out from the centre, scrolling at speed

*N.B.  Wave / radial w/ a frequency of 0 pulse the whole effect between A and B.*

### Authoring:  Battle Effect Preview

The [Battle Effect Preview](../../../../Packages/com.lowdefmustard.rendering/Editor/BattleEffects/BattleEffectPreviewWindow/BattleEffectPreviewWindow.cs) window plays a recipe w/o entering play mode.  Open the preview window by double-clicking a recipe, from the menu item `Tools/Battle Effects/Preview`, or from a recipe's inspector menu.  

An example preview window is shown below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectPreview.png" width="400">

As shown above, the preview provides the below functionality:
* recipe, tint and background at a size preset (or the reference frames' size), with a fit mode (aspect fit for slides, stretch for full screen)
  * Frankie registers its enemy slide sizes in [BattleEffectPreviewSizeRegistration](../../../Scripts/Combat/BattleAction/Editor/BattleEffectPreviewSizeRegistration.cs)
* play / pause, restart, frame stepping, looping + playback speed, and toggles for stepped time / flicker / jitter (to compare w/ + w/o the retro settings)
* a timeline w/ one bar per item (click / drag to scrub)
* reference frames:  a folder of PNGs played at their own frame rate, side by side or overlaid on the effect
* export frames:  renders every frame offscreen to `Temp/BattleEffectPreview/<recipe>/frame_####.png` (optionally w/ a transparent background)

Recipe edits show live, both in the preview and in a running battle.

### The Shared BattleEffect Shader

The [BattleEffect](./BattleEffectsEntities/BattleEffect.shadergraph) graph is thin:  its blackboard properties are wired into the [Battle Effect Composite](#battle-effect-composite) node, whose colour + alpha are multiplied by the element tint / opacity into the Render Type Branch.  

Per spawn, the [BattleEffectElement](../../../Scripts/UI/Combat/BattleStage/BattleEffectElement.cs) sets the below properties through a `MaterialDefinition`:
* `_Phase` (spawn time)
* the recipe:
  * its data texture `_ItemData`
  * `_ItemCount`
  * `_FrameRate`
  * `_EdgeSoftness`
  * `_GlowWidth`
* the tint
  * `_TintColourA`
  * `_TintColourB`
  * `_TintSettings`

`_IsPreview` / `_PreviewTime` / `_DisableFlicker` / `_DisableJitter` are for the preview window only.

The recipe reaches the shader as a small float texture (one row per item, columns per [BattleEffectDataLayout](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectDataLayout.cs)), baked + cached by the recipe and updated in place on edit.

### Adding a New Shape

A new shape is three small pieces:
1. a primitive in [SDFShapes](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFShapes.hlsl) (a reusable library function, centred on the origin)
2. an adapter in [BattleEffectShapes](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectShapes.hlsl), named `<ClassName>Distance(BattleEffectShapeInput)`, which maps the shape's parameters onto the primitive
3. a C# `[Serializable]` subclass of [BattleEffectShape](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/Shapes/BattleEffectShape.cs) (e.g. [BattleEffectDiamond](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/Shapes/BattleEffectDiamond.cs)), w/ a unique `[BattleEffectShapeId(n)]` and its settings as ranges, written in order by `GetParameters` (up to 8;  the adapter reads them in the same order)

Nothing else needs touching:  the [inspector](../../../../Packages/com.lowdefmustard.rendering/Editor/BattleEffects/BattleEffectShapeDrawer.cs) lists every subclass, and the HLSL dispatch ([Generated/BattleEffectDispatch.generated.hlsl](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/Generated/BattleEffectDispatch.generated.hlsl)) is regenerated by the [BattleEffectDispatchGenerator](../../../../Packages/com.lowdefmustard.rendering/Editor/BattleEffects/BattleEffectDispatchGenerator.cs) on script reload (only when it changes).  A missing adapter shows up as a shader error naming the function, and a duplicate shape ID as an editor error.

*N.B.  The generator also mirrors [BattleEffectDataLayout](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectDataLayout.cs) and any `[BattleEffectMirroredEnum]` enum into HLSL defines, so new per-item data is a layout const + its write in [BattleEffectItem](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectItem.cs) + its read in the composite.*

## Battle Entities

[SpriteWhiteOverride](./BattleEffectsEntities/SpriteWhiteOverride.shadergraph) draws an enemy sprite as a flat white silhouette, used for the shadow behind the currently targeted enemy.  It is applied as the shadow element's `style.unityMaterial`, and samples the sprite as the element texture.

## Shader Utilities

Shader logic beyond simple node chains is written as HLSL in [HLSLUtilities](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/), exposed to Shader Graph through Unity's [reflection API](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-nodes-hlsl-create-node-by-reflection.html).  

These form a reusable library, in layers:

|           File           |                                              Nodes                                              |                                                                         Role                                                                         |
|:------------------------:|:-----------------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------------------------------------------------------------:|
| [SDFShapes](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFShapes.hlsl) |                  `SDF Circle`, `SDF Rhombus`, `SDF Segment`, `SDF Chevron`, `SDF Squiggle`, `SDF Onion`                  | [signed distance functions](https://en.wikipedia.org/wiki/Signed_distance_function) (negative inside, positive outside);  onion turns any SDF into an outline |
| [SDFDomain](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFDomain.hlsl) | `SDF Aspect Fit`, `SDF Transform`, `SDF Mirror`, `SDF Repeat Line`, `SDF Repeat Grid`, `SDF Elongate` |          move the point a shape is evaluated at (so shapes are written once, at the origin):  aspect correction, position / rotation / scale, mirroring, repetition, stretching           |
| [TimeUtility](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/TimeUtility.hlsl) | `Time Since Initialization`, `Period Time`, `Stepped Time`, `Ease Progress`, `Flicker`, `Pulse`, `Jitter` |                                         time + animation:  time-zero (see [Time-Zero / Shader Phase](#time-zero--shader-phase)), held frames, easing, retro modifiers                                          |
| [ColourUtility](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/ColourUtility.hlsl) |                                       `Colour Gradient`                                        |                                                         solid / linear / wave / radial colour gradients                                                          |
| [HashUtility](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/HashUtility.hlsl) |                                           `Hash 2-2`                                           |                                                       stable pseudo-random numbers (e.g. for random jitter)                                                       |
| [SDFParser](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFParser.hlsl) |                                          `SDF Parser`                                          |                                           a single SDF -> colour + alpha, w/ a `crispness` edge (for simple graphs)                                            |

*N.B.  Guards against bad inputs (e.g. a zero width, scale or spacing) live in these library functions, so every graph using a node is protected.*

### Battle Effect Composite

[BattleEffectComposite](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectComposite.hlsl) is the node behind the [shared BattleEffect shader](#the-shared-battleeffect-shader) (specific to that graph, alongside its shape adapters in [BattleEffectShapes](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectShapes.hlsl) + the generated dispatch).  It steps the effect's clock, aspect-fits the UV, then for each item active at that time:
1. eases the item's progress through its time window
2. evaluates its flicker, jitter + pulse (on the effect's clock)
3. transforms the point into the shape's local space, interpolating its ranges
4. mirrors, repeats + elongates, then measures the shape's distance at the nearest copies (via the generated dispatch)
5. applies its style (fill / outline), converts the distance to coverage (anti-aliased in screen pixels via `fwidth`, plus glow)
6. composites it over the items before it

, before multiplying the result by the tint.
