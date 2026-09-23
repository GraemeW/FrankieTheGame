using UnityEngine;
using UnityEngine.Rendering.Universal;
using Frankie.Zones;

namespace Frankie.Rendering
{
    public class BattleEntryShaderControl : MonoBehaviour
    {
        // Tunables
        [SerializeField] private Renderer2DData renderer2DData;
        [SerializeField] private Material battleEntryMaterial;
        [SerializeField] private Color goodEntryColor = Color.lightGreen;
        [SerializeField] private Color badEntryColor = Color.softRed;
        [SerializeField] private Color neutralEntryColor = Color.mediumPurple;
        [SerializeField] private float twirlStrength = 5.0f;

        private void OnEnable()
        {
            EndFade(); 
        }

        private void OnDisable()
        {
            EndFade();
        }

        public void SetBattleEntryParameters(TransitionType transitionType, float fadeTime, float fadeOutTime)
        {
            switch (transitionType)
            {
                case TransitionType.BattleGood:
                    ShaderPropertyRefs.SetTint(battleEntryMaterial, goodEntryColor);
                    break;
                case TransitionType.BattleBad:
                    ShaderPropertyRefs.SetTint(battleEntryMaterial, badEntryColor);
                    break;
                case TransitionType.BattleNeutral:
                    ShaderPropertyRefs.SetTint(battleEntryMaterial, neutralEntryColor);
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
            ShaderPropertyRefs.ToggleBattleEntry(renderer2DData, true);
        }

        public void StartFadeOut()
        {
            ShaderPropertyRefs.SetFadeOutToggle(battleEntryMaterial, true);
        }

        public void EndFade()
        {
            ShaderPropertyRefs.ToggleBattleEntry(renderer2DData, false);
        }
    }
}
