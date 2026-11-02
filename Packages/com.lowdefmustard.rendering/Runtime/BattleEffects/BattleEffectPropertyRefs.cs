using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering
{
    internal static class BattleEffectPropertyRefs
    {
        // Note:  Properties of the shared BattleEffect graph (see BattleEffectComposite.hlsl) ~ MaterialDefinition only takes property names

        #region ShaderProperties
        private const string _isPreviewReference = "_IsPreview";
        private const string _previewTimeReference = "_PreviewTime";
        private const string _itemCountReference = "_ItemCount";
        private const string _itemDataReference = "_ItemData";
        private const string _tintColourAReference = "_TintColourA";
        private const string _tintColourBReference = "_TintColourB";
        private const string _tintSettingsReference = "_TintSettings";
        private const string _frameRateReference = "_FrameRate";
        private const string _edgeSoftnessReference = "_EdgeSoftness";
        private const string _glowWidthReference = "_GlowWidth";
        private const string _fitModeReference = "_FitMode";
        private const string _disableFlickerReference = "_DisableFlicker";
        private const string _disableJitterReference = "_DisableJitter";
        #endregion

        #region Static
        [NoAutoStaticsCleanup] private static readonly int _itemDataID = Shader.PropertyToID(_itemDataReference);
        #endregion

        #region PublicMaterialDefinitionProperties
        public static void SetTint(ref MaterialDefinition materialDefinition, Color colourA, Color colourB, BattleEffectTintSettings settings)
        {
            materialDefinition.SetColor(_tintColourAReference, colourA);
            materialDefinition.SetColor(_tintColourBReference, colourB);
            materialDefinition.SetVector(_tintSettingsReference, settings.ToVector4());
        }

        public static void SetRecipe(ref MaterialDefinition materialDefinition, Texture2D itemData, int itemCount, float frameRate, float edgeSoftness, float glowWidth)
        {
            materialDefinition.SetTexture(_itemDataReference, itemData);
            materialDefinition.SetFloat(_itemCountReference, itemCount);
            materialDefinition.SetFloat(_frameRateReference, frameRate);
            materialDefinition.SetFloat(_edgeSoftnessReference, edgeSoftness);
            materialDefinition.SetFloat(_glowWidthReference, glowWidth);
        }

        public static void SetFit(ref MaterialDefinition materialDefinition, BattleEffectFit fit)
        {
            materialDefinition.SetFloat(_fitModeReference, (int)fit);
        }
        
        // Verify if a material is drawn by the shared BattleEffect shader (i.e. takes a recipe)
        public static bool IsBattleEffectMaterial(Material material) => material != null && material.HasProperty(_itemDataID); 
        
        public static void SetPreview(ref MaterialDefinition materialDefinition, float previewTime, float frameRate, bool isFlickerEnabled, bool isJitterEnabled)
        {
            // Editor preview only:  draws the effect at a given time (seconds since spawn) + frame rate, instead of from _Phase, w/ retro toggles
            materialDefinition.SetFloat(_isPreviewReference, 1f);
            materialDefinition.SetFloat(_previewTimeReference, previewTime);
            materialDefinition.SetFloat(_frameRateReference, frameRate);
            materialDefinition.SetFloat(_disableFlickerReference, isFlickerEnabled ? 0f : 1f);
            materialDefinition.SetFloat(_disableJitterReference, isJitterEnabled ? 0f : 1f);
        }
        #endregion
    }
}
