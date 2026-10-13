# Assets:  Game - VFX, Shaders

## Battle Backgrounds

In Frankie, enemies have unique background animations defined on their [Character Objects](../../CharacterObjects/README.md#combat-setup), under `Moving Background Properties`.  These properties define:
* `Tile Sprite Image`:  the tiled background base
* `Shader Material`:  the effects to be applied to background base

The latter shaders are defined here in [BattleBackgrounds](./BattleBackgrounds/).  In general, battle backgrounds overlay two copies of the tiled sprite image, and move them relative to each other in order to give a psychedelic effect.  

For example, see below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleBackgroundExample.gif" width="320">

### Battle Background Shader Properties

[BattleBackground](./BattleBackgrounds/) shaders have three key properties:
* `_MainTex` (Texture2D):  background tile, set via the `Tile Sprite Image` property noted above
* `_PeriodA` (float):  defines the period (impacting the speed) of a first sequence of image transformations
* `_PeriodB` (float):  defines the period of a second sequence of image transformations

As an example, consider [LateralMovementTwirl](./BattleBackgrounds/BattleBackground_LateralMovementTwirl.shadergraph), which was used to generate the above example image:

<img src="../../../../InfoTools/Documentation/Game/VFX/ShaderLateralTwirlGraph.png" width="800">

In this shader, we:
1. use `_PeriodB` to apply a simple time-dependent x-direction (lateral) shift to the UV of the material
2. extract the x,y coordinate of the center-point of the screen
   1. use `_PeriodB` to apply a time-dependent shift to the x-value
   2. the the sine of  `_PeriodA` to apply a time-dependent shift to the y-value
3. use this modified center point as the input to a [Twirl Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Twirl-Node.html) with the above material
4. apply this modified UV to the `_MainTex` and output this to the shader fragment's base color / sprite mask

## Battle Entry Animations

The [BattleEntry](./BattleEntry/) shader is used to apply the 'swirl' animation when a member of Frankie's party makes contact with an enemy.  This animation is specifically employed by the [Fader](../../Core/README.md#persistent-objects-singleton) via the [BattleEntryShaderControl](../../../Scripts/Rendering/Shaders/BattleEntryShaderControl.cs) script when the combat interface is loaded onto the screen, as shown below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEntryExample.gif" width="320">

The type of swirl depends on if Frankie is facing the enemy (and vice versa) during contact, such that we have three different types of [TransitionTypes](../../../Scripts/Zones/TransitionType.cs) for battle entry:
* `BattleNeutral`:  when Frankie and the enemy are facing each other (purple swirl)
* `BattleGood`:  when the enemy is facing away from Frankie (green swirl)
* `BattleBad`:  when Frankie is facing away from the enemy (red swirl)

These different swirl types are handled via different input images applied to the same [BattleEntry](./BattleEntry/) shader material.

### Time-Zero / Shader Phase

Since many custom shaders warp the view/image in a continuous forward fashion, they need to have some concept of time-zero as a reference point to subtract from the current time.  For example, this is accomplished using the `_Phase` property in the [BattleEntryShader](./BattleEntry/BattleEntryByURP.shadergraph) shadergraph.

In turn, `_Phase` is set using a MonoBehaviour, either with [BattleEntryShaderControl](../../../Scripts/Rendering/Shaders/BattleEntryShaderControl.cs) or directly on a [BattleEffectElement](../../../Scripts/UI/Combat/BattleStage/BattleEffectElement.cs) via its [BattleEffectHandle](../../../Scripts/UI/Combat/BattleStage/BattleEffectHandle.cs).  In any case, this is notable because time is a simple float with limited precision, which becomes problematic for very large numbers.  In other words, shaders using an input `_Phase` will begin to lack precision, impacting swirl smoothness, for very, very long continuous play-times.

## Battle Effects

[BattleEffectShaders](./BattleEffectShaders/) are employed by [BattleActions](../../Combat/BattleActions/), specifically in the [SpawnedArtwork](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/) effect strategies, to generate animations/artwork during combat.  

Briefly:
* a character will use a [Skill](../../OnLoadAssets/Skills/) or [ActionItem](../../OnLoadAssets/Inventory/ActionItems/), which implements a [BattleAction](../../OnLoadAssets/BattleActions/)
* the battle action includes targeting strategies, filtering strategies and effects strategies, as detailed [here](../../Combat/BattleActions/)
  * these effects can be used to apply a change in character state (e.g. adjust HP, apply status effect, etc.)
  * , but they are also used to generate any relevant animations/artwork when the [BattleAction](../../OnLoadAssets/BattleActions/) is activated

The standard [SpawnedArtwork](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/) effect strategy uses the [SpawnBattleEffect](../../../Scripts/Combat/BattleAction/Effects/SpawnBattleEffect.cs) scriptable object to spawn a visual effect either at the location of the target(s) (if `isGlobalEffect` is set to `false`), or over the entire battle screen (if `true`).  

The [BattleEffectShaders](./BattleEffectShaders/) serve to generate that visual effect, primarly with the use of [signed distance functions](https://en.wikipedia.org/wiki/Signed_distance_function), such as those detailed in [Shader Utilities](#shader-utilities) below.  

For example, see the [CirclePopEffect](./BattleEffectShaders/CirclePopEffect.shadergraph) shader graph:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectsShaderGraph.png" width="800">

, which is used to generate the [CirclePopPink](../../Combat/BattleActions/EffectStrategiesSpawnedArtwork/SpawnCirclePopPink.asset) below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEffectsCirclePopPink.gif" width="320">

## Shader Utilities

The shader graphs detailed above make use of shadergraph sub-graphs located in [Shader Utilities](./ShaderUtilities/).  These utilities include various intuitively named helper functions -- such as [GetScreenCenterPoint](./ShaderUtilities/GetScreenCenterPoint.shadersubgraph), [GetPeriodTime](./ShaderUtilities/GetPeriodTime.shadersubgraph) and [InheritVertexColorAlpha](./ShaderUtilities/InheritVertexColorAlpha.shadersubgraph).  These utilities also include the functionality required for the [signed distance functions](https://en.wikipedia.org/wiki/Signed_distance_function) noted in [Battle Effects](#battle-effects).

As an alternate to shadergraph sub-graphs, many combat and battle effects also use custom .hlsl sub-functions, as written in [HLSLUtilities](../../../Scripts/Rendering/Shaders/HLSLUtilities), and hooked up via reflection with `UNITY_EXPORT_REFLECTION `.

### Signed Distance Functions

A shader graph will use one or more HLSL sub-functions to translate an input UV point into an output a float value -- where positive values indicate the point is outside the SDF shape, negative values indicate the point is inside, and zero values indicate the point is on the boundary.

An example is the [SDFCircle](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFShapes.hlsl), with tunables of:
* `position` (ranging [0,0] to [1,1])
* , and `radius` to define where on the image texture the circle will appear

Increasing in complexity is the [SDFSquiggle](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFShapes.hlsl), with tunables of:
* `isHorizontal` to set if the wave is horizontal or vertical
* `position` and `width` to define where on the image the sinusoid wave will appear
* `frequency` and `phaseOffset` to define the shape of the wave
* `minMax` and `intensityModifier` to define the height of the wave

, and so on.

These sub-function SDFs are then fed into the standard [SDF parser](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFParser.hlsl), which converts the 1D float into a color and alpha output channel for rendering (i.e. to pass to the fragment of the shader graph).  The ArbitrarySDFParser itself has tunables for rendering SDF, including:
* `crispness`:  how smoothed the SDF boundary edge should be
* `drawColor` / `backgroundColor` to define the colors to lerp between in drawing the SDF shape
  * *note:  practically these are kept as W255 and W0, with color set by a tint property*

For SDF outlines, as shown in above/prior examples, the thickness of the outline is defined by passing the SDF into an `Onion` function in [SDFShapes](../../../Scripts/Rendering/Shaders/HLSLUtilities/SDFShapes.hlsl) prior to parsing it with SDFParser.

