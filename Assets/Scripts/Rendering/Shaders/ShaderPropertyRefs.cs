using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Frankie.Rendering
{
    public static class ShaderPropertyRefs
    {
        #region GlobalShaderProperties
        // Full Screen Pass Renderer Features
        private const string _globalRendererBattleEntry = "BattleEntry";

        // Parameters
        private const string _globalMainTextureReference = "_MainTex";
        private static readonly int _mainTex = Shader.PropertyToID(_globalMainTextureReference);
        private const string _globalTintReference = "_Tint";
        private static readonly int _tintReference = Shader.PropertyToID(_globalTintReference);
        private const string _globalWorldTextureReference = "_WorldTex";
        private static readonly int _worldTex = Shader.PropertyToID(_globalWorldTextureReference);
        private const string _globalShaderPhaseReference = "_Phase";
        private static readonly int _phase = Shader.PropertyToID(_globalShaderPhaseReference);
        private const string _globalStrengthReference = "_Strength";
        private static readonly int _strength = Shader.PropertyToID(_globalStrengthReference);
        private const string _globalFadeTimeReference = "_FadeTime";
        private static readonly int _fadeTime = Shader.PropertyToID(_globalFadeTimeReference);
        private const string _globalFadeOutTimeReference = "_FadeOutTime";
        private static readonly int _fadeOutTime = Shader.PropertyToID(_globalFadeOutTimeReference);
        private const string _globalFadeOutToggleReference = "_ToggleFadeOut";
        private static readonly int _toggleFadeOut = Shader.PropertyToID(_globalFadeOutToggleReference);
        #endregion
        
        #region Static
        private static ScriptableRendererFeature GetRendererFeature(Renderer2DData renderer2DData, string featureName)
        {
            if (renderer2DData == null || renderer2DData.rendererFeatures == null) { return null; }
            return renderer2DData.rendererFeatures.FirstOrDefault(rendererFeature => rendererFeature.name == featureName);
        }
        
        // Reused across calls to avoid a per-call allocation (read -> write -> hand-off on every use)
        private static MaterialPropertyBlock _workingPropertyBlock;
        #endregion

        #region PublicUtility
        public static void ToggleBattleEntry(Renderer2DData renderer2DData, bool enable)
        {
            GetRendererFeature(renderer2DData, _globalRendererBattleEntry)?.SetActive(enable);
        }
        #endregion

        #region PublicRendererProperties
        // Applicable for SpriteRenderers
        // Note:  Cannot apply for UI elements (i.e. CanvasRenderer-base) - must use material base (shared property across all materials)
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
        
        public static void SetStrength(Renderer renderer, float strength)
        {
            if (renderer == null) { return; }
            _workingPropertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_workingPropertyBlock);
            _workingPropertyBlock.SetFloat(_strength, strength);
            renderer.SetPropertyBlock(_workingPropertyBlock);
        }
        #endregion
        
        #region PublicMaterialProperties
        // Applicable for global renderer effects (e.g. URP renderer pipeline) and UI elements
        // For in-game elements, use renderer-base calls instead (avoid global parameter overlap issue)
        
        public static void SetMainTexture(Material material, Texture2D mainTexture)
        {
            if (material == null || mainTexture == null) { return; }
            material.SetTexture(_mainTex, mainTexture);
        }

        public static void SetWorldRenderTexture(Material material, RenderTexture worldRenderTexture)
        {
            if (material == null || worldRenderTexture == null) { return; }
            material.SetTexture(_worldTex, worldRenderTexture);
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

        public static void SetStrength(Material material, float strength)
        {
            if (material == null) { return; }
            material.SetFloat(_strength, strength);
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

        public static void SetFadeOutToggle(Material material, bool enable)
        {
            if (material == null) { return; }
            material.SetInt(_toggleFadeOut, enable ? 1 : 0);
        }
        #endregion
    }
}
