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

The [BattleEntry](./BattleEntry/) shader is used to apply the 'swirl' animation when a member of Frankie's party makes contact with an enemy.  This animation is specifically employed by the [Fader](../../Core/README.md#persistent-objects-singleton) as the combat interface is loaded onto the screen, as shown below:

<img src="../../../../InfoTools/Documentation/Game/VFX/BattleEntryExample.gif" width="320">

The type of swirl applies depends on if Frankie is facing the enemy (and vice versa) during contact, such that we have three different types of [TransitionTypes](../../../Scripts/Zones/TransitionType.cs) for battle entry:
* `BattleNeutral`:  when Frankie and the enemy are facing each other (purple swirl)
* `BattleGood`:  when the enemy is facing away from Frankie (green swirl)
* `BattleBad`:  when Frankie is facing away from the enemy (red swirl)

These different swirl types are handled via different input images, with the same [BattleEntry](./BattleEntry/) shader applied to them.  

### Time-Zero / Shader Phase

Since the shader warps each image in a continuous forward fashion, the shader needs to have some concept of time-zero as a reference point to subtract from the current time.  This is accomplished using the `_Phase` property in the  [BattleEntryShader](./BattleEntry/BattleEntryShader.shadergraph) shadergraph.

In turn, `_Phase` is set using the MonoBehaviour [SetMaterialTimeSinceInstantiation](../../../Scripts/Utils/Shaders/SetMaterialTimeSinceInstantiation.cs), which simply sets the attached image material's to property to [Time.time](https://docs.unity3d.com/ScriptReference/Time-time.html) when the game object is enabled.  This is notable because time is a simple float with limited precision, which becomes problematic for very large numbers.  In other words, this shader will begin to lack precision, impacting swirl smoothness, for very, very long continuous play-times (e.g. if the application has not been closed for >12hr).

## Battle Effects

## World Objects (Background Shaders)

## Shader Utilities
