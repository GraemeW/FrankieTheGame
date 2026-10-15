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
        private const string _itemCountReference = "_ItemCount";
        private const string _edgeSoftnessReference = "_EdgeSoftness";
        private const string _glowWidthReference = "_GlowWidth";
        private static readonly string[] _itemHeaderReferences = GetItemReferences("Header");
        private static readonly string[] _itemParamsAReferences = GetItemReferences("ParamsA");
        private static readonly string[] _itemParamsBReferences = GetItemReferences("ParamsB");
        private static readonly string[] _itemColourReferences = GetItemReferences("Colour");

        public const int battleEffectItemCapacity = 8;
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

        public static void SetBattleEffectEdge(ref MaterialDefinition materialDefinition, float edgeSoftness, float glowWidth)
        {
            materialDefinition.SetFloat(_edgeSoftnessReference, edgeSoftness);
            materialDefinition.SetFloat(_glowWidthReference, glowWidth);
        }

        public static void SetBattleEffectItemCount(ref MaterialDefinition materialDefinition, int itemCount)
        {
            materialDefinition.SetFloat(_itemCountReference, Mathf.Clamp(itemCount, 0, battleEffectItemCapacity));
        }

        public static void SetBattleEffectItem(ref MaterialDefinition materialDefinition, int index, Vector4 header, Vector4 paramsA, Vector4 paramsB, Color colour)
        {
            if (index is < 0 or >= battleEffectItemCapacity) { return; }
            materialDefinition.SetVector(_itemHeaderReferences[index], header);
            materialDefinition.SetVector(_itemParamsAReferences[index], paramsA);
            materialDefinition.SetVector(_itemParamsBReferences[index], paramsB);
            materialDefinition.SetColor(_itemColourReferences[index], colour);
        }
        #endregion

        #region PrivateMethods
        private static string[] GetItemReferences(string suffix)
        {
            var references = new string[battleEffectItemCapacity];
            for (int i = 0; i < battleEffectItemCapacity; i++) { references[i] = $"_Item{i}{suffix}"; }
            return references;
        }
        #endregion
    }
}
