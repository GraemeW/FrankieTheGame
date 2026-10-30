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
        // Note:
        //  - Stash an alternate copy of the recipe texture w/ hidden entries skipped (see BattleEffectPreviewVisibility)
        //  - This is done instead of overriding the Recipe texture to prevent cross-contamination b/w editor testing vs. runtime actual
        //      - Deliberately NOT simply housing the editor texture here (i.e. even with zero hidden entries), because when all items are shown, we want to reproduce the exact runtime path
        
        public BattleEffectRecipe recipe;
        public Material material;
        public BattleEffectTint tint = new();
        public Color backgroundColour = Color.black;
        public BattleEffectFit fit = BattleEffectFit.AspectFit;
        [NonSerialized] private Texture2D visibleDataTexture; // Owned bake w/ the preview-hidden entries skipped

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
            if (BattleEffectPreviewVisibility.HasAnyHidden())
            {
                if (visibleDataTexture == null) { visibleDataTexture = recipe.CreateDataTexture("Battle Effect Preview Data"); }
                recipe.ApplyTo(ref materialDefinition, fit, visibleDataTexture, BattleEffectPreviewVisibility.IsHidden);
            }
            else { recipe.ApplyTo(ref materialDefinition, fit); }
            tint.ApplyTo(ref materialDefinition);
            BattleEffectPropertyRefs.SetPreview(ref materialDefinition, time, retro.GetFrameRate(recipe), retro.isFlickerEnabled, retro.isJitterEnabled);
            return materialDefinition;
        }

        public void ReleaseTextures()
        {
            if (visibleDataTexture != null) { UnityEngine.Object.DestroyImmediate(visibleDataTexture); }
            visibleDataTexture = null;
        }
    }
}
#endif
