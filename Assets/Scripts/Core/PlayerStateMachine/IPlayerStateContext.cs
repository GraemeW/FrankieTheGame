namespace Frankie.Core
{
    public interface IPlayerStateContext : IPlayerStateInfo, ITransitionStateContext, ICombatStateContext, IDialogueStateContext, ITradeStateContext, IOptionStateContext
    {
        void SetPlayerState(IPlayerState playerState);
        void TogglePlayerVisibility(bool? enable = null);
        void QueueActionUnderConsideration();
        void RequestQueuePop();
        void ClearPlayerStateMemory();
    }

    public interface ITransitionStateContext
    {
        void ConfirmTransitionType();
        bool IsZoneTransitionComplete();
        bool InBattleEntryTransition();
    }

    public interface ICombatStateContext
    {
        bool AreCombatParticipantsValid();
        void ConfirmEnemiesUnderConsideration();
        void SetupBattleController();
        bool StartBattleSequence();
        bool IsCombatFadeComplete();
        bool EndBattleSequence();
    }

    public interface IDialogueStateContext
    {
        bool SetupDialogueController();
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
