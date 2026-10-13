using Unity.Properties;

namespace Frankie.Combat.UI
{
    public sealed class CharacterSlideModel : BattleSlideModel
    {
        // Character slide-only bound state (labels, highlight) - rest is on CombatParticipantModel

        // State
        private string internalHPLabel = "";
        private string internalAPLabel = "";
        private CharacterSlideState internalSlideState = CharacterSlideState.Ready;

        #region BoundProperties
        [CreateProperty] public string hpLabel
        {
            get => internalHPLabel;
            set => SetProperty(ref internalHPLabel, value);
        }

        [CreateProperty] public string apLabel
        {
            get => internalAPLabel;
            set => SetProperty(ref internalAPLabel, value);
        }

        [CreateProperty] public CharacterSlideState slideState
        {
            get => internalSlideState;
            set => SetProperty(ref internalSlideState, value);
        }
        #endregion
    }
}
