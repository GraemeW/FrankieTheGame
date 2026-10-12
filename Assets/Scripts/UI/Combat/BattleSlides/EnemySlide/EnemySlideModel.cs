using Unity.Properties;

namespace Frankie.Combat.UI
{
    public sealed class EnemySlideModel : BattleSlideModel
    {
        // Enemy slide-only bound state (targeting highlight, defeat) + enemy-only damage text - rest is on CombatParticipantModel

        // State
        private bool internalIsTargeted = false;
        private bool internalIsDefeated = false;
        private string internalFriendFoundText = "";
        private string internalFriendIgnoredText = "";

        #region BoundProperties
        [CreateProperty] public bool isTargeted
        {
            get => internalIsTargeted;
            set => SetProperty(ref internalIsTargeted, value);
        }

        [CreateProperty] public bool isDefeated
        {
            get => internalIsDefeated;
            set => SetProperty(ref internalIsDefeated, value);
        }

        [CreateProperty] public string friendFoundText
        {
            get => internalFriendFoundText;
            set => SetProperty(ref internalFriendFoundText, value);
        }

        [CreateProperty] public string friendIgnoredText
        {
            get => internalFriendIgnoredText;
            set => SetProperty(ref internalFriendIgnoredText, value);
        }
        #endregion
    }
}
