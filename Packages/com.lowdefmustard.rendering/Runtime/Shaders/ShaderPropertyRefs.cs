using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering
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

        #region Static
        // Reused across calls to avoid a per-call allocation (read -> write -> hand-off on every use)
        [NoAutoStaticsCleanup] private static MaterialPropertyBlock _workingPropertyBlock;
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
        #endregion
    }
}
