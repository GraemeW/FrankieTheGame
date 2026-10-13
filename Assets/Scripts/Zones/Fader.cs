using UnityEngine;
using LowDefMustard.UIBox;
using LowDefMustard.Zones;
using Frankie.Rendering;
using Frankie.Saving;
using Frankie.Zones.UI;

namespace Frankie.Zones
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    [RequireComponent(typeof(BattleEntryShaderControl))]
    public sealed class Fader : FaderBase<TransitionType>
    {
        // Note:  Overlays live on a top-sorted panel (see FaderPanelSettings) - i.e. fades cover every other box / UI element rendered on the screen

        // State
        private readonly FaderModel faderModel = new();

        // Cached References
        private BattleEntryShaderControl battleEntryShaderControl;

        #region UnityMethods
        protected override void Awake()
        {
            base.Awake();

            battleEntryShaderControl = GetComponent<BattleEntryShaderControl>();
            GetComponent<UIToolkitMenuView>().SetDataSource(faderModel);
        }
        #endregion

        #region FadeSetup
        protected override bool IsSkipFade(TransitionType transitionType) => transitionType == TransitionType.None;
        protected override bool IsSceneLoadFade(TransitionType transitionType) => transitionType == TransitionType.Zone;
        protected override TransitionType GetSceneLoadTransitionType() => TransitionType.Zone;
        protected override void TriggerSave() => SaveFileManager.SaveSession();
        protected override void TriggerLoad() => SaveFileManager.LoadSession();

        protected override bool PreFadeSetup(TransitionType transitionType)
        {
            if (transitionType != TransitionType.None) { return true; }

            fading = false;
            return false;
        }
        #endregion

        #region TransitionSpecificBehaviour
        protected override void SetOverlayAlpha(TransitionType transitionType, float alpha)
        {
            switch (transitionType)
            {
                case TransitionType.Zone:
                    faderModel.zoneFadeOpacity = alpha;
                    break;
                case TransitionType.BattleComplete:
                    faderModel.battleFadeOpacity = alpha;
                    break;
                case TransitionType.BattleGood:
                case TransitionType.BattleBad:
                case TransitionType.BattleNeutral:
                case TransitionType.None:
                default:
                    break;
            }
        }

        protected override void ResetOverlays()
        {
            faderModel.zoneFadeOpacity = 0f;
            faderModel.battleFadeOpacity = 0f;
            if (battleEntryShaderControl != null) { battleEntryShaderControl.EndFade(); }
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
