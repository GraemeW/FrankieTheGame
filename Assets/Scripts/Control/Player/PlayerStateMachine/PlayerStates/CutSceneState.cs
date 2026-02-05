namespace Frankie.Control.PlayerStates
{
    public class CutSceneState : IPlayerState
    {
        public void EnterCombat(IPlayerStateContext playerStateContext)
        {
            // No state change (will dequeue next time in world)
            playerStateContext.QueueActionUnderConsideration();
        }

        public void EnterCutScene(IPlayerStateContext playerStateContext)
        {
            // Queue then kick to world to bump next cutscene immediately
            playerStateContext.QueueActionUnderConsideration();
            EnterWorld(playerStateContext);
        }

        public void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            // No state change (will dequeue next time in world)
            playerStateContext.QueueActionUnderConsideration();
        }

        public void EnterOptions(IPlayerStateContext playerStateContext)
        {
        }

        public void EnterTrade(IPlayerStateContext playerStateContext)
        {
        }

        public void EnterTransition(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.InZoneTransition())
            {
                playerStateContext.TogglePlayerVisibility(true);
                playerStateContext.SetPlayerState(new TransitionState()); // Force state to transition, going to get pulled to a new scene
            }
        }

        public void EnterWorld(IPlayerStateContext playerStateContext)
        {
            playerStateContext.TogglePlayerVisibility(true);
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(new WorldState());
        }
    }
}
