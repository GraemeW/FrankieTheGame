namespace Frankie.Control.PlayerStates
{
    public class WorldState : IPlayerState
    {
        public void EnterCombat(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.AreCombatParticipantsValid(true)) { EnterWorld(playerStateContext); return; }

            playerStateContext.SetupBattleController();
            playerStateContext.AddEnemiesUnderConsideration();
            playerStateContext.ConfirmTransitionType();
            playerStateContext.SetPlayerState(new TransitionState());
            if (!playerStateContext.StartBattleSequence())  // State change from Transition to Combat handled by coroutine
            {
                EnterWorld(playerStateContext); // Protection to default back to world on fail to enter battle
            }
        }

        public void EnterCutScene(IPlayerStateContext playerStateContext)
        {
            playerStateContext.SetPlayerState(new CutSceneState());
        }

        public void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            playerStateContext.SetupDialogueController();
            if (playerStateContext.StartDialogueSequence())
            {
                playerStateContext.SetPlayerState(new DialogueState());
            }
        }

        public void EnterOptions(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.StartOptionSequence())
            {
                playerStateContext.SetPlayerState(new OptionState());
            }
        }

        public void EnterTrade(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.StartTradeSequence())
            {
                playerStateContext.SetPlayerState(new TradeState());
            }
        }

        public void EnterTransition(IPlayerStateContext playerStateContext)
        {
            playerStateContext.SetPlayerState(new TransitionState());
        }

        public void EnterWorld(IPlayerStateContext playerStateContext)
        {
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(new WorldState());
        }
    }
}
