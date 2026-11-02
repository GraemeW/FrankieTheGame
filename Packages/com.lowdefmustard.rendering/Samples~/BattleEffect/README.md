# Battle Effect Sample

The shared battle effect shader graph, a material using it, and an example recipe.

- **`BattleEffect.shadergraph`** — A UI Toolkit (`UI` target) graph built around the `Battle Effect Composite` node (`LDM/BattleEffects/Composite` in the node search). Its blackboard properties are wired into the node as inputs, and the node's colour + alpha are multiplied by the element tint / opacity. Every recipe is drawn by this one graph
- **`BattleEffect.mat`** — The material to assign to a battle effect element (or to the Battle Effect Preview window, which also finds it automatically once imported under `Assets`)
- **`CirclePopRecipe.asset`** — A small example recipe (a single expanding circle outline that fades out through the glow). Double-click it to open the Battle Effect Preview

To draw an effect at runtime, give a `VisualElement` a `MaterialDefinition` of the material, then apply the recipe + a tint and set the phase when it spawns:

```csharp
var materialDefinition = new MaterialDefinition(battleEffectMaterial);
recipe.ApplyTo(ref materialDefinition);
tint.ApplyTo(ref materialDefinition);
ShaderPropertyRefs.SetShaderPhase(ref materialDefinition, Time.time);
element.style.unityMaterial = new StyleMaterialDefinition(materialDefinition);
element.generateVisualContent += context => ShaderQuad.Draw(context, new Rect(0f, 0f, 1f, 1f), Color.white);
```
