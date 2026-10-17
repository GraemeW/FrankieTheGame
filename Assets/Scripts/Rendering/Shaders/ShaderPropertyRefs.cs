using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Rendering
{
    public static class ShaderPropertyRefs
    {
        #region GlobalShaderProperties
        // Parameters
        private const string _globalMainTextureReference = "_MainTex";
        private static readonly int _mainTex = Shader.PropertyToID(_globalMainTextureReference);
        private const string _globalTintReference = "_Tint";
        private static readonly int _tintReference = Shader.PropertyToID(_globalTintReference);
        private const string _globalShaderPhaseReference = "_Phase";
        private static readonly int _phase = Shader.PropertyToID(_globalShaderPhaseReference);
        private const string _globalFadeTimeReference = "_FadeTime";
        private static readonly int _fadeTime = Shader.PropertyToID(_globalFadeTimeReference);
        private const string _globalFadeOutTimeReference = "_FadeOutTime";
        private static readonly int _fadeOutTime = Shader.PropertyToID(_globalFadeOutTimeReference);
        #endregion

        #region BattleEffectShaderProperties
        // Shared BattleEffect graph (see BattleEffectComposite.hlsl);  MaterialDefinition only takes property names
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
        private const string _disableFlickerReference = "_DisableFlicker";
        private const string _disableJitterReference = "_DisableJitter";
        #endregion
        
        #region Static
        // Reused across calls to avoid a per-call allocation (read -> write -> hand-off on every use)
        [NoAutoStaticsCleanup] private static MaterialPropertyBlock _workingPropertyBlock;
        [NoAutoStaticsCleanup] private static readonly int _itemDataID = Shader.PropertyToID(_itemDataReference);
        #endregion

        #region PublicRendererProperties
        // Applicable for SpriteRenderers
        public static void SetMainTexture(Renderer renderer, Texture2D mainTexture)
        {
            if (renderer == null) { return; }
            _workingPropertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_workingPropertyBlock);
            _workingPropertyBlock.SetTexture(_mainTex, mainTexture);
            renderer.SetPropertyBlock(_workingPropertyBlock);
        }
        
        public static void SetTint(Renderer renderer, Color color)
        {
            if (renderer == null) { return; }
            _workingPropertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_workingPropertyBlock);
            _workingPropertyBlock.SetColor(_tintReference, color);
            renderer.SetPropertyBlock(_workingPropertyBlock);
        }
        
        public static void SetShaderPhase(Renderer renderer, float phase)
        {
            if (renderer == null) { return; }
            _workingPropertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_workingPropertyBlock);
            _workingPropertyBlock.SetFloat(_phase, phase);
            renderer.SetPropertyBlock(_workingPropertyBlock);
        }
        #endregion
        
        #region PublicMaterialProperties
        // Applicable for global renderer effects (e.g. URP renderer pipeline) and UI elements (global material setting)
        public static void SetMainTexture(Material material, Texture2D mainTexture)
        {
            if (material == null || mainTexture == null) { return; }
            material.SetTexture(_mainTex, mainTexture);
        }

        public static void SetTint(Material material, Color color)
        {
            if (material == null) { return; }
            material.SetColor(_tintReference, color);
        }

        public static void SetShaderPhase(Material material, float phase)
        {
            if (material == null) { return; }
            material.SetFloat(_phase, phase);
        }

        public static void SetFadeTime(Material material, float fadeTime)
        {
            if (material == null) { return; }
            material.SetFloat(_fadeTime, fadeTime);
        }

        public static void SetFadeOutTime(Material material, float fadeOutTime)
        {
            if (material == null) { return; }
            material.SetFloat(_fadeOutTime, fadeOutTime);
        }
        #endregion

        #region PublicMaterialDefinitionProperties
        // Applicable for UI Toolkit elements drawn with style.unityMaterial (per-element overrides of a shared material)
        // Note:  MaterialDefinition is a struct, so by ref
        public static void SetShaderPhase(ref MaterialDefinition materialDefinition, float phase)
        {
            materialDefinition.SetFloat(_globalShaderPhaseReference, phase);
        }

        // Settings:  (mode, angle [radians], frequency, speed)
        public static void SetBattleEffectTint(ref MaterialDefinition materialDefinition, Color colourA, Color colourB, Vector4 settings)
        {
            materialDefinition.SetColor(_tintColourAReference, colourA);
            materialDefinition.SetColor(_tintColourBReference, colourB);
            materialDefinition.SetVector(_tintSettingsReference, settings);
        }

        public static void SetBattleEffectRecipe(ref MaterialDefinition materialDefinition, Texture2D itemData, int itemCount, float frameRate, float edgeSoftness, float glowWidth)
        {
            materialDefinition.SetTexture(_itemDataReference, itemData);
            materialDefinition.SetFloat(_itemCountReference, itemCount);
            materialDefinition.SetFloat(_frameRateReference, frameRate);
            materialDefinition.SetFloat(_edgeSoftnessReference, edgeSoftness);
            materialDefinition.SetFloat(_glowWidthReference, glowWidth);
        }

        // Whether a material is drawn by the shared BattleEffect shader (i.e. takes a recipe)
        public static bool IsBattleEffectMaterial(Material material) => material != null && material.HasProperty(_itemDataID);

        // Editor preview only:  draws the effect at a given time (seconds since spawn) + frame rate, instead of from _Phase, w/ retro toggles
        public static void SetBattleEffectPreview(ref MaterialDefinition materialDefinition, float previewTime, float frameRate, bool isFlickerEnabled, bool isJitterEnabled)
        {
            materialDefinition.SetFloat(_isPreviewReference, 1f);
            materialDefinition.SetFloat(_previewTimeReference, previewTime);
            materialDefinition.SetFloat(_frameRateReference, frameRate);
            materialDefinition.SetFloat(_disableFlickerReference, isFlickerEnabled ? 0f : 1f);
            materialDefinition.SetFloat(_disableJitterReference, isJitterEnabled ? 0f : 1f);
        }
        #endregion
    }
}
