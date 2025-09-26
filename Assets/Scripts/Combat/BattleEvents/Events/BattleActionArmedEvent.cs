namespace Frankie.Combat
{
    public struct BattleActionArmedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleActionArmed;

        public IBattleActionSuper battleActionSuper;

        public BattleActionArmedEvent(IBattleActionSuper battleActionSuper)
        {
            this.battleActionSuper = battleActionSuper;
        }
    }
}
