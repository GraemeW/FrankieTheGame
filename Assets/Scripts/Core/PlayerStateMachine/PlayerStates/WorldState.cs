namespace Frankie.Core.PlayerStates
{
    public class WorldState : IPlayerState
    {
        public void EnterCombat(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.AreCombatParticipantsValid()) { EnterWorld(playerStateContext); return; }

            playerStateContext.SetupBattleController();
            playerStateContext.AddEnemiesUnderConsideration();
            playerStateContext.ConfirmTransitionType();
            playerStateContext.SetPlayerState(PlayerStateMachine.TransitionState);
            if (!playerStateContext.StartBattleSequence())  // State change from Transition to Combat handled by coroutine
            {
                EnterWorld(playerStateContext); // Protection to default back to world on fail to enter battle
            }
        }

        public void EnterCutScene(IPlayerStateContext playerStateContext)
        {
            playerStateContext.TogglePlayerVisibility();
            playerStateContext.SetPlayerState(PlayerStateMachine.CutSceneState);
        }

        public void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            playerStateContext.SetupDialogueController();
            if (playerStateContext.StartDialogueSequence())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.DialogueState);
            }
        }

        public void EnterOptions(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.StartOptionSequence())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.OptionState);
            }
        }

        public void EnterTrade(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.StartTradeSequence())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.TradeState);
            }
        }

        public void EnterTransition(IPlayerStateContext playerStateContext)
        {
            playerStateContext.SetPlayerState(PlayerStateMachine.TransitionState);
        }

        public void EnterWorld(IPlayerStateContext playerStateContext)
        {
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(PlayerStateMachine.WorldState);
        }
    }
}
