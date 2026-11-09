namespace Frankie.Core.PlayerStates
{
    public class WorldState : PlayerStateBase
    {
        public override PlayerStateType playerStateType => PlayerStateType.InWorld;
        
        public override void EnterCombat(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.AreCombatParticipantsValid()) { EnterWorld(playerStateContext); return; }

            playerStateContext.SetupBattleController();
            playerStateContext.ConfirmEnemiesUnderConsideration();
            playerStateContext.ConfirmTransitionType();
            playerStateContext.SetPlayerState(PlayerStateMachine.transitionState);
            if (!playerStateContext.StartBattleSequence())  // State change from Transition to Combat handled by coroutine
            {
                EnterWorld(playerStateContext); // Protection to default back to world on fail to enter battle
            }
        }

        public override void EnterCutScene(IPlayerStateContext playerStateContext)
        {
            playerStateContext.TogglePlayerVisibility();
            playerStateContext.SetPlayerState(PlayerStateMachine.cutSceneState);
        }

        public override void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.SetupDialogueController()) // i.e. current controller is closing
            {
                playerStateContext.QueueActionUnderConsideration(); // No World transition follows, so ask for the pop directly (runs once the old controller is gone)
                playerStateContext.RequestQueuePop();
                return;
            }
            if (!playerStateContext.StartDialogueSequence()) { playerStateContext.ClearPlayerStateMemory(); return; }
            
            playerStateContext.SetPlayerState(PlayerStateMachine.dialogueState);
        }

        public override void EnterOptions(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.StartOptionSequence()) { playerStateContext.ClearPlayerStateMemory(); return; }
            
            playerStateContext.SetPlayerState(PlayerStateMachine.optionState);
        }

        public override void EnterTrade(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.StartTradeSequence()) { playerStateContext.ClearPlayerStateMemory(); return; }
            
            playerStateContext.SetPlayerState(PlayerStateMachine.tradeState);
        }

        public override void EnterTransition(IPlayerStateContext playerStateContext)
        {
            playerStateContext.SetPlayerState(PlayerStateMachine.transitionState);
        }
    }
}
