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
* a [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) spawns its effect by its [placement](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/BattleEffectPlacement.cs)
  * placement defines whether the effect is over each target's enemy slide, across each target's row (spanning that row's enemies, per row), or over the whole battle stage
  * row and full-screen effects stretch to fill their area (see `BattleEffectFit`), while slide effects keep their proportions (sized to match)

The framework itself (incl. recipe library, items, shapes, tints, HLSL library, preview window + the dispatch generator) lives in the [com.lowdefmustard.rendering](../../../../Packages/com.lowdefmustard.rendering/README.md) package.  Frankie implements the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) strategy, the battle stage elements that draw it, the shader graphs + materials, and the recipe asset files.

For example, see the pink circle pop below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectsCirclePopPink.gif" width="320">

### Recipes

Animated battle effects are comprised of a list of timed, animated shape items, with each effect using a shared [BattleEffect](./BattleEffectsEntities/BattleEffect.shadergraph) shader, and its own distinct recipe ([BattleEffectRecipe](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectRecipe.cs) asset).  [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs) references a recipe, and then adds the effect's tint, so one recipe serves many colour variants (e.g. [SpawnMultiPopBlue](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnMultiPopBlue.asset) and [SpawnMultiPopYellow](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnMultiPopYellow.asset)).

A recipe has:
* `Frame Rate`:  steps the whole animation at this many frames per second for a retro, held-frame look (0 = smooth)
* `Scale Ratio`:  grows / shrinks the whole effect about its centre (positions, sizes, outlines, jitter + glow)
* `Edge Softness Pixels`:  anti-aliasing (so edges are equally crisp at any size)
* `Glow Width EU`:  an extra soft edge
* `Items`:  drawn in order, so later items paint over earlier ones
* `Instances`:  other recipes drawn after the items in order (flattened in at bake), w/ each:
  * re-timed (start offset)
  * mirrored left / right and / or top / bottom about the effect's centre
  * placed:  scaled per axis about the effect's centre (`Scale Ratio`), then moved (`Offset EU`)
  * recoloured (via colour swaps)

Important Notes:
1. In the standard `fit=aspectFit` configuration, positions and sizes are in effect units:  
   - 0-1 spans the effect's shorter side, centred, with v running upwards
   - In this manner, shapes keep their proportions on any slide size (the longer side simply reaches beyond 0-1).
2. In the `fit=stretch` configuration, e.g. with row and full-screen effects, the effect stretches to fill the element
   - 0-1 spans the whole element on each axis for this case
3. A material other than BattleEffect can still be used for a one-off effect
    - simply leave the SpawnBattleEffect's recipe empty, and the material is drawn as-is (w/ `_Phase` set, and the tint's colour A as its vertex tint)

#### Recipe Items

Most item settings are ranges, running from their start to end value over the item's eased duration.  

Setting names carry their unit as a suffix (items, recipes and shapes alike):
* `EU`:  effect units - the effect space
  * 0-1 spans the effect's shorter side when aspect fit
  * 0-1 spans the whole element when stretched
* `SU`:  shape units - the shape's own space
  * 1 corresponds to the item's scale
  * patterns scale + rotate w/ the item
* `Ratio`:  one length over another (e.g. height / width)
* `Fraction`:  a 0-1 amount
  * signed where noted, e.g. pulse amount
  * phases are fractions of a cycle
* `Seconds`, `Degrees`, `Pixels` (screen):  as named
* `Rate`:  per second
* `PerEU`:  per effect unit
* no suffix:  counts, seeds, enums, colours + toggles

Each [item](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/BattleEffectItem.cs) has:

|          Group          |                                                              Settings                                                              |                                                                                                                                                                                                                  Detail                                                                                                                                                                                                                   |
|:-----------------------:|:----------------------------------------------------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
|         `Shape`         |                                                   the shape type + its settings                                                    |                                                                                                                                                                                                        see [Shapes](#shapes) below                                                                                                                                                                                                        |
|        `Timing`         |                            start time seconds, duration seconds, easing, approach ratio, progress steps                            |                                                                                              start is after the effect spawns;  the item only draws within [start, start + duration);  easing = linear / ease in / ease out / ease in-out / approach (as if rushing at the camera);  progress steps hold every range on evenly spaced values                                                                                              |
|       `Transform`       |                        position EU, scale EU, elongation SU, item rotation degrees, shape rotation degrees                         |                                                 a unit shape's size is its scale;  elongation stretches a shape's middle (e.g. diamond -> long hexagon, circle -> capsule;  distances stay exact inside);  item rotation turns the whole item (its repetition pattern too);  shape rotation turns the shape within each copy (after repetition);  a negative scale flips that local axis                                                  |
|         `Style`         | fill / outline / fill and outline, blend (over / erase), outline thickness EU, colour, outline colour (or from the palette + step) | colour = the shape + palette entry A, multiplied by the effect's tint;  outline colour = fill and outline's stroke (or, stepped from palette: border follows the fill's cycle / pattern / beats);  erase punches the item's coverage out of the items before it (fill and outline: punches fill only);  stripes' mask likewise applies to fill on fill and outline;  thickness may end below zero to fade an outline out through the glow |
|      `Alt Colours`      |                                                      count (0-3), colours B-D                                                      |                                                                                                                                                                        palette entries after the colour (alpha kept);  stepped through by the colour cycle + beats                                                                                                                                                                        |
| `Outline Edge Gradient` |                                                         on, colour, steps                                                          |                                                                                                                                                     outline only:  gradient outline stroke's inside edge -> this colour on outside edge;  steps band it (e.g. 2 = a two-tone stroke)                                                                                                                                                      |
|        `Shade`          |                       on, angle (range), offset SU (range), smooth, fill palette step, outline palette step                        |                                                                                                                              steps one side of each copy through the palette, past a line through the copy's centre;  option for anti-aliasing && angle + offset run start -> end over the item's progress                                                                                                                                |
|        `Stripes`        |                                    stripes per EU, angle degrees, duty fraction, phase fraction                                    |                                                                                                                                                hard-edged band mask fixed to the effect, e.g. scanlines;  0 stripes = off;  two items w/ duty 0.5 + phases 0.5 apart interleave perfectly                                                                                                                                                 |
|         `Clip`          |                                    none / box / diamond, half size SU, offset SU, angle degrees                                    |                                                                                                                                                        cuts what the item draws (after its style + repetition) to a shape in its local space, centred on its position + the offset                                                                                                                                                        |
|      `Repetition`       |         none / line / cross / grid / radial / staggered grid, count, spacing SU, copy scale ratio, copy wave, mirror x / y         | copies of the shape in its own space (patterns scale / rotate w/ the item);  radial rotates count copies about the item's centre;  staggered grid gives odd rows one fewer copy (honeycomb);  copy scale ratio sizes copy n by ratio ^ n;  copy wave (line / cross) sets copy n across its line by amplitude * waveform at phase + n / copies per cycle;  mirror folds one half onto the other (spread beat offsets apply after the fold) |
|     `Colour Cycle`      |                                source (time / copy / pattern), rate, phase offset (steps), pattern                                 |                                            steps through the palette (colour, then alt colours):  time source = cycle on the effect clock; copy source = each repetition gets next entry (line / radial:  along;  grid:  column + row); pattern source = palette index per step from the item's start (e.g. `0112`, up to 36 steps, repeating); all: phase offset starts that many steps ahead                                            |
|         `Beats`         |                 count (0-4), each:  start seconds, offset EU, elongation SU, colour index;  pattern + pattern rate                 |                                                                                      held steps within one item, each moving it, stretching it + shifting its palette;  a beat holds until the next starts;  or a pattern picks the beat per step (`0` = none, `1-4` = A-D, e.g. `01123`, <= 20 steps at its rate from the item's start, repeating)                                                                                       |
|        `Jitter`         |                                   rate, random / orbit, amount EU (x / y), seed, phase fraction                                    |                                                                                                                                                  held offsets:  random re-rolls each step (same seed + rate = same jumps);  orbit steps clockwise around the corners of the ± amount box                                                                                                                                                  |
|        `Flicker`        |                                           rate, depth fraction, phase fraction, pattern                                            |                                                                                                                        a square-wave bright / dim on the effect's clock (phase 0.5 = dim half first);  or a pattern (e.g. `11011 0000`, up to 23 steps at rate steps per second, repeating) from the item's start                                                                                                                         |
|         `Pulse`         |                                               rate, amount fraction, phase fraction                                                |                                                                                                                                                                                              a square-wave size change on the effect's clock                                                                                                                                                                                              |

*N.B.  Flicker (except patterns), jitter and pulse all run on the effect's clock to ensure items w/ the same settings stay in sync -- e.g. a shape's fill + outline items move as one.*

#### Shapes

Each [shape](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/Shapes/BattleEffectShape.cs) is drawn centred on the item's position, sized by its scale:

|     Shape     |                                                      Settings                                                       |                                                                               Detail                                                                                |
|:-------------:|:-------------------------------------------------------------------------------------------------------------------:|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
|   `Circle`    |                                                          -                                                          |                                                                   a unit circle (radius = scale)                                                                    |
|   `Diamond`   |                                                    aspect ratio                                                     |                                                          points at x = ±1;  aspect ratio = height / width                                                           |
|   `Segment`   |                                                          -                                                          |                                                           a line from x = -1 to 1 (use the outline style)                                                           |
|   `Chevron`   |                                                     depth ratio                                                     |                                 a '^' from x = -1 to 1, its arms sweeping back by depth ratio x its half width (use outline style)                                  |
|  `Squiggle`   |      half length SU, wavelength SU, amplitude SU, scroll SU, waveform, plateau fraction, duty fraction, bumps       | a sine / triangle / square / steps / trapezoid / pulse wave along local x w/ round ends; scroll range slides it along the line (use  outline style + uniform scale) |
|    `Lens`     |                                                    aspect ratio                                                     |                                    a pointed ellipse (eye / leaf / spark), tips at x = ±1;  aspect ratio = height / width (≤ 1)                                     |
| `Jagged Line` | half length SU, cell width SU, amplitude SU, calm fraction, seed, bump half width SU, variation fraction, scroll SU |     a flat line along local x w/ a seeded up / down / flat triangular bump per cell, round ends;  same seed = same bumps -> offset / rotated copies make echoes     |
|    `Wedge`    |                                         half angle degrees, apex offset SU                                          |                an infinite filled 'V' opening up from (0, apex offset);  over 90° it fills all but a downward wedge;  the apex offset may cross zero                |
|     `Box`     |                                           aspect ratio, corner radius SU                                            |                a rectangle w/ edges at x = ±1;  aspect ratio = height / width;  corners round inward;  full-screen fill = stretch fit, scale ≥ 0.52                 |
|  `Triangle`   |                                                    aspect ratio                                                     |                    isosceles, pointing up, centred on its bounding box:  base corners at x = ±1, apex at y = +aspect ratio (0.866 = equilateral)                    |
| `Trapezoid`   |                                       bottom half width SU, top half width SU                                       |                                                 isosceles, parallel edges horizontal at y = -1 (bottom) / +1 (top)                                                  |
|   `Polygon`   |                                                        sides                                                        |                                                           standard, corners at radius 1, one pointing up                                                            |
|    `Star`     |                                                 points, inner ratio                                                 |             tips at radius 1, one pointing up;  inner ratio = inner corner radius / tip radius (lower = spikier;  cos(180° / points) = straight sides)              |
|  `Ziggurat`   |   base half height SU, tiers, riser size SU, tread width SU, top half width SU, half length SU, riser slope ratio   |                         a horizontal band stepping up into tiers at its centre, mirrored top / bottom, closing to a point at ± half length                          |

#### Tint

The [tint](../../../../Packages/com.lowdefmustard.rendering/Runtime/BattleEffects/Properties/BattleEffectTint.cs) (on the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/SpawnBattleEffect/SpawnBattleEffect.cs)) colours the whole effect (multiplied over its items' own colours):
* `Solid`:  colour A
* `Linear`:  A -> B along the angle (degrees), over 1 / bands per EU
* `Wave`:  bands per EU along the angle, A at the peaks + B in the troughs, scrolling at the scroll rate (bands per second)
* `Radial`:  rings out from the centre, scrolling at the scroll rate

*N.B.  Wave / radial w/ 0 bands per EU pulse the whole effect between A and B.*

### Authoring:  Battle Effect Preview

The [Battle Effect Preview](../../../../Packages/com.lowdefmustard.rendering/Editor/BattleEffects/BattleEffectPreviewWindow/BattleEffectPreviewWindow.cs) window plays a recipe w/o entering play mode.  Open the preview window by double-clicking a recipe, from the menu item `Tools/Battle Effects/Preview`, or from a recipe's inspector menu.  

An example preview window is shown below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectPreview.png" width="400">

As shown above, the preview provides the below functionality:
* recipe, tint and background at a size preset, with a fit mode (aspect fit for slides, stretch for full screen)
  * Frankie registers its enemy slide sizes in [BattleEffectPreviewSizeRegistration](../../../Scripts/Combat/BattleAction/Editor/BattleEffectPreviewSizeRegistration.cs)
  * Reference frames' sizes also available at x0.25 / x0.5 / x1 / x2 scale
* play / pause, restart, frame stepping, looping + playback speed, and toggles for stepped time / flicker / jitter (to compare w/ + w/o the retro settings)
* a timeline w/ one bar per item (click / drag to scrub)
  * incl. expandable header bar per instance, spanning its items, sorted in among the items by start time (re-timed by the instance's offset, incl. nested instances)
  * scrubbing selects the recipe in the inspector, and while paused, selects the items + instances active at the scrub time in its inspector
* reference frames:  a folder of PNGs played at their own frame rate, side by side or overlaid on the effect
* export frames:  renders every frame offscreen to `Temp/BattleEffectPreview/<recipe>/frame_####.png` (optionally w/ a transparent background)

### Authoring:  Recipe Inspector

A custom recipe inspector is provided to simplify editing recipes.  It contains a listview with all items + instances in timeline order, as well as entry view to display the editable properties of selected items/instances.

Some notable additional features:
* list view:
  * editable core properties (label, shape type, start time) from the list view
  * item/instance-specific highlighting
  * `Add` / `Add+` buttons with `From selected` discriminator to add items adjacent / after selected entries
  * shift selected button to move selected items, with left-shift clamped by the viable offset of the most-limited item
* entry view:
  * auto-expand in-use sections && auto-collapse unused sections
  * green-highlight in-use section headers 
* full copy/paste functionality (via header or list view right click copy/paste) w/ option for copying/leaving out start time  
* full undo/redo functionality

*N.B.  The preview + export both draw through an offscreen runtime panel (`BattleEffectOffscreenRenderer`) instead of the editor window itself:  editor UI renders in gamma even in a linear project, so a material drawn straight into it shows its linear output unconverted / gamma mismatched.*

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

|                                                        File                                                        |                                                                                                               Nodes                                                                                                                |                                                                                         Role                                                                                         |
|:------------------------------------------------------------------------------------------------------------------:|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
|     [SDFShapes](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFShapes.hlsl)     | `SDF Circle`, `SDF Rhombus`, `SDF Segment`, `SDF Chevron`, `SDF Squiggle`, `SDF Lens`, `SDF Jagged Line`, `SDF Wedge`, `SDF Box`, `SDF Triangle`, `SDF Star`, `SDF Regular Polygon`, `SDF Ziggurat`, `SDF Trapezoid`, `SDF Onion`  |            [signed distance functions](https://en.wikipedia.org/wiki/Signed_distance_function) (negative inside, positive outside);  onion turns any SDF into an outline             |
|     [SDFDomain](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFDomain.hlsl)     |                    `SDF Aspect Fit`, `SDF Transform`, `SDF Mirror`, `SDF Repeat Line`, `SDF Repeat Grid`, `SDF Repeat Radial`, `SDF Elongate`, `SDF Stripe Mask`, `SDF Repeat Staggered Grid`, `Waveform Value`                    | move the point a shape is evaluated at (so shapes are written once, at the origin):  aspect correction, position / rotation / scale, mirroring, repetition, stretching;  stripe mask |
|   [TimeUtility](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/TimeUtility.hlsl)   |                           `Time Since Initialization`, `Period Time`, `Stepped Time`, `Ease Progress`, `Step Progress`, `Cycle Index`, `Pattern Index`, `Flicker`, `Flicker Pattern`, `Pulse`, `Jitter`                            |                            time + animation:  time-zero (see [Time-Zero / Shader Phase](#time-zero--shader-phase)), held frames, easing, retro modifiers                             |
| [ColourUtility](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/ColourUtility.hlsl) |                                                                                                         `Colour Gradient`                                                                                                          |                                                                   solid / linear / wave / radial colour gradients                                                                    |
|   [HashUtility](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/HashUtility.hlsl)   |                                                                                                             `Hash 2-2`                                                                                                             |                                                                stable pseudo-random numbers (e.g. for random jitter)                                                                 |
|     [SDFParser](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFParser.hlsl)     |                                                                                                            `SDF Parser`                                                                                                            |                                                      a single SDF -> colour + alpha, w/ a `crispness` edge (for simple graphs)                                                       |

*N.B.  Guards against bad inputs (e.g. a zero width, scale or spacing) live in these library functions, so every graph using a node is protected.*

### Battle Effect Composite

[BattleEffectComposite](../../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectComposite.hlsl) is the node behind the [shared BattleEffect shader](#the-shared-battleeffect-shader).  It steps the effect's clock, aspect-fits the UV, then for each item active at that time:
1. eases the item's progress through its time window (+ finds its current beat)
2. evaluates its flicker, jitter + pulse (on the effect's clock)
3. transforms the point into the shape's local space, interpolating its ranges
4. mirrors (+ spread beat offsets), repeats, turns + elongates, then measures the shape's distance at the nearest copies (via the generated dispatch)
5. applies its style (fill, then outline) + clip, converting each to coverage (anti-aliased in screen pixels, plus glow)
6. colours it (palette [+ shade], outline colour + edge gradient) + composites it over / erases it from the items before it

, before multiplying the result by the tint.
