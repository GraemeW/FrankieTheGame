namespace Frankie.Core.PlayerStates
{
    public class TransitionState : PlayerStateBase
    {
        public override PlayerStateType playerStateType => PlayerStateType.InTransition;
        
        public override void EnterCombat(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.InBattleEntryTransition()) { return; } // Battle entry only -- e.g. a zone transition must not be pulled into combat by stale combat memory

            if (playerStateContext.IsCombatFadeComplete())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.combatState);
            }
            else if (playerStateContext.AreCombatParticipantsValid())
            {
                // Swarm mechanic
                playerStateContext.ConfirmEnemiesUnderConsideration();
            }
        }

        public override void EnterCutScene(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.InBattleEntryTransition()) { playerStateContext.QueueActionUnderConsideration(); }
        }

        public override void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.InBattleEntryTransition()) { playerStateContext.QueueActionUnderConsideration(); }
        }

        public override void EnterWorld(IPlayerStateContext playerStateContext)
        {
            // Hold in transition if still ongoing
            if (playerStateContext.InZoneTransition() && !playerStateContext.IsZoneTransitionComplete()) { return; }

            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(PlayerStateMachine.worldState);
        }
    }
}
