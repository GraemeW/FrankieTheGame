namespace Frankie.Core.PlayerStates
{
    public class TransitionState : PlayerStateBase
    {
        public override PlayerStateType playerStateType => PlayerStateType.InTransition;
        
        public override void EnterCombat(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.IsCombatFadeComplete())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.combatState);
            }
            else
            {
                // Swarm mechanic
                if (playerStateContext.InBattleEntryTransition() && playerStateContext.AreCombatParticipantsValid()) { playerStateContext.ConfirmEnemiesUnderConsideration(); }
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
