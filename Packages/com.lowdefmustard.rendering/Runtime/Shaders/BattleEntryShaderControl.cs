using UnityEngine;

namespace LowDefMustard.Rendering
{
    public class BattleEntryShaderControl : MonoBehaviour
    {
        // Note:  Sets the battle entry filter's material once per fade - the filter runs on shader time from there

        // Tunables
        [SerializeField] private Material battleEntryMaterial;

        public void SetBattleEntryParameters(Color tint, float fadeTime, float fadeOutTime)
        {
            ShaderPropertyRefs.SetTint(battleEntryMaterial, tint);
            ShaderPropertyRefs.SetShaderPhase(battleEntryMaterial, Time.time);
            ShaderPropertyRefs.SetFadeTime(battleEntryMaterial, fadeTime);
            ShaderPropertyRefs.SetFadeOutTime(battleEntryMaterial, fadeOutTime);
        }
    }
}
