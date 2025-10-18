namespace Frankie.Combat
{
    public class BattleQueueUpdatedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleQueueUpdated;
        
        public BattleSequence battleSequence;

        public BattleQueueUpdatedEvent(BattleSequence battleSequence)
        {
            this.battleSequence = battleSequence;
        }
    }
}
