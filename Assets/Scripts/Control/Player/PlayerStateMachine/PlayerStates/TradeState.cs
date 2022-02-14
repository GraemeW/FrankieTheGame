using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class TradeState : IPlayerState
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
            playerStateContext.SetPlayerState(new TransitionState()); // Force state to transition, going to get pulled to a new scene
        }

        public void EnterWorld(IPlayerStateContext playerStateContext)
        {
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(new WorldState());
        }
    }
}
