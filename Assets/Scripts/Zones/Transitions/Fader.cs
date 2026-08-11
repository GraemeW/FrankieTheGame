using UnityEngine;
using UnityEngine.UI;
using Frankie.Rendering;
using Frankie.Saving;


namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(BattleEntryShaderControl))]
    public class Fader : FaderBase<TransitionType>
    {
        // Tunables
        [SerializeField] private Image battleComplete;
        
        // Cached References
        private BattleEntryShaderControl battleEntryShaderControl;
        
        #region UnityMethods
        private void Awake()
        {
            battleEntryShaderControl = GetComponent<BattleEntryShaderControl>();
        }
        #endregion
        
        #region FadeSetup
        protected override bool IsSkipFade(TransitionType transitionType) => transitionType == TransitionType.None;
        protected override bool IsSceneLoadFade(TransitionType transitionType) => transitionType == TransitionType.Zone;
        protected override TransitionType GetSceneLoadTransitionType() => TransitionType.Zone;
        protected override void TriggerSave() => SavingWrapper.SaveSession();
        protected override void TriggerLoad() => SavingWrapper.LoadSession();
        
        protected override bool PreFadeSetup(TransitionType transitionType)
        {
            switch (transitionType)
            {
                case TransitionType.Zone:
                    nodeEntry.gameObject.SetActive(true);
                    currentTransitionImage = nodeEntry;
                    break;
                case TransitionType.BattleComplete:
                    battleComplete.gameObject.SetActive(true);
                    currentTransitionImage = battleComplete;
                    break;
                case TransitionType.BattleGood:
                case TransitionType.BattleBad:
                case TransitionType.BattleNeutral:
                    break;
                case TransitionType.None:
                default:
                    fading = false;
                    return false;
            }
            return true;
        }
        #endregion

        #region TransitionSpecificBehaviour
        protected override void ResetOverlays()
        {
            base.ResetOverlays();
            battleComplete?.gameObject.SetActive(false);
            battleEntryShaderControl?.EndFade();
        }

        protected override bool TransitionUsesStandaloneFadeControl(TransitionType transitionType) => transitionType is TransitionType.BattleGood or TransitionType.BattleBad or TransitionType.BattleNeutral;

        protected override void TriggerStandaloneFadeIn(TransitionType transitionType)
        {
            battleEntryShaderControl.SetBattleEntryParameters(transitionType, GetFadeTime(true, transitionType), GetFadeTime(false, transitionType));
            battleEntryShaderControl.StartFadeIn();
        }

        protected override void TriggerStandaloneFadeOut(TransitionType _)
        {
            battleEntryShaderControl.StartFadeOut();
        }

        protected override void TriggerStandaloneFadeCleanup()
        {
            battleEntryShaderControl.EndFade();
        }
        #endregion
    }
}
