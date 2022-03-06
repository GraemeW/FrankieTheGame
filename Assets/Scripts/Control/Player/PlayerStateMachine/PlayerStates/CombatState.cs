namespace Frankie.Control.PlayerStates
{
    public class CombatState : IPlayerState
    {
        public void EnterCombat(IPlayerStateContext playerStateContext)
        {
            // No state change (will dequeue next time in world)
            playerStateContext.QueueActionUnderConsideration();
        }

        public void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            // No state change (will dequeue next time in world)
            playerStateContext.QueueActionUnderConsideration();
        }

        public void EnterOptions(IPlayerStateContext playerStateContext) // Ignore
        {
        }

        public void EnterTrade(IPlayerStateContext playerStateContext) // Ignore
        {
        }

        public void EnterTransition(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.InBattleExitTransition())
            {
                playerStateContext.SetPlayerState(new TransitionState());
                if (!playerStateContext.EndBattleSequence())  // State change from Transition to World handled by coroutine
                {
                    EnterWorld(playerStateContext); // Protection to default back to world on fail to exit battle
                }
            }
            else if (playerStateContext.InZoneTransition())
            {
                playerStateContext.SetPlayerState(new TransitionState()); // Force state to transition, going to get pulled to a new scene
            }
        }

        public void EnterWorld(IPlayerStateContext playerStateContext) // Kill rogue controllers for safety, unexpected to call this route
        {
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(new WorldState());
        }
    }
}
