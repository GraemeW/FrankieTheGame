namespace Frankie.Combat
{
    public enum BattleEventType
    {
        BattleStaging,
        BattleStateChanged,
        BattleEntityAdded,
        BattleEntitySelected,
        BattleActionSelected,
        BattleActionArmed,
        BattleQueueAddAttemptEvent,
        BattleQueueUpdated,
        BattleSequencedProcessed,
        BattleEntityStateAltered,
        BattleEntityRemovedFromBoard,
        BattleFadeTransition
    }
}
