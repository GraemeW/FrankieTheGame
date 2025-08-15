# Assets:  Game - VFX, Shaders

## Battle Backgrounds

In Frankie, enemies have unique background animations defined on their [Character Objects](../../CharacterObjects/README.md#combat-setup), under `Moving Background Properties`.  These properties define:
* `Tile Sprite Image`:  the tiled background base
* `Shader Material`:  the effects to be applied to background base

The latter shaders are defined here in [BattleBackgrounds](./BattleBackgrounds/).  In general, battle backgrounds overlay two copies of the tiled sprite image, and move them relative to each other in order to give a psychedelic effect.  

For example, see below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleBackgroundExample.gif" width="320">

### Battle Background Shader Properties

[Battle Background](./BattleBackgrounds/) shaders have three key properties:
* `_MainTex` (Texture2D):  background tile, set via the `Tile Sprite Image` property noted above
* `_PeriodA` (float):  defines the period (impacting the speed) of a first sequence of image transformations
* `_PeriodB` (float):  defines the period of a second sequence of image transformations

As an example, consider [LateralMovementTwirl](./BattleBackgrounds/LateralMovementTwirl.shadergraph), which was used to generate the above example image:

<img src="../../../../InfoTools/Documentation/Game/VFX/ShaderLateralTwirlGraph.png" width="800">

In this shader, we:
1. use `_PeriodB` to apply a simple time-dependent x-direction (lateral) shift to the UV of the material
2. extract the x,y coordinate of the center-point of the screen
   1. use `_PeriodB` to apply a time-dependent shift to the x-value
   2. the the sine of  `_PeriodA` to apply a time-dependent shift to the y-value
3. use this modified center point as the input to a [Twirl Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Twirl-Node.html) with the above material
4. apply this modified UV to the `_MainTex` and output this to the shader fragment's base color / sprite mask

## Battle Entry Animations



## Battle Effects

## World Objects (Background Shaders)

## Shader Utilities
