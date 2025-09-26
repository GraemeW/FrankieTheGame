namespace Frankie.Combat
{
    public struct BattleEntityAddedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEntityAdded;

        public BattleEntity battleEntity { get; private set; }

        public BattleEntityAddedEvent(BattleEntity battleEntity)
        {
            this.battleEntity = battleEntity;
        }
    }
}
