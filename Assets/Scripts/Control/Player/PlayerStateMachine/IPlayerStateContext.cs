using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IPlayerStateContext
    {
        void SetPlayerState(IPlayerState playerState);
        
        // Utility
        public void QueueActionUnderConsideration();
        public void ClearPlayerStateMemory();

        // Transition
        public void ConfirmTransitionType();
        public bool InZoneTransition();
        public bool IsZoneTransitionComplete();
        public bool InBattleEntryTransition();
        public bool InBattleExitTransition();

        // Combat
        public bool AreCombatParticipantsValid(bool announceCannotFight = false);
        public void AddEnemiesUnderConsideration();
        public void SetupBattleController();
        public bool StartBattleSequence();
        public bool IsCombatFadeComplete();
        public bool EndBattleSequence();

        // Dialogue
        public void SetupDialogueController();
        public bool StartDialogueSequence();

        // Trade
        public bool StartTradeSequence();

        // Option
        public bool StartOptionSequence();
    }
}
