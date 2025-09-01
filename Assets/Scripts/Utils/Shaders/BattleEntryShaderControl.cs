using UnityEngine;
using UnityEngine.Rendering.Universal;
using Frankie.ZoneManagement;

namespace Frankie.Utils
{
    public class BattleEntryShaderControl : MonoBehaviour
    {
        // Tunables
        [SerializeField] Renderer2DData renderer2DData = null;
        [SerializeField] Material battleEntryMaterial = null;
        [SerializeField] Texture2D goodEntryTexture = null;
        [SerializeField] Texture2D badEntryTexture = null;
        [SerializeField] Texture2D neutralEntryTexture = null;
        [SerializeField] float twirlStrength = 5.0f;

        public void SetBattleEntryParameters(TransitionType transitionType, float fadeTime, float fadeOutTime)
        {
            switch (transitionType)
            {
                case TransitionType.BattleGood:
                    ShaderPropertyRefs.SetMainTexture(battleEntryMaterial, goodEntryTexture);
                    break;
                case TransitionType.BattleBad:
                    ShaderPropertyRefs.SetMainTexture(battleEntryMaterial, badEntryTexture);
                    break;
                case TransitionType.BattleNeutral:
                    ShaderPropertyRefs.SetMainTexture(battleEntryMaterial, neutralEntryTexture);
                    break;
                case TransitionType.BattleComplete:
                case TransitionType.Zone:
                case TransitionType.None:
                default:
                    return;
            }
            ShaderPropertyRefs.SetShaderPhase(battleEntryMaterial, Time.time);
            ShaderPropertyRefs.SetStrength(battleEntryMaterial, twirlStrength);
            ShaderPropertyRefs.SetFadeTime(battleEntryMaterial, fadeTime);
            ShaderPropertyRefs.SetFadeOutTime(battleEntryMaterial, fadeOutTime);
        }

        public void StartFadeIn()
        {
            ShaderPropertyRefs.SetFadeOutToggle(battleEntryMaterial, false);
            ShaderPropertyRefs.ToggleBattleEntryFeature(renderer2DData, true);
        }

        public void StartFadeOut()
        {
            ShaderPropertyRefs.SetFadeOutToggle(battleEntryMaterial, true);
        }

        public void EndFade()
        {
            ShaderPropertyRefs.ToggleBattleEntryFeature(renderer2DData, false);
        }
    }
}
