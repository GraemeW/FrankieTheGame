using System.Collections.Generic;
using Frankie.Combat;
using Frankie.Core;

namespace Frankie.Tests.Editor
{
    internal sealed class FakePlayerStateContext : IPlayerStateContext
    {
        // Configuration
        public bool setupDialogueResult = true;
        public bool startDialogueResult = true;
        public bool startTradeResult = true;
        public bool startOptionResult = true;
        public bool inBattleEntryTransition = false;
        public bool combatFadeComplete = false;
        public bool combatParticipantsValid = false;

        // State
        public readonly List<string> calls = new();
        public IPlayerState committedState { get; private set; }

        // IPlayerStateContext
        public void SetPlayerState(IPlayerState playerState) { committedState = playerState; calls.Add(nameof(SetPlayerState)); }
        public void TogglePlayerVisibility(bool? enable = null) => calls.Add(nameof(TogglePlayerVisibility));
        public void QueueActionUnderConsideration() => calls.Add(nameof(QueueActionUnderConsideration));
        public void RequestQueuePop() => calls.Add(nameof(RequestQueuePop));
        public void ClearPlayerStateMemory() => calls.Add(nameof(ClearPlayerStateMemory));

        // IPlayerStateInfo
        public bool CanMoveInCutscene() => false;
        public bool InZoneTransition() => false;
        public bool InBattleExitTransition() => false;
        public bool IsAnyPartyMemberAlive() => true;
        public bool IsPlayerFearsome(CombatParticipant combatParticipant) => false;

        // ITransitionStateContext
        public void ConfirmTransitionType() => calls.Add(nameof(ConfirmTransitionType));
        public bool IsZoneTransitionComplete() => true;
        public bool InBattleEntryTransition() => inBattleEntryTransition;

        // ICombatStateContext
        public bool AreCombatParticipantsValid() => combatParticipantsValid;
        public void ConfirmEnemiesUnderConsideration() => calls.Add(nameof(ConfirmEnemiesUnderConsideration));
        public void SetupBattleController() => calls.Add(nameof(SetupBattleController));
        public bool StartBattleSequence() => false;
        public bool IsCombatFadeComplete() => combatFadeComplete;
        public bool EndBattleSequence() => false;

        // IDialogueStateContext
        public bool SetupDialogueController() { calls.Add(nameof(SetupDialogueController)); return setupDialogueResult; }
        public bool StartDialogueSequence() { calls.Add(nameof(StartDialogueSequence)); return startDialogueResult; }

        // ITradeStateContext
        public bool StartTradeSequence() { calls.Add(nameof(StartTradeSequence)); return startTradeResult; }

        // IOptionStateContext
        public bool StartOptionSequence() { calls.Add(nameof(StartOptionSequence)); return startOptionResult; }
    }
}
