namespace Frankie.Combat
{
    public enum BattleEventType
    {
        BattleEnter,
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
        BattleExit
    }
}
