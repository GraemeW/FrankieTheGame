#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [Serializable]
    public sealed class BattleEffectRenderSettings
    {
        public BattleEffectRecipe recipe;
        public Material material;
        public BattleEffectTint tint = new();
        public Color backgroundColour = Color.black;
        public BattleEffectFit fit = BattleEffectFit.AspectFit;

        // Editor Field References
        public const string tintField = nameof(tint);

        public bool isRenderable => recipe != null && material != null;

        public void FindMaterialIfUnset()
        {
            // Note:  matched on the shader's recipe property (not by name / path)
            tint ??= new BattleEffectTint(); // e.g. state saved before the tint existed
            if (material != null) { return; }
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(Material)}", new[] { "Assets" }))
            {
                var candidate = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!BattleEffectPropertyRefs.IsBattleEffectMaterial(candidate)) { continue; }
                material = candidate;
                return;
            }
        }

        public MaterialDefinition CreateMaterialDefinition(float time, BattleEffectRetroToggles retro)
        {
            var materialDefinition = new MaterialDefinition(material);
            recipe.ApplyTo(ref materialDefinition, fit);
            tint.ApplyTo(ref materialDefinition);
            BattleEffectPropertyRefs.SetPreview(ref materialDefinition, time, retro.GetFrameRate(recipe), retro.isFlickerEnabled, retro.isJitterEnabled);
            return materialDefinition;
        }
    }
}
#endif
