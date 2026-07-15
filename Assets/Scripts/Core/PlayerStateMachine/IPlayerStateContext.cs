using Frankie.Combat;

namespace Frankie.Core
{
    public interface IPlayerStateContext : ITransitionStateContext, ICombatStateContext, IDialogueStateContext, ITradeStateContext, IOptionStateContext
    {
        void SetPlayerState(IPlayerState playerState);
        void TogglePlayerVisibility(bool? enable = null);
        void QueueActionUnderConsideration();
        bool CanMoveInCutscene();
        void ClearPlayerStateMemory();
    }

    public interface ITransitionStateContext
    {
        void ConfirmTransitionType();
        bool InZoneTransition();
        bool IsZoneTransitionComplete();
        bool InBattleEntryTransition();
        bool InBattleExitTransition();
    }

    public interface ICombatStateContext
    {
        bool IsAnyPartyMemberAlive();
        bool IsPlayerFearsome(CombatParticipant combatParticipant);
        bool AreCombatParticipantsValid();
        void ConfirmEnemiesUnderConsideration();
        void SetupBattleController();
        bool StartBattleSequence();
        bool IsCombatFadeComplete();
        bool EndBattleSequence();
    }

    public interface IDialogueStateContext
    {
        void SetupDialogueController();
        bool StartDialogueSequence();
    }

    public interface ITradeStateContext
    {
        bool StartTradeSequence();
    }

    public interface IOptionStateContext
    {
        bool StartOptionSequence();
    }
}
