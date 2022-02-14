using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class TransitionState : IPlayerState
    {
        public void EnterCombat(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.IsCombatFadeComplete())
            {
                playerStateContext.SetPlayerState(new CombatState());
            }
            else
            {
                if (playerStateContext.InBattleEntryTransition() && playerStateContext.AreCombatParticipantsValid()) // Swarm mechanic
                {
                    playerStateContext.AddEnemiesUnderConsideration();
                }
            }
        }

        public void EnterDialogue(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.InBattleEntryTransition())
            {
                playerStateContext.QueueActionUnderConsideration();
            }
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
            if (playerStateContext.InZoneTransition() && !playerStateContext.IsZoneTransitionComplete()) { return; } // Hold in transition if still ongoing

            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(new WorldState());
        }
    }
}
