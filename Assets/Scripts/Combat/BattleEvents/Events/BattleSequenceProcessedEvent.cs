namespace Frankie.Combat
{
    public struct BattleSequenceProcessedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleSequencedProcessed;

        public BattleSequence battleSequence;

        public BattleSequenceProcessedEvent(BattleSequence battleSequence)
        {
            this.battleSequence = battleSequence;
        }
    }
}
