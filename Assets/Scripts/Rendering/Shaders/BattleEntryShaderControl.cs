using UnityEngine;
using Frankie.Zones;

namespace Frankie.Rendering
{
    public class BattleEntryShaderControl : MonoBehaviour
    {
        // Note:  Sets the battle entry filter's material once per fade - the filter runs on shader time from there (see BattleEntryOverlay)

        // Tunables
        [SerializeField] private Material battleEntryMaterial;
        [SerializeField] private Color goodEntryColor = Color.lightGreen;
        [SerializeField] private Color badEntryColor = Color.softRed;
        [SerializeField] private Color neutralEntryColor = Color.mediumPurple;

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
            ShaderPropertyRefs.SetFadeTime(battleEntryMaterial, fadeTime);
            ShaderPropertyRefs.SetFadeOutTime(battleEntryMaterial, fadeOutTime);
        }
    }
}
