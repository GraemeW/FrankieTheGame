using Frankie.Zones;

namespace Frankie.Core.PlayerStateMemory
{
    public class TransitionMemory
    {
        public TransitionType transitionTypeUnderConsideration = TransitionType.None;
        public TransitionType currentTransitionType = TransitionType.None;
        public bool zoneTransitionComplete = true;
        
        public static bool IsBattleTransition(TransitionType transitionType) => transitionType is TransitionType.BattleNeutral or TransitionType.BattleGood or TransitionType.BattleBad;
        public void ConfirmTransitionType() => currentTransitionType = transitionTypeUnderConsideration;
        public bool InBattleEntryTransition() => IsBattleTransition(currentTransitionType);
        public bool InBattleExitTransition() => currentTransitionType == TransitionType.BattleComplete;
    }
}
