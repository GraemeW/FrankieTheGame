using Frankie.Combat;

namespace Frankie.Control
{
    public interface IPlayerStateContext
    {
        void SetPlayerState(IPlayerState playerState);

        #region Utility
        public void TogglePlayerVisibility(bool? enable = null);
        public void QueueActionUnderConsideration();
        public bool CanMoveInCutscene();
        public void ClearPlayerStateMemory();
        #endregion

        #region Transition
        public void ConfirmTransitionType();
        public bool InZoneTransition();
        public bool IsZoneTransitionComplete();
        public bool InBattleEntryTransition();
        public bool InBattleExitTransition();
        #endregion

        #region Combat
        public bool IsAnyPartyMemberAlive();
        public bool IsPlayerFearsome(CombatParticipant combatParticipant);
        public bool AreCombatParticipantsValid(bool announceCannotFight = false);
        public void AddEnemiesUnderConsideration();
        public void SetupBattleController();
        public bool StartBattleSequence();
        public bool IsCombatFadeComplete();
        public bool EndBattleSequence();
        #endregion
        
        #region Dialogue
        public void SetupDialogueController();
        public bool StartDialogueSequence();
        #endregion

        #region Trade
        public bool StartTradeSequence();
        #endregion

        #region Option
        public bool StartOptionSequence();
        #endregion
    }
}
