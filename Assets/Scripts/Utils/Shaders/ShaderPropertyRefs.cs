using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Frankie.Utils
{
    public class ShaderPropertyRefs
    {
        // Global Shader References
        // Full Screen Pass Renderer Features
        private static string GLOBAL_RENDERER_FEATURE_BATTLE_ENTRY = "BattleEntryPostPass";

        // Parameters
        private static string GLOBAL_MAIN_TEXTURE_REFERENCE = "_MainTex";
        private static string GLOBAL_WORLD_TEXTURE_REFERENCE = "_WorldTex";
        private static string GLOBAL_SHADER_PHASE_REFERENCE = "_Phase";
        private static string GLOBAL_STRENGTH_REFERENCE = "_Strength";
        private static string GLOBAL_FADE_TIME_REFERENCE = "_FadeTime";
        private static string GLOBAL_FADEOUT_TIME_REFERENCE = "_FadeOutTime";
        private static string GLOBAL_FADEOUT_TOGGLE_REFERENCE = "_ToggleFadeOut";

        public static void ToggleBattleEntryFeature(Renderer2DData renderer2DData, bool enable)
        {
            if (renderer2DData == null || renderer2DData.rendererFeatures == null) { return; }
            foreach (ScriptableRendererFeature rendererFeature in renderer2DData.rendererFeatures)
            {
                if (rendererFeature.name == GLOBAL_RENDERER_FEATURE_BATTLE_ENTRY)
                {
                    rendererFeature.SetActive(enable);
                    break;
                }
            }
        }

        public static void SetMainTexture(Material material, Texture2D mainTexture)
        {
            if (material == null || mainTexture == null) { return; }
            material.SetTexture(GLOBAL_MAIN_TEXTURE_REFERENCE, mainTexture);
        }

        public static void SetWorldRenderTexture(Material material, RenderTexture worldRenderTexture)
        {
            if (material == null || worldRenderTexture == null) { return; }
            material.SetTexture(GLOBAL_WORLD_TEXTURE_REFERENCE, worldRenderTexture);
        }

        public static void SetShaderPhase(Material material, float phase)
        {
            if (material == null) { return; }
            material.SetFloat(GLOBAL_SHADER_PHASE_REFERENCE, phase);
        }

        public static void SetStrength(Material material, float strength)
        {
            if (material == null) { return; }
            material.SetFloat(GLOBAL_STRENGTH_REFERENCE, strength);
        }

        public static void SetFadeTime(Material material, float fadeTime)
        {
            if (material == null) { return; }
            material.SetFloat(GLOBAL_FADE_TIME_REFERENCE, fadeTime);
        }

        public static void SetFadeOutTime(Material material, float fadeOutTime)
        {
            if (material == null) { return; }
            material.SetFloat(GLOBAL_FADEOUT_TIME_REFERENCE, fadeOutTime);
        }

        public static void SetFadeOutToggle(Material material, bool enable)
        {
            if (material == null) { return; }
            material.SetInt(GLOBAL_FADEOUT_TOGGLE_REFERENCE, enable ? 1 : 0);
        }
    }
}
