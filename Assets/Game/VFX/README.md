# Assets:  Game - VFX

## Particle Effects

Various 2D particle effects making use of Unity's [Particle System](https://docs.unity3d.com/ScriptReference/ParticleSystem.html) can be found in [Particles](./Particles/).  

### Edge Miasma

[Edge Miasma](./Particles/EdgeMiasma.prefab) spawns a large number of shifting black square/triangle/circle particles in a pre-defined shape that can be placed around maps, objects, etc. to create a black miasma effect, as below:

<img src="../../../InfoTools/Documentation/Game/VFX/BlackEdgeMiasma.gif" width="400">

Notably, this effect is used to generate the dream-like miasma used in the opening scene, [OfficeExterior](../../Scenes/OfficeExterior.unity).

### Ramen Trail

[Ramen Trail](./Particles/RamenTrail.prefab) continously spawns yellow/red squares of varying size/orientation, disappearing as they move outward from the source/origin, as below:

<img src="../../../InfoTools/Documentation/Game/VFX/RamenTrail.gif" width="400">

Notably, this effect is used to generate the mystical trail that follows [PhilRamen](../CharacterObjects/PCs/Assist/PhilRamen/PhilRamenAssist.prefab) as he dances around the screen in the scene, [OfficeInterior](../../Scenes/OfficeInterior.unity)

## Shaders

[Custom shaders](./Shaders/) in Frankie are built using [Unity's Shader Graph](https://docs.unity3d.com/Manual/shader-graph.html) tool.  They are used for a host of applications, including:
* psychedelic battle backgrounds
* swirling battle entry screens
* special attack animations during combat
* to enhance specific world objects *(make them pop)*

More detail on these specific use cases is provided in the [Shaders](./Shaders/) sub-directory.
